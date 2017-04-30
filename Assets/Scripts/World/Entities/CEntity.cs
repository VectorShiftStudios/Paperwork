using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for things that can be serialized, ID'd and factoried.
/// Originaly used as a concept for things that existed in the physical world space of the simulation,
/// but has grown to represent abstract concepts so that they can use all the systems.
/// </summary>
public abstract class CEntity
{
	public enum EType
	{
		NONE,
		UNIT,
		ITEM,
		ITEM_START,
		ITEM_DECO,
		ITEM_DESK,
		ITEM_SAFE,
		ITEM_REST,
		ITEM_FOOD,
		ITEM_DOOR,
		RESUME,
		CONTRACT,
		PICKUP,
		MISSILE,
		VOLUME,
		DECAL
	}

	private static int _NextID = 1;

	/// <summary>
	/// Get the current next auto ID without incrementing it.
	/// </summary>
	public static int GetCurrentID()
	{
		return _NextID;
	}

	/// <summary>
	/// Set the next auto ID to be used when creating an entity.
	/// </summary>
	public static void SetCurrentID(int ID)
	{
		_NextID = ID;
	}

	/// <summary>
	/// Get next auto ID and increment it.
	/// </summary>	
	public static int GetNextID()
	{
		return _NextID++;
	}

	/// <summary>
	/// Initiallize an entity created by a factory method.
	/// </summary>
	private static void _InitNewEntity(CEntity Entity, CWorld World, int ID)
	{
		if (ID == 0)
			Entity.mID = GetNextID();
		else
			Entity.mID = ID;

		Entity.Init(World);
	}

	/// <summary>
	/// Creates entity and assigns ID.
	/// </summary>
	public static CEntity Create(EType Type, CWorld World, int ID = 0)
	{
		CEntity entity = null;

		switch (Type)
		{
			case EType.UNIT: entity = new CUnit(); break;
			case EType.ITEM_START: entity = new CItemStart(); break;
			case EType.ITEM_DECO: entity = new CItemDeco(); break;
			case EType.ITEM_DESK: entity = new CItemDesk(); break;
			case EType.ITEM_SAFE: entity = new CItemSafe(); break;
			case EType.ITEM_REST: entity = new CItemRest(); break;
			case EType.ITEM_FOOD: entity = new CItemFood(); break;
			case EType.ITEM_DOOR: entity = new CItemDoor(); break;
			case EType.RESUME: entity = new CResume(); break;
			case EType.CONTRACT: entity = new CContract(); break;
			case EType.PICKUP: entity = new CPickup(); break;
			case EType.MISSILE: entity = new CMissile(); break;
			case EType.VOLUME: entity = new CVolume(); break;
			case EType.DECAL: entity = new CDecal(); break;
		}

		if (entity != null)
			_InitNewEntity(entity, World, ID);
		else
			Debug.LogError("Entity Factory couldn't create a " + (int)Type + " is");

		return entity;
	}

	/// <summary>
	/// Create an entity and assign ID.
	/// </summary>
	public static T Create<T>(CWorld World, int ID = 0)
		where T : CEntity, new()
	{
		T entity = new T();
		_InitNewEntity(entity, World, ID);

		return entity;
	}

	/// <summary>
	/// Convert asset item types to entity types.
	/// </summary>
	public static EType AssetItemTypeToEntityType(EItemType ItemType)
	{
		switch (ItemType)
		{
			case EItemType.START: return EType.ITEM_START;
			case EItemType.DECO: return EType.ITEM_DECO;
			case EItemType.SAFE: return EType.ITEM_SAFE;
			case EItemType.DESK: return EType.ITEM_DESK;
			case EItemType.REST: return EType.ITEM_REST;
			case EItemType.FOOD: return EType.ITEM_FOOD;
			case EItemType.DOOR: return EType.ITEM_DOOR;
		}

		return EType.NONE;
	}
	
	public bool mActive;
	public CWorld mWorld;
	public int mID;
	public EType mType;
	public int mOwner;
	
	public virtual bool IsPickup() { return false; }
	public virtual bool IsUnit() { return false; }
	public virtual bool IsItem() { return false; }
	public virtual bool IsDesk() { return false; }
	public virtual bool IsCouch() { return false; }
	public virtual bool IsFood() { return false; }
	public virtual bool IsSafe() { return false; }

	public virtual void Init(CWorld World)
	{
		mType = EType.NONE;
		mOwner = 0;
		mWorld = World;
		mActive = true;
	}

	public virtual void Destroy()
	{
		mActive = false;
	}

	public virtual void SetOwner(int PlayerID)
	{
		mOwner = PlayerID;
	}

	public virtual void SimTick() { }
	
	public virtual void DrawDebugPrims() { }
}