using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the game environment??
/// </summary>
public class CGameSession
{
	public enum EPlayType
	{
		SINGLE,
		LOAD,
		HOST,
		CLIENT,
		REPLAY
	}

	/// <summary>
	/// Results generate after a game finishes to allow the results screen to show stuff.
	/// </summary>
	public class CGameResults
	{
	}

	public class CPlayerStartData
	{
		public int mIndex;
		public string mName;
	}

	/// <summary>
	/// Parameters used to start the game session.
	/// </summary>
	public class CStartParams
	{
		public EPlayType mPlayType;
		public string mLevelName;
		public string mFileName;
		public int mUserPlayerIndex;
		public CPlayerStartData[] mPlayerStartData;
	}

	public CStartParams mStartParams;

	private CWorld _world;
	private CSimThread _simThread;
	private CUserWorldView _userWorldView;
	private CReplay _replay;
	private CUserSession _userSession;

	private bool _crashed = false;

	public GameObject mPrimaryScene;

	public int mUserPlayerIndex;
		
	private float _lastUpdateTime = 0.0f;
	private float _primaryLoopDuration = 0.0f;
	private bool _started = false;

	public CGameSession()
	{

	}

	public bool Init(CStartParams Params)
	{
		mStartParams = Params;

		CLevelAsset level = CGame.AssetManager.GetAsset<CLevelAsset>(Params.mLevelName);

		if (level == null)
		{
			Debug.LogError("Could not load level " + Params.mLevelName);
			return false;
		}
		
		_world = new CWorld();

		if (!_world.mScriptManager.Init(Params.mLevelName + CGame.FIBRE_FILE_EXTENSION))
		{
			_world.mScriptManager.Destroy();
			return false;
		}

		_userWorldView = new CUserWorldView();

		mPrimaryScene = new GameObject("Primary Scene");
		mPrimaryScene.transform.position = Vector3.zero;

		CGame.UIManager.SetupForGameSession();

		SCameraState camState = new SCameraState();
		camState.mBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1.0f);
		camState.SetViewGame(EViewDirection.VD_FRONT);
		camState.mLockedToMap = true;

		if (Params.mPlayType == EPlayType.SINGLE)
		{
			level.CreateMap(_world.mMap);
			_world.InitCompanies();
			_world.PopulateWithMetaData(level);
			_world.PostInit();
			_world.mMap.GetLevelGOB().transform.SetParent(mPrimaryScene.transform);

			CPlayer userPlayer = _world.GetFirstPlayablePlayer();
			userPlayer.mHumanInput = true;
			mUserPlayerIndex = userPlayer.mID;

			camState.mBackgroundColor = level.mBackgroundColor;
			
			//_replay.StartRecording("replay");

			_userSession = new CUserSession(this, _userWorldView);
			_world.mScriptManager.Start(_world);
		}
		else if (Params.mPlayType == EPlayType.LOAD)
		{
			// TODO: Load all state from file.
			// Base map stuff is also saved into file.
			// Session, UI, UserWorldView, World
			//_DeserializeState(CGame.PersistentDataDirectory + CGame.SAVE_DIRECTORY + StateFileName + CGame.SAVE_FILE_EXTENSION);
		}
		else if (Params.mPlayType == EPlayType.HOST)
		{
			mUserPlayerIndex = Params.mUserPlayerIndex;
			// TODO: Load level exactly like singleplayer
			//if (Net.Host(5000)) Debug.Log("Hosting Game");
		}
		else if (Params.mPlayType == EPlayType.CLIENT)
		{
			mUserPlayerIndex = Params.mUserPlayerIndex;
			// TODO: Load level exactly like singleplayer
			// if (Net.Connect(Datastore.mConfig.mHostIP, 5000)) Debug.Log("Joined Game");
		}
		else if (Params.mPlayType == EPlayType.REPLAY)
		{
			mUserPlayerIndex = Params.mUserPlayerIndex;
			// TODO: Load level exactly like singleplayer
			//_DeserializeState(CGame.PersistentDataDirectory + CGame.REPLAYS_DIRECTOY + FileName + CGame.REPLAY_FILE_EXTENSION);
			_replay = new CReplay();
			//_replay.StartPlayback(FileName);
		}
		
