using System;
using System.Collections;
using System.IO;

public class CSendMsgWelcome : CNetSendMsg
{
	public int mLevelID;
	
	public CSendMsgWelcome(int LevelID)
	{
		mLevelID = LevelID;
	}

	public override byte[] Serialize()
	{
		_writer.Write(mLevelID);
		_writer.Write(CGame.VERSION_MAJOR);
		_writer.Write(CGame.VERSION_MINOR);
		_WriteHeader(type_e.T_WELCOME);
		return _buffer.ToArray();
	}
}
