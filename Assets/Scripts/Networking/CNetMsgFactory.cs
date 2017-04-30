using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CNetMsgFactory
{
	public enum ERecvMsgType
	{
		T_NONE = 0,
		T_TURN = 1,
		T_WELCOME = 2,
		T_COUNT
	};

	public static void DispatchResponse(INetHandler NetHandler, byte[] Buffer, int Size)
	{
		ushort ResponseID = BitConverter.ToUInt16(Buffer, 0);

		switch ((ERecvMsgType)ResponseID)
		{
			case ERecvMsgType.T_TURN: NetHandler.Message(new CRecvMsgTurn(Buffer)); return;
			case ERecvMsgType.T_WELCOME: NetHandler.Message(new CRecvMsgWelcome(Buffer)); return;

			default:
				Debug.LogError("Network Response " + ResponseID + " unknown");
				return;
		};
	}
}
