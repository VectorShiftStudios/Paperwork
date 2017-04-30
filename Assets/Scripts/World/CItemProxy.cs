using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Given out to units that are queuing at an item.
/// As soon as the actual item is visible, the queue is processed.
/// If an enemy unit is occupying the item then we drop the whole queue.
/// </summary>
public class CQueueToken
{
	public int mItemProxyID;
	public int mQueueIndex;
	public Vector2 mPosition;
	public Quaternion mRotation;
	public bool mExpired;
	public int mUsageSlot;
}

public class CCompletedContractStack
{
	public float mPapers;

	public CCompletedContractStack(float Papers)
	{
		mPapers = Papers;
	}
}

/// <summary>
/// Used to represent a single player's view of an item entity.
/// </summary>
public class CItemProxy
{
	private static int _NextID = 1;

	public static int GetCurrentID()
	{
		return _NextID;
	}

	public static void SetCurrentID(int ID)
	{
		_NextID = ID;
	}

	public static int GetNextID()
	{
		return _NextID++;
	}

	public enum EState
	{
		S_VISIBLE,
		S_HIDDEN,
		S_DESTROYED
	}
		
	public EState mState;
	public int mPlayerID;	
	public int mID;
	public int mItemID;

	private CItem _item;
	public CWorld mWorld;

	// Item State
	public CItemAsset mAsset;
	public Bounds mBounds;
	public Vector2 mPosition;
	public int mRotation;
	public Color mSurfaceColor;
	public bool mBlueprint;
	public bool mTaggedUsable;
	public int mOwnerID;
	public float mDurability;
	public float mMaxDurability;
	public float mDoorPosition;
	public bool mLocked;
	public int mUserID;
	public int mUserOwner;
	public int mValue;
		
	public List<CQueueToken> mQueueList;

	// Desk
	public List<CContractStack> mAssignedPaperStacks;
	public int mAssignedUnitID;
	public int mCompletedPaperStacks;
	public int mMaxCompletedPapers;
	public int mPaperStackUpdateTick;
	public int mMaxPaperStackSlots;

	// Visual Hook
	public CItemView mVisuals = null;

	public void Init(int PlayerID, CItem Item, CWorld World, bool Blueprint)
	{
		if (PlayerID == -1)
			Debug.LogError("CItemProxy should never have a playerID of -1!");

		mID = GetNextID();
		mItemID = Item.mID;
		mState = EState.S_VISIBLE;
		mPlayerID = PlayerID;
		mWorld = World;
		_item = Item;
		mBlueprint = Blueprint;
		mAsset = Item.mAsset;
		mDurability = Item.mDurability;
		mMaxDurability = Item.mMaxDurability;
		mQueueList = new List<CQueueToken>();
		mPosition = Item.mPosition;
		mRotation = Item.mItemRot;
		mBounds = Item.mBounds;
		mOwnerID = Item.mOwner;
		mAssignedPaperStacks = new List<CContractStack>();
		mPaperStackUpdateTick = 0;
		mAssignedUnitID = -1;

		mSurfaceColor = mWorld.mMap.mBackgroundColor;
		mBounds = CItem.CalculateBounds(mPosition, mRotation, mAsset.mWidth, mAsset.mLength);

		if (!mBlueprint)
		{
			ModifyLocalCollisionMap(true);
			
			if (Item.mType == CEntity.EType.ITEM_DOOR)
			{
				mLocked = true;

				if (mWorld.IsAllied(Item.mOwner, PlayerID))
					mLocked = ((CItemDoor)Item).mLocked;

				DoorModifyLocalCollisionMap(mLocked);
			}
			else if (Item.mType == CEntity.EType.ITEM_SAFE)
			{
				mValue = ((CItemSafe)Item).mValue;
			}
			else if (Item.mType == CEntity.EType.ITEM_DESK)
			{
				mMaxPaperStackSlots = ((CItemDesk)Item).mMaxPaperStackSlots;
			}
		}

		SetVisible();
		UpdateState(Item);
	}

	public bool IsActive()
	{
		return mState != EState.S_DESTROYED;
	}

