using System;
using System.Collections.Generic;

/// <summary>
/// Full turn packet.
/// </summary>
public class CActionTurn
{
	public int mHash;
	public int mPlayerID;
	public int mTurn;
	public List<CUserAction> mActionBuffer;

	public CActionTurn(int PlayerID, int Turn)
	{
		mPlayerID = PlayerID;
		mTurn = Turn;
		mActionBuffer = new List<CUserAction>();
	}

	public void CopyActionBuffer(List<CUserAction> Buffer)
	{
		mActionBuffer.Clear();

		for (int i = 0; i < Buffer.Count; ++i)
			mActionBuffer.Add(Buffer[i]);
	}
}

/// <summary>
/// Represents an action performed by the player that can affect the simulation.
/// </summary>
public class CUserAction
{
	public enum EType
	{
		NONE,
		PLACE_OBJECT,
		MOVE_UNIT,
		SELECT_ENTITY,
		ACCEPT_RESUME,
		ACCEPT_CONTRACT,
		DISTRIBUTE_CONTRACT,
		ASSIGN_WORKSPACE,		
		ENSLAVE_INTERN,
		RETURN_CONTRACT_PAPERS,
		FIRE_EMPLOYEE,
		PROMOTE,
		RAISE,
		BONUS,
		TAG_ITEM,
		CANCEL_BLUEPRINT,
		FORCE_ATTACK,
		LOCK_ITEM,
	}

	public EType mID;
	public int mInfo;
	public int mX;
	public int mY;
	public string mStringInfo;

	// This is wrapped in a turn packet (CActionTurn)
	// TODO: Not really needed in here too	
	public int mPlayerID;
	public int mTurn;

	public CUserAction()
	{
	}

	public CUserAction(EType ID, int Info, int X, int Y, string StringInfo = "")
	{
		mID = ID;
		mInfo = Info;
		mX = X;
		mY = Y;
		mStringInfo = StringInfo;
	}
}
