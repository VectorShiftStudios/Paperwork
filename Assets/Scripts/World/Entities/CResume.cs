using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CResume : CEntity
{	
	public int mAvailableUntil;
	public int mAvailableFor;

	public SUnitBasicStats mStats;

	public CResumeView mStateView = null;

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.RESUME;

		mAvailableFor = 70 * CWorld.TICKS_PER_SECOND;
		mAvailableUntil = mWorld.mGameTick + mAvailableFor;
	}

	public float GetNormalizedAvailableTime()
	{
		return (float)(mAvailableUntil - mWorld.mGameTick) / (float)mAvailableFor;
	}

	public void Generate(CWorld World, int Tier, int Level)
	{
		mOwner = 0;

		CTierInfo tier = CGame.AssetManager.mUnitRules.GetTier(Tier);
		
		if (tier == null)
		{
			Debug.LogError("Requested tier did not exist in resume generation");
			return;
		}

		if (Tier == 1)
			Level = 1;// (int)(World.SimRnd.GetNextFloat() * );

		if (tier.mMaxLevel != 0 && Level > tier.mMaxLevel)
			Level = tier.mMaxLevel;

		mStats.mName = CUtility.GenerateRandomName(World.SimRnd);
		mStats.mTier = tier.mTierIndex;
		mStats.mLevel = Level;

		mStats.mSalary = tier.mSalary.Get(Level);
		mStats.mIntelligence = tier.mIntelligence.Get(Level);
		mStats.mMaxStamina = tier.mMaxStamina.Get(Level);
		mStats.mMaxSpeed = tier.mMaxSpeed.Get(Level);
		mStats.mHungerRate = tier.mHungerRate.Get(Level);
		mStats.mMaxHunger = tier.mMaxHunger.Get(Level);
		mStats.mPaperCapacity = tier.mPaperCapacity.Get(Level);
		mStats.mAttackDamage = tier.mAttackDamage.Get(Level);
		mStats.mDefense = tier.mDefense.Get(Level);
		mStats.mIdleTime = tier.mIdleTime.Get(Level);
		mStats.mWorkStaminaRate = tier.mWorkStaminaRate.Get(Level);
		mStats.mRequiredXP = tier.mRequiredXP.Get(Level);
		mStats.mPromotionDemand = 1;

		mStats.mMaxStress = 100;
		mStats.mRestDesired = 40;
		mStats.mRestForced = 20;
		mStats.mEatDesired = 50;
		mStats.mEatForced = 80;
		mStats.mStressRecoveryRate = 1;
		mStats.mEatRate = 10;
		mStats.mAttackSpeed = 1;
	}

	public override void SimTick()
	{
		if (mOwner != -1 && mWorld.mGameTick >= mAvailableUntil)
		{
			mWorld.DespawnEntity(this);
		}
	}

	/// <summary>
	/// Place resume into a limbo state that players can't interact with, but it can be used
	/// to spawn in a unit later.
	/// </summary>
	public void Consume()
	{
		mOwner = -1;
	}
}
