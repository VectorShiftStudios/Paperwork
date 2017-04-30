using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CContractView : CStateView
{
	public int mID;
	public int mOwner;
	public string mName;
	public int mTier;
	public string mCompanyName;
	public int mPenalty;
	public int mDeadline;
	public int mAcceptedTime;
	public int mValue;
	public int mAvailableUntil;
	public int mAvailableFor;

	public int mNewOwner;
	public int mStackCount;
	public int mUndistributedStacks;
	public int mUncompletedStacks;
	
	// UI Stuff
	public CNotifyStackIcon mNotifyIcon;
	public CContractInTray mContractInTray;
	public GameObject mUIElement;

	public void CopyInitialState(CContract Contract)
	{
		mID = Contract.mID;
		mOwner = Contract.mOwner;
		mName = Contract.mName;
		mTier = Contract.mTier;
		mCompanyName = Contract.mCompany.mName;
		mPenalty = Contract.mPenalty;
		mDeadline = Contract.mDeadlineTime;
		mValue = Contract.mTotalStartPapers;
		mAvailableUntil = Contract.mAvailableUntil;
		mAvailableFor = Contract.mAvailableFor;
	}

	public void CopyState(CContract Contract)
	{
		mNewOwner = Contract.mOwner;
		mStackCount = Contract.mStackCount;
		mUndistributedStacks = Contract.GetUndistributedStacks();
		mAcceptedTime = Contract.mAcceptedTime;
		mUncompletedStacks = Contract.GetUncompletedStacks();
	}
	
	protected override void _New(CUserSession UserSession)
	{
		UserSession.OnContractAdded(this);
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		UserSession.OnContractRemoved(this);
	}

	protected override void _Update(CUserSession UserSession)
	{
		if (mNewOwner != mOwner)
		{
			mOwner = mNewOwner;
			UserSession.OnContractChangedOwner(this);
		}

		if (mNotifyIcon != null)
		{
			float normalizedAvailableTime = (mAvailableUntil - _worldView.mGameTick) / (float)mAvailableFor;
			mNotifyIcon.mTimerBar.localScale = new Vector3(normalizedAvailableTime, 1.0f, 1.0f);
		}
	}
}
