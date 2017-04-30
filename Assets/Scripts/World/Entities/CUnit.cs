using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct SUnitBasicStats
{
	public string mName;
	public int mLevel;
	public int mTier;
	public float mExperience;

	public float mMaxStamina;
	public float mMaxHunger;
	public float mMaxStress;
	public float mRestDesired;
	public float mRestForced;
	public float mEatDesired;
	public float mEatForced;
	public float mHungerRate;
	public float mIdleTime;
	public float mStressRecoveryRate;
	public float mEatRate;
	public float mIntelligence;
	public float mMaxSpeed;
	public float mSalary;
	public float mAttackDamage;
	public float mAttackSpeed;
	public float mDefense;
	public float mPaperCapacity;
	public float mWorkStaminaRate;
	public float mRequiredXP;
	public float mPromotionDemand;

	public void Seriallize()
	{

	}

	public void Deserialize()
	{

	}
}

/// <summary>
/// Humanoid actor in the simulation.
/// </summary>
public class CUnit : CEntity
{
	public enum EThoughtState
	{
		UNKNOWN,
		IDLE,
		FOLLOWING_MOVE_ORDER,
		WORKING,
		BUILDING,
		DELIVERING_FOOD,
		RESTING,
		EATING,
		FIGHTING,
		BREAK_ITEM,
		COLLECTING_SALARY,
		FRUSTRATED,
		QUITTING,
		DEAD
	}

	public enum ECombatType
	{
		MELEE,
		RANGED
	}

	public enum ETargetType
	{
		ITEM_PROXY,
		UNIT
	}
	
	public void SetAction(CUnitActions.EType ActionID)
	{
		//Debug.Log("Set Action " + ActionID + " (" + mThoughts + ")");

		if (CUnitActions.actionTable[(int)mActionID].mExit != null)
			CUnitActions.actionTable[(int)mActionID].mExit(this);

		mThoughts = "";
		mActionID = ActionID;
		mActionPhase = CUnitActions.EPhase.NONE;

		if (CUnitActions.actionTable[(int)mActionID].mEnter != null)
			CUnitActions.actionTable[(int)mActionID].mEnter(this);

		// TOOD: Do at least 1 tick of new action?
		// Beware of loops.
	}

	public void TickAction()
	{
		if (mActionID == CUnitActions.EType.NONE)
			SetAction(CUnitActions.EType.IDLE);

		if (CUnitActions.actionTable[(int)mActionID].mUpdate != null)
			CUnitActions.actionTable[(int)mActionID].mUpdate(this);
	}

	// Core Stats
	public SUnitBasicStats mStats;

	// Simulation Properties
	public string mName;
	public bool mIntern;
	public bool mDead;
	public bool mCollide;
	public bool mFrustrated;
	public int mCollectedSalary;
	public int mOwedSalary;
	public int mOwedBonus;
	public float mStamina;
	public float mStress;
	public float mHunger;
	public float mSpeed;
	public float mPromotionCounter;
	public int mPromotionTimeout;
	public int mQuitCounter;
	public float mStressPromoteDemandTimer;

	public bool mCanAcceptTask;
	public bool mCanAcceptPlayerCommands;

	public Vector2 mPosition;
	public Quaternion mRotation;
	public Bounds mBounds;
	public Vector2 mBeingPushedForce;
	public Vector2 mDirection;

	public int mPlayerVisibility;

	public string mThoughts;
	public EThoughtState mThoughtState;
	
	// Animation
	// TODO: Need to consolidate this with normal walking, just used as hack for elevator stuff.
	public bool mAnimWalk;
	public string mActionAnim;
	public float mAnimSpeed;
	public int mActionAnimStartTime;
	
	// Action State
	public CUnitActions.EType mActionID;
	public CUnitActions.EPhase mActionPhase;
	public int mActionTick;
	public int mActionTempTimer;
	public float mWorkIdleTimer;
	public float mWorkTotalIdleTime;
	public Vector2 mActionTempVec2;

	// Navigation
	public CNRSearchNode mNRNavPath;
	public Vector2 mPathDest;
	private int _pathCounter;
	public bool mPathing;
	
	// Sitting
	public int mSittingEntityID;
	public int mSittingUsageSlotID;
		
	// Tasks
	public CBuildOrder mOrder;
	public CPickup mCarryingPickup;
	public Vector2 mPlayerMoveOrderLocation;

