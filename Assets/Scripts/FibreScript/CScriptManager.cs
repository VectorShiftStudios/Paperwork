using System;
using System.Collections.Generic;
using UnityEngine;

public class CScriptManager
{
	public enum ETriggerType
	{
		TICK,
		ENTITY_GONE,
		UNIT_DEAD,
	}

	public class CTrigger
	{
		public ETriggerType mType;
		public int mInfo;
		public string mCallbackFuncName;
		public CFibre mCallbackFibre;
	}

	private CWorld _world;
	private CFibreVM _fibreVM;

	private List<CTrigger> _triggers = new List<CTrigger>();

	public CScriptManager()
	{
	}

	public bool Init(string LevelScriptFileName)
	{
		_triggers.Clear();
		_fibreVM = new CFibreVM();

		// Game
		_fibreVM.PushInteropFunction("game_get_version", GetVersion);				
		_fibreVM.PushInteropFunction("game_spawn_contract", ContractSpawn);
		_fibreVM.PushInteropFunction("game_spawn_resume", ResumeSpawn);
		//_fibreVM.PushInteropFunction("game_despawn", GameDespawn);

		// Item
		_fibreVM.PushInteropFunction("item_spawn", SpawnItem);
		_fibreVM.PushInteropFunction("item_set_locked", ItemSetLocked);
		_fibreVM.PushInteropFunction("item_set_available", SetItemAvailable);
		_fibreVM.PushInteropFunction("item_spawn_notify", NotifyNewItem);

		// Unit
		_fibreVM.PushInteropFunction("unit_spawn", SpawnUnit);
		_fibreVM.PushInteropFunction("unit_set_speech", SetUnitSpeech);
		_fibreVM.PushInteropFunction("unit_set_stamina", UnitSetStamina);
		_fibreVM.PushInteropFunction("unit_get_alive", UnitGetAlive);

		// Player
		_fibreVM.PushInteropFunction("game_set_win_money", SetWinMoney);
		_fibreVM.PushInteropFunction("game_set_fail_seconds", SetFailSeconds);
		_fibreVM.PushInteropFunction("game_set_population_cap", SetPopulationCap);
		_fibreVM.PushInteropFunction("game_set_intern_cap", SetInternCap);
		_fibreVM.PushInteropFunction("player_set_money", SetPlayerMoney);
		_fibreVM.PushInteropFunction("player_move_camera", CameraSetPosition);
		_fibreVM.PushInteropFunction("player_set_rep", PlayerSetRep);

		// Decal		
		//_fibreVM.PushInteropFunction("decal_spawn", DecalSpawn);
		//_fibreVM.PushInteropFunction("decal_modify", DecalModify);

		// Audio
		//_fibreVM.PushInteropFunction("volume_spawn", VolumeSpawn);
		//_fibreVM.PushInteropFunction("volume_modify", VolumeModify);
		_fibreVM.PushInteropFunction("music_play", MusicPlay);
		_fibreVM.PushInteropFunction("sound_play", SoundPlay);

		// Cinematics
		_fibreVM.PushInteropFunction("cinematic_start", CinematicStart);
		_fibreVM.PushInteropFunction("cinematic_stop", CinematicStop);
		//_fibreVM.PushInteropFunction("cinematic_set_skip_start", CinematicStop);
		//_fibreVM.PushInteropFunction("cinematic_set_skip_end", CinematicStop);
		
		// AI
		_fibreVM.PushInteropFunction("ai_enable", AIEnable);

		// Waits
		_fibreVM.PushInteropFunction("wait_time", WaitTime);

		// Triggers
		_fibreVM.PushInteropFunction("trigger_tick", TriggerTick);
		_fibreVM.PushInteropFunction("trigger_entity_gone", TriggerEntityGone);
		_fibreVM.PushInteropFunction("trigger_unit_dead", TriggerUnitDead);

		try
		{
			_fibreVM.Compile(LevelScriptFileName);
		}
		catch (CFibreCompileException E)
		{
			Debug.LogError("FibreScript Compile: " + E.Message + " " + E.StackTrace);
			CGame.UIManager.ShowErrorMessage(E.Message, "Fibre Script Compile Exception");
			Destroy();
			return false;
		}

		return true;
	}

	public void Destroy()
	{
		// TODO: Destroy more stuff properly?
		_world = null;
		_fibreVM = null;
	}

	/// <summary>
	/// Start from scratch for a fresh level context.
	/// </summary>
	public void Start(CWorld World)
	{
		_world = World;
		_fibreVM.ExecuteGlobalFibre();
		_fibreVM.ExecuteFibre("on_start");
	}

