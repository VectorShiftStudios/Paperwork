using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CContractStack
{
	public CContract mContract;
	public bool mCompleted = false;
	public bool mDistributed = false;
	public float mTotalWork = 0;
	public float mDoneWork = 0;

	public bool DoWork(float Work)
	{
		mDoneWork += Work;
		//Debug.Log("Do work " + Work + " : " + mDoneWork + "/" + mTotalWork);

		if (mDoneWork >= mTotalWork)
		{
			mCompleted = true;
			mContract.CompletedStack();
			return true;
		}

		return false;
	}
}

public class CContract : CEntity
{
	public bool mAccepted;
	public string mName;
	public int mTier;
	public CClientCompany mCompany;
	public int mStackCount;
	public CContractStack[] mStacks;
	public int mPenalty;
	public int mAvailableUntil;
	public int mAvailableFor;
	public int mDeadlineTime;
	public int mAcceptedTime;
	public int mDeadlineRemaining;
	public int mTotalStartPapers;

	// State View
	public CContractView mStateView;

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.CONTRACT;
		mAccepted = false;
	}

	public override void SimTick()
	{
		if (mAccepted)
		{
			if (mWorld.mGameTick >= mAcceptedTime + mDeadlineTime)
			{
				mWorld.RemoveContractFromDesks(this);
				mWorld.mPlayers[mOwner].Spend(mPenalty);
				mWorld.DespawnEntity(this);
			}
		}
		else
		{
			if (mWorld.mGameTick >= mAvailableUntil)
			{
				mWorld.DespawnEntity(this);
			}
		}
	}

	public float GetNormalizedAvailableTime()
	{
		return (float)(mAvailableUntil - mWorld.mGameTick) / (float)mAvailableFor;
	}

	/// <summary>
	/// Fill out properties for a new contract.
	/// </summary>
	public void InitContract(CClientCompany Company, int Tier, int Player)
	{
		mTier = Tier;
		mCompany = Company;
		mOwner = Player;

		//CContractDefinition def = CGame.Datastore.mContract;
		mOwner = -1;
		mName = "Audit Report";
		
		float stackLow = 5;
		float stackHigh = 10;
		float deadlineLow = 18;
		float deadlineHigh = 36;
		float penaltyRate = 10;
		float acceptExpiryTime = 90;

		CContractTier tier = mWorld.mContractTiers[Tier];
		

		float rngMod = mWorld.SimRnd.GetNextFloat();
		
		int totalPapers = (int)((int)((float)(tier.mMulHigh - tier.mMulLow) * rngMod) + tier.mMulLow) * 100;
		totalPapers += (int)(totalPapers * Company.mPaperMul * Tier);

		mStackCount = (int)((int)((float)(stackHigh - stackLow) * rngMod) + stackLow);
		mStackCount += (int)(mStackCount * Company.mStackMul * Tier);
		mTotalStartPapers = 0;

		int papersPerStack = totalPapers / mStackCount;
		mStacks = new CContractStack[mStackCount];
		for (int i = 0; i < mStackCount; ++i)
		{
			mStacks[i] = new CContractStack();
			mStacks[i].mContract = this;
			mStacks[i].mTotalWork = papersPerStack;
			mTotalStartPapers += papersPerStack;
		}

		mPenalty = Tier / 10 * totalPapers;

		int deadline = (int)(mWorld.SimRnd.GetNextFloat() * (float)(deadlineHigh - deadlineLow) * 10.0f);
		deadline += (int)(deadline * Company.mDeadlineMul * Tier);
		mDeadlineTime = deadline * CWorld.TICKS_PER_SECOND;

		mAvailableFor = (int)acceptExpiryTime * CWorld.TICKS_PER_SECOND;
		mAvailableUntil = mWorld.mGameTick + mAvailableFor;

		//Debug.Log("Contract generated - Papers: " + mStartPapers + " Deadline: " + deadlineSecsRounded + "secs");
	}

	public void Accept(int PlayerId)
	{
		mAcceptedTime = mWorld.mGameTick;
		mAccepted = true;
		mOwner = PlayerId;
	}
	
	public CContractStack DistributeStack()
	{
		for (int i = 0; i < mStackCount; ++i)
		{
			if (!mStacks[i].mCompleted && !mStacks[i].mDistributed)
				return mStacks[i];
		}

		return null;
	}

	public void CompletedStack()
	{
		for (int i = 0; i < mStackCount; ++i)
		{
			if (!mStacks[i].mCompleted)
				return;
		}

		mWorld.DespawnEntity(this);
	}

	public int GetUndistributedStacks()
	{
		int count = 0;
		for (int i = 0; i < mStackCount; ++i)
		{
			if (!mStacks[i].mDistributed && !mStacks[i].mCompleted)
				++count;
		}

		return count;
	}

	public int GetUncompletedStacks()
	{
		int count = 0;
		for (int i = 0; i < mStackCount; ++i)
		{
			if (!mStacks[i].mCompleted)
				++count;
		}

		return count;
	}
}
