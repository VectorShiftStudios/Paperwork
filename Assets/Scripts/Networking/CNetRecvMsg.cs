using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

abstract public class CNetRecvMsg
{
	public float mRecvTime;
	protected MemoryStream _buffer;
	protected BinaryReader _reader;
	private byte[] _byteBuffer;

	public CNetRecvMsg(byte[] Buffer)
	{
		_byteBuffer = new byte[Buffer.Length];
		Buffer.CopyTo(_byteBuffer, 0);

		_buffer = new MemoryStream(_byteBuffer);
		_reader = new BinaryReader(_buffer);
		_buffer.Seek(2, SeekOrigin.Begin);
		_Deserialize();
	}

	protected string _ReadString()
	{
		byte[] str = new byte[1024];
		byte c = 0;
		int strPos = 0;

		while (true)
		{
			if (strPos > 1024)
				return null;

			c = _reader.ReadByte();

			if (c == 0)
				return Encoding.UTF8.GetString(str, 0, strPos);

			str[strPos] = c;
			++strPos;
		}
	}

	public void SetReadPos(long Offset)
	{
		_buffer.Seek(Offset, SeekOrigin.Begin);
	}

	public BinaryReader GetReader()
	{
		return _reader;
	}

	abstract protected void _Deserialize();
}
