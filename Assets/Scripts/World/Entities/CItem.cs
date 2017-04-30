using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeable object in the world.
/// </summary>
public abstract class CItem : CEntity
{
	public class CEntryPoint
	{
		public Vector2 mPosition;
		public int mRotation;
	}

	public class CUsageSlot
	{
		public int mSlotID;
		public bool mOccupied;
		public CItem mItem;
		public Vector2 mPosition;
		public Quaternion mRotation;
		public List<CEntryPoint> mEntryPoints;

		public CUsageSlot(CItem Item, int ID, Vector3 Position, int Rotation)
		{
			mSlotID = ID;
			mOccupied = false;
			mItem = Item;
			mPosition = Position;
			mRotation = CUtility.FacingTable[Rotation];
			mEntryPoints = new List<CEntryPoint>();
		}
	}

	public struct SAxis
	{
		public Vector2 mX;
		public Vector2 mY;

		public SAxis(Vector2 X, Vector2 Y)
		{
			mX = X;
			mY = Y;
		}

		public Vector2 Transform(Vector2 LocalPoint)
		{
			return mX * LocalPoint.x + mY * LocalPoint.y;
		}
	}

	public static SAxis[] RotationAxisTable = 
	{
		new SAxis(new Vector2(1.0f, 0.0f), new Vector2(0.0f, 1.0f)),
		new SAxis(new Vector2(0.0f, 1.0f), new Vector2(-1.0f, 0.0f)),
		new SAxis(new Vector2(-1.0f, 0.0f), new Vector2(0.0f, -1.0f)),
		new SAxis(new Vector2(0.0f, -1.0f), new Vector2(1.0f, 0.0f))
	};

	public static Vector2[] PivotRelativeToTile = 
	{
		new Vector2(0.0f, 0.0f),
		new Vector2(1.0f, 0.0f),
		new Vector2(1.0f, 1.0f),
		new Vector2(0.0f, 1.0f)
	};

	public static Quaternion[] RotationTable =
	{
		Quaternion.identity,
		Quaternion.Euler(0, 270, 0),
		Quaternion.Euler(0, 180, 0),
		Quaternion.Euler(0, 90, 0)
	};

	public CItemAsset mAsset;

	private CItemProxy[] _playerItemViews;
	private int _playerVisibility;
	
	public bool mBluerprint = false;
	public bool mPlaceable = true;

	public Vector2 mPosition;
	public int mX;
	public int mY;
	public int mWidth;	
	public int mLength;
	public Bounds mBounds;
	public int mItemRot;
	
	public int mOffsetX;
	public int mOffsetY;

	public int mCost;

	public float mDurability;
	public float mMaxDurability;

	private bool _placedInWorld;

	public CEntryPoint[] _usageSlotDefinition;

	public int mUserUnitID;

	//public int mBoundsCount;
	//public Bounds[] _hitBounds;
	
	private int _placeable = -1;
	
	protected CUsageSlot[] _usageSlots;

	public static bool IsPlaceable(CTile[,] MapTiles, CCollisionTile[,] CollisionTiles, CItemAsset Asset, int X, int Y, int Rotation)
	{
		// Do a world space run of the asset to check if tiles are unoccupied.
		Vector2 p1 = CItem.PivotRelativeToTile[Rotation];
		Vector2 size = CItem.RotationAxisTable[Rotation].Transform(new Vector2(Asset.mWidth, Asset.mLength));
		Vector2 p2 = p1 + size;
		int startX = ((int)Mathf.Min(p1.x, p2.x) + X) * 2;
		int startY = ((int)Mathf.Min(p1.y, p2.y) + Y) * 2;
		int width = (int)Mathf.Abs(size.x )* 2;
		int length = (int)Mathf.Abs(size.y) * 2;

		for (int iX = 0; iX < width; ++iX)
		{
			for (int iY = 0; iY < length; ++iY)
			{
				int worldX = iX + startX;
				int worldY = iY + startY;

				if (CollisionTiles[worldX, worldY].IsOccupied)
					return false;

				if (iY > 0 && CollisionTiles[worldX, worldY].mWallXSolid)
					return false;

				if (iX > 0 && CollisionTiles[worldX, worldY].mWallZSolid)
					return false;
			}
		}

		// Doors must be placed within frames.
		if (Asset.mItemType == EItemType.DOOR)
		{
			// Get core tile
			if (Rotation == 0 && MapTiles[X, Y + 1].mWallX.mType >= 100) return true;
			else if (Rotation == 1 && MapTiles[X, Y].mWallZ.mType >= 100) return true;
			else if (Rotation == 2 && MapTiles[X, Y].mWallX.mType >= 100) return true;
			else if (Rotation == 3 && MapTiles[X + 1, Y].mWallZ.mType >= 100) return true;

			return false;
		}

		return true;
	}