	public Rect GetWorldCollisionRect()
	{
		//return new Rect(mBounds.min.x, mBounds.min.z, mBounds.size.x, mBounds.size.z);
		return new Rect(mBounds.min.x * 2.0f, mBounds.min.z * 2.0f, mBounds.size.x * 2.0f, mBounds.size.z * 2.0f);
	}

	public void DoorModifyLocalCollisionMap(bool Active)
	{
		IntVec2 pos = new IntVec2(mPosition);

		if (mRotation == 0)
		{
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 0, pos.Y * 2 + 2].mWallXSolid = Active;
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 1, pos.Y * 2 + 2].mWallXSolid = Active;
		}
		else if (mRotation == 1)
		{
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2, pos.Y * 2 + 0].mWallZSolid = Active;
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2, pos.Y * 2 + 1].mWallZSolid = Active;
		}
		else if (mRotation == 2)
		{
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 0, pos.Y * 2].mWallXSolid = Active;
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 1, pos.Y * 2].mWallXSolid = Active;
		}
		else if (mRotation == 3)
		{
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 2, pos.Y * 2 + 0].mWallZSolid = Active;
			mWorld.mMap.mLocalCollisionTiles[mPlayerID][pos.X * 2 + 2, pos.Y * 2 + 1].mWallZSolid = Active;
		}

		Rect mr = new Rect(mBounds.min.x * 2.0f, mBounds.min.z * 2.0f, mBounds.size.x * 2.0f, mBounds.size.z * 2.0f);
		mWorld.mMap.CollisionModified(mPlayerID, mr);
	}

	public void ModifyLocalCollisionMap(bool Active)
	{
        for (int iX = 0; iX < mAsset.mWidth * 2; ++iX)
        {
            for (int iY = 0; iY < mAsset.mLength * 2; ++iY)
            {
				IntVec2 worldTile = CItem.GetWorldCollisionTile(iX, iY, mPosition, mRotation);
				CCollisionTile tile = mWorld.mMap.mLocalCollisionTiles[mPlayerID][worldTile.X, worldTile.Y];

                if (Active)
                {
                    tile.mOccupied = mID;
					tile.mSolid = mAsset.mTiles[iX, iY].mSolid;
                }
                else
                {
                    tile.mOccupied = 0;
                    tile.mSolid = false;
                }
            }
        }

		Rect mr = new Rect(mBounds.min.x * 2.0f, mBounds.min.z * 2.0f, mBounds.size.x * 2.0f, mBounds.size.z * 2.0f);
		mWorld.mMap.CollisionModified(mPlayerID, mr);
	}

	/// <summary>
	/// Unlink this view from its item resource.
	/// </summary>
	public void UnlinkItem()
	{
		_item = null;
	}

	public CItem GetItem()
	{
		return _item;
	}

	public void SetDoorLock(bool Locked)
	{
		DoorModifyLocalCollisionMap(Locked);
		mLocked = Locked;
	}

	public void UpdateState(CItem Item)
	{
		if (!mBlueprint)
		{
			mDurability = Item.mDurability;
			mMaxDurability = Item.mMaxDurability;

			if (Item.mType == CEntity.EType.ITEM_DOOR)
			{
				CItemDoor door = (CItemDoor)Item;
				mDoorPosition = door.mAngle;

				if (mPlayerID == mOwnerID)
				{
					door.mLocked = mLocked;
				}
				else if (mWorld.IsAllied(mPlayerID, mOwnerID))
				{
					if (mLocked != door.mLocked)
						SetDoorLock(door.mLocked);
				}
			}
			else if (Item.mType == CEntity.EType.ITEM_START)
			{
				mDoorPosition = ((CItemStart)Item).mDoorPosition;
			}
			else if (Item.mType == CEntity.EType.ITEM_DESK)
			{
				mUserID = ((CItemDesk)Item).mUserID;
				mCompletedPaperStacks = ((CItemDesk)Item).mCompletedStacks.Count;
				mMaxCompletedPapers = ((CItemDesk)Item).mMaxCompletedPapers;
			}
			else if (Item.mType == CEntity.EType.ITEM_SAFE)
			{
				mValue = ((CItemSafe)Item).mValue;
			}

			// TODO: Update State is only called when the item is visible. We need to update queue tokens every tick.
			UpdateQueueTokens();
		}
	}

	public void SetVisible()
	{
		mState = EState.S_VISIBLE;

		if (mBlueprint)
		{
			mSurfaceColor = CGame.COLOR_BLUEPRRINT;
		}
		else
		{
			if (mAsset.mItemType == EItemType.DOOR || mAsset.mItemType == EItemType.START || mAsset.mItemType == EItemType.SAFE)
				mSurfaceColor = mWorld.mPlayers[mOwnerID].mColor;
			else
				mSurfaceColor = mWorld.mMap.GetTileColor((int)mPosition.x, (int)mPosition.y);
		}
	}

	public void SetHidden()
	{
		mState = EState.S_HIDDEN;

		if (mAsset.mItemType == EItemType.DOOR || mAsset.mItemType == EItemType.START || mAsset.mItemType == EItemType.SAFE)
			mSurfaceColor = Color.Lerp(mWorld.mPlayers[mOwnerID].mColor, mWorld.mMap.mBackgroundColor, 0.65f);
		else
			mSurfaceColor = mWorld.mMap.mBackgroundColor;
	}
	
	/// <summary>
	/// Check if this item view should still be displayed.
	/// </summary>
	public bool ShouldExist()
	{
		if (_item != null)
			return true;

		for (int iX = 0; iX < mAsset.mWidth; ++iX)
		{
			for (int iY = 0; iY < mAsset.mLength; ++iY)
			{
				Vector2 worldPos = CItem.RotationAxisTable[mRotation].Transform(new Vector2(iX + 0.5f, iY + 0.5f)) + CItem.PivotRelativeToTile[mRotation] + mPosition;

				if (mWorld.mMap.IsTileVisible(mPlayerID, (int)worldPos.x, (int)worldPos.y))
					return false;
			}
		}

		return true;
	}

	public void Destroy()
	{
		mState = EState.S_DESTROYED;

		if (!mBlueprint)
		{
			ModifyLocalCollisionMap(false);

			if (mAsset.mItemType == EItemType.DESK)
			{
				if (mAssignedUnitID != -1)
				{
					CUnit unit = mWorld.GetEntity<CUnit>(mAssignedUnitID);
					if (unit != null)
						unit.mAssignedDeskID = -1;
				}
			}
			else if (mAsset.mItemType == EItemType.DOOR)
			{
				DoorModifyLocalCollisionMap(false);
			}

			KillQueue();
		}
	}

	/// <summary>
	/// Checks if a unit can queue to this item.
	/// </summary>
	public bool CanBeQueued(int PlayerID)
	{
		if (mBlueprint)
			return false;

		if (mAsset.mItemType == EItemType.SAFE)
		{
			if (mOwnerID != PlayerID || mValue <= 0)
				return false;
		}

		// If we are visible and there are no enemy units dirrectly occupying item
		
		// If we are not visible, then we can probably be queued for.

		return true;
	}

	/// <summary>
	/// Get a queue token.
	/// </summary>
	public CQueueToken GetQueueToken()
	{
		CQueueToken token = new CQueueToken();
		token.mExpired = false;
		token.mItemProxyID = mID;
		token.mQueueIndex = mQueueList.Count;
		token.mUsageSlot = -1;

		mQueueList.Add(token);

		// If we can see our item, then directly check if we can assign to it.
		// First get close before assigning?

		// We only want to assign when standing on the entry point?

		// Units could hold the token in their main unit, then we could call the unit
		// we we expire and get them to give up their token.

		UpdateQueueTokens();

		return token;
	}

	/// <summary>
	/// Give up token.
	/// </summary>
	public void DequeueQueueToken(CQueueToken Token)
	{
		Token.mExpired = true;
		mQueueList.Remove(Token);
		UpdateQueueTokens();
	}

	/// <summary>
	/// Manage the queue.
	/// </summary>
	public void UpdateQueueTokens()
	{
		if (mQueueList.Count == 0)
			return;

		// Check if we can assign an item usage slot to the first guy in queue.
		if (_item != null && _item.mUserUnitID == -1)
			mQueueList[0].mUsageSlot = 0;
		else
			mQueueList[0].mUsageSlot = -1;

		// if we are visible
		Vector2 startPos = mPosition * 2.0f + new Vector2(0.5f, 0.5f);
		int dir = mRotation;

		for (int i = 0; i < mQueueList.Count; ++i)
		{
			while (true)
			{
				startPos -= CItem.RotationAxisTable[mRotation].Transform(new Vector2(0.0f, 1.3f));

				int x = (int)(startPos.x);
				int y = (int)(startPos.y);

				if (x < 0 || x > 99 || y < 0 || y > 99)
				{
					mQueueList[i].mPosition = mPosition;
					break;
				}

				// TODO: Check rect fit in map collision.

				if (!mWorld.mMap.mGlobalCollisionTiles[x, y].mSolid)
				{
					mQueueList[i].mPosition = startPos / 2.0f;
					break;
				}
			}
		}
	}

	/// <summary>
	/// Remove all queue tokens.
	/// </summary>
	public void KillQueue()
	{
		for (int i = 0; i < mQueueList.Count; ++i)
		{
			mQueueList[i].mExpired = true;
		}

		mQueueList.Clear();
	}

	public Vector2 FindFreeTileAroundPosition(Vector2 Position)
	{
		Vector2 freePos = Position;

		int width = 1;
		int x = (int)(Position.x);
		int y = (int)(Position.y);

		while (true)
		{
			for (int iX = x - width; iX <= x + width; ++iX)
				for (int iY = y - width; iY <= y + width; ++iY)
				{
					if (iX != x && iY != y)
					{
						if (iX > 3 && iX < 97 && iY > 3 && iY < 97)
						{
							if (mWorld.mMap.mTiles[iX, iY].mItem == -1)
							{
								Debug.Log("x " + x + " y " + y + " iX " + iX + " iY " + iY);
								return new Vector2(iX + 0.5f, iY + 0.5f);
							}
						}
					}
				}

			++width;
		}
	}

	/// <summary>
	/// Assign a new worker to this desk and unassign the existing worker.
	/// </summary>	
	public void AssignWorker(CUnit Worker)
	{
		if (mAssignedUnitID != -1)
		{
			CUnit oldWorker = mWorld.GetEntity<CUnit>(mAssignedUnitID);
			if (oldWorker != null)
				oldWorker.mAssignedDeskID = -1;
		}

		mAssignedUnitID = Worker.mID;
		Worker.mAssignedDeskID = mID;
	}

	public int GetFreePaperStackSlots()
	{
		return mMaxPaperStackSlots - mAssignedPaperStacks.Count;
	}

	/// <summary>
	/// Remove a contract assignment.
	/// </summary>	
	public void RemoveContractPapers(CContract Contract)
	{
		for (int i = 0; i < mAssignedPaperStacks.Count; ++i)
		{
			if (mAssignedPaperStacks[i].mContract == Contract)
			{
				mPaperStackUpdateTick = mWorld.mGameTick;
				mAssignedPaperStacks[i].mDistributed = false;
				mAssignedPaperStacks.RemoveAt(i);
				--i;
			}
		}
	}

	/// <summary>
	/// Assign contract units to this desk.
	/// </summary>
	public void AssignContractUnits(CContractStack Paper)
	{
		if (Paper == null)
		{
			Debug.LogError("Can't assign null to desk");
			return;
		}

		if (GetFreePaperStackSlots() == 0)
		{
			Debug.LogError("Put too many contract papers on desk.");
			return;
		}

		Paper.mDistributed = true;
		mAssignedPaperStacks.Add(Paper);
		mPaperStackUpdateTick = mWorld.mGameTick;
	}

	/// <summary>
	/// Do work on the paper stacks of this desk.
	/// Returns true if there is still more work to be done.
	/// </summary>
	public bool DoWork(float PapersPerTick)
	{
		if (mAssignedPaperStacks.Count == 0)
			return false;

		CContractStack stack = mAssignedPaperStacks[0];

		if (stack.DoWork(PapersPerTick))
		{
			if (_item != null)
			{
				((CItemDesk)_item).mCompletedStacks.Add(new CCompletedContractStack(stack.mDoneWork));
				//.mCompletedPapers += (int)stack.mDoneWork;
			}

			mAssignedPaperStacks.Remove(stack);
			mPaperStackUpdateTick = mWorld.mGameTick;
		}
	
		if (mAssignedPaperStacks.Count == 0)
			return false;

		return true;
	}
}
