using System;
using System.Collections;
using System.IO;

public class CSendMsgTurn : CNetSendMsg
{
	public CActionTurn mTurn;

	public CSendMsgTurn(CActionTurn Turn)
	{
		mTurn = Turn;
	}

	public override byte[] Serialize()
	{
		_writer.Write(mTurn.mTurn);
		_writer.Write((byte)mTurn.mPlayerID);
		_writer.Write((byte)mTurn.mActionBuffer.Count);
		_writer.Write((int)mTurn.mHash);

		for (int i = 0; i < mTurn.mActionBuffer.Count; ++i)
		{
			CUserAction action = mTurn.mActionBuffer[i];
			_writer.Write((int)action.mID);
			_writer.Write(action.mInfo);
			_writer.Write(action.mX);
			_writer.Write(action.mY);
		}

		_WriteHeader(type_e.T_TURN);

		return _buffer.ToArray();
	}
}
