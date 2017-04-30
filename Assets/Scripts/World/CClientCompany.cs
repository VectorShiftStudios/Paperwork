using System;
using System.Collections.Generic;
using UnityEngine;

public class CContractTier
{
	public float mNextTier;
	public float mMulLow;
	public float mMulHigh;

	public CContractTier(float NextTier, float MulLow, float MulHigh)
	{
		mNextTier = NextTier;
		mMulLow = MulLow;
		mMulHigh = MulHigh;
	}
}

public class CClientCompany
{
	private CWorld _world;
	public bool mActive;

	public string mName;
	public Color mColor;
	public float mPaperMul;
	public float mDeadlineMul;
	public float mStackMul;
	
	public int[] mPlayerReputation;
	public int[] mPlayerTier;
	// 0 = company will not participate
	// Increases by one for every completed contract
	// Decreases by 1 for each failure
	
	/// <summary>
	/// Standard constructor.
	/// </summary>
	public CClientCompany(CWorld World, string Name, Color Colour, float PaperMul, float DeadlineMul, float StackMul)
	{
		_world = World;
		mPlayerReputation = new int[CWorld.MAX_PLAYERS];
		mPlayerTier = new int[CWorld.MAX_PLAYERS];

		mName = Name;
		mColor = Colour;
		mPaperMul = PaperMul;
		mDeadlineMul = DeadlineMul;
		mStackMul = StackMul;
	}

	/// <summary>
	/// Create a contract for specified player.
	/// </summary>
	public CContract GenerateContract(int PlayerID)
	{
		CContract contract = CEntity.Create<CContract>(_world);
		contract.InitContract(this, mPlayerTier[PlayerID], PlayerID);
		return contract;
	}

	public void SetPlayerReputation(int PlayerID, int Value)
	{
		mPlayerReputation[PlayerID] = Value;
		mPlayerTier[PlayerID] = GetTierForPlayer(PlayerID);
	}

	public void ModifyPlayerReputation(int PlayerID, int Value)
	{
		mPlayerReputation[PlayerID] += Value;
		mPlayerTier[PlayerID] = GetTierForPlayer(PlayerID);
	}

	public int GetTierForPlayer(int PlayerID)
	{
		int rep = mPlayerReputation[PlayerID];

		if (rep == 0)
			return 0;

		int tier = 1;
		int tierCount = _world.mContractTiers.Count;

		for (int i = 1; i <= tierCount; ++i)
		{
			if (rep < _world.mContractTiers[i].mNextTier)
			{
				tier = i;
				break;
			}
		}

		tier = Mathf.Clamp(tier, 0, tierCount);

		return tier;
	}
}