	public static bool IsPlaceable(CUserWorldView WorldView, CItemAsset Asset, int X, int Y, int Rotation)
	{
		// Check for collision with other blueprints
		Bounds bounds = CItem.CalculateBounds(new Vector2(X, Y), Rotation, Asset.mWidth, Asset.mLength);
		bounds.max -= new Vector3(0.1f, 0.1f, 0.1f);
		bounds.min += new Vector3(0.1f, 0.1f, 0.1f);

		for (int i = 0; i < WorldView.mStateViews.Count; ++i)
		{
			CItemView v = WorldView.mStateViews[i] as CItemView;
			if (v != null && v.mBlueprint)
			{
				if (v.mBounds.Intersects(bounds))
					return false;
			}
		}

		return IsPlaceable(WorldView.GetTileView(), WorldView.GetCollisionView(), Asset, X, Y, Rotation);
	}

	public static bool IsPlaceable(CWorld World, int PlayerID, CItemAsset Asset, int X, int Y, int Rotation)
	{
		// Check for collision with other blueprints
		Bounds bounds = CItem.CalculateBounds(new Vector2(X, Y), Rotation, Asset.mWidth, Asset.mLength);
		bounds.max -= new Vector3(0.1f, 0.1f, 0.1f);
		bounds.min += new Vector3(0.1f, 0.1f, 0.1f);

		for (int i = 0; i < World.mBlueprints.Count; ++i)
		{
			CItem b = World.mBlueprints[i];
			if (b.mOwner == PlayerID && b.mBluerprint)
			{
				if (b.mBounds.Intersects(bounds))
					return false;
			}
		}

		return IsPlaceable(World.mMap.mTiles, World.mMap.mGlobalCollisionTiles, Asset, X, Y, Rotation);
	}

	public override bool IsItem()
	{
		return true;
	}

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM;
		mItemRot = 0;

