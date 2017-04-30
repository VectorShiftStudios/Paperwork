using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Container for the simulation.
/// </summary>
public class CWorld
{
	public const int MAX_PLAYERS = 5;
	public const int SALARAY_INTERVAL_SECONDS = 180;
	public const int CONTRACT_INTERVAL_SECONDS = 60;
	public const int TICKS_PER_SECOND = 20;
	public const float SECONDS_PER_TICK = 1.0f / TICKS_PER_SECOND;

	public struct SContractTurn
	{
		public int mTime;
		public int mNumContracts;

		public SContractTurn(int Time, int NumContracts)
		{
			mTime = Time;
			mNumContracts = NumContracts;
		}
	}

	public struct SResumeTurn
	{
		public int mTime;

		public SResumeTurn(int Time)
		{
			mTime = Time;
		}
	}

	public bool mCrashed = false;
	public string mCrashMessage = "";

	public CRandomStream SimRnd;
	public CMap mMap;
	public CScriptManager mScriptManager;

	public CPlayer[] mPlayers;
	public List<CClientCompany> mClientCompanies;
	public List<CContractTier> mContractTiers;

	private List<CEntity> _entitiesToDestroy;
	public List<CEntity> mEntities;
	public Dictionary<int, CEntity> mEntHash;
	public List<CUnit> mUnits;
	public List<CItem> mItems;
	public List<CMissile> mMissiles;
	public List<CItem> mBlueprints;
	public List<CPickup> mPickups;
	public List<CResume> mResumes;
	public List<CContract> mContracts;
	public List<CVolume> mVolumes;
	public List<CDecal> mDecals;
	public List<CItemProxy>[] mItemProxies;
	public List<CBuildOrder> mBuildOrders;
	public List<SContractTurn> mContractTurns;
	public List<SResumeTurn> mResumeTurns;
	public List<CTransientEvent> mTransientEvents;

	public int mGameTick = 0;
	private int _nextSalaryTime;
	public int mWinMoney;
	public int mFailSeconds;
	public int mPopCap;
	public int mInternCap;

	/// <summary>
	/// Standard constructor.
	/// </summary>
	public CWorld()
	{
		SimRnd = new CRandomStream(0);		
		mMap = new CMap();
		mScriptManager = new CScriptManager();
		mPlayers = new CPlayer[MAX_PLAYERS];
		mClientCompanies = new List<CClientCompany>();
		mContractTiers = new List<CContractTier>();
		mEntities = new List<CEntity>();
		mEntHash = new Dictionary<int, CEntity>();
		mMissiles = new List<CMissile>();
		mUnits = new List<CUnit>();
		mItems = new List<CItem>();
		mBlueprints = new List<CItem>();
		mPickups = new List<CPickup>();
		mResumes = new List<CResume>();
		mContracts = new List<CContract>();
		mVolumes = new List<CVolume>();
		mDecals = new List<CDecal>();
		mItemProxies = new List<CItemProxy>[MAX_PLAYERS];
		mContractTurns = new List<SContractTurn>();
		mResumeTurns = new List<SResumeTurn>();
		mBuildOrders = new List<CBuildOrder>();
		_entitiesToDestroy = new List<CEntity>();
		mTransientEvents = new List<CTransientEvent>();

		for (int i = 0; i < MAX_PLAYERS; ++i)
		{
			mPlayers[i] = new CPlayer(this, i);
			mItemProxies[i] = new List<CItemProxy>();
		}
	}

	/// <summary>
	/// Put an order on the order queue.
	/// </summary>	
	public void AddBuildOrder(CBuildOrder Order)
	{
		mBuildOrders.Add(Order);
	}

	public void RemoveBuildOrder(CBuildOrder Order)
	{
		mBuildOrders.Remove(Order);
	}

	/// <summary>
	/// Update all orders in queue.
	/// </summary>
	private void _UpdateBuildOrders()
	{
		for (int i = 0; i < mBuildOrders.Count; ++i)
		{
			mBuildOrders[i].SimTick();
		}
	}

	/// <summary>
	/// Add item proxy.
	/// </summary>
	public void AddItemProxy(int PlayerID, CItemProxy ItemView)
	{
		mItemProxies[PlayerID].Add(ItemView);
	}

	/// <summary>
	/// Remove item proxy.
	/// </summary>
	public void RemoveItemProxy(int PlayerID, CItemProxy ItemView)
	{
		mItemProxies[PlayerID].Remove(ItemView);
	}
	
	/// <summary>
	/// Create companies.
	/// </summary>
	public void InitCompanies()
	{
		// TODO: Companies should be set in script for more flexibility.
		mClientCompanies.Add(new CClientCompany(this, "Company A", new Color(1.0f, 1.0f, 1.0f), -0.025f, 0.025f, 0.0f));
		mClientCompanies.Add(new CClientCompany(this, "Company B", new Color(1.0f, 0.0f, 0.0f), 0.025f, -0.025f, 0.0f));
		mClientCompanies.Add(new CClientCompany(this, "Company C", new Color(0.0f, 1.0f, 0.0f), 0.025f, -0.025f, 0.0f));
		mClientCompanies.Add(new CClientCompany(this, "Company D", new Color(0.0f, 0.0f, 1.0f), 0.025f, -0.025f, 0.0f));
		mClientCompanies.Add(new CClientCompany(this, "Company E", new Color(1.0f, 1.0f, 0.0f), 0.025f, -0.025f, 0.0f));
		mClientCompanies.Add(new CClientCompany(this, "Company F", new Color(0.0f, 1.0f, 1.0f), 0.025f, -0.025f, 0.0f));

		mContractTiers.Add(new CContractTier(5, 5, 10));
		mContractTiers.Add(new CContractTier(10, 6, 12));
	}

