using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CMissile : CEntity
{
	public const float GRAVITY = -0.05f;
	public int mFiredByUnitID;

	public Vector2 mPosition;
	public Vector3 mRealPosition;
	public Vector3 mVelocity;
	public Bounds mBounds;

	public int mDead;
	public int mPlayerVisibility;

	// State View
	public CMissileView mStateView = null;

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.MISSILE;

		mFiredByUnitID = -1;
		mDead = -1;
	}

	public void SetPosition(float X, float Y)
	{
		mPosition = new Vector2(X, Y);
		CalcBounds();
	}

	public void Set(Vector3 StartPosition, Vector3 TargetPosition)
	{
		mRealPosition = StartPosition;
		mVelocity = CMath.GetMissileVelocity(StartPosition, TargetPosition, 45.0f, GRAVITY * 20.0f * 20.0f);
		mVelocity *= CWorld.SECONDS_PER_TICK;
	}

	public void CalcBounds()
	{
		//mBounds = new Bounds(new Vector3(mPosition.x, 0.3f, mPosition.y), new Vector3(0.5f, 0.6f, 0.5f));
		mBounds = new Bounds(mRealPosition, new Vector3(0.3f, 0.3f, 0.3f));
	}

	/// <summary>
	/// Perform updates on simulation tick (20Hz).
	/// </summary>
	public override void SimTick()
	{
		mPlayerVisibility = mWorld.GetPlayerFlagsForTileVisibility(mRealPosition.ToWorldVec2());

		if (mDead != -1)
		{
			if (mDead++ > 40)
				Terminate();

			return;
		}

		// Move in direction with force.
		// Drop over time?
		// Explode on impact with enemy or map collision.

		//mRealPosition += mDirection * 0.01f;
		mVelocity.y += GRAVITY;
		//mVelocity.y -= 0.01f;
		Vector3 vec3TargetPos = mRealPosition + mVelocity;

		Vector2 startPos = mRealPosition.ToWorldVec2();
		Vector2 targetPos = vec3TargetPos.ToWorldVec2();

		if (targetPos.x < 1.0f || targetPos.x > 99.0f || targetPos.y < 1.0f || targetPos.y > 99.0f)
		{
			Terminate();
			return;
		}

		// TODO: If coliding with solid blocks that belong to an item, then damage that item.
		/*
		if (mWorld.mMap.Collide(startPos, targetPos, 0.25f))
		{
			mDead = 0;
			return;
		}
		*/

		mRealPosition = vec3TargetPos;

		CalcBounds();

		for (int i = 0; i < mWorld.mUnits.Count; ++i)
		{
			CUnit u = mWorld.mUnits[i];

			if (!u.mDead && u.mOwner != mOwner && u.mBounds.Intersects(mBounds))
			{
				// TODO: What if unit is sitting somewhere?
				// Only first unit hit will take damage.
				u.TakeDamage(10, Vector2.zero);
				mRealPosition.x = u.mBounds.center.x;
				mRealPosition.z = u.mBounds.center.z;
				mDead = 20;
				return;
			}
		}

		// Collide with floor
		if (mRealPosition.y <= 0.0f)
		{
			mRealPosition.y = 0.0f;
			mDead = 0;
			return;
		}
	}

	/// <summary>
	/// Terminate this missile.
	/// </summary>
	public void Terminate()
	{
		//mWorld.AddTransientEventFOW(mRealPosition.ToWorldVec2()).SetEffect(mRealPosition);
		mWorld.DespawnEntity(this);
	}

	public bool IsVisibleToPlayer(int PlayerID)
	{
		return (mPlayerVisibility & (1 << PlayerID)) != 0;
	}
}