using System;
using System.Collections.Generic;
using UnityEngine;

public static class CUnitActions
{
	public delegate void UnitActionDelegate(CUnit Unit);

	public enum EPhase
	{
		NONE,

		GET_TOKEN,
		MANAGE_TOKEN,
		APPROACH_ITEM,
		USE_ITEM,

		COMBAT_VERIFY,
		COMBAT_MELEE,
		COMBAT_RANGED,

		WORK_NORMAL,
		WORK_IDLE,
	}

	public enum EType
	{
		NONE,

		ENTER,
		IDLE,
		DIE,
		QUIT,
		EAT,
		GET_FOOD,
		REST,
		COLLECT_SALARY,
		PROMOTION,
		ANGRY,
		HAPPY,
		VACATE,

		WORK,
		COLLECT_PAPERS,
		DELIVER_PAPERS,

		COMBAT,
		FLEE,

		TASK_BUILD,
		TASK_MOVE,
		TASK_PACKUP,

		CMD_MOVE,
		CMD_ATTACK,
		CMD_CLAIM_DESK
	}
	
	public struct SAction
	{
		public EType mID;
		public UnitActionDelegate mEnter;
		public UnitActionDelegate mUpdate;
		public UnitActionDelegate mExit;

		public SAction(EType ActionID, UnitActionDelegate Enter, UnitActionDelegate Update, UnitActionDelegate Exit)
		{
			mID = ActionID;
			mEnter = Enter;
			mUpdate = Update;
			mExit = Exit;
		}
	}
	
	// NOTE: These MUST be in the same index order as CUnitActions.EType.
	public static SAction[] actionTable = {
		new SAction(EType.NONE,             null,					null,					null),

		new SAction(EType.ENTER,            _enter_Enter,			_tick_Enter,			_exit_Enter),
		new SAction(EType.IDLE,             _enter_Idle,			_tick_Idle,				_exit_Idle),
		new SAction(EType.DIE,              _enter_Die,				_tick_Die,				_exit_Die),
		new SAction(EType.QUIT,             _enter_Quit,			_tick_Quit,				_exit_Quit),
		new SAction(EType.EAT,              _enter_Eat,				_tick_Eat,				_exit_Eat),
		new SAction(EType.GET_FOOD,         _enter_GetFood,			_tick_GetFood,			_exit_GetFood),
		new SAction(EType.REST,             _enter_Rest,			_tick_Rest,				_exit_Rest),
		new SAction(EType.COLLECT_SALARY,   _enter_Salary,			_tick_Salary,			_exit_Salary),
		new SAction(EType.PROMOTION,		_enter_Promotion,       _tick_Promotion,		_exit_Promotion),
		new SAction(EType.ANGRY,            _enter_Angry,			_tick_Angry,			_exit_Angry),
		new SAction(EType.HAPPY,            _enter_Happy,           _tick_Happy,			_exit_Happy),
		new SAction(EType.VACATE,           null,                   null,					null),
		new SAction(EType.WORK,             _enter_Work,            _tick_Work,             _exit_Work),
		new SAction(EType.COLLECT_PAPERS,   _enter_CollectPapers,   _tick_CollectPapers,    _exit_CollectPapers),
		new SAction(EType.DELIVER_PAPERS,   _enter_DeliverPapers,   _tick_DeliverPapers,    _exit_DeliverPapers),

		new SAction(EType.COMBAT,			_enter_Combat,			_tick_Combat,			_exit_Combat),
		new SAction(EType.FLEE,				_enter_Flee,			_tick_Flee,				_exit_Flee),

		new SAction(EType.TASK_BUILD,		_enter_Build,			_tick_Build,			_exit_Build),
		new SAction(EType.TASK_MOVE,		null,					null,					null),
		new SAction(EType.TASK_PACKUP,      _enter_Packup,			_tick_Packup,			_exit_Packup),

		new SAction(EType.CMD_MOVE,			_enter_CmdMove,			_tick_CmdMove,			_exit_CmdMove),
		new SAction(EType.CMD_ATTACK,		null,					null,					null),
		new SAction(EType.CMD_CLAIM_DESK,	_enter_CmdClaim,		_tick_CmdClaim,			_exit_CmdClaim),
	};

	//---------------------------------------------------------------------
	// State Functions
	//---------------------------------------------------------------------

	private static bool _checkHunger(CUnit Unit, bool Forced)
	{
		if (Unit.mHunger >= (Forced ? 80.0f : 60.0f))
		{
			if (Unit.mFoodRate > 0.0f)
				Unit.SetAction(EType.EAT);
			else
				Unit.SetAction(EType.GET_FOOD);

			return true;
		}

		return false;
	}

	private static bool _checkPromotion(CUnit Unit)
	{
		if (Unit.mPromotionCounter >= 50 && Unit.mWorld.mGameTick >= Unit.mPromotionTimeout)
		{
			Unit.SetAction(EType.PROMOTION);
			Unit.mPromotionTimeout = Unit.mWorld.mGameTick + (60 * CWorld.TICKS_PER_SECOND);
			Unit.mQuitCounter += 5;
			Unit.AdjustStress(20);

			return true;
		}

		return false;
	}

	private static bool _checkQuitting(CUnit Unit)
	{
		if (Unit.mQuitCounter >= 15)
		{
			Unit.Fire();
			return true;
		}

		return false;
	}
	
	//---------------------------------------------------------------------
	// Idle
	//---------------------------------------------------------------------
	private static void _enter_Idle(CUnit Unit)
	{
		Unit.mCanAcceptPlayerCommands = true;

		if (Unit.mIntern)
			Unit.mCanAcceptTask = true;

		Unit.mActionTempTimer = 80;
		Unit.mActionTempVec2 = Vector2.zero;
	}

	private static void _tick_Idle(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);
		Unit.AdjustStress(-Unit.mStats.mStressRecoveryRate * CWorld.SECONDS_PER_TICK);

		if (_checkQuitting(Unit))
			return;

		if (_checkHunger(Unit, false))
			return;

		if (Unit.mStamina <= 40.0f)
		{
			Unit.SetAction(EType.REST);
			return;
		}

		if (Unit.mWorld.GetPaydayTimeNormalize() <= 0.01f)
		{
			Unit.SetAction(EType.HAPPY);
			return;
		}

		if (Unit.mOwedSalary > 0)
		{
			Unit.SetAction(EType.COLLECT_SALARY);
			return;
		}

		/*
		if (Unit.mDeskToClaimProxyItemID != -1)
		{
			Unit.SetAction(EType.CMD_CLAIM_DESK);
			return;
		}
		*/

		if (Unit.mForceAttackItemProxyID != -1)
		{
			Unit.SetAction(EType.COMBAT);
			return;
		}

		if (_checkPromotion(Unit))
		{
			return;
		}