	public void SimTick(int Tick)
	{
		for (int i = 0; i < _triggers.Count; ++i)
		{
			CTrigger trigger = _triggers[i];

			bool execute = false;

			if (trigger.mType == ETriggerType.TICK && trigger.mInfo == Tick)
			{
				execute = true;
			}
			else if (trigger.mType == ETriggerType.ENTITY_GONE && _world.GetEntity<CEntity>(trigger.mInfo) == null)
			{
				execute = true;
			}
			else if (trigger.mType == ETriggerType.UNIT_DEAD)
			{
				CUnit unit = _world.GetEntity<CUnit>(trigger.mInfo);

				if (unit == null || unit.mDead)
					execute = true;
			}

			if (execute)
			{
				_triggers.RemoveAt(i);
				--i;

				if (trigger.mCallbackFibre != null)
					_fibreVM.ContinueFibre(trigger.mCallbackFibre);
				else
					_fibreVM.ExecuteFibre(trigger.mCallbackFuncName);
					// TODO: We can shortcut interop execution here. Don't need to create a whole fibre, just call interop directly.
			}
		}

		_fibreVM.ExecuteFibre("on_tick", new CFibreReg[] { new CFibreReg(Tick), new CFibreReg(Tick * CWorld.SECONDS_PER_TICK) });
	}

	public CFibreReg GetVersion(CFibre Fibre, int ArgCount)
	{
		return new CFibreReg(CGame.VERSION_MAJOR + "." + CGame.VERSION_MINOR);
	}

	public CFibreReg TriggerTick(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			CTrigger trigger = new CTrigger();
			trigger.mType = ETriggerType.TICK;
			trigger.mInfo = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			trigger.mCallbackFuncName = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mString;
			_triggers.Add(trigger);
		}