		_playerItemViews = new CItemProxy[CWorld.MAX_PLAYERS];
		_playerVisibility = 0;
		_placedInWorld = false;
		mUserUnitID = -1;
	}

	/// <summary>
	/// Test if a player can see this item.
	/// </summary>
	public bool GetVisibility(int PlayerID)
	{
		for (int x = 0; x < mWidth; ++x)
		{
			for (int y = 0; y < mLength; ++y)
			{
				Vector2 worldPos = ItemToWorldSpacePosition(new Vector2(x + 0.5f, y + 0.5f));
				
				if (mWorld.mMap.IsTileVisible(PlayerID, (int)worldPos.x, (int)worldPos.y))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Get flags for each player's visibility of this item.
	/// </summary>
	public int GetPlayerVisibility()
	{
		int visFlag = 0;

		for (int x = 0; x < mWidth; ++x)
		{
			for (int y = 0; y < mLength; ++y)
			{
				Vector2 worldPos = ItemToWorldSpacePosition(new Vector2(x + 0.5f, y + 0.5f));

				for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
				{
					if ((visFlag & (1 << i)) != 0)
						continue;

					if (mWorld.mMap.IsTileVisible(i, (int)worldPos.x, (int)worldPos.y))
					{
						visFlag |= (1 << i);
					}
				}
			}
		}

		return visFlag;
	}

	public void UpdateVisibility()
	{
		int visFlag = GetPlayerVisibility();

		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
		{
			if ((visFlag & (1 << i)) != (_playerVisibility & (1 << i)))
			{
				if ((visFlag & (1 << i)) != 0)
				{
					if (_playerItemViews[i] != null)
					{
						_playerItemViews[i].SetVisible();
					}
					else
					{
						CItemProxy itemView = new CItemProxy();
						mWorld.AddItemProxy(i, itemView);
						itemView.Init(i, this, mWorld, false);
						_playerItemViews[i] = itemView;
					}
				}
				else
				{
					if (_playerItemViews[i] != null)
					{
						_playerItemViews[i].SetHidden();
					}
				}
			}
			else
			{
				if ((visFlag & (1 << i)) != 0)
				{
					_playerItemViews[i].UpdateState(this);
				}
			}
		}

		_playerVisibility = visFlag;
	}

	public void PlaceAsBlueprint()
	{
		mBluerprint = true;
		mBounds = CalculateBounds(mPosition, mItemRot, mWidth, mLength);
		// TODO: move item orders into own entity type
		CItemProxy itemView = new CItemProxy();
		itemView.Init(mOwner, this, mWorld, true);
		_playerItemViews[mOwner] = itemView;
		mWorld.AddItemProxy(mOwner, itemView);
	}

	/// <summary>
	/// Check if we could be placed in world at this location.
	/// </summary>
	public void UpdatePlaceability()
	{
		mPlaceable = false;

		Vector2 p1 = CItem.PivotRelativeToTile[mItemRot];
		Vector2 size = CItem.RotationAxisTable[mItemRot].Transform(new Vector2(mAsset.mWidth, mAsset.mLength));
		Vector2 p2 = p1 + size;

		int startX = ((int)Mathf.Min(p1.x, p2.x) + mX) * 2;
		int startY = ((int)Mathf.Min(p1.y, p2.y) + mY) * 2;

		int width = (int)Mathf.Abs(size.x) * 2;
		int length = (int)Mathf.Abs(size.y) * 2;

		for (int iX = 0; iX < width; ++iX)
		{
			for (int iY = 0; iY < length; ++iY)
			{
				int worldX = iX + startX;
				int worldY = iY + startY;

				if (mWorld.mMap.mGlobalCollisionTiles[worldX, worldY].IsOccupied)
					return;

				// Check we can reach our previous neighbour
				if (iY > 0 && mWorld.mMap.mGlobalCollisionTiles[worldX, worldY].mWallXSolid)
					return;

				if (iX > 0 && mWorld.mMap.mGlobalCollisionTiles[worldX, worldY].mWallZSolid)
					return;
			}
		}

		mPlaceable = true;
	}

	public override void SimTick()
	{
		base.SimTick();

		if (mBluerprint)
			UpdatePlaceability();
	}

	public override void Destroy()
	{
		base.Destroy();

		if (_placedInWorld)
			RemoveFromWorld();

		for (int p = 0; p < CWorld.MAX_PLAYERS; ++p)
		{
			if (_playerItemViews[p] != null)
			{
				if (mBluerprint || ((_playerVisibility & (1 << p)) != 0))
				{
					mWorld.RemoveItemProxy(p, _playerItemViews[p]);
					_playerItemViews[p].Destroy();
				}
				else
				{
					_playerItemViews[p].UnlinkItem();
				}
			}
		}
	}

	public virtual void InitItem(CAsset Asset)
	{
		mAsset = (CItemAsset)Asset;

		mWidth = mAsset.mWidth;
		mLength = mAsset.mLength;
		mOffsetX = 0;
		mOffsetY = 0;
		mDurability = mAsset.mDurability;
		mMaxDurability = mDurability;
		mCost = mAsset.mCost;

		/*
		_usageSlotDefinition = new CEntryPoint[mDefinition.mSlots];
		for (int i = 0; i < mDefinition.mSlots; ++i)
		{
			_usageSlotDefinition[i] = new CEntryPoint();
			_usageSlotDefinition[i].mPosition = new Vector2(mDefinition.mSlotTransform[i].x, mDefinition.mSlotTransform[i].y);
			_usageSlotDefinition[i].mRotation = (int)mDefinition.mSlotTransform[i].z;
		}
		*/
	}

	public override void DrawDebugPrims()
	{
		CDebug.DrawLine(new Vector3(mPosition.x, 0.0f, mPosition.y), new Vector3(mPosition.x, 3.0f, mPosition.y));
		CDebug.DrawBounds(mBounds, Color.blue);

		CDebug.DrawYSquare(new Vector3(mPosition.x + 0.5f, 0.0f, mPosition.y + 0.5f), 0.25f, Color.white);

		Rect trigger = new Rect(mBounds.min.x - 0.5f, mBounds.min.z - 0.5f, mBounds.size.x + 1.0f, mBounds.size.z + 1.0f);
		CDebug.DrawYRect(trigger, Color.red, false);

		/*
		if (_usageSlots != null)
		{
			for (int i = 0; i < _usageSlots.Length; ++i)
			{
				Vector3 org = new Vector3(_usageSlots[i].mPosition.x, 0.0f, _usageSlots[i].mPosition.y);

				CDebug.DrawYSquare(org, 0.5f, Color.magenta);

				for (int j = 0; j < _usageSlots[i].mEntryPoints.Count; ++j)
				{
					Vector2 pos = _usageSlots[i].mEntryPoints[j].mPosition;
					CDebug.DrawYSquare(new Vector3(pos.x, 0.0f, pos.y), 0.4f, Color.green);
					CDebug.DrawLine(new Vector3(pos.x, 0.0f, pos.y), org, Color.cyan);
				}
			}
		}
		*/
	}
	
	public Vector2 ItemToWorldSpacePosition(Vector2 ItemSpacePos)
	{
		return RotationAxisTable[mItemRot].Transform(ItemSpacePos) + PivotRelativeToTile[mItemRot] + new Vector2(mX, mY);
	}

	public float ItemToWorldSpaceRotation(float DegreesOnY)
	{
		return DegreesOnY - (mItemRot * 90.0f);
	}

	public static Bounds CalculateBounds(Vector2 Position, int Rotation, int Width, int Length)
	{
		int mDepth = 2;

		Vector2 p1 = CItem.PivotRelativeToTile[Rotation];
		Vector2 p2 = p1 + CItem.RotationAxisTable[Rotation].Transform(new Vector2(Width, Length));
		Vector2 center = (p2 - p1) * 0.5f + p1 + Position;
		float sizeX = Mathf.Abs(p2.x - p1.x);
		float sizeY = Mathf.Abs(p2.y - p1.y);
		Vector2 boundsMin = Vector2.zero;
		
		boundsMin += Position;
		return new Bounds(new Vector3(center.x, mDepth * 0.5f, center.y), new Vector3(sizeX, mDepth, sizeY));
	}

	public virtual void PlaceInWorld()
	{
		mBluerprint = false;
		_placedInWorld = true;
		mBounds = CalculateBounds(mPosition, mItemRot, mWidth, mLength);
		int colWidth = mWidth * 2;
		int colLength = mLength * 2;

		for (int iX = 0; iX < colWidth; ++iX)
		{
			for (int iY = 0; iY < colLength; ++iY)
			{
				IntVec2 worldTile = GetWorldCollisionTile(iX, iY, mPosition, mItemRot);
				CCollisionTile tile = mWorld.mMap.mGlobalCollisionTiles[worldTile.X, worldTile.Y];

				tile.mOccupied = -1;
				tile.mSolid = mAsset.mTiles[iX, iY].mSolid;
			}
		}

		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mLength; ++iY)
			{
				Vector2 worldPoint = RotationAxisTable[mItemRot].Transform(new Vector2(iX, iY));
				int worldX = (int)worldPoint.x + mX;
				int worldY = (int)worldPoint.y + mY;
				CTile worldTile = mWorld.mMap.mTiles[worldX, worldY];

				worldTile.mItem = mID;
			}
		}

		mWorld.RebuildInfluenceMap();
	}
	
	public void RemoveFromWorld()
	{
		_placedInWorld = false;
		int colWidth = mWidth * 2;
		int colLength = mLength * 2;

		for (int iX = 0; iX < colWidth; ++iX)
		{
			for (int iY = 0; iY < colLength; ++iY)
			{
				IntVec2 worldTile = GetWorldCollisionTile(iX, iY, mPosition, mItemRot);
				CCollisionTile tile = mWorld.mMap.mGlobalCollisionTiles[worldTile.X, worldTile.Y];

				tile.mOccupied = 0;
				tile.mSolid = false;
			}
		}

		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mLength; ++iY)
			{
				Vector2 worldPoint = RotationAxisTable[mItemRot].Transform(new Vector2(iX, iY));
				int worldX = (int)worldPoint.x + mX;
				int worldY = (int)worldPoint.y + mY;
				CTile worldTile = mWorld.mMap.mTiles[worldX, worldY];

				worldTile.mItem = -1;
			}
		}

		mWorld.RebuildInfluenceMap();
	}

	/// <summary>
	/// Book a usage on this item.
	/// It is up to the user to tell the slot when he is done.
	/// </summary>	
	public void BookUsageSlot(int SlotID)
	{
		if (SlotID == -1)
		{
			Debug.LogError("Tried to book no slot!");
			return;
		}

		if (_usageSlots[SlotID].mOccupied)
		{
			Debug.LogError("Tried to book occupied slot!");
			return;
		}

		_usageSlots[SlotID].mOccupied = true;
	}

	public CUsageSlot GetFreeUsageSlot()
	{
		for (int i = 0; i < _usageSlots.Length; ++i)
			if (!_usageSlots[i].mOccupied)
				return _usageSlots[i];

		return null;
	}

	/// <summary>
	/// Return the slot back to the item.
	/// </summary>
	public void FreeUsageSlot(int ID)
	{
		if (!_usageSlots[ID].mOccupied)
			Debug.LogError("Usage Slot wasn't Occupied!");
		else
			_usageSlots[ID].mOccupied = false;
	}

	/// <summary>
	/// Get a slot by ID.
	/// </summary>
	public CUsageSlot GetUsageSlot(int Index)
	{
		return _usageSlots[Index];
	}
	
	public void SetPosition(float X, float Y)
	{
		mPosition = new Vector2((int)X, (int)Y);
		mX = (int)X;
		mY = (int)Y;
		//_Gob.transform.position = new Vector3(mPosition.x + 0.5f, 0, mPosition.y + 0.5f);
		//_Gob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", mWorld.mMap.GetTileColor(mX, mY));
	}

	public void SetRotation(int Rot)
	{
		mItemRot = Rot;
		//_Gob.transform.rotation = CUtility.FacingTable[(mItemRot + 2) % 4];
	}

	public void TakeDamage(float Damage, Vector2 Position)
	{
		// NOTE: Item can't be damaged with max durability of 0.
		if (mMaxDurability == 0)
			return;

		mDurability -= Damage;

		Vector3 pos = mBounds.center;
		pos.y = 0.0f;
		mWorld.AddTransientEvent(_playerVisibility).SetEffect(pos);

		if (mDurability <= 0)
		{
			mWorld.DespawnEntity(this);
		}
	}

	public static IntVec2 GetWorldCollisionTile(int LocalTileX, int LocalTileY, Vector2 RelativeTo, int Rotation)
	{
		Vector2 worldPoint = RotationAxisTable[Rotation].Transform(new Vector2(LocalTileX, LocalTileY)) + PivotRelativeToTile[Rotation];
		int worldX = (int)(worldPoint.x + RelativeTo.x * 2);
		int worldY = (int)(worldPoint.y + RelativeTo.y * 2);

		return new IntVec2(worldX, worldY);
	}
}