		if (Unit.mClosestVisibleEnemyID != -1)
		{
			CUnit enemy = Unit.mWorld.GetEntity<CUnit>(Unit.mClosestVisibleEnemyID);
			if (enemy == null)
			{
				Unit.mClosestVisibleEnemyID = -1;
			}
			else
			{
				if (Unit.mIntern && !enemy.mIntern)
					Unit.SetAction(EType.FLEE);
				else
					Unit.SetAction(EType.COMBAT);

				return;
			}
		}

		// Check promotion


		// TODO: If we are carrying papers then drop them off

		if (Unit.mAssignedDeskID != -1)
		{
			CItemProxy deskProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mAssignedDeskID);
			if (deskProxy != null)
			{
				if (deskProxy.mCompletedPaperStacks > 0)
				{
					Unit.SetAction(EType.COLLECT_PAPERS);
					return;
				}

				if (deskProxy.mAssignedPaperStacks.Count > 0)
				{
					Unit.SetAction(EType.WORK);
					return;
				}
			}
		}

		// We need to get the proxy desk here becuase we don't know if it still exists.
		//CItemDesk Unit.GetAssignedDesk();
		//if (!= null

		// Random chance to express current mood
		if (Unit.mWorld.SimRnd.GetNextFloat() > 0.999f)
		{
			Unit.SetAction(EType.HAPPY);
			return;
		}

		--Unit.mActionTempTimer;
		if (Unit.mActionTempTimer <= 0)
		{
			Unit.mActionTempTimer = 80;

			// Check surrounding tiles for a free one
			int startX = (int)Unit.mPosition.x;
			int startY = (int)Unit.mPosition.y;
			int destX = (int)(Unit.mWorld.SimRnd.GetNextFloat() * 5.0f) - 2 + startX;
			int destY = (int)(Unit.mWorld.SimRnd.GetNextFloat() * 5.0f) - 2 + startY;

			if (destX < 3) destX = 3;
			if (destX > 97) destX = 97;
			if (destY < 3) destY = 3;
			if (destY > 97) destY = 97;

			Unit.mActionTempVec2 = new Vector2(destX + 0.25f, destY + 0.25f);
		}

		if (Unit.mActionTempVec2 != Vector2.zero)
		{
			if (Unit.WalkTo(Unit.mActionTempVec2) != 0)
				Unit.mActionTempVec2 = Vector2.zero;
		}
	}

	private static void _exit_Idle(CUnit Unit)
	{
		Unit.CancelWalking();
		Unit.mCanAcceptTask = false;
	}

	//---------------------------------------------------------------------
	// Eat
	//---------------------------------------------------------------------
	private static void _enter_Eat(CUnit Unit)
	{
		Unit.mCanAcceptPlayerCommands = true;
		Unit.mActionAnim = "consume_food";
	}

	private static void _tick_Eat(CUnit Unit)
	{
		if (Unit.mHunger > 0)
		{
			
			Unit.AdjustHunger(-Unit.mFoodRate * CWorld.SECONDS_PER_TICK);
		}
		else
		{
			Unit.SetAction(EType.IDLE);
		}
	}

	private static void _exit_Eat(CUnit Unit)
	{
		Unit.mCanAcceptPlayerCommands = true;
		Unit.mActionAnim = "";
		Unit.mFoodRate = 0.0f;
	}

	//---------------------------------------------------------------------
	// Get Food
	//---------------------------------------------------------------------
	private static void _enter_GetFood(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.GET_TOKEN;
	}

	private static void _tick_GetFood(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);
		// Adjust stats
		// Check for transitions

		if (Unit.mActionPhase == EPhase.GET_TOKEN)
		{
			CQueueToken token = Unit.mWorld.QueueForClosestItem(EItemType.FOOD, Unit.mPosition, Unit.mOwner);

			if (token == null)
			{
				Unit.mThoughts = "I Couldn't find a vendor to give food!";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Unit.SetQueueToken(token);
			Unit.mActionPhase = EPhase.MANAGE_TOKEN;
		}

		if (Unit.mActionPhase == EPhase.MANAGE_TOKEN)
		{
			if (Unit.mQueueToken.mExpired)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then I was kicked out.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			CItemProxy itemProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mQueueToken.mItemProxyID);

			if (itemProxy == null)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then it was destroyed.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Vector2 pos = itemProxy.mBounds.center.ToWorldVec2();

			if (Unit.mQueueToken.mUsageSlot == -1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 2.0f, itemProxy.mBounds.min.z - 2.0f, itemProxy.mBounds.size.x + 4.0f, itemProxy.mBounds.size.z + 4.0f);
				if (!trigger.Contains(Unit.mPosition))
					Unit.WalkTo(pos, itemProxy.mID);
				else
					Unit.CancelWalking();

				return;
			}

			int walkResult = Unit.WalkTo(pos, itemProxy.mID);

			if (walkResult == -1)
			{
				// TODO: Can't reach it?
			}

			if (walkResult == 0 || walkResult == 1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 0.3f, itemProxy.mBounds.min.z - 0.3f, itemProxy.mBounds.size.x + 0.6f, itemProxy.mBounds.size.z + 0.6f);
				if (trigger.Contains(Unit.mPosition))
				{
					Unit.CancelWalking();
					// TODO: Check for actual item, we should be within range enough to see it now at least.
					// If we have gotten this far then we can assume we can use it, but just incase we still
					// check.

					// TODO: Tell item that we are using it, tell unit the item that it's using.

					CItem item = itemProxy.GetItem();

					if (item == null)
					{
						Unit.mThoughts = "I was using a safe but it doesn't exist!";
						Unit.SetAction(EType.ANGRY);
						return;
					}

					item.mUserUnitID = Unit.mID;
					Unit.mUsedItemID = item.mID;
					Unit.ReturnQueueToken();
					Unit.mCollide = false;
					Unit.mActionPhase = EPhase.USE_ITEM;
					Unit.mCanAcceptPlayerCommands = false;
					Unit.mActionTempTimer = 60;
					Unit.mActionAnim = "use_object_standing";
				}
			}
		}

		if (Unit.mActionPhase == EPhase.USE_ITEM)
		{
			if (Unit.mActionTempTimer-- <= 0)
			{
				CItemFood food = Unit.mWorld.GetEntity<CItemFood>(Unit.mUsedItemID);

				if (food == null)
				{
					Unit.mThoughts = "I was using a food vendor but it doesn't exist!";
					Unit.SetAction(EType.ANGRY);
					return;
				}

				Unit.mFoodRate = 10.0f;
				Unit.SetAction(EType.IDLE);
				return;
			}
		}
	}

	private static void _exit_GetFood(CUnit Unit)
	{
		Unit.ReturnQueueToken();

		if (Unit.mUsedItemID != -1)
		{
			CItem item = Unit.mWorld.GetEntity<CItem>(Unit.mUsedItemID);

			if (item != null)
				item.mUserUnitID = -1;

			Unit.mUsedItemID = -1;
		}

		Unit.mCanAcceptPlayerCommands = true;
		Unit.mActionAnim = "";
		Unit.mCollide = true;
	}

	//---------------------------------------------------------------------
	// Rest
	//---------------------------------------------------------------------
	private static void _enter_Rest(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.GET_TOKEN;
	}

	private static void _tick_Rest(CUnit Unit)
	{
		// Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);
		// Adjust stats
		// Check for transitions

		if (Unit.mActionPhase == EPhase.GET_TOKEN)
		{
			CQueueToken token = Unit.mWorld.QueueForClosestItem(EItemType.REST, Unit.mPosition, Unit.mOwner);

			if (token == null)
			{
				Unit.mThoughts = "I Couldn't find a couch to chill on!";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Unit.SetQueueToken(token);
			Unit.mActionPhase = EPhase.MANAGE_TOKEN;
		}

		if (Unit.mActionPhase == EPhase.MANAGE_TOKEN)
		{
			if (Unit.mQueueToken.mExpired)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then I was kicked out.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			CItemProxy itemProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mQueueToken.mItemProxyID);

			if (itemProxy == null)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then it was destroyed.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Vector2 pos = itemProxy.mBounds.center.ToWorldVec2();

			if (Unit.mQueueToken.mUsageSlot == -1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 2.0f, itemProxy.mBounds.min.z - 2.0f, itemProxy.mBounds.size.x + 4.0f, itemProxy.mBounds.size.z + 4.0f);
				if (!trigger.Contains(Unit.mPosition))
					Unit.WalkTo(pos, itemProxy.mID);
				else
					Unit.CancelWalking();

				return;
			}

			int walkResult = Unit.WalkTo(pos, itemProxy.mID);

			if (walkResult == -1)
			{
				// TODO: Can't reach it?
			}

			if (walkResult == 0 || walkResult == 1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 0.3f, itemProxy.mBounds.min.z - 0.3f, itemProxy.mBounds.size.x + 0.6f, itemProxy.mBounds.size.z + 0.6f);
				if (trigger.Contains(Unit.mPosition))
				{
					Unit.CancelWalking();

					// TODO: Check for actual item, we should be within range enough to see it now at least.
					// If we have gotten this far then we can assume we can use it, but just incase we still
					// check.

					CItem item = itemProxy.GetItem();
					if (item == null)
					{
						Unit.mThoughts = "I was using a couch but it doesn't exist!";
						Unit.SetAction(EType.ANGRY);
						return;
					}

					item.mUserUnitID = Unit.mID;
					Unit.mUsedItemID = item.mID;
					Unit.ReturnQueueToken();
					Unit.mCollide = false;
					Unit.mActionPhase = EPhase.USE_ITEM;
					Unit.mCanAcceptPlayerCommands = false;
					Unit.mActionAnim = "resting_sitting_on_couch";
					Unit.mPosition = item.ItemToWorldSpacePosition(new Vector2(0.5f, 0.3f));
					Unit.mRotation = Quaternion.AngleAxis(item.ItemToWorldSpaceRotation(180), Vector3.up);
				}
			}
		}

		if (Unit.mActionPhase == EPhase.USE_ITEM)
		{
			CItemRest rest = Unit.mWorld.GetEntity<CItemRest>(Unit.mUsedItemID);
			if (rest == null)
			{
				// TODO: Move to 'stunned/fall-over' state
				Unit.mThoughts = "I was using a couch but it was destroyed!";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			if (Unit.mStamina >= Unit.mStats.mMaxStamina)
			{
				Unit.SetAction(EType.IDLE);
				return;
			}

			Unit.AdjustStamina(5.0f * CWorld.SECONDS_PER_TICK);
			return;
		}
	}

	private static void _exit_Rest(CUnit Unit)
	{
		Unit.ReturnQueueToken();

		if (Unit.mUsedItemID != -1)
		{
			CItem item = Unit.mWorld.GetEntity<CItem>(Unit.mUsedItemID);

			if (item != null)
				item.mUserUnitID = -1;

			Unit.mUsedItemID = -1;
		}

		Unit.mCanAcceptPlayerCommands = true;
		Unit.mActionAnim = "";
		Unit.mCollide = true;
	}

	//---------------------------------------------------------------------
	// Salary
	//---------------------------------------------------------------------
	private static void _enter_Salary(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.GET_TOKEN;
	}

	private static void _tick_Salary(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;
		
		if (Unit.mActionPhase == EPhase.GET_TOKEN)
		{
			CQueueToken token = Unit.mWorld.QueueForClosestItem(EItemType.SAFE, Unit.mPosition, Unit.mOwner);

			if (token == null)
			{
				Unit.mThoughts = "I Couldn't find a safe to get owed money.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Unit.SetQueueToken(token);
			Unit.mActionPhase = EPhase.MANAGE_TOKEN;
		}

		if (Unit.mActionPhase == EPhase.MANAGE_TOKEN)
		{
			if (Unit.mQueueToken.mExpired)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then I was kicked out.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			CItemProxy itemProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mQueueToken.mItemProxyID);

			if (itemProxy == null)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then it was destroyed.";
				Unit.SetAction(EType.ANGRY);
				return;
			}
			
			Vector2 pos = itemProxy.mBounds.center.ToWorldVec2();

			if (Unit.mQueueToken.mUsageSlot == -1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 2.0f, itemProxy.mBounds.min.z - 2.0f, itemProxy.mBounds.size.x + 4.0f, itemProxy.mBounds.size.z + 4.0f);
				if (!trigger.Contains(Unit.mPosition))
					Unit.WalkTo(pos, itemProxy.mID);
				else
					Unit.CancelWalking();

				return;
			}
			
			int walkResult = Unit.WalkTo(pos, itemProxy.mID);
			
			if (walkResult == -1)
			{
				// TODO: Can't reach it?
			}

			if (walkResult == 0 || walkResult == 1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 0.3f, itemProxy.mBounds.min.z - 0.3f, itemProxy.mBounds.size.x + 0.6f, itemProxy.mBounds.size.z + 0.6f);
				if (trigger.Contains(Unit.mPosition))
				{
					Unit.CancelWalking();

					// TODO: Check for actual item, we should be within range enough to see it now at least.
					// If we have gotten this far then we can assume we can use it, but just incase we still
					// check.

					// TODO: Tell item that we are using it, tell unit the item that it's using.

					CItem item = itemProxy.GetItem();

					if (item == null)
					{
						Unit.mThoughts = "I was using a safe but it doesn't exist!";
						Unit.SetAction(EType.ANGRY);
						return;
					}

					item.mUserUnitID = Unit.mID;
					Unit.mUsedItemID = item.mID;
					Unit.ReturnQueueToken();
					Unit.mCollide = false;
					Unit.mActionPhase = EPhase.USE_ITEM;
					Unit.mCanAcceptPlayerCommands = false;
					Unit.mActionTempTimer = 60;
					Unit.mActionAnim = "use_object_standing";
				}
			}
		}

		if (Unit.mActionPhase == EPhase.USE_ITEM)
		{
			if (Unit.mActionTempTimer-- <= 0)
			{
				CItemSafe safe = Unit.mWorld.GetEntity<CItemSafe>(Unit.mUsedItemID);

				if (safe == null)
				{
					Unit.mThoughts = "I was using a safe but it doesn't exist!";
					Unit.SetAction(EType.ANGRY);
					return;
				}

				if (safe.mValue >= Unit.mOwedSalary)
				{
					safe.mValue -= Unit.mOwedSalary;
					Unit.mOwedSalary = 0;

					if (Unit.mPromotionCounter > 40 && Unit.mWorld.mGameTick >= Unit.mPromotionCounter)
					{
						Unit.SetAction(EType.IDLE);
						Unit.mPromotionTimeout = Unit.mWorld.mGameTick + (60 * CWorld.TICKS_PER_SECOND);
					}

					Unit.SetAction(EType.IDLE);
				}
				else
				{
					Unit.mOwedSalary -= safe.mValue;
					safe.mValue = 0;

					Unit.SetAction(EType.IDLE);
				}

				Unit.mWorld.AddTransientEvent(Unit.mWorld.GetPlayerFlags(Unit.mOwner)).SetSound(Unit.mPosition.ToWorldVec3(), 11);
				return;
			}
		}
	}

	private static void _exit_Salary(CUnit Unit)
	{
		Unit.ReturnQueueToken();

		if (Unit.mUsedItemID != -1)
		{
			CItem item = Unit.mWorld.GetEntity<CItem>(Unit.mUsedItemID);

			if (item != null)
				item.mUserUnitID = -1;

			Unit.mUsedItemID = -1;
		}

		Unit.mActionAnim = "";
		Unit.mCanAcceptPlayerCommands = true;
		Unit.mCollide = true;
	}

	//---------------------------------------------------------------------
	// Promotion
	//---------------------------------------------------------------------
	private static void _enter_Promotion(CUnit Unit)
	{
		Unit.mActionAnim = "complaining";
		Unit.mActionTempTimer = 0;
	}

	private static void _tick_Promotion(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (Unit.mActionTempTimer++ > 60)
			Unit.SetAction(EType.IDLE);
	}

	private static void _exit_Promotion(CUnit Unit)
	{
		Unit.mActionAnim = "";
	}

	//---------------------------------------------------------------------
	// Frustrate
	//---------------------------------------------------------------------
	private static void _enter_Angry(CUnit Unit)
	{
		Unit.mActionAnim = "angry";
		Unit.mActionTempTimer = 0;
	}

	private static void _tick_Angry(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (Unit.mActionTempTimer++ > 60)
			Unit.SetAction(EType.IDLE);
	}

	private static void _exit_Angry(CUnit Unit)
	{
		Unit.mActionAnim = "";
	}

	//---------------------------------------------------------------------
	// Happy
	//---------------------------------------------------------------------
	private static void _enter_Happy(CUnit Unit)
	{
		Unit.mActionAnim = "happy";
		Unit.mActionTempTimer = 0;
	}

	private static void _tick_Happy(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (Unit.mActionTempTimer++ > 60)
			Unit.SetAction(EType.IDLE);
	}

	private static void _exit_Happy(CUnit Unit)
	{
		Unit.mActionAnim = "";
	}

	//---------------------------------------------------------------------
	// Flee
	//---------------------------------------------------------------------
	private static void _enter_Flee(CUnit Unit)
	{
		Unit.mSpeed = 4.0f;
	}

	private static void _tick_Flee(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		// TODO: Can't just flee to spawn, since enemy might be there!
		// Need to use similar logic to 'Vacate' to find destination.

		CItemStart start = Unit.mWorld.GetEntity<CItemStart>(Unit.mWorld.mPlayers[Unit.mOwner].mSpawnItemID);
		Vector2 fleeLocation;

		if (start != null)
		{
			fleeLocation = start.mExitPos;

			if (Unit.WalkTo(fleeLocation) == 1)
				Unit.SetAction(EType.IDLE);
		}
		else
		{
			// TODO: Find some place to flee to?
			Unit.SetAction(EType.ANGRY);
		}
	}

	private static void _exit_Flee(CUnit Unit)
	{
		Unit.mSpeed = Unit.mStats.mMaxSpeed;
	}

	//---------------------------------------------------------------------
	// Enter
	//---------------------------------------------------------------------
	private static void _enter_Enter(CUnit Unit)
	{
		Unit.mThoughts = "I'm entering the world";
		Unit.mCollide = false;
		Unit.mAnimWalk = true;
		Unit.mCanAcceptPlayerCommands = false;
		Unit.mSpeed = 1.0f;
	}

	private static void _tick_Enter(CUnit Unit)
	{
		CItemStart start = Unit.mWorld.GetEntity<CItemStart>(Unit.mWorld.mPlayers[Unit.mOwner].mSpawnItemID);

		if (start != null)
		{
			Vector2 exitPos = start.mExitPos;

			if (Unit.ForceWalkTo(exitPos, 0.01f) == 1)
				Unit.SetAction(EType.IDLE);
		}
		else
		{
			Unit.SetAction(EType.IDLE);
		}
	}

	private static void _exit_Enter(CUnit Unit)
	{
		Unit.mCollide = true;
		Unit.mAnimWalk = false;
		Unit.mSpeed = Unit.mStats.mMaxSpeed;
	}

	//---------------------------------------------------------------------
	// Die
	//---------------------------------------------------------------------
	private static void _enter_Die(CUnit Unit)
	{
		Unit.mDead = true;
		Unit.CleanupWorldState();
		Unit.mActionTempTimer = 0;
	}

	private static void _tick_Die(CUnit Unit)
	{
		if (Unit.mActionTempTimer++ > 80)
			Unit.mWorld.DespawnEntity(Unit);
	}

	private static void _exit_Die(CUnit Unit)
	{
		Unit.mCollide = true;
		Unit.mAnimWalk = false;
	}

	//---------------------------------------------------------------------
	// Quit
	//---------------------------------------------------------------------
	private static void _enter_Quit(CUnit Unit)
	{
		Unit.CleanupWorldState();
		Unit.mActionAnim = "angry";
		Unit.mActionTempTimer = 0;
	}

	private static void _tick_Quit(CUnit Unit)
	{
		if (Unit.mActionTempTimer++ > 20)
		{
			Unit.mWorld.DespawnEntity(Unit);			
			Unit.mWorld.AddTransientEventFOW(Unit.mPosition).SetEffect(Unit.mPosition.ToWorldVec3());
		}
	}

	private static void _exit_Quit(CUnit Unit)
	{
	}

	//---------------------------------------------------------------------
	// Task Build
	//---------------------------------------------------------------------
	private static void _enter_Build(CUnit Unit)
	{

	}

	private static void _tick_Build(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;

		if (Unit.mOrder == null)
		{
			Unit.DropPickup();
			Unit.SetAction(EType.IDLE);
			return;
		}
		else
		{
			if (Unit.mCarryingPickup == null)
			{
				CPickup pickup = Unit.mWorld.GetPickupWithBuildTag(Unit.mOrder.mBuildTag);
				Vector2 pos = pickup.mPosition;

				if (Unit.IsWithinRange(pos, 0.5f))
				{
					Unit.CancelWalking();
					Unit.PickupPickup(pickup);
				}
				else
				{
					// TODO: Verify we can walk there
					Unit.WalkTo(pos);
				}
			}
			else
			{
				CItem blueprint = Unit.mWorld.GetEntity<CItem>(Unit.mOrder.mItemBlueprintID);
				
				if (blueprint == null)
				{
					Unit.AbandonOrder();
					Unit.SetAction(EType.IDLE);
					return;
				}

				Unit.AdjustStress(1 * CWorld.SECONDS_PER_TICK);

				Vector2 blueprintCenter = blueprint.mBounds.center.ToWorldVec2();

				int walkResult = Unit.WalkTo(blueprintCenter);

				if (walkResult == -1)
				{
					// TODO: Handle unreachable point.
					return;
				}

				if (walkResult == 1)
				{
					// TODO: Temp.
					return;
				}

				if (walkResult == 0)
				{
					Rect trigger = new Rect(blueprint.mBounds.min.x - 0.5f, blueprint.mBounds.min.z - 0.5f, blueprint.mBounds.size.x + 1.0f, blueprint.mBounds.size.z + 1.0f);

					if (!trigger.Contains(Unit.mPosition))
						return;

					// TODO: Nasty fix here, seems there is a precision issue with TraceNodes, adding a tiny offset fixes it?
					if (!Unit.mWorld.mMap.TraceNodes(Unit.mOwner, Unit.mPosition, blueprintCenter + new Vector2(0.0001f, 0.0001f)))
						return;
				}

				Unit.CancelWalking();
				// Check if units in the way, if so, then tell them to fuck off.
				// Check for building timer when we build the item.
				// Start building the item.
				Unit.BuildItem();
				Unit.SetAction(EType.IDLE);
			}
		}
	}

	private static void _exit_Build(CUnit Unit)
	{
		Unit.AbandonOrder();
	}

	//---------------------------------------------------------------------
	// Task Packup
	//---------------------------------------------------------------------
	private static void _enter_Packup(CUnit Unit)
	{

	}

	private static void _tick_Packup(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;

		if (Unit.mOrder == null)
		{
			Unit.DropPickup();
			Unit.SetAction(EType.IDLE);
			return;
		}
		else
		{
			// Check the state of the order and do stuff accordingly.

			if (Unit.mCarryingPickup == null)
			{
				// Check if pickup exists in world.
				// How to handle when not visible.
				// Try path to pickup.

				// Fetch item.
				// Go Fetch item
				CPickup pickup = Unit.mWorld.GetPickupWithBuildTag(Unit.mOrder.mBuildTag);
				Vector2 pos = pickup.mPosition;

				//Debug.Log("Go get pickup");

				if (Unit.WalkTo(pos) == 1)
				{
					Unit.PickupPickup(pickup);
				}
			}
			else
			{
				CItem blueprint = Unit.mWorld.GetEntity<CItem>(Unit.mOrder.mItemBlueprintID);

				if (blueprint == null)
				{
					Unit.AbandonOrder();
					Unit.SetAction(EType.IDLE);
					return;
				}

				Vector2 blueprintCenter = blueprint.mBounds.center.ToWorldVec2();

				int walkResult = Unit.WalkTo(blueprintCenter);

				if (walkResult == -1)
				{
					// TODO: Handle unreachable point.
					return;
				}

				if (walkResult == 1)
				{
					// TODO: Temp.
					return;
				}

				if (walkResult == 0)
				{
					Rect trigger = new Rect(blueprint.mBounds.min.x - 0.5f, blueprint.mBounds.min.z - 0.5f, blueprint.mBounds.size.x + 1.0f, blueprint.mBounds.size.z + 1.0f);

					if (!trigger.Contains(Unit.mPosition))
						return;

					// TODO: Nasy fix here, seems there is a precision issue with TraceNodes, adding a tiny offset fixes it?
					if (!Unit.mWorld.mMap.TraceNodes(Unit.mOwner, Unit.mPosition, blueprintCenter + new Vector2(0.0001f, 0.0001f)))
						return;
				}

				Unit.CancelWalking();
				// Check if units in the way, if so, then tell them to fuck off.
				// Check for building timer when we build the item.
				// Start building the item.
				Unit.BuildItem();
				Unit.SetAction(EType.IDLE);
			}
		}
	}

	private static void _exit_Packup(CUnit Unit)
	{
		Unit.AbandonOrder();
	}

	//---------------------------------------------------------------------
	// Command Move
	//---------------------------------------------------------------------
	private static void _enter_CmdMove(CUnit Unit)
	{
		Unit.ResetPathCounter();
	}

	private static void _tick_CmdMove(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		//int r = Unit.WalkTo(Unit.mPlayerMoveOrderLocation, 0.01f);
		//Unit.mThoughts = "Can't reach that point!";
		//Unit.SetAction(EType.ANGRY);

		// TODO: Validate path.

		if (Unit.WalkTo(Unit.mPlayerMoveOrderLocation) != 0)
			Unit.SetAction(EType.IDLE);
	}

	private static void _exit_CmdMove(CUnit Unit)
	{
		//if (Unit.mAbstractNavPath != null)
			//Unit.CancelWalking();

		Unit.CancelWalking();
	}

	//---------------------------------------------------------------------
	// Command Claim Desk
	//---------------------------------------------------------------------
	private static void _enter_CmdClaim(CUnit Unit)
	{
	}

	private static void _tick_CmdClaim(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		/*
		CItemProxy deskProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mDeskToClaimProxyItemID);
		if (deskProxy == null)
		{
			Unit.mThoughts = "The desk I was trying to claim is gone!";
			Unit.SetAction(EType.ANGRY);
			return;
		}

		Vector2 pos = new Vector2(deskProxy.mPosition.x + 0.5f, deskProxy.mPosition.y + 0.5f);

		if (Unit.WalkTo(pos, (1.0f * 1.0f)) == 1)
		{
			CItemDesk desk = deskProxy.GetItem() as CItemDesk;
			if (desk == null)
			{
				Unit.mThoughts = "The desk I was trying to claim doesn't exist!";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Unit.mAssignedDeskID = desk.mID;
			//desk.AssignWorker(Unit);
			Unit.SetAction(EType.IDLE);
			return;
		}
		*/
	}

	private static void _exit_CmdClaim(CUnit Unit)
	{
		//Unit.mDeskToClaimProxyItemID = -1;
	}

	//---------------------------------------------------------------------
	// Sit At Desk
	//---------------------------------------------------------------------
	private static void _enter_Work(CUnit Unit)
	{
	}

	private static void _tick_Work(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;

		if (Unit.mStamina <= 20.0f)
		{
			Unit.SetAction(EType.REST);
			return;
		}

		CItemProxy deskProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mAssignedDeskID);
		if (deskProxy == null)
		{
			Unit.mThoughts = "The desk I was trying to work at is gone!";
			Unit.SetAction(EType.ANGRY);
			return;
		}

		// Are we already sitting at our desk?
		CItemDesk desk = deskProxy.GetItem() as CItemDesk;
		if (desk != null && Unit.mUsedItemID == desk.mID)
		{
			// Move papers if we have no work or papers are full.
			if (desk.mCompletedStacks.Count > 0)
			{
				if (desk.mCompletedStacks.Count >= desk.mMaxCompletedPapers || deskProxy.mAssignedPaperStacks.Count == 0)
				{
					Unit.mThoughts = "Clearing completed papers on desk.";
					Unit.SetAction(EType.COLLECT_PAPERS);
					return;
				}
			}

			if (Unit.mWorkTotalIdleTime <= 0.0f)
			{
				Unit.mWorkIdleTimer += CWorld.SECONDS_PER_TICK;

				if (Unit.mWorkIdleTimer >= (int)(Unit.mStress / 10) + 10)
				{
					Unit.mWorkIdleTimer = 0;
					Unit.mWorkTotalIdleTime += Unit.mStats.mIdleTime;
				}

				float workRate = Unit.mStats.mIntelligence * CWorld.SECONDS_PER_TICK;
				if (deskProxy.DoWork(workRate))
				{
					Unit.AdjustExperience(workRate);
					// TODO: Stress should be affected by comfort, desk & local comfort heatmap.
					Unit.AdjustStress(1 * CWorld.SECONDS_PER_TICK);
					Unit.AdjustStamina(-Unit.mStats.mWorkStaminaRate * CWorld.SECONDS_PER_TICK);

					float t = 0.0f;

					if (Unit.mStress >= 66)
					{
						Unit.mActionAnim = "work_working_action_3a";
						t = Unit.mStress - 66;
					}
					else if (Unit.mStress >= 33)
					{
						Unit.mActionAnim = "work_working_action_2";
						t = Unit.mStress - 33;
					}
					else
					{
						Unit.mActionAnim = "work_working_action_1a";
						t = Unit.mStress;
					}

					t = 1 + (t / 33.0f);
					Unit.mAnimSpeed = t;
				}
				else
				{
					Unit.mActionAnim = "idle_sitting_action_1";
				}
			}
			else
			{
				Unit.mActionAnim = "idle_sitting_general";
				Unit.mWorkTotalIdleTime -= CWorld.SECONDS_PER_TICK;
			}

			return;
		}

		// TODO: What happens if the desk we are stting at (but not using) is destroyed? Is that possible?
		// TODO: We need to check if someone is using the desk we want to use.

		Vector2 pos = deskProxy.mBounds.center.ToWorldVec2();
		int walkResult = Unit.WalkTo(pos, deskProxy.mID);

		if (walkResult == -1)
		{
			// TODO: Can't reach it?
			return;
		}

		if (walkResult == 0 || walkResult == 1)
		{
			Rect trigger = new Rect(deskProxy.mBounds.min.x - 0.3f, deskProxy.mBounds.min.z - 0.3f, deskProxy.mBounds.size.x + 0.6f, deskProxy.mBounds.size.z + 0.6f);
			if (trigger.Contains(Unit.mPosition))
			{
				Unit.CancelWalking();

				if (desk == null)
				{
					Unit.mThoughts = "The desk I was trying to work at doesn't exist!";
					Unit.SetAction(EType.ANGRY);
					return;
				}

				desk.mUserUnitID = Unit.mID;
				Unit.mUsedItemID = desk.mID;
				Unit.mCollide = false;
				Unit.mPosition = desk.ItemToWorldSpacePosition(desk.mAsset.mUsageSlots[0].mUsePosition.ToWorldVec2());
				Unit.mRotation = Quaternion.AngleAxis(desk.ItemToWorldSpaceRotation(desk.mAsset.mUsageSlots[0].mEntryRotation.y), Vector3.up);
				Unit.mActionAnim = "idle_sitting_general";
			}
		}
	}

	private static void _exit_Work(CUnit Unit)
	{
		if (Unit.mUsedItemID != -1)
		{
			CItem item = Unit.mWorld.GetEntity<CItem>(Unit.mUsedItemID);

			if (item != null)
				item.mUserUnitID = -1;

			Unit.mUsedItemID = -1;

			// TODO: Move to sensible exit posision
		}

		Unit.mActionAnim = "";
		Unit.mCollide = true;
		Unit.mAnimSpeed = 1.0f;
	}

	//---------------------------------------------------------------------
	// Collect Papers
	//---------------------------------------------------------------------
	private static void _enter_CollectPapers(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.APPROACH_ITEM;
	}

	private static void _tick_CollectPapers(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;

		CItemProxy deskProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mAssignedDeskID);
		if (deskProxy == null)
		{
			Unit.mThoughts = "The desk I was trying to get to is gone!";
			Unit.SetAction(EType.ANGRY);
			return;
		}

		if (deskProxy.mCompletedPaperStacks == 0)
		{
			Unit.mThoughts = "The desk I was collecting papers from now has no papers!";
			Unit.SetAction(EType.ANGRY);
			return;
		}

		if (Unit.mActionPhase == EPhase.APPROACH_ITEM)
		{
			Vector2 pos = deskProxy.mBounds.center.ToWorldVec2();
			int walkResult = Unit.WalkTo(pos, deskProxy.mID);

			if (walkResult == -1)
			{
				// TODO: Can't reach it?
				return;
			}

			if (walkResult == 0 || walkResult == 1)
			{
				Rect trigger = new Rect(deskProxy.mBounds.min.x - 0.3f, deskProxy.mBounds.min.z - 0.3f, deskProxy.mBounds.size.x + 0.6f, deskProxy.mBounds.size.z + 0.6f);
				if (trigger.Contains(Unit.mPosition))
				{
					Unit.CancelWalking();
					Unit.mActionPhase = EPhase.USE_ITEM;
					Unit.mCollide = false;
					Unit.mActionTempTimer = 60;
					Unit.mActionAnim = "use_object_standing";
				}
			}

			/*
			Vector2 pos = new Vector2(deskProxy.mPosition.x + 0.5f, deskProxy.mPosition.y + 0.5f);
			float range = 2.0f;
			if (Unit.WalkTo(pos, range * range) == 1)
			{
				Unit.mActionPhase = EPhase.USE_ITEM;
				Unit.mCollide = false;
				Unit.mActionTempTimer = 60;
				Unit.mActionAnim = "use_object_standing";
			}
			*/
		}

		if (Unit.mActionPhase == EPhase.USE_ITEM)
		{
			if (Unit.mActionTempTimer-- <= 0)
			{
				CItemDesk desk = deskProxy.GetItem() as CItemDesk;
				if (desk == null)
				{
					Unit.mThoughts = "The desk I was collecting papers from doesn't exist!";
					Unit.SetAction(EType.ANGRY);
					return;
				}

				int takenPapers = desk.TakeCompletedPaper();
				Unit.mPapersCarried += takenPapers;

				Unit.SetAction(EType.DELIVER_PAPERS);
				return;
			}
		}
	}

	private static void _exit_CollectPapers(CUnit Unit)
	{
		Unit.mCollide = true;
		Unit.mActionAnim = "";
	}

	//---------------------------------------------------------------------
	// Deliver Papers
	//---------------------------------------------------------------------
	private static void _enter_DeliverPapers(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.GET_TOKEN;
	}

	private static void _tick_DeliverPapers(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (_checkHunger(Unit, true))
			return;

		if (Unit.mActionPhase == EPhase.GET_TOKEN)
		{
			CQueueToken token = Unit.mWorld.QueueForClosestItem(EItemType.SAFE, Unit.mPosition, Unit.mOwner);

			if (token == null)
			{
				Unit.mThoughts = "I Couldn't find a safe to deliver papers.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Unit.SetQueueToken(token);
			Unit.mActionPhase = EPhase.MANAGE_TOKEN;
		}

		if (Unit.mActionPhase == EPhase.MANAGE_TOKEN)
		{
			if (Unit.mQueueToken.mExpired)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then I was kicked out.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			CItemProxy itemProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mQueueToken.mItemProxyID);

			if (itemProxy == null)
			{
				Unit.mThoughts = "I was in the queue for a safe, but then it was destroyed.";
				Unit.SetAction(EType.ANGRY);
				return;
			}

			Vector2 pos = itemProxy.mBounds.center.ToWorldVec2();

			if (Unit.mQueueToken.mUsageSlot == -1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 2.0f, itemProxy.mBounds.min.z - 2.0f, itemProxy.mBounds.size.x + 4.0f, itemProxy.mBounds.size.z + 4.0f);
				if (!trigger.Contains(Unit.mPosition))
					Unit.WalkTo(pos, itemProxy.mID);
				else
					Unit.CancelWalking();

				return;
			}

			int walkResult = Unit.WalkTo(pos, itemProxy.mID);

			if (walkResult == -1)
			{
				// TODO: Can't reach it?
			}

			if (walkResult == 0 || walkResult == 1)
			{
				Rect trigger = new Rect(itemProxy.mBounds.min.x - 0.3f, itemProxy.mBounds.min.z - 0.3f, itemProxy.mBounds.size.x + 0.6f, itemProxy.mBounds.size.z + 0.6f);
				if (trigger.Contains(Unit.mPosition))
				{
					Unit.CancelWalking();

					CItem item = itemProxy.GetItem();
					if (item == null)
					{
						Unit.mThoughts = "I was using a safe but it doesn't exist!";
						Unit.SetAction(EType.ANGRY);
						return;
					}

					item.mUserUnitID = Unit.mID;
					Unit.mUsedItemID = item.mID;
					Unit.ReturnQueueToken();
					Unit.mCollide = false;
					Unit.mActionPhase = EPhase.USE_ITEM;
					Unit.mCanAcceptPlayerCommands = false;
					Unit.mActionTempTimer = 60;
					Unit.mActionAnim = "use_object_standing";
				}
			}
		}

		if (Unit.mActionPhase == EPhase.USE_ITEM)
		{
			if (Unit.mActionTempTimer-- <= 0)
			{
				CItemSafe safe = Unit.mWorld.GetEntity<CItemSafe>(Unit.mUsedItemID);
				if (safe == null)
				{
					Unit.mThoughts = "I was using a safe but it doesn't exist!";
					Unit.SetAction(EType.ANGRY);
					return;
				}

				safe.mValue += (int)Unit.mPapersCarried;
				Unit.mPapersCarried = 0;

				Unit.SetAction(EType.IDLE);
				return;
			}
		}
	}

	private static void _exit_DeliverPapers(CUnit Unit)
	{
		Unit.ReturnQueueToken();

		if (Unit.mUsedItemID != -1)
		{
			CItem item = Unit.mWorld.GetEntity<CItem>(Unit.mUsedItemID);

			if (item != null)
				item.mUserUnitID = -1;

			Unit.mUsedItemID = -1;
		}

		Unit.mActionAnim = "";
		Unit.mCanAcceptPlayerCommands = true;
		Unit.mCollide = true;
	}

	//---------------------------------------------------------------------
	// Combat
	//---------------------------------------------------------------------
	private static void _enter_Combat(CUnit Unit)
	{
		Unit.mActionPhase = EPhase.COMBAT_VERIFY;
	}

	private static void _tick_Combat(CUnit Unit)
	{
		Unit.AdjustHunger(Unit.mStats.mHungerRate * CWorld.SECONDS_PER_TICK);

		if (Unit.mActionPhase == EPhase.COMBAT_VERIFY)
		{
			CItemProxy itemProxyTarget = null;
			CUnit unitTarget = null;

			// Pick most desired target. (Player instructed or AI decided)
			Unit.mEngagedTargetID = -1;

			if (Unit.mForceAttackItemProxyID != -1)
			{
				if ((itemProxyTarget = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mForceAttackItemProxyID)) != null)
				{
					Unit.mEngagedTargetID = Unit.mForceAttackItemProxyID;
					Unit.mEngagedTargetType = CUnit.ETargetType.ITEM_PROXY;
				}
				else
				{
					Unit.mForceAttackItemProxyID = -1;
				}
			}

			if (Unit.mEngagedTargetID == -1 && Unit.mClosestVisibleEnemyID != -1)
			{
				if ((unitTarget = Unit.mWorld.GetEntity<CUnit>(Unit.mClosestVisibleEnemyID)) != null)
				{
					Unit.mEngagedTargetID = Unit.mClosestVisibleEnemyID;
					Unit.mEngagedTargetType = CUnit.ETargetType.UNIT;
				}
				else
				{
					Unit.mClosestVisibleEnemyID = -1;
				}
			}

			if (Unit.mEngagedTargetID == -1)
			{
				Unit.mEngagedTargetID = -1;
				Unit.mThoughts = "No Target";
				Unit.SetAction(EType.IDLE);
				return;
			}

			Unit.mTargetPosition = Vector2.zero;

			if (Unit.mEngagedTargetType == CUnit.ETargetType.ITEM_PROXY)
			{
				//Unit.mTargetPosition = new Vector2(itemProxyTarget.mPosition.x + 0.5f, itemProxyTarget.mPosition.y + 0.5f);
				Unit.mTargetPosition = itemProxyTarget.mBounds.center.ToWorldVec2();
			}
			else if (Unit.mEngagedTargetType == CUnit.ETargetType.UNIT)
			{
				Unit.mTargetPosition = unitTarget.mPosition;
			}

			if (Unit.mCombatType == CUnit.ECombatType.RANGED)
			{
				// Since we are ranged, we just want to get close enough to both see the target
				// and be within combat range.
				float range = 6.0f;
				if ((Unit.mPosition - Unit.mTargetPosition).sqrMagnitude > range * range)
				{
					if (Unit.WalkTo(Unit.mTargetPosition) != 1)
					{
						return;
					}
				}
			}
			else if (Unit.mCombatType == CUnit.ECombatType.MELEE)
			{
				if (Unit.mEngagedTargetType == CUnit.ETargetType.UNIT)
				{
					// Just get within combat range of the target.
					// TODO: What happens when we see the enemy but can't path there, some kind of retreat/taunt state?
					if (Unit.WalkTo(Unit.mTargetPosition) != 1)
						return;
				}
				else
				{
					// TODO: Determine if this item is reachable from the same region as this unit. If so, then path to it and ignore
					int walkResult = Unit.WalkTo(Unit.mTargetPosition, itemProxyTarget.mID);

					if (walkResult == -1)
					{
						// TODO: Handle unreachable point.
						return;
					}

					if (walkResult == 1)
					{
						// TODO: We should never be able to get this close D: But we'll accept for now.
					}

					if (walkResult == 0)
					{
						Rect trigger = new Rect(itemProxyTarget.mBounds.min.x - 0.3f, itemProxyTarget.mBounds.min.z - 0.3f, itemProxyTarget.mBounds.size.x + 0.6f, itemProxyTarget.mBounds.size.z + 0.6f);

						if (!trigger.Contains(Unit.mPosition))
							return;

						// TODO: Attack needs to confirm LOS with some portion of the structure.
						// Can't just go for item centre, might not be visible while some other portion is.

						// TODO: Nasy fix here, seems there is a precision issue with TraceNodes, adding a tiny offset fixes it?
						//if (!Unit.mWorld.mMap.TraceNodes(Unit.mOwner, Unit.mPosition, itemCenter + new Vector2(0.0001f, 0.0001f)))
							//return;
					}
				}
			}

			Unit.CancelWalking();

			// Track target
			Vector2 dir = Unit.mTargetPosition - Unit.mPosition;
			Unit.mRotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.y));

			// Determine what attack can be performed
			if (Unit.mWorld.mGameTick >= Unit.mAttackTimeout)
			{
				if (Unit.mCombatType == CUnit.ECombatType.RANGED)
				{
					Unit.mActionPhase = EPhase.COMBAT_RANGED;
					Unit.mActionTempTimer = 0;
					Unit.mActionAnim = "combat_ranged";
					Unit.mAttackTimeout = Unit.mWorld.mGameTick + 15;
				}
				else
				{
					Unit.mActionPhase = EPhase.COMBAT_MELEE;
					Unit.mActionTempTimer = 0;
					Unit.mActionAnim = "combat_bashing";
					Unit.mAttackTimeout = Unit.mWorld.mGameTick + 30;
				}
			}
		}

		if (Unit.mActionPhase == EPhase.COMBAT_MELEE)
		{
			++Unit.mActionTempTimer;
			if (Unit.mActionTempTimer == 10)
			{
				Unit.PlayAttack();
			}
			else if (Unit.mActionTempTimer == 15)
			{
				if (Unit.mEngagedTargetType == CUnit.ETargetType.ITEM_PROXY)
				{
					CItemProxy targetItemProxy = Unit.mWorld.GetItemPorxy(Unit.mOwner, Unit.mEngagedTargetID);
					if (targetItemProxy != null && targetItemProxy.GetItem() != null)
					{
						CItem targetItem = targetItemProxy.GetItem();
						targetItem.TakeDamage(Unit.mStats.mAttackDamage, Vector2.zero);
					}
				}
				else if (Unit.mEngagedTargetType == CUnit.ETargetType.UNIT)
				{
					CUnit targetUnit = Unit.mWorld.GetEntity<CUnit>(Unit.mEngagedTargetID);
					if (targetUnit != null)
					{
						targetUnit.TakeDamage(Unit.mStats.mAttackDamage, Vector2.zero);
					}
				}
			}
			else if (Unit.mActionTempTimer == 20)
			{
				Unit.mActionTempTimer = 0;
				Unit.mActionPhase = EPhase.COMBAT_VERIFY;
				Unit.mActionAnim = "";
				return;
			}
		}

		if (Unit.mActionPhase == EPhase.COMBAT_RANGED)
		{
			++Unit.mActionTempTimer;
			if (Unit.mActionTempTimer == 8)
			{
				//Unit.mWorld.SpawnMissile(Unit.mOwner, Unit.mPosition.ToWorldVec3() + new Vector3(0, 0.5f, 0), Unit.mRotation * Vector3.forward);
				//Unit.mWorld.SpawnMissile(Unit.mOwner, Unit.mPosition.ToWorldVec3() + new Vector3(0, 0.5f, 0), Unit.mTargetPosition.ToWorldVec3() + new Vector3(0, 0.5f, 0));
				//Unit.PlayAttack();

				if (Unit.mEngagedTargetType == CUnit.ETargetType.UNIT)
				{
					CUnit targetUnit = Unit.mWorld.GetEntity<CUnit>(Unit.mEngagedTargetID);
					if (targetUnit != null)
					{
						// Lead target
						Unit.mWorld.SpawnMissile(Unit.mOwner, Unit.mPosition.ToWorldVec3() + new Vector3(0, 0.5f, 0), targetUnit.mPosition.ToWorldVec3() + new Vector3(0, 0.5f, 0));
						Unit.PlayAttack();
					}
					else
					{
						// TODO: Cancel out of state.
					}
				}
			}
			else if (Unit.mActionTempTimer == 12)
			{
				Unit.mActionTempTimer = 0;
				Unit.mActionPhase = EPhase.COMBAT_VERIFY;
				Unit.mActionAnim = "";
				return;
			}
		}
	}

	private static void _exit_Combat(CUnit Unit)
	{
		Unit.mEngagedTargetID = -1;
		Unit.mActionAnim = "";
	}
}
