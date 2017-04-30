using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI controlled player.
/// </summary>
public class CPlayerAI
{
	public enum AIPhase
	{
		START,
		DECIDE,
	}

	public CWorld mWorld;
	public int mPlayerId;
	public bool mActive;

	private int _counter;
	private AIPhase _phase;

	/// <summary>
	/// Set initial parameters.
	/// </summary>
	public void Init(CWorld World, int PlayerIndex)
	{
		mWorld = World;
		mPlayerId = PlayerIndex;
		mActive = false;
		_counter = 0;
	}

	/// <summary>
	/// Called every simulation tick.
	/// </summary>
	public void Update()
	{
		if (!mActive)
			return;

		// TODO: Maybe stagger updates over multiple ticks.
		// TODO: Maybe worker threads to help with decision making perf.

		// AI Follows a schedule of actions to be taken.
		// State driven? Checks for various conditions then jumps to a certain state.
		// State stack? Allows to return to what it was doing.

		// Fitting objects in the level:
		// A hint system where the level designer places hints for object placement
		// Procedural placement fitting
		//		Don't block of corridors
		//		Favour placing against walls
		//		Have gaps around objects placed in the middle of rooms

		++_counter;

		if (_counter == 1)
		{
			//CItemStart spawn = _world.GetEntity<CItemStart>(_world.mPlayers[1].mSpawnItemID);
			//spawn.QueueEvent(new CElevatorEvent(CElevatorEvent.EType.T_SPAWN_UNIT, 1, 0));
			_phase = AIPhase.DECIDE;

			//_GetFirstAvailableResume();

			//_world.ControllerPlaceBlueprint("item_desk_test", 19, 15, 2, _playerIndex);

			//_world.ControllerPlaceBlueprint("item_food_test", 17, 12, 1, _playerIndex);

			//_world.ControllerPlaceBlueprint("item_safe_test", 20, 10, 0, _playerIndex);
			//_world.ControllerPlaceBlueprint("item_safe_test", 21, 10, 0, _playerIndex);
			//_world.ControllerPlaceBlueprint("item_safe_test", 22, 10, 0, _playerIndex);
			//mWorld.ControllerPlaceBlueprint("item_safe_test", 23, 10, 0, mPlayerId);

			//_world.ControllerPlaceBlueprint("item_test_0", 20, 7, 1, _playerIndex);

			//_world.ControllerPlaceBlueprint("item_door", 16, 6, 1, _playerIndex);
			//mWorld.ControllerPlaceBlueprint("item_door", 26, 12, 1, mPlayerId);
		}
		else
		{
			/*
			// Check if we have an unassigned desk, and then assign someone to it
			// TODO: Ideally we only want to assign to desks we know are within reach/safe.
			CItemProxy deskProxy = _GetUnassignedDesk();
			if (deskProxy != null)
			{
				CUnit unit = _GetUnassignedWorker();
				if (unit != null)
				{
					Debug.Log("Assigning worker " + unit.mID + " to desk " + deskProxy.mID);
					_world.ControllerSelectEntity(_playerIndex, unit.mID);
					_world.ControllerClaimDesk(_playerIndex, deskProxy.mID);
				}
			}

			_DistributeContractToAssignedDesks();

			if (_counter % 60 == 0)
			{
				_GetFirstAvailableResume();
				if (_GetContractCount() < 4)
					_GetFirstAvailableContract();

				if (_GetTotalInterns(true) < 5)
					_world.ControllerHireIntern(_playerIndex);
			}
			*/

			if (_counter % 60 == 0)
			{
				if (_GetTotalInterns(true) < 5)
					mWorld.ControllerHireIntern(mPlayerId);
			}
		}

		/*
		if (_phase == AIPhase.START)
		{
			Debug.LogWarning("The AI is here");

			
		}
		else if (_phase == AIPhase.DECIDE)
		{
		}
		*/
	}

	private int _GetTotalInterns(bool IncludeWaitingToSpawn)
	{
		int count = 0;

		for (int i = 0; i < mWorld.mUnits.Count; ++i)
		{
			CUnit unit = mWorld.mUnits[i];
			if (unit.mOwner == mPlayerId && unit.mIntern)
				++count;
		}

		if (IncludeWaitingToSpawn)
		{
			CItemStart itemStart = mWorld.GetEntity<CItemStart>(mWorld.mPlayers[mPlayerId].mSpawnItemID);

			if (itemStart != null)
			{
				for (int i = 0; i < itemStart.mEvents.Count; ++i)
				{
					if (itemStart.mEvents[i].mType == CElevatorEvent.EType.T_SPAWN_UNIT)
					{
						if (itemStart.mEvents[i].mInfo2 == 0)
							++count;
					}
				}
			}
		}

		return count;
	}

	private CItemProxy _GetUnassignedDesk()
	{
		for (int i = 0; i < mWorld.mItemProxies[mPlayerId].Count; ++i)
		{
			CItemProxy proxy = mWorld.mItemProxies[mPlayerId][i];

			if (proxy.mAsset.mItemType == EItemType.DESK && !proxy.mBlueprint)
			{
				if (proxy.mAssignedUnitID == -1)
					return proxy;
			}
		}
		
		return null;
	}

	private CUnit _GetUnassignedWorker()
	{
		for (int i = 0; i < mWorld.mUnits.Count; ++i)
		{
			CUnit unit = mWorld.mUnits[i];
			if (unit.mOwner == mPlayerId && !unit.mIntern && unit.mAssignedDeskID == -1)
				return unit;
		}

		return null;
	}

	private void _GetFirstAvailableResume()
	{
		for (int i = 0; i < mWorld.mResumes.Count; ++i)
		{
			CResume r = mWorld.mResumes[i];

			if (r.mOwner != -1)
			{
				mWorld.ControllerAcceptNotify(r.mID, mPlayerId);
				break;
			}
		}
	}

	private void _GetFirstAvailableContract()
	{
		for (int i = 0; i < mWorld.mContracts.Count; ++i)
		{
			CContract c = mWorld.mContracts[i];

			if (c.mOwner == -1)
			{
				mWorld.ControllerAcceptNotify(c.mID, mPlayerId);
				break;
			}
		}
	}

	private int _GetContractCount()
	{
		int result = 0;

		for (int i = 0; i < mWorld.mContracts.Count; ++i)
		{
			if (mWorld.mContracts[i].mOwner == mPlayerId)
				++result;
		}

		return result;
	}

	private void _DistributeContractToAssignedDesks()
	{
		CContract contract = null;

		for (int i = 0; i < mWorld.mContracts.Count; ++i)
		{
			CContract c = mWorld.mContracts[i];
			if (c.mOwner == mPlayerId && c.GetUndistributedStacks() > 0)
			{
				contract = c;
				break;
			}
		}

		if (contract == null)
			return;

		for (int i = 0; i < mWorld.mItemProxies[mPlayerId].Count; ++i)
		{
			CItemProxy proxy = mWorld.mItemProxies[mPlayerId][i];

			if (proxy.mAsset.mItemType == EItemType.DESK && !proxy.mBlueprint && proxy.mAssignedUnitID != -1)
			{
				if (proxy.mAssignedPaperStacks.Count < proxy.mMaxPaperStackSlots)
				{
					mWorld.ControllerDistributeContract(mPlayerId, contract.mID, proxy.mID);
					return;
				}
			}
		}
	}
}
