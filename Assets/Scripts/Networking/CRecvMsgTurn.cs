using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CRecvMsgTurn : CNetRecvMsg
{
	public CActionTurn mTurn;

	public CRecvMsgTurn(byte[] Buffer) : base(Buffer) { }

	override protected void _Deserialize()
	{	
		int currentTurn = _reader.ReadInt32();
		int playerID = _reader.ReadByte();
		int actionCount = _reader.ReadByte();
		int hash = _reader.ReadInt32();

		mTurn = new CActionTurn(playerID, currentTurn);
		mTurn.mHash = hash;

		for (int i = 0; i < actionCount; ++i)
		{
			CUserAction action = new CUserAction();
			action.mID = (CUserAction.EType)_reader.ReadInt32();
			action.mInfo = _reader.ReadInt32();
			action.mX = _reader.ReadInt32();
			action.mY = _reader.ReadInt32();
			action.mPlayerID = playerID;
			action.mTurn = currentTurn;

			mTurn.mActionBuffer.Add(action);
			//Debug.Log("Got Action " + action.mPlayerID + " " + action.mID.ToString());
		}
	}
}