	public void PopulateWithMetaData(CLevelAsset Level)
	{
		int highestId = 0;

		for (int i = 0; i < Level.mObjects.Count; ++i)
		{
			CLevelMetaObject meta = Level.mObjects[i];

			if (meta.mID > highestId)
				highestId = meta.mID;

			if (meta.mType == CLevelMetaObject.EType.ITEM)
			{
				CItemAsset itemAsset = CGame.AssetManager.GetAsset<CItemAsset>(meta.mIdentifier);
				CItem item = SpawnItem(itemAsset, new Vector2(meta.mPositionA.x, meta.mPositionA.y), meta.mRotation, meta.mOwner, meta.mID);

				if (itemAsset.mItemType == EItemType.DOOR)
				{
					((CItemDoor)item).mLocked = meta.mExtraBoolData;
				}
			}
			else if (meta.mType == CLevelMetaObject.EType.UNIT)
			{
				CResume resume = new CResume();
				resume.Generate(this, meta.mSubtype, meta.mExtraIntData);
				CUnit unit = SpawnUnit(meta.mOwner, resume, new Vector2(meta.mPositionA.x, meta.mPositionA.y), meta.mRotation, meta.mID);
			}
			else if (meta.mType == CLevelMetaObject.EType.VOLUME)
			{
				SpawnVolume(meta.mOwner, meta.mPositionA, meta.mPositionB, meta.mID);
			}
			else if (meta.mType == CLevelMetaObject.EType.DECAL)
			{
				SDecalInfo info = new SDecalInfo();
				info.mPosition = meta.mPositionA;
				info.mSize = meta.mPositionB;
				info.mRotation = Quaternion.Euler(meta.mOrientation);
				info.mText = meta.mIdentifier;
				info.mType = (CDecal.EDecalType)meta.mSubtype;
				info.mVisualId = meta.mData;
				info.mColor = meta.mColor;
				info.mVis = (CDecal.EDecalVis)meta.mExtraIntData;

				SpawnDecal(meta.mOwner, info, meta.mID);
			}
			else if (meta.mType == CLevelMetaObject.EType.PLAYER)
			{
				mPlayers[meta.mID].Init(CUtility.CheckFlag(meta.mExtraIntData, CPlayer.FLAG_PLAYABLE), CUtility.CheckFlag(meta.mExtraIntData, CPlayer.FLAG_COMPETING), meta.mColor, meta.mData);
			}
		}

		CEntity.SetCurrentID(++highestId);
	}

	public CPlayer GetFirstPlayablePlayer()
	{
		for (int i = 0; i < MAX_PLAYERS; ++i)
		{
			if (mPlayers[i].mPlayable == true)
				return mPlayers[i];
		}

		return null;
	}

	/// <summary>
	/// Happens after loading, by deserialize or new map
	/// </summary>
	public void PostInit()
	{
		mMap.CollisionModified();
		mMap.RebuildMesh();
		mMap.UpdateNavMeshes(true);
	}
	
	/// <summary>
	/// Serialize the state of the current simulation.
	/// </summary>
	public void SerializeState(BinaryWriter W)
	{
		// Serialize:
		// Save types & IDs
		// Save props for those things

		// Deserialize:
		// Create all instances of things
		// Init them with the serialized state

		// Save RND seed

		mMap.Serialize(W);
		//gametick
		//_nextsalarytime
		//_nextcontractTime
		//mwinMoney
		//mfailseconds
		//mpopcap
		//minterncap

		//companies
			//id
			//name
			//playerloyalties
		
		//players
			//id
			//color
			//money
			//debts
			//units waiting for order
			//orders
			//debtTimer

		/*
		//ents
		W.Write(CEntity.GetCurrentID());
		W.Write(mEntities.Count);
		for (int i = 0; i < mEntities.Count; ++i)
		{
			mEntities[i].SerializeState(W);
		}
		*/

		// Basic data, then pointer related data in 2nd pass?

		//ghosts
		// Why aren't ghosts just normal ents?
	}

	/// <summary>
	/// Deserialize a state of the simulation.
	/// </summary>
	public void DeserializeState(BinaryReader R)
	{
		// Set RND seed

		/*
		mMap.Deserialize(R);
		mMap.BuildRoomMap();

		// Read Entities
		CEntity.SetCurrentID(R.ReadInt32());

		int count = R.ReadInt32();

		for (int i = 0; i < count; ++i)
		{
			int id = R.ReadInt32();
			int type = R.ReadInt32();

			// TOOD: Can we just use normal spawning here with some desirialize flag?
			CEntity entity = CEntity.Create((CEntity.EType)type, this, id);
			entity.DeserializeState(R);
			entity.PostDeserialize();

			// Register with world lists
			mEntities.Add(entity);
			if (entity.IsUnit())
				mUnits.Add((CUnit)entity);
			else
				mItems.Add((CItem)entity);

			entity.PlaceInWorld();
		}

		PostInit();
		*/
	}

	/// <summary>
	/// Get an entity with specified ID and cast to specified type.
	/// Returns null if can't cast or ID invalid.
	/// </summary>
	public T GetEntity<T>(int ID)
		where T : CEntity
	{
		if (ID == -1)
			return null;

		CEntity entity = null;

		if (mEntHash.TryGetValue(ID, out entity))
			return entity as T;

		return null;
	}

	/// <summary>
	/// Get an item proxy by ID.
	/// </summary>
	public CItemProxy GetItemPorxy(int PlayerID, int ID)
	{
		for (int i = 0; i < mItemProxies[PlayerID].Count; ++i)
		{
			if (mItemProxies[PlayerID][i].mID == ID)
				return mItemProxies[PlayerID][i];
		}

		return null;
	}
	
	/// <summary>
	/// Get unit by ID.
	/// </summary>
	public CUnit GetUnit(int ID)
	{
		if (ID != 0)
			for (int i = 0; i < mUnits.Count; ++i)
			{
				if (mUnits[i].mID == ID)
					return mUnits[i];
			}

		return null;
	}

	/// <summary>
	/// Get a pickup that has specified build tag.
	/// </summary>
	public CPickup GetPickupWithBuildTag(int BuildTag)
	{
		for (int i = 0; i < mPickups.Count; ++i)
		{
			if (mPickups[i].mBuildTag == BuildTag)
				return mPickups[i];
		}

		return null;
	}

	/// <summary>
	/// Add a transient event for certain players.
	/// </summary>
	public CTransientEvent AddTransientEvent(int PlayerFlags)
	{
		CTransientEvent tev = new CTransientEvent(mGameTick, PlayerFlags);
		mTransientEvents.Add(tev);

		return tev;
	}

	/// <summary>
	/// Only players who can see this location will see this event.
	/// </summary>
	public CTransientEvent AddTransientEventFOW(Vector2 VisiblityPosition)
	{
		return AddTransientEvent(GetPlayerFlagsForTileVisibility(VisiblityPosition));
	}

	/// <summary>
	/// Get player flags for a single player.
	/// </summary>
	public int GetPlayerFlags(int PlayerID)
	{
		// TODO: Move to a utility function & make static?
		return (1 << PlayerID);
	}