	// Item Usage
	// TODO: Maybe remove the concept of a queue token. Have it more explicitly defined in the unit state.
	public CQueueToken mQueueToken;
	public int mUsedItemID;
	public int mUseTick;

	// Player Commands
	// TODO: need to transfer desired target to actual engaging target.
	public int mForceAttackItemProxyID;
	//public int mDeskToClaimProxyItemID;

	// Work
	public int mAssignedDeskID;
	public float mPapersCarried;

	// Food/Rest
	public float mFoodRate;

	// Combat
	public ECombatType mCombatType;
	public float mCombatRange;
	public int mAttackTimeout;
	public bool mEngagingEnemey;
	public bool mAttacking;
	public int mClosestVisibleEnemyID;
	public int mTargetEnemyID;
	public float mSqrCombatViewDistance;
	public Vector2 mTargetPosition;

	public int mEngagedTargetID;
	public ETargetType mEngagedTargetType;

	public string mSpeech;
	public int mSpeechTimeout;

	// State View
	public CUnitView mStateView = null;
	
	public override bool IsUnit() { return true; }

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.UNIT;

		mDead = false;
		mCollide = true;
		mAssignedDeskID = -1;		
		mOwedSalary = 0;
		mCollectedSalary = 0;
		mThoughts = "\"My mind is clear.\"";
		mActionAnim = "";
		mAnimSpeed = 1.0f;
		mUsedItemID = -1;
		mEngagedTargetID = -1;
		mForceAttackItemProxyID = -1;
		mCombatType = ECombatType.MELEE;
		mPromotionCounter = 0;
		mPromotionTimeout = 0;
		mQuitCounter = 0;
		mStressPromoteDemandTimer = 0;
		SetThoughtState(EThoughtState.IDLE);
	}

	/// <summary>
	/// Cleanly destroy the queue token we may posses.
	/// </summary>
	public void ReturnQueueToken()
	{	
		if (mQueueToken != null)
		{
			CItemProxy proxy = mWorld.GetItemPorxy(mOwner, mQueueToken.mItemProxyID);

			if (proxy != null)
				proxy.DequeueQueueToken(mQueueToken);
		}

		mQueueToken = null;
	}

	/// <summary>
	/// Get the queue token and clean it up if expired.
	/// </summary>
	public CQueueToken GetQueueToken()
	{
		if (mQueueToken != null && mQueueToken.mExpired)
			ReturnQueueToken();

		return mQueueToken;
	}

	/// <summary>
	/// Cleanly set a new token, discarding the old one.
	/// </summary>
	public void SetQueueToken(CQueueToken Token)
	{
		ReturnQueueToken();
		mQueueToken = Token;
	}

	/*
	/// <summary>
	/// 
	/// </summary>
	public void InitiateItemUse(int ItemID)
	{
		// What if we are already using an item??

		CItem item = mWorld.GetEntity<CItem>(ItemID);

		if (item == null)
		{
			mUsedItemID = -1;
			return;
		}

		mUsedItemID = ItemID;
	}

	/// <summary>
	/// Make sure we are not using an item.
	/// </summary>
	public void StopUsingItem()
	{
		if (mUsedItemID != -1)
		{
			CItem item = mWorld.GetEntity<CItem>(ItemID);

			if (item != null)
				item.u
		}

		mUsedItemID = -1;
	}
	*/
	
	public override void Destroy()
	{
		base.Destroy();
	}

	public void InitUnit(CResume Resume)
	{
		mStats = Resume.mStats;

		mName = mStats.mName;
		mIntern = (mStats.mTier == 0);
		mStamina = mStats.mMaxStamina;
		mStress = 0.0f;
		mHunger = 0.0f;
		mSqrCombatViewDistance = 7 * 7;
		mSpeed = mStats.mMaxSpeed;

		if (mIntern) // || mWorld.SimRnd.GetNextFloat() >= 0.5f)
			mCombatType = ECombatType.MELEE;
		else
			mCombatType = ECombatType.RANGED;
	}

	public void SetPosition(float X, float Y)
	{
		mPosition = new Vector2(X, Y);
		CalcBounds();
	}

	public void SetRotation(int Rot)
	{
		mRotation = Quaternion.AngleAxis(Rot, Vector3.up);
	}

	public void CalcBounds()
	{
		mBounds = new Bounds(new Vector3(mPosition.x, 0.6f, mPosition.y), new Vector3(0.5f, 1.2f, 0.5f));
	}
	
	public void SetThoughtState(EThoughtState State)
	{
		mThoughtState = State;
	}

	public void SetBuildOrder(CBuildOrder Order)
	{
		if (!mCanAcceptTask || mOrder != null || Order == null)
		{
			Debug.LogError("Unit SetBuildOrder Problem.");
			return;
		}

		mOrder = Order;
		SetAction(CUnitActions.EType.TASK_BUILD);
	}

	public void SetSpeech(string Text)
	{
		mSpeech = Text;
		mSpeechTimeout = mWorld.mGameTick + CWorld.TICKS_PER_SECOND * 3;
	}

	/// <summary>
	/// Tells this unit that he can pickup some salary!
	/// </summary>
	public void CollectSalary()
	{
		mOwedSalary += (int)mStats.mSalary + mOwedBonus;
		mOwedBonus = 0;
		mPromotionCounter += mStats.mPromotionDemand;
	}
	
	/*
	public override void PostDeserialize()
	{
		base.PostDeserialize();
		SetOwner(mOwner);
	}
	*/

	/// <summary>
	/// Trigger the attack audio/visuals.
	/// </summary>
	public void PlayAttack()
	{
		// Trigger attack animation at game tick time X
		// The surface will have to catch up when it can.
		// Force start frame of animation in relation to when we started? CAN IT BE DONE?

		mWorld.AddTransientEventFOW(mPosition).SetSound(mPosition.ToWorldVec3(), 4);
	}

	/// <summary>
	/// Perform updates on simulation tick.
	/// </summary>
	public override void SimTick()
	{
		if (!mActive)
			return;

		mPlayerVisibility = mWorld.GetPlayerFlagsForTileVisibility(mPosition);

		++_pathCounter;
		_Unstick();
		_CheckNearbyUnits();
		TickAction();
		// NOTE: It's possible for the tick action to Destroy() this unit.
		_UpdatePosition();
		
		if (mSpeech != "" && mWorld.mGameTick >= mSpeechTimeout)
			mSpeech = "";

		if (mStress >= 100.0f)
		{
			mStressPromoteDemandTimer += CWorld.SECONDS_PER_TICK;

			if (mStressPromoteDemandTimer >= 20.0f)
			{
				mStressPromoteDemandTimer = 0.0f;
				mPromotionCounter += mStats.mPromotionDemand;
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

		if (!mCollide)
			return;

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

	/// <summary>
	/// Remove any interaction the unit has with the world.
	/// </summary>
	public void CleanupWorldState()
	{
		mCanAcceptPlayerCommands = false;
		mCanAcceptTask = false;

		ReturnQueueToken();

		if (mAssignedDeskID != -1)
		{
			CItemProxy deskProxy = mWorld.GetItemPorxy(mOwner, mAssignedDeskID);
			if (deskProxy != null)
				deskProxy.mAssignedUnitID = -1;

			mAssignedDeskID = -1;
		}

		DropPickup();
		CancelWalking();

		if (mOrder != null)
			mOrder.OnAbandoned();
	}

	/// <summary>
	/// Adjust experience.
	/// </summary>
	public void AdjustExperience(float Value)
	{
		CTierInfo tier = CGame.AssetManager.mUnitRules.GetTier(mStats.mTier);

		if (mStats.mLevel >= tier.mMaxLevel)
			return;

		mStats.mExperience += Value;

		if (mStats.mExperience >= mStats.mRequiredXP)
		{
			mStats.mExperience = 0;
			mStats.mLevel += 1;
			mStats.mSalary += tier.mSalary.mIncrease;
			mStats.mIntelligence += tier.mIntelligence.mIncrease;
			mStats.mMaxStamina += tier.mMaxStamina.mIncrease;
			mStats.mMaxSpeed += tier.mMaxSpeed.mIncrease;
			mStats.mHungerRate += tier.mHungerRate.mIncrease;
			mStats.mMaxHunger += tier.mMaxHunger.mIncrease;
			mStats.mPaperCapacity += tier.mPaperCapacity.mIncrease;
			mStats.mAttackDamage += tier.mAttackDamage.mIncrease;
			mStats.mDefense += tier.mDefense.mIncrease;
			mStats.mIdleTime += tier.mIdleTime.mIncrease;
			mStats.mWorkStaminaRate += tier.mWorkStaminaRate.mIncrease;
			mStats.mRequiredXP += tier.mRequiredXP.mIncrease;
			mStats.mPromotionDemand += tier.mPromotionDemand.mIncrease;
		}
	}

	/// <summary>
	/// Modify current stamina.
	/// </summary>	
	public void AdjustStamina(float Value)
	{
		mStamina += Value;
		mStamina = Mathf.Clamp(mStamina, 0.0f, mStats.mMaxStamina);

		if (mStamina <= 0.0f)
			SetAction(CUnitActions.EType.DIE);
	}

	/// <summary>
	/// Modify current stress.
	/// </summary>	
	public void AdjustStress(float Value)
	{
		mStress += Value;
		mStress = Mathf.Clamp(mStress, 0.0f, mStats.mMaxStress);
	}

	/// <summary>
	/// Modify hunger.
	/// </summary>
	public void AdjustHunger(float Value)
	{
		mHunger += Value;

		if (mHunger > mStats.mMaxHunger)
			AdjustStamina(mStats.mMaxHunger - mHunger);
		
		mHunger = Mathf.Clamp(mHunger, 0.0f, mStats.mMaxHunger);
	}

	public void TakeDamage(float Damage, Vector2 Force)
	{
		//mVisuals.PlaySound(2, 1.0f);

		Color c = Color.white;

		Damage -= mStats.mDefense;

		if (Damage < 0.0f)
			Damage = 0.0f;

		mBeingPushedForce += Force;

		//if (mOwner == CGame.mSingleton.mGameSession._currentPlayer)
			//c = new Color(0.5f, 0.0f, 0.0f);
		//CGame.mSingleton.mPinfoManager.CreateInfo(Damage.ToString(), new Vector3(mPosition.x, 2.0f, mPosition.y), c);

		AdjustStamina(-Damage);
	}

	/// <summary>
	/// Instruct unit to attack a target.
	/// </summary>
	public void CommandForceAttackItem(int ProxyID)
	{
		if (!mCanAcceptPlayerCommands)
			return;

		mForceAttackItemProxyID = ProxyID;
	}

	/// <summary>
	/// Instruct unit to claim a new desk.
	/// </summary>
	public void CommandClaimDesk(int DeskProxyID)
	{
		if (mIntern)
			return;

		// TODO: If we already have a desk then we need to un-claim it.
		if (mAssignedDeskID != -1)
		{
			CItemProxy oldDeskProxy = mWorld.GetItemPorxy(mOwner, mAssignedDeskID);
			if (oldDeskProxy != null)
				oldDeskProxy.mAssignedUnitID = -1;

			mAssignedDeskID = -1;
		}

		CItemProxy deskProxy = mWorld.GetItemPorxy(mOwner, DeskProxyID);
		if (deskProxy != null)
		{
			deskProxy.AssignWorker(this);
		}
	}

	/// <summary>
	/// Instruct unit to move to location.
	/// </summary>
	public void CommandMoveTo(Vector2 Position)
	{
		if (!mCanAcceptPlayerCommands)
			return;

		// TODO: Move should be intelligent if there is a blockage where the user clicks.
		// Move should attempt closest location to where user clicked. Radiating area dest?
		//if (mMap.mLocalCollisionTiles[action.mPlayerID][action.mX, action.mY].mSolid)// mTiles[action.mX, action.mY].mFloor.mSolid)
		mPlayerMoveOrderLocation = Position;
		mForceAttackItemProxyID = -1;

		SetAction(CUnitActions.EType.CMD_MOVE);
	}

	/// <summary>
	/// Fire this unit.
	/// </summary>
	public void Fire()
	{
		mOwedSalary += (int)(mStats.mSalary);
		mWorld.mPlayers[mOwner].Spend(mOwedSalary);
		mOwedSalary = 0;

		SetAction(CUnitActions.EType.QUIT);
	}

	/// <summary>
	/// Promote this unit.
	/// </summary>
	public void Promote()
	{
		if (mIntern)
			return;

		CTierInfo tier = CGame.AssetManager.mUnitRules.GetTier(mStats.mTier);
		CTierInfo nextTier = CGame.AssetManager.mUnitRules.GetTier(mStats.mTier + 1);

		if (nextTier != tier)
		{
			mStats.mTier = nextTier.mTierIndex;
			mStats.mLevel = 1;
			mStats.mPromotionDemand = 1;

			if (tier.mSalary.Get(1) > mStats.mSalary) mStats.mSalary += tier.mSalary.mIncrease;
			if (tier.mPaperCapacity.Get(1) > mStats.mPaperCapacity) mStats.mPaperCapacity += tier.mPaperCapacity.mIncrease;
			if (tier.mAttackDamage.Get(1) > mStats.mAttackDamage) mStats.mAttackDamage += tier.mAttackDamage.mIncrease;
			if (tier.mIdleTime.Get(1) > mStats.mIdleTime) mStats.mIdleTime += tier.mIdleTime.mIncrease;
			if (tier.mWorkStaminaRate.Get(1) > mStats.mWorkStaminaRate) mStats.mWorkStaminaRate += tier.mWorkStaminaRate.mIncrease;
			if (tier.mRequiredXP.Get(1) > mStats.mRequiredXP) mStats.mRequiredXP += tier.mRequiredXP.mIncrease;

			mWorld.AddTransientEventFOW(mPosition).SetEffect(mPosition.ToWorldVec3());
		}

		mPromotionCounter = 0;
		mStress = 0;
		mQuitCounter = 0;
	}

	public void GiveBonus()
	{
		mOwedBonus += (int)(mStats.mSalary * 0.15f);
		mPromotionCounter -= 20;
		if (mPromotionCounter < 0) mPromotionCounter = 0;
		++mQuitCounter;

		mWorld.AddTransientEventFOW(mPosition).SetEffect(mPosition.ToWorldVec3());
	}

	public void GiveRaise()
	{
		mStats.mSalary += mStats.mSalary * 0.1f;
		mPromotionCounter -= 10;
		if (mPromotionCounter < 0) mPromotionCounter = 0;
		mQuitCounter -= 2;
		if (mQuitCounter < 0) mQuitCounter = 0;
		AdjustStress(-40);

		mWorld.AddTransientEventFOW(mPosition).SetEffect(mPosition.ToWorldVec3());
	}
	
	/// <summary>
	/// Pick up a pickup item.
	/// </summary>
	public void PickupPickup(CPickup Pickup)
	{
		Pickup.PickUp(mID);
		mCarryingPickup = Pickup;
	}

	/// <summary>
	/// Drop the carried pickup.
	/// </summary>
	public void DropPickup()
	{
		if (mCarryingPickup != null)
		{
			mCarryingPickup.Drop(mPosition);
			mCarryingPickup = null;
		}
	}

	public bool IsVisibleToPlayer(int PlayerID)
	{
		return (mPlayerVisibility & (1 << PlayerID)) != 0;
	}

	/// <summary>
	/// Construct the item in the build order.
	/// </summary>
	public void BuildItem()
	{
		// TODO: Need to check if item can be built at this spot

		CItem blueprint = mWorld.GetEntity<CItem>(mOrder.mItemBlueprintID);

		blueprint.UpdatePlaceability();
		if (blueprint.mPlaceable)
		{
			Vector3 pos = CItem.CalculateBounds(blueprint.mPosition, blueprint.mItemRot, blueprint.mAsset.mWidth, blueprint.mAsset.mLength).center;
			pos.y = 0.0f;
			mWorld.AddTransientEventFOW(new Vector2(pos.x, pos.z)).SetEffect(pos);

			mWorld.PromoteBlueprintToItem(blueprint);

			// Dispose of the carried pickup.
			mWorld.DespawnEntity(mCarryingPickup);
			mCarryingPickup = null;
		}
		else
		{
			mWorld.DespawnEntity(blueprint);
			DropPickup();
		}

		// Tell order we are done with it.
		mOrder.OnCompleted();
		mOrder = null;
		//AdjustStamina(-CGame.Datastore.mGame.mBuildStamina);
	}

	/// <summary>
	/// The order could not be completed so we just abandon it.
	/// </summary>
	public void AbandonOrder()
	{
		// TODO: Should maybe tell the order about this and let it handle things.
		DropPickup();

		if (mOrder != null)
		{
			mOrder.OnAbandoned();
			mOrder = null;
		}
	}

	private void _UpdatePosition()
	{
		if (!mActive)
			return;

		Vector2 targetPos = mPosition;		

		if (mPathing)
		{
			// If we have a waypoint we must navigate to it.
			if (mNRNavPath != null)
			{
				while (mNRNavPath.mNextMove != null && CNavRectPather.IsNodeReachable(mWorld.mMap, mOwner, mPosition, mNRNavPath.mNextMove))
					mNRNavPath = mNRNavPath.mNextMove;

				// Face direction to target.
				// Movement Code
				//Vector2 target = new Vector2(mNavPath.mPosition.X * 0.5f + 0.25f, mNavPath.mPosition.Y * 0.5f + 0.25f);
				Vector2 target = mNRNavPath.mPosition;
				Vector2 dir = target - mPosition;

				//mDirection = Vector3.Slerp(mDirection.ToWorldVec3(), dir.normalized.ToWorldVec3(), 0.5f).ToWorldVec2();
				mDirection = dir.normalized;

				float speed = mSpeed;
				if (mCarryingPickup != null)
					speed = 2;
				speed *= CWorld.SECONDS_PER_TICK;

				//targetPos = mPosition + (mDirection * speed);

				// Target is closer than walk speed
				if (dir.SqrMagnitude() <= speed * speed)
				{
					targetPos = target;
				}
				else
				{
					targetPos = mPosition + (mDirection * speed);
				}

				mRotation = Quaternion.LookRotation(new Vector3(mDirection.x, 0.0f, mDirection.y));
				
				/*
				if (dir.magnitude > 0.1f)
				{	
					float speed = mSpeed;
					speed *= CWorld.SECONDS_PER_TICK;

					dir.Normalize();
					targetPos = mPosition + (dir * speed);
					mRotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.y));
				}
				*/
			}
			// Path to exact path location
			else
			{
				if (mPathDest != Vector2.zero)
				{
					// Movement Code
					Vector2 target = mPathDest;
					Vector2 dir = target - mPosition;

					if (dir.magnitude > 0.1f)
					{
						float speed = mSpeed;
						if (mCarryingPickup != null)
							speed = 2;
						speed *= CWorld.SECONDS_PER_TICK;

						dir.Normalize();
						targetPos = mPosition + (dir * speed);
						mRotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.y));
					}
					else
					{
						// Pathing complete
						mPathing = false;
						mPathDest = Vector2.zero;
					}
				}
			}
		}

		if (!mPathing && mCollide)
			targetPos += mBeingPushedForce;
		else
			mBeingPushedForce = Vector2.zero;

		if (mPosition != targetPos)
		{
			if (mCollide)
				mPosition = mWorld.mMap.Move(mPosition, targetPos, 0.15f);
			else
				// Move at unit speed to target
				mPosition = targetPos;
		}

		mBeingPushedForce *= 0.2f;

		// Check if we have hit the nav point
		/*
		if (mNavPath != null)
		{
			Vector2 target = new Vector2(mNavPath.mPosition.X * 0.5f + 0.25f, mNavPath.mPosition.Y * 0.5f + 0.25f);
			Vector2 dir = target - mPosition;
			if (dir.magnitude <= 0.1f)
			{
				mNavPath = mNavPath.mNextMove;
				mPosition = target;
			}
		}
		*/

		// Snap to waypoint if we are close enough?
		if (mNRNavPath != null)
		{
			Vector2 target = mNRNavPath.mPosition;
			Vector2 dir = target - mPosition;
			if (dir.magnitude <= 0.001f)
			{
				mNRNavPath = mNRNavPath.mNextMove;
				mPosition = target;
			}

			// TODO: Test for end of path and indicate such.
		}

		CalcBounds();		
	}

	/// <summary>
	/// Stop walking RIGHT FUCKING NOW.
	/// </summary>
	public void CancelWalking()
	{
		mPathDest = Vector2.zero;
		mPathing = false;
		mNRNavPath = null;
	}

	public void ResetPathCounter()
	{
		_pathCounter = 20;
	}

	public void StartForceWalk()
	{
		// Make sure standing up.
		CancelWalking();
		mCollide = false;
		mAnimWalk = true;
	}

	public void StopForceWalk()
	{
		mCollide = true;
		mAnimWalk = false;
	}
	
	/// <summary>
	/// Walks immediately and directly to point, no collision.
	/// </summary>
	public int ForceWalkTo(Vector2 Location, float SqrDistance)
	{
		if ((mPosition - Location).sqrMagnitude <= SqrDistance)
		{
			return 1;
		}
		else
		{
			Vector2 dir = (Location - mPosition).normalized;
			mPosition += dir * CWorld.SECONDS_PER_TICK;
			mRotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.y));
		}

		return 0;
	}

	/// <summary>
	/// Checks if within distance to a point.
	/// </summary>
	public bool IsWithinRange(Vector2 Location, float Distance)
	{
		float sqrDist = Distance * Distance;
		return ((mPosition - Location).sqrMagnitude <= sqrDist);
	}

	/// <summary>
	/// Drive locomotion to walk towards point.
	/// Returns:
	///  1 - arrived at location.
	///  0 - walking(pathing) to location.
	/// -1 - can't reach location.
	/// </summary>
	public int WalkTo(Vector2 Location, int OccupiedId = 0)
	{
		StandUp();

		// Destination position can be shifted, but can only be shifted by 0.25 max. Therefore a successful walk
		// is 0.3 from destination.
		if ((mPosition - Location).sqrMagnitude <= (0.3f * 0.3f))
		{
			CancelWalking();
			return 1;
		}

		if (_pathCounter >= 20)
		{
			_pathCounter = 0;

			//mNavPath = CPathFinder.FindPathSmoothed(mOwner, mWorld.mMap, mPosition, mPathDest);
			// TODO: Make sure owner is a valid player?
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			mNRNavPath = CNavRectPather.FindPath(mWorld.mMap.mNavMeshes[mOwner], mPosition, Location, OccupiedId);
			sw.Stop();
			//Debug.LogWarning("Path: " + sw.Elapsed.TotalMilliseconds + "ms");

			if (mNRNavPath != null)
			{
				// Eliminate first node because we are already standing on it
				//mNRNavPath = mNRNavPath.mNextMove;
				mPathing = true;
			}
			else
			{
				mPathing = false;
				mPathDest = Vector2.zero;
			}
		}

		return 0;
	}

	/// <summary>
	/// Sits us down at a slot. Doesn't matter how far the unit is from the slot.
	/// </summary>
	public bool SitDown(CItem.CUsageSlot Slot)
	{
		mCollide = false;
		mSittingEntityID = Slot.mItem.mID;
		mSittingUsageSlotID = Slot.mSlotID;
		mPosition = Slot.mPosition;
		mRotation = Slot.mRotation;

		return true;
	}

	/// <summary>
	/// Stand up from current sitting item. Move to closest best position.
	/// If the unit can't stand up then return false.
	/// </summary>
	public bool StandUp()
	{
		if (mSittingEntityID == 0)
			return true;

		CItem sittingItem = mWorld.GetEntity<CItem>(mSittingEntityID);
		if (sittingItem == null)
		{
			Debug.LogError("We're sitting on nothing?");
		}
		else
		{
			CItem.CUsageSlot slot = sittingItem.GetUsageSlot(mSittingUsageSlotID);

			if (slot == null)
			{
				Debug.LogError("We're sitting on something with no slot?");
			}
			else
			{
				// TODO: Exit at an entry point for this slot, not at slot position
				// Check if we can stand up at all. Iterate entry points, and find free one
				mPosition = slot.mPosition;
				mRotation = slot.mRotation;
				mCollide = true;
				mSittingEntityID = 0;
				mSittingUsageSlotID = -1;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Drive locomotion to sit at a usage slot.
	/// </summary>
	public bool SitAt(CItem.CUsageSlot TargetSlot, int EntryPointID)
	{
		if (mSittingEntityID == TargetSlot.mItem.mID && mSittingUsageSlotID == TargetSlot.mSlotID)
			return true;

		if (mSittingEntityID != 0 && mSittingEntityID != TargetSlot.mItem.mID)
		{
			// We are sitting somewhere, just not the target.
			if (!StandUp())
			{
				// Couldn't stand up! Maybe blocked.
			}
		}

		// Do we assume there is an acceptable path to this item when sit at is called?

		if (WalkTo(TargetSlot.mEntryPoints[EntryPointID].mPosition) == 1)
		{
			if (SitDown(TargetSlot))
				return true;
		}

		

		/*
			if (!mSitting)
			{
			}
			else
			{
				if (mSittingTimer < 20)
				{
					++mSittingTimer;

					if (mSittingTimer == 20)
					{
						mPosition = mUsageSlot.mPosition;
						mRotation = mUsageSlot.mRotation;
					}
				}
				else
				{
					return true;
				}
			}
		}
	*/
		return false;
	}

	/// <summary>
	/// Check for nearby units.
	/// </summary>
	private void _CheckNearbyUnits()
	{
		CUnit enemy = mWorld.GetNearbyEnemy(this);

		if (enemy != null)
			mClosestVisibleEnemyID = enemy.mID;
		else
			mClosestVisibleEnemyID = -1;
	}

	/// <summary>
	/// Returns true if this unit can see the specidifed unit.
	/// </summary>
	public bool CanSeeUnit(CUnit Unit)
	{
		Vector2 dir = Unit.mPosition - mPosition;
		float sqrDist = dir.sqrMagnitude;
		
		if (sqrDist <= mSqrCombatViewDistance && mWorld.mMap.IsPointVisible(mPosition, Unit.mPosition, dir.normalized))
			return true;
			
		return false;
	}
	
	public void DebugDrawPathing()
	{
		Vector3 dest = new Vector3(mPathDest.x, 0.0f, mPathDest.y);
		CDebug.DrawYRectQuad(dest, 0.3f, 0.3f, new Color(0.0f, 1.0f, 0.0f, 0.5f), false);

		CNRSearchNode node = mNRNavPath;

		if (node != null)
		{
			dest = node.mPosition.ToWorldVec3();
			CDebug.DrawLine(mPosition.ToWorldVec3(), dest, new Color(0.0f, 1.0f, 0.0f, 1.0f), false);
		}

		while (node != null)
		{
			dest = node.mPosition.ToWorldVec3();
			CDebug.DrawYRectQuad(dest, 0.2f, 0.2f, new Color(0.8f, 0.8f, 0.8f, 1.0f), false);

			if (node.mNextMove != null)
			{
				Vector3 nextDest = node.mNextMove.mPosition.ToWorldVec3();
				CDebug.DrawLine(dest, nextDest, new Color(1.0f, 1.0f, 1.0f, 1.0f), false);
			}

			node = node.mNextMove;
		}

		/*
		Vector3 dest = new Vector3(mPathDest.x, 0.0f, mPathDest.y);
		CDebug.DrawYRectQuad(dest, 0.3f, 0.3f, new Color(0.0f, 1.0f, 0.0f, 0.5f), false);

		CSearchNode node = mNavPath;
		
		if (node != null)
		{
			dest = new Vector3(node.mPosition.X * 0.5f + 0.25f, 0.0f, node.mPosition.Y * 0.5f + 0.25f);
			CDebug.DrawLine(new Vector3(mPosition.x, 0.0f, mPosition.y), dest, new Color(1.0f, 0.3f, 0.1f, 0.5f), false);
		}

		while (node != null)
		{
			dest = new Vector3(node.mPosition.X * 0.5f + 0.25f, 0.0f, node.mPosition.Y * 0.5f + 0.25f);
			CDebug.DrawYRectQuad(dest, 0.2f, 0.2f, new Color(1.0f, 0.3f, 0.1f, 0.5f), false);

			if (node.mNextMove!= null)
			{
				Vector3 nextDest = new Vector3(node.mNextMove.mPosition.X * 0.5f + 0.25f, 0.0f, node.mNextMove.mPosition.Y * 0.5f + 0.25f);
				CDebug.DrawLine(dest, nextDest, new Color(1.0f, 0.3f, 0.1f, 0.5f), false);
			}

			node = node.mNextMove;
		}
		*/
	}

	public int GetRemainingPaperCapactiy()
	{
		int capacity = (int)(mStats.mPaperCapacity - mPapersCarried);

		if (capacity < 0)
			capacity = 0;

		return capacity;
	}

	public override void DrawDebugPrims()
	{
		CDebug.DrawLine(new Vector3(mPosition.x, 0.0f, mPosition.y), new Vector3(mPosition.x, 3.0f, mPosition.y));
		CDebug.DrawYRect(mPosition.ToWorldVec3(), 0.4f, 0.4f, Color.green);
		CDebug.DrawBounds(mBounds, Color.blue);
	}
}