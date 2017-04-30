using System;
using System.Collections.Generic;
using UnityEngine;

public class CElevatorEvent
{
	public enum EType
	{
		T_SPAWN_UNIT,
		T_DELIVERY,
		T_EXIT
	}

	public EType mType;

	public int mInfo1;
	public int mInfo2;
	public int mInfo3;
	public string mInfoStr;

	public CElevatorEvent(EType Type)
	{
		mType = Type;
	}

	public CElevatorEvent(EType Type, int Info1)
	{
		mType = Type;
		mInfo1 = Info1;
	}

	public CElevatorEvent(EType Type, int Info1, int Info2)
	{
		mType = Type;
		mInfo1 = Info1;
		mInfo2 = Info2;
	}

	public CElevatorEvent(EType Type, int Info1, int Info2, int Info3, string InfoStr = "")
	{
		mType = Type;
		mInfo1 = Info1;
		mInfo2 = Info2;
		mInfo3 = Info3;
		mInfoStr = InfoStr;
	}
}

public class CItemStart : CItem
{
	// TODO: Give the elevator queue to the player.
	// Spawn point will just manage the queue directly from the player.
	public List<CElevatorEvent> mEvents = new List<CElevatorEvent>();
	public CElevatorEvent mCurrentEvent;
		
	public float mDoorPosition;

	public Vector2 mSpawnPos;
	public Vector2 mExitPos;
	
	private float _deliveryTimer;
	private float _waitingTimer;
	
	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_START;

		mDoorPosition = 0.0f;
		_deliveryTimer = 0.0f;
		_waitingTimer = 0.0f;
	}

	public override void PlaceInWorld()
	{
		base.PlaceInWorld();

		mSpawnPos = mPosition + RotationAxisTable[mItemRot].Transform(new Vector2(1.0f, 2.5f)) + PivotRelativeToTile[mItemRot];
		mExitPos = mPosition + RotationAxisTable[mItemRot].Transform(new Vector2(1.0f, 0.5f)) + PivotRelativeToTile[mItemRot];

		mWorld.mPlayers[mOwner].mSpawnItemID = mID;

		Debug.LogWarning("Set Player " + mOwner + " Spawn " + mID);
	}

	public override void SimTick()
	{
		base.SimTick();

		if (mCurrentEvent == null)
		{
			if (mEvents.Count > 0)
			{
				mCurrentEvent = mEvents[0];
				mEvents.RemoveAt(0);

				mDoorPosition = 0.0f;

				if (mCurrentEvent.mType != CElevatorEvent.EType.T_EXIT)
					_deliveryTimer = 2.0f;
			}
		}
		else
		{
			if (mCurrentEvent.mType != CElevatorEvent.EType.T_EXIT)
			{
				if (_waitingTimer > 0.0f)
				{
					_waitingTimer -= CWorld.SECONDS_PER_TICK;

					if (_waitingTimer <= 0.0f)
					{
						_waitingTimer = 0.0f;
						mDoorPosition = 0.0f;						
						mCurrentEvent = null;
					}
				}
				else
				{
					_deliveryTimer -= CWorld.SECONDS_PER_TICK;

					if (_deliveryTimer <= 0.0f)
					{
						mDoorPosition = 1.0f;
						_deliveryTimer = 0.0f;
						_waitingTimer = 2.0f;

						if (mCurrentEvent.mType == CElevatorEvent.EType.T_SPAWN_UNIT)
						{
							CResume resume = mWorld.GetEntity<CResume>(mCurrentEvent.mInfo2);

							if (resume == null)
							{
								resume = new CResume();
								resume.Generate(mWorld, 0, 1);
								CUnit unit = mWorld.SpawnUnitAtElevator(this, mCurrentEvent.mInfo1, resume);
								mWorld.PushMessage("Intern " + unit.mName + " has arrived", 0);
							}
							else
							{
								CUnit unit = mWorld.SpawnUnitAtElevator(this, mCurrentEvent.mInfo1, resume);
								mWorld.PushMessage("Employee " + unit.mName + " has arrived", 0);
								mWorld.DespawnEntity(resume);
							}
						}
						else if (mCurrentEvent.mType == CElevatorEvent.EType.T_DELIVERY)
						{
							CPickup pickup = mWorld.SpawnPickup(this, mCurrentEvent.mInfo1, mCurrentEvent.mInfoStr);
							pickup.mBuildTag = mCurrentEvent.mInfo3;
						}
					}
				}
			}
		}
	}

	public void QueueEvent(CElevatorEvent Event, bool AtFront = false)
	{
		if (AtFront)
		{
			mEvents.Insert(0, Event);
		}
		else
		{
			mEvents.Add(Event);
		}
	}
}