		_userWorldView.Init(this, _world, _userSession, mUserPlayerIndex);
		_simThread = new CSimThread(this);
		camState.mTargetPosition = _world.mPlayers[mUserPlayerIndex].GetSpawnPos().ToWorldVec3();
		CGame.CameraManager.SetCamState(camState);

		/*
		SDecalInfo decalInfo = new SDecalInfo();
		decalInfo.mType = CDecal.EDecalType.TEXT;
		decalInfo.mText = "Paperwork";
		decalInfo.mSize = new Vector2(1, 1);
		decalInfo.mPosition = new Vector3(13, 0.1f, 11);
		decalInfo.mRotation = Quaternion.Euler(90, 0, 0);
		decalInfo.mColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
		decalInfo.mVis = CDecal.EDecalVis.LOS;
		_world.SpawnDecal(0, decalInfo);
		
		decalInfo.mType = CDecal.EDecalType.IMAGE;
		decalInfo.mPosition = new Vector3(13, 0.1f, 14);
		decalInfo.mSize = new Vector2(4, 4);
		decalInfo.mRotation = Quaternion.Euler(90, 0, 0);
		decalInfo.mVisualId = 1;
		//decalInfo.mColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
		_world.SpawnDecal(0, decalInfo);

		decalInfo.mType = CDecal.EDecalType.TEXT;
		decalInfo.mText = "Lobby";
		decalInfo.mSize = new Vector2(0.8f, 0.8f);
		decalInfo.mPosition = new Vector3(7, 0.1f, 13);
		decalInfo.mRotation = Quaternion.Euler(90, 90, 0);
		decalInfo.mColor = new Color(1,1,1,1);
		decalInfo.mVis = CDecal.EDecalVis.FOW;
		_world.SpawnDecal(0, decalInfo);
		*/

		//_world.SpawnItem(CGame.AssetManager.GetAsset<CItemAsset>("item_couch_test"), new Vector2(10, 6), 0, 0);

		/*
		for (int i = 0; i < 10; ++i)
		{
			CResume resume = new CResume();
			resume.Generate(_world, 1, 1);
			CUnit entity = _world.SpawnUnit(0, resume, new Vector2(20, 20), 0);
		}
		//*/