	/// <summary>
	/// Get flags for all players who can see this (large) tile.
	/// </summary>
	public int GetPlayerFlagsForTileVisibility(Vector2 Tile)
	{
		int visFlag = 0;

		for (int i = 0; i < MAX_PLAYERS; ++i)
		{
			if (mMap.IsTileVisible(i, (int)Tile.x, (int)Tile.y))
				visFlag |= (1 << i);
		}

		return visFlag;
	}

	/// <summary>
	/// Take control of the current transient events list,
	/// and create a new empty one to replace it.
	/// </summary>
	public List<CTransientEvent> SwapAndClearTransientEvents()
	{
		List<CTransientEvent> temp = mTransientEvents;
		mTransientEvents = new List<CTransientEvent>();
		
		return temp;
	}

	private void _AddEntity(CEntity Entity)
	{
		mEntities.Add(Entity);
		mEntHash.Add(Entity.mID, Entity);
	}

	private void _RemoveEntity(CEntity Entity)
	{
		mEntities.Remove(Entity);
		mEntHash.Remove(Entity.mID);
	}

	/// <summary>
	/// Spawn an item in the world.
	/// </summary>
	public CItem SpawnItem(CItemAsset Asset, Vector2 Position, int Rot, int Owner, int Id = 0)
	{
		CItem item = CEntity.Create(CEntity.AssetItemTypeToEntityType(Asset.mItemType), this, Id) as CItem;
		mItems.Add(item);
		item.InitItem(Asset);
		item.SetPosition(Position.x, Position.y);
		item.SetRotation(Rot);
		item.SetOwner(Owner);
		item.PlaceInWorld();

		_AddEntity(item);
		
		return item;
	}

	/// <summary>
	/// Spawn a blueprint in the world.
	/// </summary>
	public CItem SpawnBlueprint(CItemAsset Asset, Vector2 Position, int Rot, int Owner)
	{
		CItem item = CEntity.Create(CEntity.AssetItemTypeToEntityType(Asset.mItemType), this) as CItem;
		item.InitItem(Asset);
		item.SetPosition(Position.x, Position.y);
		item.SetRotation(Rot);
		item.SetOwner(Owner);
		item.PlaceAsBlueprint();

		_AddEntity(item);
		mBlueprints.Add(item);

		return item;
	}

	/// <summary>
	/// Spawn a unit in the world.
	/// </summary>
	public CUnit SpawnUnit(int PlayerID, CResume Resume, Vector2 Position, int Rotation, int Id = 0)
	{
		CUnit unit = CEntity.Create<CUnit>(this, Id);
		unit.InitUnit(Resume);
		unit.SetPosition(Position.x, Position.y);
		unit.SetRotation(Rotation);
		unit.SetOwner(PlayerID);
		unit.CalcBounds();

		_AddEntity(unit);
		mUnits.Add(unit);

		return unit;
	}

	/// <summary>
	/// Create a unit that enters from the player's spawn point.
	/// </summary>
	public CUnit SpawnUnitAtElevator(CItemStart Start, int PlayerID, CResume Resume)
	{
		CUnit unit = SpawnUnit(PlayerID, Resume, Start.mSpawnPos, 0);
		unit.SetAction(CUnitActions.EType.ENTER);

		return unit;
	}

	/// <summary>
	/// Spawn a pickup that enters from the player's spawn point.
	/// </summary>
	public CPickup SpawnPickup(CItemStart Start, int PlayerID, string ContainedAsset)
	{
		Vector2 pos = Start.mSpawnPos;
		pos.x += SimRnd.GetNextFloat() - 0.5f;
		pos.y += SimRnd.GetNextFloat() - 0.5f;

		CPickup pickup = CEntity.Create<CPickup>(this);
		pickup.SetPosition(pos.x, pos.y);
		pickup.SetOwner(PlayerID);
		pickup.CalcBounds();
		pickup.mContainedItemAsset = CGame.AssetManager.GetAsset<CItemAsset>(ContainedAsset);
		pickup.ExitElevator(Start.mExitPos);

		_AddEntity(pickup);
		mPickups.Add(pickup);

		return pickup;
	}

	public CMissile SpawnMissile(int PlayerID, Vector3 Position, Vector3 TargetPosition)
	{
		CMissile missile = CEntity.Create<CMissile>(this);

		missile.SetOwner(PlayerID);
		missile.Set(Position, TargetPosition);

		_AddEntity(missile);
		mMissiles.Add(missile);

		return null;
	}

	public CVolume SpawnVolume(int PlayerID, Vector3 Position, Vector3 Size, int Id = 0)
	{
		CVolume volume = CEntity.Create<CVolume>(this, Id);

		volume.SetOwner(PlayerID);
		volume.SetPosition(Position, Size);

		_AddEntity(volume);
		mVolumes.Add(volume);

		return null;
	}

	public CVolume SpawnDecal(int PlayerID, SDecalInfo Info, int Id = 0)
	{
		CDecal decal = CEntity.Create<CDecal>(this, Id);

		decal.SetOwner(PlayerID);
		decal.SetInfo(Info);

		_AddEntity(decal);
		mDecals.Add(decal);

		return null;
	}

	/// <summary>
	/// Spawn a resume.
	/// </summary>
	public void SpawnResume()
	{
		CResume r = CEntity.Create<CResume>(this);
		CResume resume = CEntity.Create(CEntity.EType.RESUME, this) as CResume;
		resume.Generate(this, 1, 1);
		_AddEntity(resume);
		mResumes.Add(resume);
	}

	/// <summary>
	/// Spawn a contract for a player.
	/// </summary>
	public void SpawnContract(int PlayerID, int ContractCount)
	{
		Debug.Log("Generate " + ContractCount + " contracts for player " + PlayerID);

		List<CClientCompany> participatingCompanies = new List<CClientCompany>();
		int totalCompanyRep = 0;

		for (int i = 0; i < mClientCompanies.Count; ++i)
		{
			int rep = mClientCompanies[i].mPlayerReputation[PlayerID];

			if (rep != 0)
			{
				totalCompanyRep += rep;
				participatingCompanies.Add(mClientCompanies[i]);
			}
		}

		if (totalCompanyRep != 0)
		{
			float contractPerRep = (float)ContractCount / (float)totalCompanyRep;

			for (int i = 0; i < participatingCompanies.Count; ++i)
			{
				int rep = participatingCompanies[i].mPlayerReputation[PlayerID];
				float share = (float)rep / (float)totalCompanyRep;
				int contracts = (int)(share * ContractCount + 0.5f);
				contracts = Mathf.Max(1, contracts);

				Debug.Log("[" + i + "] Share: " + share + " Contracts: " + contracts);

				for (int c = 0; c < contracts; ++c)
				{
					CContract contract = participatingCompanies[i].GenerateContract(PlayerID);
					if (contract != null)
					{
						_AddEntity(contract);
						mContracts.Add(contract);
					}
				}
			}
		}
	}

