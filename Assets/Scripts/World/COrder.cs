using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the process of building/placing items in the world.
/// </summary>
public class CBuildOrder
{
	public enum EState
	{		
		CANCELLED,

		WAIT_FOR_DELIVERY,
		TRANSIT_TO_BUILD_SITE,

		START_PACKUP,
		TRANSIT_TO_PACKUP,
	}

	public CWorld mWorld;
	public EState mState;
	public int mPlayerID;
	public int mBuildTag;
	public int mItemBlueprintID;
	public int mBuilderID;

	private int _tickDelay;
	private EState _abandonState;

	public CBuildOrder(CWorld World)
	{
		_tickDelay = 0;
		mWorld = World;
	}

	private CUnit _GetTaskAcceptingUnit(CWorld World, int PlayerID, int PathingRoomID)
	{
		List<CUnit> units = World.mUnits;

		for (int i = 0; i < units.Count; ++i)
		{
			if (units[i].mOwner == PlayerID && units[i].mCanAcceptTask)
				return units[i];
		}

		return null;
	}

	public void SimTick()
	{
		if (_tickDelay > 0)
		{
			--_tickDelay;
			return;
		}

		if (mState == EState.WAIT_FOR_DELIVERY)
		{
			_abandonState = EState.WAIT_FOR_DELIVERY;
			CPickup pickup = mWorld.GetPickupWithBuildTag(mBuildTag);

			if (pickup != null && pickup.IsReady())
			{
				// TODO: Check if blueprint and pickup are in the same pathing room.
				// Pass that info to make sure we get a builder in the same room.
				CUnit builder = _GetTaskAcceptingUnit(mWorld, mPlayerID, 0);

				if (builder != null)
				{
					builder.SetBuildOrder(this);
					mState = EState.TRANSIT_TO_BUILD_SITE;
					return;
				}
			}

			_tickDelay = 20;
		}
		else if (mState == EState.TRANSIT_TO_BUILD_SITE)
		{
			_abandonState = EState.WAIT_FOR_DELIVERY;
			// Wait for builder to pickup the pickup, do the delivery, and begin building.
			// Wait for builder to tell us status things.
		}
		else if (mState == EState.START_PACKUP)
		{
			_abandonState = EState.START_PACKUP;

		}
	}

	/// <summary>
	/// Called by builder when it has finished executing the order.
	/// </summary>
	public void OnCompleted()
	{

	}

	/// <summary>
	/// Called by builder when it abandons this order.
	/// </summary>
	public void OnAbandoned()
	{
		mState = _abandonState;
		mBuilderID = -1;
	}

	/// <summary>
	/// Called when the player cancels this order.
	/// </summary>
	public void OnCancelled()
	{
		CUnit builder = mWorld.GetEntity<CUnit>(mBuilderID);
		if (builder != null)
			builder.AbandonOrder();

		mState = EState.CANCELLED;
		mBuilderID = -1;
		mItemBlueprintID = -1;

		mWorld.RemoveBuildOrder(this);
	}
}
