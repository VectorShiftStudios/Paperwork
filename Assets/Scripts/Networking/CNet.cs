using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public interface INetHandler
{
	void ClientConnected();
	void Message(CRecvMsgTurn Msg);
	void Message(CRecvMsgWelcome Msg);
}

/*
//-------------------------------------------------------------
// Network Handlers
//-------------------------------------------------------------
void INetHandler.ClientConnected()
{

	// Send welcome
	Net.SendRequest(new CSendMsgWelcome(_hostLevelID));

	UIResources.GamePanel.SetActive(true);
	_gameSession = new CGameSession();
	_gameSession.InitMultiplayer(0, _hostLevelID);

}

void INetHandler.Message(CRecvMsgTurn Msg)
{

	if (_gameSession != null)
		_gameSession.HandleMessage(Msg);

}

void INetHandler.Message(CRecvMsgWelcome Msg)
{

	if (Msg.mVersionMajor != VERSION_MAJOR || Msg.mVersionMinor != VERSION_MINOR)
	{
		Debug.LogError("Version mismatch - Local: " + VERSION_MAJOR + "." + VERSION_MINOR + " Host: " + Msg.mVersionMajor + "." + Msg.mVersionMinor);
		// TODO: Let the user know, let the host know.
	}
	else
	{
		UIResources.GamePanel.SetActive(true);
		_gameSession = new CGameSession();
		_gameSession.InitMultiplayer(1, Msg.mLevelID);
	}

}
*/

public class CNet
{
	private enum ERecvState
	{
		RS_NONE,
		RS_HEADER,
		RS_PAYLOAD,
		RS_DONE,
		RS_ERROR
	};

	private ushort _dataSize;
	private byte[] _dataBuffer = new byte[10240];
	private ERecvState _recvState = ERecvState.RS_HEADER;

	public int sentBytes = 0;
	public int recvBytes = 0;

	public bool mConnected;

	private Socket _socket;
	private Socket _listenSocket;

	private INetHandler _netHandler;

	public CNet()
	{
		// TODO: The net class needs a way to interact with GameSession or something?
		_netHandler = null;
	}

	public bool Connect(string IP, int Port)
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		try
		{
			_socket.Connect(IP, Port);
			_socket.Blocking = false;
			_socket.NoDelay = true;
			mConnected = true;
			return true;
		}
		catch (Exception e)
		{
			Debug.LogError("Trying to connect: " + e.Message);
			mConnected = false;
			return false;
		}
	}

	public bool Host(int Port)
	{
		_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IPAddress hostIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
		//IPAddress hostIP = Dns.GetHostEntry("127.0.0.1").AddressList[0];
		Debug.Log("Host IP: " + hostIP.ToString());
		IPEndPoint ep = new IPEndPoint(IPAddress.Any, Port);

		try
		{
			_listenSocket.Bind(ep);
			_listenSocket.Listen(10);
			_listenSocket.Blocking = false;
			return true;
		}
		catch (Exception e)
		{
			Debug.LogError("Trying to host: " + e.Message);
			return false;
		}
	}

	/// <summary>
	/// Send a data packet that contains a request.
	/// </summary>
	public void SendRequest(CNetSendMsg Request)
	{
		if (!mConnected)
			return;

		byte[] ms = Request.Serialize();
		int bytes = _socket.Send(ms);
		sentBytes += bytes;
	}

	/// <summary>
	/// Pump the networking system.
	/// </summary>
	public void Update()
	{
		if (_listenSocket != null)
		{
			try
			{
				_socket = _listenSocket.Accept();

				if (_socket != null)
				{
					Debug.Log("Connection Accepted!");

					mConnected = true;
					_listenSocket.Close();
					_listenSocket = null;
					_netHandler.ClientConnected();
				}
			}
			catch (Exception e)
			{

			}
		}
		else if (_socket != null)
		{
			try
			{
				if (!_socket.Connected)
				{
					mConnected = false;
					Debug.LogError("Network Disconnection");
				}

				while (_socket.Available != 0)
				{
					//Console.WriteLine("New Data: " + _socket.Available);
					//Debug.Log("New Data: " + _socket.Available);

					switch (_recvState)
					{
						case ERecvState.RS_HEADER:
							{
								_dataSize = 0;

								if (_socket.Available < 2)
									return;

								int bytesRead = _socket.Receive(_dataBuffer, 2, SocketFlags.None);
								recvBytes += bytesRead;

								_dataSize = BitConverter.ToUInt16(_dataBuffer, 0);

								if (_dataSize < 10240 && _dataSize > 0)
								{
									_recvState = ERecvState.RS_PAYLOAD;
								}
								else
								{
									Debug.LogError("Warning! Bad Packet Size: " + _dataSize);
									// TODO: Fatal									
								}

								break;
							}

						case ERecvState.RS_PAYLOAD:
							{
								if (_socket.Available < _dataSize)
									return;

								int bytesRead = _socket.Receive(_dataBuffer, _dataSize, SocketFlags.None);
								recvBytes += bytesRead;

								_recvState = ERecvState.RS_HEADER;
								CNetMsgFactory.DispatchResponse(_netHandler, _dataBuffer, _dataSize);
								break;
							}

						default:
							break;
					}
				}
			}
			catch(Exception e)
			{
				//Debug.LogError("Updating Socket: " + e.Message);
			}
		}
	}
}