		// TODO: Do we need to check a map always has correct spawns for players?
		//_world.SpawnItem(CGame.AssetManager.GetAsset<CItemAsset>("item_spawn"), new Vector2(12, 16), 0, 0);
		//_world.SpawnItem(CGame.AssetManager.GetAsset<CItemAsset>("item_spawn"), new Vector2(21, 16), 0, 1);
		return true;
	}

	public void Destroy()
	{
		// SimThread
		// UserWorldView
		// UserSession
		// World
		// ModManager?
		// Replay

		if (_simThread != null)
		{
			_simThread.StopThread();
			_simThread = null;
		}

		// Wait for thread to be destroyed? Would require update to pause. _started = false;
		if (_userWorldView != null)
		{
			_userWorldView.Destroy();
			_userWorldView = null;
		}

		if (_userSession != null)
		{
			_userSession.Destroy();
			_userSession = null;
		}

		if (mPrimaryScene != null)
		{
			GameObject.Destroy(mPrimaryScene);
			mPrimaryScene = null;
		}
	}

	/// <summary>
	/// Start the game session.
	/// </summary>
	public void Start()
	{
		_simThread.StartThread(_world, mUserPlayerIndex);
		_started = true;
	}

	//-----------------------------------------------------------
	// Sim Thread Interface
	//-----------------------------------------------------------

	// (Called from sim thread)

	// Gets all the actions that have be queued locally and turns them into a turn.
	// Sends turn to journal/net as needed by game session config.
	// CTurn GetTurnActions();

	// Halt turn gathering

	// Resume turn gathering

	// Maybe some kind of turn pump?

	/// <summary>
	/// Save the level data.
	/// </summary>
	public void SerializeLevel(string FullFilePath)
	{		
		/*
		using (BinaryWriter w = new BinaryWriter(File.Open(FullFilePath, FileMode.Create)))
		{
			w.Write(CGame.VERSION_MAJOR);
			w.Write(CGame.VERSION_MINOR);
			w.Write(CGame.LEVEL_FILE_VERSION);

			_world.SerializeLevel(w);
		}
		*/
	}

	/// <summary>
	/// Load level data.
	/// </summary>
	private void _DesserializeLevel(string FullFilePath)
	{
		/*
		try
		{
			using (BinaryReader r = new BinaryReader(File.Open(FullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				int vMajor = r.ReadInt32();
				int vMinor = r.ReadInt32();
				int levelVersion = r.ReadInt32();

				if (levelVersion != 1)
				{
					Debug.LogError("Level File Verison Unknown: " + levelVersion);
				}
				else
				{
					_world = new CWorld(this);
					_world.DeserializeLevel(r, levelVersion);
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogError("Couldn't Load Level: " + e.StackTrace);
		}
		*/
	}

	/// <summary>
	/// Serialize session state for Saved Games.
	/// </summary>
	private void _SerializeState(string StateFileFullPath)
	{
		/*
		using (BinaryWriter w = new BinaryWriter(File.Open(StateFileFullPath, FileMode.Create)))
		{
			w.Write(CGame.VERSION_MAJOR);
			w.Write(CGame.VERSION_MINOR);
			w.Write(_currentPlayer);
						
			_world.SerializeState(w);
		}
		*/
	}

	/// <summary>
	/// Deserialize session state for Saved Games.
	/// </summary>
	private void _DeserializeState(string StateFileFullPath)
	{
		/*
		try
		{
			using (BinaryReader r = new BinaryReader(File.Open(StateFileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				int vMajor = r.ReadInt32();
				int vMinor = r.ReadInt32();
				int player = r.ReadInt32();
				
				_world = new CWorld(this);
				_world.DeserializeState(r);

				// Post Setup
				SetPlayerID(player);
			}
		}			
		catch (Exception e)
		{
			Debug.LogError("Couldn't Load Session State: " + e.Message);
		}
		*/
	}

	public void PushUserAction(CUserAction Action)
	{
		// TODO: Gather actions for distribution over network.
		_simThread.PushUserAction(Action);
	}

	/// <summary>
	/// Per frame update.
	/// </summary>
	public void Update(CInputState InputState)
	{
		if (!_started)
			return;

		float timeNow = Time.realtimeSinceStartup;
		_primaryLoopDuration = timeNow - _lastUpdateTime;
		_lastUpdateTime = timeNow;

		if (_world.mMap.GetFloorAlwaysVisible() != CGame.VarNoFow.mValue)
		{
			_world.mMap.SetFloorAlwaysVisible(CGame.VarNoFow.mValue);
		}

		CGame.CameraManager.mCanZoom = !InputState.mOverUI;

		// Intersect mouse with floor
		Ray r = CGame.PrimaryResources.PrimaryCamera.ScreenPointToRay(InputState.mMousePosition);
		Plane p = new Plane(Vector3.up, 0.0f);
		float t;
		bool hit = p.Raycast(r, out t);
		Vector3 mouseFloorPos = r.direction * t + r.origin;

		if (!_crashed)
		{
			if (!_userWorldView.Update(mouseFloorPos.ToWorldVec2()))				
			{
				_crashed = true;
				CGame.UIManager.ShowErrorMessage(_world.mCrashMessage, "Simulation Thread Exception");
			}
		}

		_userSession.Update(InputState);

		// TODO: For debug purposes, maybe put this in a more sane place so we don't stall the whole world.
		lock (_world)
		{
			_world.DrawDebug(InputState);

			if (CGame.VarShowDDATest.mValue)
				DDATest(InputState);

			if (CGame.VarShowArcTest.mValue)
				TrajectoryTest(InputState);

			if (CGame.VarShowProxies.mValue > 0)
			{
				int playerId = CGame.VarShowProxies.mValue - 1;

				for (int iX = 0; iX < 100; ++iX)
				{
					for (int iY = 0; iY < 100; ++iY)
					{
						if (_world.mMap.IsTileVisible(playerId, iX, iY))
							CDebug.DrawYRectQuad(new Vector3(iX + 0.5f, 0.0f, iY + 0.5f), 0.9f, 0.9f, new Color(1, 0, 0, 0.5f), false);
					}
				}

				for (int i = 0; i < _world.mItemProxies[playerId].Count; ++i)
				{
					CDebug.DrawBounds(_world.mItemProxies[playerId][i].mBounds, new Color(0, 1, 0, 1), false);
				}
			}
		}

		if (CGame.VarShowFlowField.mValue)
		{
			if (Input.GetMouseButtonDown(1))
			{
				_userWorldView.GenerateFlowField(new Rect(mouseFloorPos.x * 2, mouseFloorPos.z * 2, 2, 2));
			}
		}
	}

	public void SetOwed(int Value)
	{
		lock (_world)
		{
			for (int i = 0; i < _world.mUnits.Count; ++i)
				_world.mUnits[i].mOwedSalary += Value;
		}
	}

	public void SetStamina(float Value)
	{
		lock (_world)
		{
			for (int i = 0; i < _world.mUnits.Count; ++i)
				_world.mUnits[i].mStamina = Value;
		}
	}

	public void SetHunger(float Value)
	{
		lock (_world)
		{
			for (int i = 0; i < _world.mUnits.Count; ++i)
				_world.mUnits[i].mHunger = Value;
		}
	}

	/// <summary>
	/// Draw a trajectory based on start and end points.
	/// </summary>
	public void TrajectoryTest(CInputState InputState)
	{
		// TODO: Consolidate start and end points for these kinds of tests.
		// Intersect mouse with floor
		Ray r = CGame.PrimaryResources.PrimaryCamera.ScreenPointToRay(InputState.mMousePosition);
		Plane p = new Plane(Vector3.up, 0.0f);
		float t;
		bool hit = p.Raycast(r, out t);
		Vector3 mouseFloorPos = r.direction * t + r.origin;

		Vector3 start = new Vector3(12, 0.0f, 12);
		Vector3 end = mouseFloorPos;

		CDebug.DrawYRect(start, 0.25f, 0.25f, Color.red, false);
		CDebug.DrawYRect(end, 0.25f, 0.25f, Color.red, false);

		CDebug.DrawLine(start, end, Color.blue, false);

		float angle = 45.0f;
		float gravity = CMissile.GRAVITY;

		Vector3 cachePoint = start;
		Vector3 accel = new Vector3(0, gravity * 20.0f * 20.0f, 0);
		Vector3 newVel = CMath.GetMissileVelocity(start, mouseFloorPos, angle, accel.y);

		float time = 0.0f;
		while (cachePoint != mouseFloorPos)
		{
			Vector3 dest = start + newVel * time + 0.5f * accel * time * time;

			if (dest.y < 0.0f)
			{
				dest = mouseFloorPos;
			}

			CDebug.DrawYRect(dest, 0.1f, 0.1f, Color.green, false);
			CDebug.DrawLine(cachePoint, dest, Color.white, false);
			cachePoint = dest;
			time += 0.1f;
		}
	}

	private Vector2 _ddaTestStartPoint = Vector2.zero;

	/// <summary>
	/// Test the DDA query function.
	/// </summary>
	public void DDATest(CInputState InputState)
	{
		// Intersect mouse with floor
		Ray r = CGame.PrimaryResources.PrimaryCamera.ScreenPointToRay(InputState.mMousePosition);
		Plane p = new Plane(Vector3.up, 0.0f);
		float t;
		bool hit = p.Raycast(r, out t);
		Vector3 mouseFloorPos = r.direction * t + r.origin;
		Vector2 end = mouseFloorPos.ToWorldVec2();

		if (InputState.GetCommand("space").mPressed)
		{
			_ddaTestStartPoint = end;
		}

		_world.mMap.TraceNodesDebug(0, _ddaTestStartPoint, end);

		CDebug.DrawLine(_ddaTestStartPoint.ToWorldVec3(), end.ToWorldVec3(), Color.white, false);

		/*
		int cX = (int)mouseFloorPos.x;
		int cY = (int)mouseFloorPos.z;

		Vector2 startPos = new Vector2(15, 15.5f);
		Vector2 endPos = new Vector2(mouseFloorPos.x, mouseFloorPos.z);

		CDebug.DrawYSquare(new Vector3(cX + 0.5f, 0.0f, cY + 0.5f), 0.75f, Color.black, false);
		CDebug.DrawYSquare(new Vector3(endPos.x, 0.0f, endPos.y), 0.5f, Color.red, false);
		CDebug.DrawYSquare(new Vector3(startPos.x, 0.0f, startPos.y), 0.5f, Color.green, false);
		CDebug.DrawLine(new Vector3(startPos.x, 0.0f, startPos.y), new Vector3(endPos.x, 0.0f, endPos.y), Color.white, false);

		CRayGridQuery2D rq = new CRayGridQuery2D(startPos, (endPos - startPos).normalized);

		int rX = 0;
		int rY = 0;
		int tX = (int)endPos.x;
		int tY = (int)endPos.y;
		int max = 50;
		int dX = 0;
		int dY = 0;

		while (max-- > 0)
		{
			rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);
			
			// TODO: shouldn't this break??
			if (rX < 1 || rY < 1 || rX >= 99 || rY >= 99)
				continue;

			CDebug.DrawYSquare(new Vector3(rX + 0.5f, 0.0f, rY + 0.5f), 1.0f, Color.white, false);

			if (_world.mMap.mTiles[rX, rY].mFloor.mSolid)
				break;

			if ((dX > 0) && (_world.mMap.mTiles[rX + 1, rY].mWallZ.mSolid)) break;
			else if ((dX < 0) && (_world.mMap.mTiles[rX, rY].mWallZ.mSolid)) break;
			else if ((dY > 0) && (_world.mMap.mTiles[rX, rY + 1].mWallX.mSolid)) break;
			else if ((dY < 0) && (_world.mMap.mTiles[rX, rY].mWallX.mSolid)) break;
			
			if (rX == tX && rY == tY)
				break;
		}
		*/
	}

	/// <summary>
	/// Debug GUI Drawing
	/// </summary>
	public void OnGUI()
	{
		// TODO: Ugh, what to do with this, convert to new UI?

		if (CGame.VarShowDebugStats.mValue)
		{
			string debugText = "";

			debugText += "Sim Tick: " + _world.mGameTick;// + " " + _simTickCount + " " + (mWorld.mGameTick - _simTickCount);
			debugText += "\nPrim Loop Time: " + (_primaryLoopDuration * 1000.0f) + "ms";
			debugText += "\nNext Ent ID: " + CEntity.GetCurrentID();
			debugText += "\nDebug Lines: " + CDebug.mLineDesiredCount + "/" + CDebug.mLineBufferCount;

			GUI.Label(new Rect(5, 0, 200, 1000), debugText);
		}
	}
}