	/// <summary>
	/// Despawn an entity.
	/// </summary>
	public void DespawnEntity(CEntity Entity)
	{
		if (Entity.IsItem())
			mItems.Remove((CItem)Entity);
		else if (Entity.IsUnit())
			mUnits.Remove((CUnit)Entity);
		else if (Entity.IsPickup())
			mPickups.Remove((CPickup)Entity);
		else if (Entity.mType == CEntity.EType.RESUME)
			mResumes.Remove((CResume)Entity);
		else if (Entity.mType == CEntity.EType.CONTRACT)
			mContracts.Remove((CContract)Entity);
		else if (Entity.mType == CEntity.EType.MISSILE)
			mMissiles.Remove((CMissile)Entity);
		else if (Entity.mType == CEntity.EType.VOLUME)
			mVolumes.Remove((CVolume)Entity);
		else if (Entity.mType == CEntity.EType.DECAL)
			mDecals.Remove((CDecal)Entity);

		_RemoveEntity(Entity);
		Entity.Destroy();
	}

	/// <summary>
	/// Entity will be destroyed at the start of next game tick.
	/// </summary>
	public void DeferDestroyEntity(CEntity Entity)
	{
		_entitiesToDestroy.Add(Entity);
	}

	/// <summary>
	/// Poor man's hash of the world.
	/// </summary>
	public int GetWorldHash()
	{
		// TODO: Needs to be far more robust and include more things from the simulation.
		int hash = mGameTick;
		hash += mEntities.Count;

		for (int i = 0; i < mUnits.Count; ++i)
		{
			Vector2 pos = mUnits[i].mPosition;

			hash += (int)(pos.x * 10000.0f);
			hash += (int)(pos.y * 10000.0f);
		}

		return hash;
	}

	/// <summary>
	/// Draw debug primitives.
	/// </summary>
	public void DrawDebug(CInputState InputState)
	{
		// NOTE: This is called by the primary thread while the world is locked, and so can interact with Unity as normal.

        if (CGame.VarShowGrid.mValue)
		{
			mMap.DrawGrid();			
			mMap.DebugDrawTileInfo();
		}

		if (CGame.VarShowBounds.mValue)
		{
			for (int i = 0; i < mEntities.Count; ++i)
				mEntities[i].DrawDebugPrims();
		}

		if (CGame.VarShowMobility.mValue != 0)
		{
			mMap.DebugDrawMobility(CGame.VarShowMobility.mValue - 1);
		}

		if (CGame.VarShowSolidity.mValue != 0)
		{
			if (!CGame.VarShowGrid.mValue)
				mMap.DrawGrid();

			if (CGame.VarShowSolidity.mValue == 1)
				mMap.DebugDrawCollision(mMap.mGlobalCollisionTiles);
			else
				mMap.DebugDrawCollision(mMap.mLocalCollisionTiles[CGame.VarShowSolidity.mValue - 2]);
		}

        if (CGame.VarShowPathing.mValue)
		{
			for (int i = 0; i < mUnits.Count; ++i)
				mUnits[i].DebugDrawPathing();
		}

		if (CGame.VarShowComfort.mValue)
		{
			mMap.DebugDrawTileInfluence();
		}
	}

	/// <summary>
	/// Checks if a unit is in a radius.
	/// Owner flags is a bitfield for player IDs.
	/// </summary>
	public bool IsUnitWithinRadius(Vector2 Position, float Radius, int OwnersFlags)
	{
		// TODO: Use spacial query system to make this much faster.
		for (int i = 0; i < mUnits.Count; ++i)
		{
			if (((1 << mUnits[i].mOwner) & OwnersFlags) != 0)
			{
				float distSqr = (mUnits[i].mPosition - Position).SqrMagnitude();

				if (distSqr <= (Radius * Radius))
				{
					return true;
				}
			}
		}

		return false;
	}
	
