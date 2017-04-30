using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CItemDoor : CItem
{
	public const float DOORSPEED = 5.0f;

	public float mAngle;
	public bool mOpen;
	public bool mLocked;
	
	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_DOOR;

		mAngle = 0.0f;
		mOpen = false;
		mLocked = false;
	}

	public override void PlaceInWorld()
	{
		base.PlaceInWorld();
		_SetGlobalCollision(true);
	}

	public override void SimTick()
	{
		base.SimTick();

		if (mBluerprint)
			return;

		Vector2 pos = new Vector2(mBounds.center.x, mBounds.center.z);

		if (mLocked)
			mOpen = false;
		else
			mOpen = mWorld.IsUnitWithinRadius(pos, 1.0f, mWorld.mPlayers[mOwner].mAllies);

		if (mOpen)
		{
			if (mAngle < 1.0f)
			{
				mAngle += DOORSPEED * CWorld.SECONDS_PER_TICK;

				if (mAngle > 1.0f)
					mAngle = 1.0f;
			}
		}
		else
		{
			if (mAngle > 0)
			{
				mAngle -= DOORSPEED * CWorld.SECONDS_PER_TICK;

				if (mAngle < 0.0f)
					mAngle = 0.0f;
			}
		}

		_SetGlobalCollision(!mOpen);
	}

	public override void Destroy()
	{
		_SetGlobalCollision(false);
		base.Destroy();
	}

	private void _SetGlobalCollision(bool Active)
	{
		if (mItemRot == 0)
		{
			mWorld.mMap.mTiles[mX, mY + 1].mWallX.mSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 0, mY * 2 + 2].mWallXSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 1, mY * 2 + 2].mWallXSolid = Active;
		}
		else if (mItemRot == 1)
		{
			mWorld.mMap.mTiles[mX, mY].mWallZ.mSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2, mY * 2 + 0].mWallZSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2, mY * 2 + 1].mWallZSolid = Active;
		}
		else if (mItemRot == 2)
		{
			mWorld.mMap.mTiles[mX, mY].mWallX.mSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 0, mY * 2].mWallXSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 1, mY * 2].mWallXSolid = Active;
		}
		else if (mItemRot == 3)
		{
			mWorld.mMap.mTiles[mX + 1, mY].mWallZ.mSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 2, mY * 2 + 0].mWallZSolid = Active;
			mWorld.mMap.mGlobalCollisionTiles[mX * 2 + 2, mY * 2 + 1].mWallZSolid = Active;
		}
	}
}
