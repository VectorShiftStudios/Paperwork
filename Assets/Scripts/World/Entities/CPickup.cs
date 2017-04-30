using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPickup : CEntity
{
	private static int _NextBuildID = 1;
	
	public static int GetCurrentBuildID()
	{
		return _NextBuildID;
	}

	public static void SetCurrentBuildID(int ID)
	{
		_NextBuildID = ID;
	}

	public static int GetNextBuildID()
	{
		return _NextBuildID++;
	}

	public Vector2 mPosition;
	public Bounds mBounds;
	public CItemAsset mContainedItemAsset;
	public int mCarriedByUnitID;
	public Vector2 mBeingPushedForce;
	public int mBuildTag;
	public int mPlayerVisibility;

	private bool _exitingElevator;
	private Vector2 _exitTarget;

	public CPickupView mStateView = null;

	public bool IsCollidable()
	{
		return (mCarriedByUnitID == -1);
	}
	
	public override bool IsPickup() { return true; }
	
	public override void Init(CWorld World)
	{	
		base.Init(World);
		mType = EType.PICKUP;

		mCarriedByUnitID = -1;
		mBuildTag = GetNextBuildID();
	}

	public override void Destroy()
	{
		base.Destroy();
	}


	public void SetPosition(float X, float Y)
	{
		mPosition = new Vector2(X, Y);
		CalcBounds();
	}

	public void CalcBounds()
	{
		mBounds = new Bounds(new Vector3(mPosition.x, 0.6f, mPosition.y), new Vector3(0.5f, 1.2f, 0.5f));
	}

	public void PickUp(int CarrierID)
	{
		mCarriedByUnitID = CarrierID;
	}

	public void Drop(Vector2 Position)
	{
		mCarriedByUnitID = -1;
		SetPosition(Position.x, Position.y);
	}

	public bool IsReady()
	{
		return !_exitingElevator;
	}

	/// <summary>
	/// Perform updates on simulation tick (20Hz).
	/// </summary>
	public override void SimTick()
	{
		mPlayerVisibility = mWorld.GetPlayerFlagsForTileVisibility(mPosition);

		if (_exitingElevator)
		{
			_UpdateExitingElevator();
		}
		else
		{
			if (mCarriedByUnitID == -1)
			{
				_Unstick();
				_UpdatePosition();
			}
		}
	}

	/// <summary>
	/// If this unit is stuck in a solid block, find the first nearby free one.
	/// </summary>
	private void _Unstick()
	{
		// TODO: Check for full unit radius not just single position point.
		// TODO: Maybe look for walls.
		// TODO: Had a pickup end up outside map one time, then it crashed when trying to ref the tile.

		int width = 1;
		int x = (int)(mPosition.x * 2.0f);
		int y = (int)(mPosition.y * 2.0f);

		if (!mWorld.mMap.mGlobalCollisionTiles[x, y].mSolid)
			return;
			
		while (true)
		{
			for (int iX = x - width; iX <= x + width; ++iX)
				for (int iY = y - width; iY <= y + width; ++iY)
				{
					if (iX > 3 && iX < 97 && iY > 3 && iY < 97)
					{
						if (!mWorld.mMap.mGlobalCollisionTiles[iX, iY].mSolid)
						{
							mPosition = new Vector2(iX * 0.5f + 0.25f, iY * 0.5f + 0.25f);
							return;
						}
					}
				}

			++width;
		}
	}

	public bool IsVisibleToPlayer(int PlayerID)
	{
		return (mPlayerVisibility & (1 << PlayerID)) != 0;
	}

	private void _UpdatePosition()
	{
		Vector2 targetPos = mPosition + mBeingPushedForce;

		if (mPosition != targetPos)
		{
			mPosition = mWorld.mMap.Move(mPosition, targetPos, 0.25f);
		}

		mBeingPushedForce *= 0.2f;
		CalcBounds();
	}

	private void _UpdateExitingElevator()
	{
		if ((mPosition - _exitTarget).sqrMagnitude <= 0.01f)
		{
			_exitingElevator = false;
		}
		else
		{
			Vector2 dir = (_exitTarget - mPosition).normalized;
			mPosition += dir * CWorld.SECONDS_PER_TICK;
			//mRotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.y));
		}

		mBeingPushedForce = Vector2.zero;

		CalcBounds();
	}

	public void ExitElevator(Vector2 Position)
	{
		_exitingElevator = true;
		_exitTarget = Position;
	}
}