using System;
using System.Collections.Generic;
using UnityEngine;

public class CItemDesk : CItem
{	
	public float mStressRate;
	public float mEfficiency;
	public int mMaxPaperStackSlots;
	public int mMaxCompletedPapers;

	public int mUserID;
	public List<CCompletedContractStack> mCompletedStacks = new List<CCompletedContractStack>();

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_DESK;
	}

	public override void InitItem(CAsset Asset)
	{
		base.InitItem(Asset);

		mMaxCompletedPapers = 5;
		mMaxPaperStackSlots = 5;
		mStressRate = 1.0f;
		mEfficiency = 1.0f;

		mUserID = -1;
		mCompletedStacks.Clear();
	}

	public override bool IsDesk()
	{
		return true;
	}

	/// <summary>
	/// Remove completed papers from desk.
	/// </summary>
	public int TakeCompletedPaper()
	{
		if (mCompletedStacks.Count == 0)
			return 0;

		int value = (int)mCompletedStacks[0].mPapers;
		mCompletedStacks.RemoveAt(0);

		return value;
	}
}