	/// <summary>
	/// Determine FOW for each player.
	/// </summary>
	private void _UpdateFOW()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_FOW);
		mMap.ResetFOW();

		// TODO: Group entities by owner and execute updates on worker threads.

		for (int i = 0; i < MAX_PLAYERS; ++i)
		{
			CItemStart start = mPlayers[i].GetSpawnItem();

			if (start != null)
			{
				CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_ENTITY);
				mMap.InjectLOS(i, start.mExitPos.x, start.mExitPos.y);
				CGame.SimThreadProfiler.Pop();
			}
		}

		for (int i = 0; i < mUnits.Count; ++i)
		{
			CUnit unit = mUnits[i];

			CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_ENTITY);
			mMap.InjectLOS(unit.mOwner, unit.mPosition.x, unit.mPosition.y);
			CGame.SimThreadProfiler.Pop();
		}

		// TODO: Apply security cameras etc.

		CGame.SimThreadProfiler.Pop();
	}

	/// <summary>
	/// Iterate all items, determine their visibilty and manage proxies.
	/// </summary>
	private void _UpdateProxyViews()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_ITEM_VIS);

		for (int i = 0; i < mItems.Count; ++i)
		{
			mItems[i].UpdateVisibility();
		}

		for (int p = 0; p < MAX_PLAYERS; ++p)
		{
			for (int i = 0; i < mItemProxies[p].Count; ++i)
			{
				if (!mItemProxies[p][i].ShouldExist())
				{
					mItemProxies[p][i].Destroy();
					mItemProxies[p].RemoveAt(i);
					--i;
				}
			}
		}

		CGame.SimThreadProfiler.Pop();
	}
	
	public void RebuildInfluenceMap()
	{
		mMap.ResetInfluence();

		for (int i = 0; i < mItems.Count; ++i)
		{
			if (mItems[i].mType == CEntity.EType.ITEM_DECO)
			{
				Vector2 centre = mItems[i].mBounds.center.ToWorldVec2();
				mMap.InjectInfluence(centre.x, centre.y, mItems[i].mAsset.mStress, mItems[i].mAsset.mValue, mItems[i].mAsset.mAreaOfEffect);
			}
		}
	}

	/// <summary>
	/// Called by simulation thread once per game tick.
	/// </summary>
	public void SimTick()
	{
		long simStartTime = System.Diagnostics.Stopwatch.GetTimestamp();
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_BEGIN_SIM);

		// NOTE: All modifications to simulation world state must happen within a lock on the world instance.

		lock (this)
		{
			mScriptManager.SimTick(mGameTick);
		}

		lock (this)
		{
			_UpdateProxyViews();
		}

		lock (this)
		{	
			_UpdateUnitPushes();
			_UpdateBuildOrders();
			_UpdatePlayers();
		}

		_UpdateEntities();
		
		lock (this)
		{			
			_UpdateSalaries();
			_UpdateContractSpawning();
			_UpdateResumeSpawning();
		}

		// NOTE: The following updates don't lock because we never read their data across threads.
		mMap.UpdateNavMeshes();
		_UpdateFOW();

		lock (this)
		{		
			++mGameTick;
		}

		CGame.SimThreadProfiler.Pop();
		CGame.ProfilerManager.AddProfiledData(CGame.SimThreadProfiler, simStartTime);
		CGame.SimThreadProfiler.Clear();
	}

	/// <summary>
	/// Push units away from each other.
	/// </summary>
	private void _UpdateUnitPushes()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_PHYSICS);

		float pushDistance = 0.4f;
		float pushDistSqr = pushDistance * pushDistance;

		// Units Vs Units
		for (int i = 0; i < mUnits.Count; ++i)
		{
			for (int j = i + 1; j < mUnits.Count; ++j)
			{
				Vector2 dir = (mUnits[i].mPosition - mUnits[j].mPosition);
				float sqrMag = dir.sqrMagnitude;

				if (sqrMag <= pushDistSqr)
				{
					if (sqrMag == 0.0f)
					{
						dir.x = SimRnd.GetNextFloat();
						dir.y = SimRnd.GetNextFloat();
					}

					dir.Normalize();
					dir *= 0.1f;

					mUnits[i].mBeingPushedForce += dir;
					mUnits[j].mBeingPushedForce -= dir;
				}
			}
		}

		// Units Vs Pickups
		for (int i = 0; i < mUnits.Count; ++i)
		{
			for (int j = 0; j < mPickups.Count; ++j)
			{
				if (!mPickups[j].IsCollidable())
					continue;

				Vector2 dir = (mUnits[i].mPosition - mPickups[j].mPosition);
				float sqrMag = dir.sqrMagnitude;

				if (sqrMag <= pushDistSqr)
				{
					if (sqrMag == 0.0f)
					{
						dir.x = SimRnd.GetNextFloat();
						dir.y = SimRnd.GetNextFloat();
					}

					dir.Normalize();
					dir *= 0.1f;

					mUnits[i].mBeingPushedForce += dir;
					mPickups[j].mBeingPushedForce -= dir;
				}
			}
		}

		pushDistSqr = 0.6f * 0.6f;

		// Pickups Vs Pickups
		for (int i = 0; i < mPickups.Count; ++i)
		{
			if (!mPickups[i].IsCollidable())
				continue;

			for (int j = i + 1; j < mPickups.Count; ++j)
			{
				if (!mPickups[j].IsCollidable())
					continue;

				Vector2 dir = (mPickups[i].mPosition - mPickups[j].mPosition);
				float sqrMag = dir.sqrMagnitude;

				if (sqrMag <= pushDistSqr)
				{
					if (sqrMag == 0.0f)
					{
						dir.x = SimRnd.GetNextFloat();
						dir.y = SimRnd.GetNextFloat();
					}

					dir.Normalize();
					dir *= 0.1f;

					mPickups[i].mBeingPushedForce += dir;
					mPickups[j].mBeingPushedForce -= dir;
				}
			}
		}

		CGame.SimThreadProfiler.Pop();
	}

	/// <summary>
	/// Update entities.
	/// </summary>
	private void _UpdateEntities()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_ENTITIES);

		for (int i = 0; i < _entitiesToDestroy.Count; ++i)
		{
			DespawnEntity(_entitiesToDestroy[i]);
			_entitiesToDestroy[i].Destroy();
		}
		_entitiesToDestroy.Clear();

		for (int i = 0; i < mEntities.Count; ++i)
		{
			lock (this)
			{
				CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_ENTITY);

				CEntity entity = mEntities[i];
				entity.SimTick();

				CGame.SimThreadProfiler.Pop();
			}
		}

		CGame.SimThreadProfiler.Pop();
	}
	
	/// <summary>
	/// Update players.
	/// </summary>
	private void _UpdatePlayers()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_PLAYERS);

		for (int i = 0; i < MAX_PLAYERS; ++i)
			mPlayers[i].SimTick();

		CGame.SimThreadProfiler.Pop();
	}

	/// <summary>
	/// Remove contract assignments from all desks.
	/// </summary>	
	public void RemoveContractFromDesks(CContract Contract)
	{
		for (int i = 0; i < mItemProxies[Contract.mOwner].Count; ++i)
		{
			CItemProxy itemProxy = mItemProxies[Contract.mOwner][i];

			if (itemProxy.mAsset.mItemType == EItemType.DESK)
				itemProxy.RemoveContractPapers(Contract);
		}
	}

    /*
	/// <summary>
	/// Find pathed distance between 2 points.
	/// </summary>
	public int FindDistance(int PlayerID, Vector2 Start, Vector2 End, int MaxDistance)
	{
		CSearchNode node = CPathFinder.FindPathDistance(PlayerID, mMap, new CPoint2D((int)Start.x, (int)Start.y), new CPoint2D((int)End.x, (int)End.y), MaxDistance);

		if (node == null)
			return -1;

		return node.pathCost;
	}
    */

	/// <summary>
	/// Get a queue token for the closest usable item of specified type.
	/// </summary>
	public CQueueToken QueueForClosestItem(EItemType ItemType, Vector2 UnitPosition, int UnitOwner)
	{
		float maxD = float.MaxValue;
		CItemProxy itemProxy = null;

		for (int i = 0; i < mItemProxies[UnitOwner].Count; ++i)
		{
			CItemProxy p = mItemProxies[UnitOwner][i];

			if (p.mAsset.mItemType == ItemType && p.CanBeQueued(UnitOwner))
			{
				/*
				int dist = (int)CPathFinder.FindPathCostConstrained(UnitOwner, mMap, new IntVec2(UnitPosition), new IntVec2(p.mPosition), maxD);
				if (dist != -1 && dist < maxD)
				{
					maxD = dist;
					itemProxy = p;
				}
				*/

				// TODO: Check rooms of item and if it can be reached by room ID.
				CNRSearchNode node = CNavRectPather.FindPath(mMap.mNavMeshes[UnitOwner], UnitPosition, p.mPosition, p.mID);

				if (node == null)
					continue;

				float dist = node.mPathCost;

				if (dist < maxD)
				{
					maxD = dist;
					itemProxy = p;					
				}
			}
		}

		if (itemProxy != null)
			return itemProxy.GetQueueToken();

		return null;
	}
	
	/// <summary>
	/// Get the number of workers (less interns) a player has.
	/// </summary>
	public int GetPlayerWorkerCount(int PlayerID)
	{
		int workerCount = 0;

		for (int i = 0; i < mUnits.Count; ++i)
		{
			if (mUnits[i].mOwner == PlayerID && !mUnits[i].mIntern)
				++workerCount;
		}

		return workerCount;
	}

	/// <summary>
	/// Get the number of interns a player has.
	/// </summary>
	public int GetPlayerInternCount(int PlayerID)
	{
		int internCount = 0;
		for (int j = 0; j < mUnits.Count; ++j)
		{
			if (mUnits[j].mOwner == PlayerID && mUnits[j].mIntern)
				++internCount;
		}

		return internCount;
	}

	/// <summary>
	/// Convert a blueprint into a fully spawned item.
	/// </summary>
	public void PromoteBlueprintToItem(CItem Blueprint)
	{
		mBlueprints.Remove(Blueprint);
		DespawnEntity(Blueprint);
		SpawnItem(Blueprint.mAsset, Blueprint.mPosition, Blueprint.mItemRot, Blueprint.mOwner);
	}

	/// <summary>
	/// Check for nearby units.
	/// </summary>
	public CUnit GetNearbyEnemy(CUnit SourceUnit)
	{
		CUnit closestEnemy = null;
		float closestDist = float.MaxValue;

		for (int i = 0; i < mUnits.Count; ++i)
		{
			CUnit unit = mUnits[i];

			if (!unit.mDead && !IsAllied(unit.mOwner, SourceUnit.mOwner))
			{
				Vector2 dir = unit.mPosition - SourceUnit.mPosition;
				float sqrDist = dir.sqrMagnitude;

				if (sqrDist < closestDist && sqrDist <= SourceUnit.mSqrCombatViewDistance)
				{
					if (mMap.IsPointVisible(SourceUnit.mPosition, unit.mPosition, dir.normalized))
					{
						//CDebug.DrawLine(new Vector3(SourceUnit.mPosition.x, 0.1f, SourceUnit.mPosition.y), new Vector3(unit.mPosition.x, 0.1f, unit.mPosition.y), Color.red, false);
						closestDist = sqrDist;
						closestEnemy = unit;
					}
				}
			}
		}

		return closestEnemy;
	}

	/// <summary>
	/// Tell units to collect salary if it's payday.
	/// </summary>
	private void _UpdateSalaries()
	{
		if (mGameTick >= _nextSalaryTime)
		{
			if (_nextSalaryTime == 0)
			{
				_nextSalaryTime = mGameTick + TICKS_PER_SECOND * SALARAY_INTERVAL_SECONDS;
				return;
			}

			_nextSalaryTime = mGameTick + TICKS_PER_SECOND * SALARAY_INTERVAL_SECONDS;
			Debug.Log("It is payday!");
			CTransientEvent paydayEvent = new CTransientEvent(mGameTick, 255);
			paydayEvent.mType = CTransientEvent.EType.PAYDAY;
			mTransientEvents.Add(paydayEvent);
			
			for (int i = 0; i < mUnits.Count; ++i)
				mUnits[i].CollectSalary();
		}
	}

	/// <summary>
	/// Get payday timer in the form 0.0 - 1.0.
	/// </summary>
	public float GetPaydayTimeNormalize()
	{
		int timeLeft =_nextSalaryTime - mGameTick;
		float norm = (float)timeLeft / (float)(TICKS_PER_SECOND * SALARAY_INTERVAL_SECONDS);

		return norm;
	}

	/// <summary>
	/// Add a contract that will be available X seconds from now.
	/// </summary>
	public void AddContractTurn(int SecondsFromNow, int ContractCount)
	{
		mContractTurns.Add(new SContractTurn(mGameTick + SecondsFromNow * TICKS_PER_SECOND, ContractCount));
	}

	/// <summary>
	/// Add a resume that will be available X seconds from now.
	/// </summary>
	public void AddResumeturn(int SecondsFromNow)
	{
		mResumeTurns.Add(new SResumeTurn(mGameTick + SecondsFromNow * TICKS_PER_SECOND));
	}

	/// <summary>
	/// Update contract turn queue.
	/// </summary>
	private void _UpdateContractSpawning()
	{
		for (int i = 0; i < mContractTurns.Count; ++i)
		{
			if (mGameTick >= mContractTurns[i].mTime)
			{
				int contractCount = mContractTurns[i].mNumContracts;
				mContractTurns.RemoveAt(i--);

				for (int p = 0; p < MAX_PLAYERS; ++p)
				{
					if (mPlayers[p].mCompeting)
					{
						SpawnContract(p, contractCount);
					}
				}
			}
		}
	}

	/// <summary>
	/// Update resume turn queue.
	/// </summary>
	private void _UpdateResumeSpawning()
	{
		for (int i = 0; i < mResumeTurns.Count; ++i)
		{
			if (mGameTick >= mResumeTurns[i].mTime)
			{
				mResumeTurns.RemoveAt(i--);
				SpawnResume();
			}
		}
	}

	public bool IsAllied(int PlayerA, int PlayerB)
	{
		return ((mPlayers[PlayerA].mAllies & (1 << PlayerB)) != 0) && ((mPlayers[PlayerB].mAllies & (1 << PlayerA)) != 0);
	}

	/// <summary>
	/// Alert the player with a message.
	/// </summary>
	public void PushMessage(string Text, int Type)
	{
		// TODO: This entire function ^_^
		// We don't know who the user is (Who should ultimately see the message).
		// We will need to queue all messages, then let the CUserWorldView take the list and display as
		// needed. Just like effects/sounds work.
		// Is this a chance to have message responses?
		Debug.Log("Sim Message(" + Type + "): " + Text);
	}

	/// <summary>
	/// The heart of player interaction with the simulation.
	/// Executes all actions in the specified turn.
	/// </summary>
	public void ExecuteTurnActions(CActionTurn Turn)
	{
		for (int i = 0; i < Turn.mActionBuffer.Count; ++i)
		{
			CUserAction action = Turn.mActionBuffer[i];

			// Some debug asserts
			if (action.mTurn != Turn.mTurn || action.mPlayerID != Turn.mPlayerID)
				Debug.LogError("Action doesn't match Turn info! " + action.mTurn + "/" + Turn.mTurn + " " + action.mPlayerID + "/" + Turn.mPlayerID);

			// TODO: Only execute actions that are allowed by the player interact flags.
			// This will be on a per action basis.

			switch (action.mID)
			{
				case CUserAction.EType.PLACE_OBJECT:
				{
					int x = action.mX & 0xFFFF;
					int y = action.mY;
					int rotation = action.mX >> 16;

					ControllerPlaceBlueprint(action.mStringInfo, x, y, rotation, action.mPlayerID);
					break;
				}

				case CUserAction.EType.MOVE_UNIT:
				{
					//* 0.5f + 0.25f
					float x = action.mX / 1000.0f;
					float y = action.mY / 1000.0f;

					ControllerMoveUnit(action.mPlayerID, new Vector2(x, y));
					break;
				}

				case CUserAction.EType.SELECT_ENTITY:
				{
					ControllerSelectEntity(action.mPlayerID, action.mInfo);
					break;
				}
			
				case CUserAction.EType.ACCEPT_RESUME:
				{
					ControllerAcceptNotify(action.mInfo, action.mPlayerID);
					break;
				}

				case CUserAction.EType.ACCEPT_CONTRACT:
				{
					ControllerAcceptNotify(action.mInfo, action.mPlayerID);
					break;
				}

				case CUserAction.EType.DISTRIBUTE_CONTRACT:
				{
					int deskProxyID = action.mX;
					int contractID = action.mInfo;

					ControllerDistributeContract(action.mPlayerID, contractID, deskProxyID);
					break;
				}
			
				case CUserAction.EType.ASSIGN_WORKSPACE:
				{
					int itemProxyID = action.mInfo;

					ControllerClaimDesk(action.mPlayerID, itemProxyID);
					break;
				}

				case CUserAction.EType.FORCE_ATTACK:
				{
					int itemProxyID = action.mInfo;

					ControllerForceAttack(action.mPlayerID, itemProxyID);
					break;
				}
			
				case CUserAction.EType.ENSLAVE_INTERN:
				{
					ControllerHireIntern(action.mPlayerID);
					break;
				}
			
				case CUserAction.EType.RETURN_CONTRACT_PAPERS:
				{
					int contractID = action.mInfo;

					ControllerReturnContractPapers(action.mPlayerID, contractID);
					break;
				}
				
				case CUserAction.EType.FIRE_EMPLOYEE:
				{
					int unitID = action.mInfo;

					ControllerFireEmployee(action.mPlayerID, unitID);
					break;
				}

				case CUserAction.EType.BONUS:
				{
					int unitID = action.mInfo;
					ControllerBonusEmployee(action.mPlayerID, unitID);
					break;
				}

				case CUserAction.EType.RAISE:
				{
					int unitID = action.mInfo;
					ControllerRaiseEmployee(action.mPlayerID, unitID);
					break;
				}

				case CUserAction.EType.PROMOTE:
				{
					int unitID = action.mInfo;
					ControllerPromoteEmployee(action.mPlayerID, unitID);
					break;
				}

				case CUserAction.EType.TAG_ITEM:
				{
					// TODO: ??
					break;
				}

				case CUserAction.EType.LOCK_ITEM:
				{
					int doorProxyID = action.mInfo;
					bool locked = (action.mX == 1);
					
					ControllerLockDoor(action.mPlayerID, doorProxyID, locked);
					break;
				}

				case CUserAction.EType.CANCEL_BLUEPRINT:
				{
					ControllerCancelBlueprint(action.mPlayerID, action.mInfo);
					break;
				}
			
				default:
				{
					Debug.LogError("Attempted to execute unknown action.");
					break;
				}
			}			
		}
	}

	//----------------------------------------------------------------------------
	// TODO: Since all controller code requires a playerID, it would suggest that
	// these function belong to the player class directly.
	//----------------------------------------------------------------------------

	/// <summary>
	/// Accept a resume or contract.
	/// </summary>
	public void ControllerAcceptNotify(int ID, int PlayerID)
	{
		CEntity ent = GetEntity<CEntity>(ID);

		if (ent != null && ent.mType == CEntity.EType.RESUME)
		{
			if (GetPlayerWorkerCount(PlayerID) < mPopCap)
			{
				CResume resume = ent as CResume;
				resume.Consume();

				CItemStart spawn = GetEntity<CItemStart>(mPlayers[PlayerID].mSpawnItemID);

				if (spawn != null)
				{
					spawn.QueueEvent(new CElevatorEvent(CElevatorEvent.EType.T_SPAWN_UNIT, PlayerID, resume.mID));
				}
			}
		}
		else if (ent != null && ent.mType == CEntity.EType.CONTRACT)
		{
			CContract contract = ent as CContract;
			
			if (!contract.mAccepted)
			{
				contract.Accept(PlayerID);
			}
			else
			{
				Debug.LogError("Contract has been accepted already");
			}
		}
		else
		{
			Debug.LogError("Unknown entity type for accepted notify");
		}
	}
	
	/// <summary>
	/// Place a blueprint.
	/// </summary>
	public void ControllerPlaceBlueprint(string AssetName, int X, int Y, int Rotation, int PlayerID)
	{
		CItemAsset asset = CGame.AssetManager.GetAsset<CItemAsset>(AssetName);

		if (asset == null)
			return;

		if (mPlayers[PlayerID].CanAfford(asset.mCost))
		{
			// TODO: Blueprints should still be placeable on items under FOW.
			if (CItem.IsPlaceable(this, PlayerID, asset, X, Y, Rotation))
			{
				if (CGame.VarPlaceItemDirect.mValue)
				{
					SpawnItem(asset, new Vector2(X, Y), Rotation, PlayerID);
				}
				else
				{
					CItem item = SpawnBlueprint(asset, new Vector2(X, Y), Rotation, PlayerID);
					int buildTag = CPickup.GetNextBuildID();

					CItemStart spawn = GetEntity<CItemStart>(mPlayers[PlayerID].mSpawnItemID);
					if (spawn != null)
					{
						spawn.QueueEvent(new CElevatorEvent(CElevatorEvent.EType.T_DELIVERY, PlayerID, 0, buildTag, asset.mName));

						mPlayers[PlayerID].Spend(asset.mCost);

						CBuildOrder order = new CBuildOrder(this);
						order.mPlayerID = PlayerID;
						order.mState = CBuildOrder.EState.WAIT_FOR_DELIVERY;
						order.mBuildTag = buildTag;
						order.mItemBlueprintID = item.mID;
						order.mBuilderID = -1;
						AddBuildOrder(order);
					}
					else
					{
						Debug.LogError("Couldn't find spawn point for build order");
					}
				}
			}
			else
			{
				Debug.Log("Item Placement Denied");
			}
		}
	}

	/// <summary>
	/// Cancel a blueprint.
	/// </summary>
	public void ControllerCancelBlueprint(int PlayerID, int BlueprintID)
	{
		for (int i = 0; i < mBuildOrders.Count; ++i)
		{
			if (mBuildOrders[i].mItemBlueprintID == BlueprintID)
			{
				mBuildOrders[i].OnCancelled();
				break;
			}
		}

		CItem blueprint = GetEntity<CItem>(BlueprintID);

		if (blueprint != null && blueprint.mOwner == PlayerID)
		{
			mBlueprints.Remove(blueprint);
			DespawnEntity(blueprint);
		}
	}

	public void ControllerLockDoor(int PlayerID, int DoorProxyID, bool Locked)
	{
		CItemProxy doorProxy = GetItemPorxy(PlayerID, DoorProxyID);

		if (doorProxy != null && doorProxy.mOwnerID == PlayerID)
		{
			doorProxy.SetDoorLock(Locked);
		}
	}

	/// <summary>
	/// Move the selected unit to position.
	/// </summary>
	public void ControllerMoveUnit(int PlayerID, Vector2 Position)
	{
		CUnit selectedUnit = GetEntity<CUnit>(mPlayers[PlayerID].mSelectedUnitId);

		if (selectedUnit != null && selectedUnit.mOwner == PlayerID)
		{
			selectedUnit.CommandMoveTo(Position);
		}
	}

	/// <summary>
	/// Select an entity.
	/// </summary>
	public void ControllerSelectEntity(int PlayerID, int EntityID)
	{
		// TODO: At the moment We only select units in the simulation.
		CUnit unit = GetEntity<CUnit>(EntityID);

		if (unit != null)
			mPlayers[PlayerID].mSelectedUnitId = unit.mID;
		else
			mPlayers[PlayerID].mSelectedUnitId = -1;
	}

	/// <summary>
	/// Distribute 1 unit of a contract to a desk.
	/// </summary>
	public void ControllerDistributeContract(int PlayerID, int ContractID, int DeskProxyID)
	{
		CContract contract = GetEntity<CContract>(ContractID);
		CItemProxy deskProxy = GetItemPorxy(PlayerID, DeskProxyID);

		if (contract != null && deskProxy != null)
		{
			int freeSpace = deskProxy.GetFreePaperStackSlots();

			if (freeSpace < 1)
			{
				Debug.LogError("More units distributed to desk than it has free space.");
			}
			else
			{
				CContractStack paper = contract.DistributeStack();

				if (paper == null)
					Debug.LogError("No papers to distribute!");
				else
					deskProxy.AssignContractUnits(paper);
			}
		}
		else
		{
			Debug.LogError("Contract distribution: Either desk or contract does not exist. Desk(" + DeskProxyID + ") Contract(" + contract + ")");
		}
	}

	/// <summary>
	/// Tell the selected unit to claim a desk.
	/// </summary>
	public void ControllerClaimDesk(int PlayerID, int DeskProxyID)
	{
		CUnit selectedUnit = GetEntity<CUnit>(mPlayers[PlayerID].mSelectedUnitId);
		if (selectedUnit != null && selectedUnit.mOwner == PlayerID)
		{
			CItemProxy deskProxy = GetItemPorxy(PlayerID, DeskProxyID);

			if (deskProxy != null)
				selectedUnit.CommandClaimDesk(DeskProxyID);
			else
				Debug.Log("Tried to assign a desk that doesn't exist.");
		}
	}

	/// <summary>
	/// Tell the selected unit to attack an item.
	/// </summary>
	public void ControllerForceAttack(int PlayerID, int ItemProxyID)
	{
		CUnit selectedUnit = GetEntity<CUnit>(mPlayers[PlayerID].mSelectedUnitId);

		if (selectedUnit == null || selectedUnit.mOwner != PlayerID)
			return;

		CItemProxy itemProxy = GetItemPorxy(PlayerID, ItemProxyID);

		if (itemProxy == null)
			return;

		selectedUnit.CommandForceAttackItem(itemProxy.mID);
	}

	/// <summary>
	/// Hire the next available intern.
	/// </summary>
	public void ControllerHireIntern(int PlayerID)
	{
		int internCount = GetPlayerInternCount(PlayerID);

		if (internCount >= mInternCap)
			return;

		if (!mPlayers[PlayerID].CanAfford(internCount * 100))
			return;

		CItemStart spawn = GetEntity<CItemStart>(mPlayers[PlayerID].mSpawnItemID);

		if (spawn == null)
			return;

		spawn.QueueEvent(new CElevatorEvent(CElevatorEvent.EType.T_SPAWN_UNIT, PlayerID, 0));
		mPlayers[PlayerID].Spend(internCount * 100);
	}

	/// <summary>
	/// Return all distributed papers of a contract.
	/// </summary>
	public void ControllerReturnContractPapers(int PlayerID, int ContractID)
	{
		CContract contract = GetEntity<CContract>(ContractID);

		if (contract != null)
			RemoveContractFromDesks(contract);
		else
			Debug.LogError("Can't return contract papers!");
	}

	/// <summary>
	/// Fire an employee.
	/// </summary>
	public void ControllerFireEmployee(int PlayerID, int UnitID)
	{
		CUnit unit = GetEntity<CUnit>(UnitID);

		if (unit == null || unit.mOwner != PlayerID)
			return;

		unit.Fire();
	}

	/// <summary>
	/// Gives bonus to employee.
	/// </summary>
	public void ControllerBonusEmployee(int PlayerID, int UnitID)
	{
		CUnit unit = GetEntity<CUnit>(UnitID);

		if (unit == null || unit.mOwner != PlayerID)
			return;

		unit.GiveBonus();
	}

	/// <summary>
	/// Gives raise to employee.
	/// </summary>
	public void ControllerRaiseEmployee(int PlayerID, int UnitID)
	{
		CUnit unit = GetEntity<CUnit>(UnitID);

		if (unit == null || unit.mOwner != PlayerID)
			return;

		unit.GiveRaise();
	}

	/// <summary>
	/// Promotes an employee.
	/// </summary>
	public void ControllerPromoteEmployee(int PlayerID, int UnitID)
	{
		CUnit unit = GetEntity<CUnit>(UnitID);

		if (unit == null || unit.mOwner != PlayerID)
			return;

		unit.Promote();
	}
}