		return null;
	}

	public CFibreReg TriggerEntityGone(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			CTrigger trigger = new CTrigger();
			trigger.mType = ETriggerType.ENTITY_GONE;
			trigger.mInfo = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			trigger.mCallbackFuncName = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mString;
			_triggers.Add(trigger);
		}

		return null;
	}

	public CFibreReg TriggerUnitDead(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			CTrigger trigger = new CTrigger();
			trigger.mType = ETriggerType.UNIT_DEAD;
			trigger.mInfo = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			trigger.mCallbackFuncName = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mString;
			_triggers.Add(trigger);
		}

		return null;
	}

	public CFibreReg WaitTime(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
		{
			CTrigger trigger = new CTrigger();
			trigger.mType = ETriggerType.TICK;
			trigger.mInfo = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber + _world.mGameTick;
			trigger.mCallbackFibre = Fibre;
			_triggers.Add(trigger);

			// TODO: Fix this cheap way to indicate that we need to suspend thread.
			CFibreReg reg = new CFibreReg();
			reg.mType = EFibreType.COUNT;
			return reg;
		}

		return null;
	}

	public CFibreReg SpawnItem(CFibre Fibre, int ArgCount)
	{
		if (ArgCount < 5) return null;
		bool match = true;
		int ownerId = Fibre.GetInt(0, ref match);
		string itemName = Fibre.GetString(1, ref match);
		int posX = Fibre.GetInt(2, ref match);
		int posY = Fibre.GetInt(3, ref match);
		int rot = Fibre.GetInt(4, ref match);

		if (match)
		{
			CItem item = _world.SpawnItem(CGame.AssetManager.GetAsset<CItemAsset>(itemName), new Vector2(posX, posY), rot, ownerId);
			return new CFibreReg(item.mID);
		}

		return new CFibreReg(0);
	}

	public CFibreReg ItemSetLocked(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			int itemId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			bool locked = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mBool;
			CItem item = _world.GetEntity<CItem>(itemId);

			if (item != null && item.mType == CEntity.EType.ITEM_DOOR)
				((CItemDoor)item).mLocked = locked;
		}

		return null;
	}

	public CFibreReg SpawnUnit(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 6)
		{
			int ownerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			float posX = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;
			float posY = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 2].mNumber;
			int rot = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 3].mNumber;
			int tier = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 4].mNumber;
			int level = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 5].mNumber;

			CResume resume = new CResume();
			resume.Generate(_world, tier, level);
			CUnit entity = _world.SpawnUnit(ownerId, resume, new Vector2(posX, posY), rot);

			return new CFibreReg(entity.mID);
		}

		throw new CFibreRuntimeException("unit_spawn does not take " + ArgCount + " parameters.", Fibre);
	}

	public CFibreReg SetUnitSpeech(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			bool match = true;
			int unitId = Fibre.GetInt(0, ref match);
			string text = Fibre.GetString(1, ref match);

			if (match)
			{
				CUnit unit = _world.GetEntity<CUnit>(unitId);

				if (unit != null)
					unit.SetSpeech(text);
			}
		}

		return null;
	}

	public CFibreReg UnitSetStamina(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			int unitId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			CUnit unit = _world.GetEntity<CUnit>(unitId);

			if (unit != null)
				unit.mStamina = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;
		}

		return null;
	}

	public CFibreReg UnitGetAlive(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
		{
			int unitId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			CUnit unit = _world.GetEntity<CUnit>(unitId);

			if (unit == null || unit.mDead)
				return new CFibreReg(false);

			return new CFibreReg(true);
		}

		return null;
	}

	public CFibreReg SetPlayerMoney(CFibre Fibre, int ArgCount)
	{
		if (ArgCount > 1)
		{
			bool match = true;
			int playerId = Fibre.GetInt(0, ref match);

			if (ArgCount == 1)
			{
				return new CFibreReg(_world.mPlayers[playerId].mMoney);
			}
			else
			{
				int value = Fibre.GetInt(1, ref match);

				if (match)
					_world.mPlayers[playerId].mMoney = value;
			}
		}

		return null;
	}

	public CFibreReg PlayerSetRep(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 3)
		{
			int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			int companyId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;
			int rep = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 2].mNumber;

			_world.mClientCompanies[companyId].SetPlayerReputation(playerId, rep);
		}

		return null;
	}

	public CFibreReg ContractSpawn(CFibre Fibre, int ArgCount)
	{
		_world.AddContractTurn(1, 1);
		return null;
	}

	public CFibreReg ResumeSpawn(CFibre Fibre, int ArgCount)
	{
		_world.AddResumeturn(0);
		return null;
	}
	
	public CFibreReg SetWinMoney(CFibre Fibre, int ArgCount)
	{
		if (ArgCount < 1) return null;
		bool match = true;
		int value = Fibre.GetInt(0, ref match);
		if (match) _world.mWinMoney = value;
		return null;
	}

	public CFibreReg SetFailSeconds(CFibre Fibre, int ArgCount)
	{
		if (ArgCount < 1) return null;
		bool match = true;
		int value = Fibre.GetInt(0, ref match);
		if (match) _world.mFailSeconds = value;
		return null;
	}

	public CFibreReg SetPopulationCap(CFibre Fibre, int ArgCount)
	{
		if (ArgCount < 1) return null;
		bool match = true;
		int value = Fibre.GetInt(0, ref match);
		if (match) _world.mPopCap = value;
		return null;
	}

	public CFibreReg SetInternCap(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
			_world.mInternCap = (int)Fibre.mLocal.mStore[Fibre.mFramePtr].mNumber;

		return null;
	}

	public CFibreReg SetItemAvailable(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 3)
		{
			bool match = true;
			int playerId = Fibre.GetInt(0, ref match);
			string itemName = Fibre.GetString(1, ref match);
			bool available = Fibre.GetBool(2, ref match);

			if (match)
			{
				if (available)
					_world.mPlayers[playerId].mAvailableItems.Add(itemName);
				else
					_world.mPlayers[playerId].mAvailableItems.Remove(itemName);
			}
		}

		return null;
	}

	public CFibreReg NotifyNewItem(CFibre Fibre, int ArgCount)
	{
		if (ArgCount != 2)
			return null;

		int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
		string itemName = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mString;

		_world.AddTransientEvent(1 << playerId).SetNotify(itemName);

		return null;
	}

	public CFibreReg CinematicStart(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
		{
			int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			_world.mPlayers[playerId].mCanInteractWithWorld = false;
			_world.mPlayers[playerId].mCanControlCamera = false;
			_world.mPlayers[playerId].mShowUI = false;
		}

		return null;
	}

	public CFibreReg CinematicStop(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 1)
		{
			int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			_world.mPlayers[playerId].mCanInteractWithWorld = true;
			_world.mPlayers[playerId].mCanControlCamera = true;
			_world.mPlayers[playerId].mShowUI = true;
			_world.mPlayers[playerId].mCamSpeed = 1.0f;
		}

		return null;
	}

	public CFibreReg MusicPlay(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 2)
		{
			int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			int trackId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;
			_world.mPlayers[playerId].mMusicTrack = trackId;
		}

		return null;
	}

	public CFibreReg SoundPlay(CFibre Fibre, int ArgCount)
	{
		if (ArgCount != 2)
			return null;

		int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
		int soundId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;

		//_world.AddTransientEvent(1 << playerId).SetSound(_world.mPlayers[playerId].mCamPosition, soundId);
		_world.AddTransientEvent(1 << playerId).SetUISound(_world.mPlayers[playerId].mCamPosition, soundId);

		return null;
	}

	public CFibreReg CameraSetPosition(CFibre Fibre, int ArgCount)
	{
		if (ArgCount == 6)
		{
			int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
			float x = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mNumber;
			float y = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 2].mNumber;
			float z = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 3].mNumber;
			float zoom = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 4].mNumber;
			float speed = (float)Fibre.mLocal.mStore[Fibre.mFramePtr + 5].mNumber;

			_world.mPlayers[playerId].mCamPosition = new Vector4(x, y, z, zoom);
			_world.mPlayers[playerId].mCamSpeed = speed;

			if (speed == 0)
				++_world.mPlayers[playerId].mCamTrackCount;
		}

		return null;
	}

	public CFibreReg AIEnable(CFibre Fibre, int ArgCount)
	{
		if (ArgCount != 2)
			return null;

		int playerId = (int)Fibre.mLocal.mStore[Fibre.mFramePtr + 0].mNumber;
		bool enabled = Fibre.mLocal.mStore[Fibre.mFramePtr + 1].mBool;

		_world.mPlayers[playerId].SetAI(enabled);

		return null;
	}
}
