using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CRecvMsgWelcome : CNetRecvMsg
{
	public int mVersionMajor;
	public int mVersionMinor;
	public int mLevelID;

	public CRecvMsgWelcome(byte[] Buffer) : base(Buffer) { }

	override protected void _Deserialize()
	{	
		mLevelID = _reader.ReadInt32();
		mVersionMajor = _reader.ReadInt32();
		mVersionMinor = _reader.ReadInt32();
	}
}
