using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CReplay
{
	public enum EReplayState
	{
		RS_NONE,
		RS_RECORD,
		RS_VIEW
	}

	public EReplayState _replayState;
	private BinaryWriter _replayWriter;
	private BinaryReader _replayReader;	

	public bool StartPlayback(string Filename)
	{
		Filename = CGame.PersistentDataDirectory + CGame.REPLAYS_DIRECTORY + Filename + CGame.REPLAY_FILE_EXTENSION;

		try
		{
			_replayReader = new BinaryReader(File.Open(Filename, FileMode.Open));
			_replayState = EReplayState.RS_VIEW;
			
			return true;
		}
		catch (Exception e)
		{
			Debug.LogError("Viewing Replay File: " + e.Message);
			return false;
		}
	}

	public void StartRecording(string Filename)
	{
		Filename = CGame.PersistentDataDirectory + CGame.REPLAYS_DIRECTORY + Filename;
		string replayFile = Filename + CGame.REPLAY_FILE_EXTENSION;
		string stateFile = Filename + CGame.SAVE_FILE_EXTENSION;

		try
		{
			_replayWriter = new BinaryWriter(File.Open(replayFile, FileMode.Create));
			_replayState = EReplayState.RS_RECORD;
		}
		catch (Exception e)
		{
			Debug.LogError("Recording Replay File: " + e.Message);
			_replayState = EReplayState.RS_NONE;
		}
	}

	public void Stop()
	{

	}

	/// <summary>
	/// Write each player's turns into the replay.
	/// </summary>	
	public void ReplayWriteTurns(int Turn, CActionTurn[] Turns)
	{
		if (_replayState == EReplayState.RS_RECORD)
		{
			_replayWriter.Write((byte)1);
			_replayWriter.Write(Turn);

			for (int j = 0; j < Turns.Length; ++j)
			{
				CActionTurn turn = Turns[j];

				_replayWriter.Write((byte)2);
				_replayWriter.Write((byte)turn.mPlayerID);
				_replayWriter.Write((byte)turn.mActionBuffer.Count);

				for (int i = 0; i < turn.mActionBuffer.Count; ++i)
				{
					CUserAction action = turn.mActionBuffer[i];
					_replayWriter.Write((int)action.mID);
					_replayWriter.Write(action.mInfo);
					_replayWriter.Write(action.mX);
					_replayWriter.Write(action.mY);
				}
			}

			_replayWriter.Flush();
		}
	}

	/// <summary>
	/// Get actions up to and including the specified game frame.
	/// </summary>
	public void ReplayGetTurns(int Turn, List<CActionTurn> ActionTurns)
	{
		if (_replayState != EReplayState.RS_VIEW)
			return;

		int currentTurn = 0;

		try
		{
			while (true)
			{
				int blockType = _replayReader.ReadByte();

				if (blockType == 1)
				{
					currentTurn = _replayReader.ReadInt32();

					if (currentTurn > Turn)
					{
						_replayReader.BaseStream.Seek(-5, SeekOrigin.Current);
						return;
					}
					else if (currentTurn < Turn)
					{
						Debug.LogError("Can't be reading a turn less than current!");
						return;
					}
				}
				else if (blockType == 2)
				{
					int playerID = _replayReader.ReadByte();
					int actionCount = _replayReader.ReadByte();

					CActionTurn turn = new CActionTurn(playerID, currentTurn);

					for (int i = 0; i < actionCount; ++i)
					{
						CUserAction action = new CUserAction();
						action.mID = (CUserAction.EType)_replayReader.ReadInt32();
						action.mInfo = _replayReader.ReadInt32();
						action.mX = _replayReader.ReadInt32();
						action.mY = _replayReader.ReadInt32();
						action.mPlayerID = playerID;
						action.mTurn = currentTurn;

						turn.mActionBuffer.Add(action);
					}

					ActionTurns.Add(turn);
				}
			}
		}
		catch (Exception e)
		{
			Debug.Log("Replay has ended: " + e.Message);
			_replayState = EReplayState.RS_NONE;
			return;
		}
	}
}
