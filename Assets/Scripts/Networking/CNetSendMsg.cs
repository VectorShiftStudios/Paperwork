using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

abstract public class CNetSendMsg
{
	public enum type_e
	{
		T_NONE = 0,
		T_TURN = 1,
		T_WELCOME = 2
	};

	public float mSendTime;
	protected MemoryStream _buffer;
	protected BinaryWriter _writer;

	public CNetSendMsg()
	{
		_buffer = new MemoryStream(1400);
		_writer = new BinaryWriter(_buffer);
		_writer.Seek(4, SeekOrigin.Begin);
	}

	protected void _WriteHeader(type_e ActionType)
	{
		short length = (short)_writer.Seek(0, SeekOrigin.Current);
		length -= 2;
		_writer.Seek(0, SeekOrigin.Begin);
		_writer.Write((short)length);
		_writer.Write((short)ActionType);
	}

	abstract public byte[] Serialize();
}
