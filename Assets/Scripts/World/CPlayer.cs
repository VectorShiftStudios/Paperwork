using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player (Human/AI).
/// </summary>
public class CPlayer
{
	public const int FLAG_PLAYABLE = (1 << 0);
	public const int FLAG_COMPETING = (1 << 1);

	private CWorld _world;
	public int mID;

	public bool mHumanInput;
	public bool mPlayable;
	public bool mCompeting;
	public bool mAI;

	public Color mColor;
	public int mAllies;
	public int mMoney;
	public int mDebt;
	public int mSelectedUnitId;
	public int mSpawnItemID;

	private int _debtTimer;

	private CPlayerAI _ai;

	// Cutscene Block (Game flow)
	public bool mCanInteractWithWorld;
	public bool mCanInteractWithUI;
	public bool mCanControlCamera;
	public bool mShowUI;
	// Camera target params. How the fuck are we going to do this? Create camera tracks? Pass those tracks to the user session??!?
	// Cam position is based on velocity??
	public Vector4 mCamPosition;
	public Vector4 mCamTargetPosition;
	public int mCamStep; // When cam step changes, all params jump?
	public int mCamFOV;
	public float mCamSpeed;
	public Color mFadeTarget; // Only render if alpha > 0
	public int mFadeSpeed;
	public int mCamTrackCount;
	public int mCamTrackEntity;

	public int mMusicTrack;

	public List<string> mAvailableItems;

	public CPlayer(CWorld World, int ID)
	{
		_world = World;
		mID = ID;
		mAvailableItems = new List<string>();
		mHumanInput = false;
		mSpawnItemID = -1;
	}

	public void Init(bool Playable, bool Competing, Color Colour, int AllyFlags)
	{
		mCanInteractWithWorld = true;
		mCanControlCamera = true;
		mCanInteractWithUI = true;
		mCamSpeed = 1.0f;
		mShowUI = true;
		mMusicTrack = 0;		
		mColor = Colour;
		mPlayable = Playable;
		mCompeting = Competing;
		mAllies = (1 << mID) | AllyFlags;
		mSelectedUnitId = -1;
	}

	public CItemStart GetSpawnItem()
	{
		return _world.GetEntity<CItemStart>(mSpawnItemID);
	}

	public Vector2 GetSpawnPos()
	{
		CItemStart start = _world.GetEntity<CItemStart>(mSpawnItemID);

		if (start != null)
			return start.mSpawnPos;

		return new Vector2(10, 10);
	}

	public void SetAI(bool Enabled)
	{
		if (Enabled && _ai == null)
		{
			_ai = new CPlayerAI();
			_ai.Init(_world, mID);
		}

		_ai.mActive = Enabled;
	}

	/// <summary>
	/// Simulation Tick.
	/// </summary>
	public void SimTick()
	{
		_UpdateFinance();
		_UpdateWinConditions();

		if (_ai != null)
			_ai.Update();
	}

	private void _UpdateFinance()
	{		
		mDebt = Spend(mDebt, false);
		mMoney = 0;

		for (int i = 0; i < _world.mItems.Count; ++i)
		{
			CItem item = _world.mItems[i];

			if (item.mType == CEntity.EType.ITEM_SAFE && item.mOwner == mID)
				mMoney += ((CItemSafe)item).mValue;
		}

		//mMoney -= mDebt;*/
	}

	/// <summary>
	/// Check if we have won or lost.
	/// </summary>
	private void _UpdateWinConditions()
	{
		if (mMoney >= _world.mWinMoney)
		{
			//_world.FinishGame(mID, true);
		}
		else if (mMoney < 0)
		{
			++_debtTimer;

			if (_debtTimer >= _world.mFailSeconds * CWorld.TICKS_PER_SECOND)
			{
				//_world.FinishGame(mID, false);
			}
		}		
		else
		{
			_debtTimer = 0;
		}
	}

	/// <summary>
	/// Can this player afford to spend a specified amount?
	/// </summary>
	public bool CanAfford(int Value)
	{
		if (CGame.VarFreePurchases.mValue)
			return true;

		return (mMoney - Value >= 0);
	}

	/// <summary>
	/// Remove money from player safes.
	/// </summary>	
	public int Spend(int Value, bool AddToDebt = true)
	{
		if (Value == 0)
			return 0;

		/*
		for (int i = 0; i < _world.mItems.Count; ++i)
		{
			CItem item = _world.mItems[i];

			if (item.mOwner == mID && item.IsSafe())
			{
				CItemSafe safe = (CItemSafe)item;

				if (safe.mValue >= Value)
				{
					safe.mValue -= Value;
					Value = 0;
					break;
				}
				else
				{
					Value -= safe.mValue;
					safe.mValue = 0;
				}
			}
		}
		*/

		if (Value > 0 && AddToDebt)
			mDebt += Value;

		return Value;
	}
}