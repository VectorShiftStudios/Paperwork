using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CSimThread
{
	public CThreadEventBuffer mEventBufferIn;
	// TODO: Need to process this in primary thread.
	public CThreadEventBuffer mEventBufferOut;

	public const long TICK_DURATION_MS = 50000;
	public const int LATENCY_TICKS = 5;

	private Thread _thread;
	private bool _running;
	private System.Diagnostics.Stopwatch _timer;

	private CGameSession _gameSession;

	// Lockstep
	private List<CUserAction> _localActionBuffer = new List<CUserAction>();
	private List<CActionTurn> _actionTurns = new List<CActionTurn>();
	private int _simTickCount;
	private CWorld _world;
	private int _currentPlayer;	
	private CReplay _replay = new CReplay();

	public CSimThread(CGameSession Session)
	{
		_gameSession = Session;
	}

	private long _GetTimeUS()
	{
		double msNow = _timer.Elapsed.TotalMilliseconds;
		return (long)(msNow * 1000.0);
	}

	public void StartThread(CWorld World, int PlayerID)
	{
		_world = World;
		_currentPlayer = PlayerID;
		mEventBufferIn = new CThreadEventBuffer();
		mEventBufferOut = new CThreadEventBuffer();

		_timer = new System.Diagnostics.Stopwatch();
		_timer.Start();

		_thread = new Thread(_ThreadStart);
		_thread.Start();
	}

	private void _ThreadStart()
	{
		try
		{
			Debug.Log("Simulation Thread Started");

			long startTime = _GetTimeUS();
			long tickDuration = 0;
			long tickNextTime = startTime + TICK_DURATION_MS;
			long sleepTime = 0;

			_running = true;
			while (_running)
			{
				// TODO: Quick tick if we are waiting for incoming commands
				// Does _Tick() not already do a quick tick while waiting?

				long tickStartTime = _GetTimeUS();

				int msTime = (int)((tickStartTime - startTime) / 1000);
				int msShouldBe = (int)(((tickNextTime - TICK_DURATION_MS) - startTime) / 1000);
				//Debug.Log("Sim Tick: " + msTime + " " + msShouldBe);

				// Tick until we get all turns submitted and can complete a full game tick.
				_Tick();

				long tickEndTime = _GetTimeUS();
				tickDuration = tickEndTime - tickStartTime;
				sleepTime = tickNextTime - tickEndTime;

				if (sleepTime >= 0)
				{
					tickNextTime += TICK_DURATION_MS;
					Thread.Sleep((int)(sleepTime / 1000));
				}
				else
				{
					// TODO: Better scheme for overflow compensation.
					// TODO: Do we only want to measure to world sim tick here?
					tickNextTime = tickEndTime + TICK_DURATION_MS;
					Debug.LogWarning((_simTickCount - 1) + ": Sim tick did not complete within 50ms");
				}
			}

			mEventBufferOut.PushEvent(new CThreadEvent(CThreadEvent.EType.T_END));
			Debug.Log("Simulation Thread Terminated");
		}
		catch (CFibreRuntimeException E)
		{
			Debug.LogError("FibreScript Runtime: " + E.Message + " " + E.mFibreInfo + E.StackTrace);
			if (_world != null)
			{
				_world.mCrashed = true;
				_world.mCrashMessage = "FibreScript Runtime: " + E.Message + " " + E.mFibreInfo;
			}
		}
		catch (Exception E)
		{
			Debug.LogError("SimThreadEx: " + E.Message + " " + E.StackTrace);
			if (_world != null)
			{
				_world.mCrashed = true;
				_world.mCrashMessage = E.Message;
			}

			// TODO: Push event so that main thread knows we crashed.
			//mEventBufferOut.PushEvent(new CThreadEvent(CThreadEvent.EType.T_FAILED));
		}

	}

	public void StopThread()
	{
		mEventBufferIn.PushEvent(new CThreadEvent(CThreadEvent.EType.T_END));
	}

	public void PushUserAction(CUserAction Action)
	{
		mEventBufferIn.PushEvent(new CThreadEvent(CThreadEvent.EType.T_ACTION, Action));
	}

	private bool _ParseEventQueue()
	{
		CThreadEvent evnt = null;

		while (true)
		{
			evnt = mEventBufferIn.PopEvent();

			if (evnt == null)
				break;

			switch (evnt.mType)
			{
				case CThreadEvent.EType.T_END:
					_running = false;
					return false;

				case CThreadEvent.EType.T_ACTION:
					_AddUserAction((CUserAction)evnt.mData);
					break;
			}
		}

		return true;
	}

	private void _AddUserAction(CUserAction Action)
	{
		// TODO: don't gather actions while simulation is paused waiting for turns??

		Action.mPlayerID = _currentPlayer;
		Action.mTurn = _simTickCount;
		_localActionBuffer.Add(Action);
	}

	private void _Tick()
	{
		bool gatheringTurns = true;
		bool waitingForTurns = false;		

		while (gatheringTurns)
		{

			if (!_ParseEventQueue())
				return;

			if (!waitingForTurns)
			{
				_SendCurrentLocalActions(_currentPlayer, _simTickCount);
				++_simTickCount;
			}

			//_TickNetwork();

			if (_simTickCount >= LATENCY_TICKS)
			{
				int executeTurn = _simTickCount - LATENCY_TICKS;
				CActionTurn[] playerTurns = new CActionTurn[CWorld.MAX_PLAYERS];

				// Time to execute turn, so query Game Session for all the player turns for turn X.

				// Get replay turns from journal
				_replay.ReplayGetTurns(executeTurn, _actionTurns);

				// TODO: Since we are moving journal 'up a level' don't search entire actionTurns,
				// only search until all players have a valid turn (combine get and check steps).

				// Get all player turns
				for (int i = 0; i < _actionTurns.Count; ++i)
				{
					if (_actionTurns[i].mTurn == executeTurn)
						playerTurns[_actionTurns[i].mPlayerID] = _actionTurns[i];
				}

				bool turnsMissing = false;

				// If there are turns missing then halt the simulation
				for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
				{
					if (_world.mPlayers[i].mHumanInput && playerTurns[i] == null)
					{
						turnsMissing = true;
						break;
					}
				}

				if (!waitingForTurns && turnsMissing)
					Debug.LogError("Simulation Wait " + _simTickCount);

				if (!turnsMissing)
				{
					// At this point we have all required turns for this simulation tick

					gatheringTurns = false;

					if (waitingForTurns)
						Debug.LogError("Simulation Resumed " + _simTickCount);

					int compareHash = -1;

					// Execute all the turns & remove from recvd turns
					for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
					{
						if (_world.mPlayers[i].mHumanInput)
						{
							if (compareHash == -1)
							{
								compareHash = playerTurns[i].mHash;
							}
							else
							{
								if (compareHash != playerTurns[i].mHash)
								{
									// TODO: Inform player & unity thread we are out of sync!
									//CGame.UIManager.DisplayLargeMessage("Out of Sync!");
									Debug.LogError("Out Of Sync");
									_running = false;
									return;
								}
							}

							_actionTurns.Remove(playerTurns[i]);
							_world.ExecuteTurnActions(playerTurns[i]);
						}
					}

					_replay.ReplayWriteTurns(executeTurn, playerTurns);
				}
			}
		}

		// If we get this far then we can do a game tick.
		//Debug.Log("Sim Tick " + _simTickCount + " " + (_simTickCount - LATENCY_TICKS) + " " + (_GetTimeUS() / 1000) + "ms");
		//Debug.Log("ST: " + ((double)System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency - 198000.0));
		_world.SimTick();
	}

	private void _SendCurrentLocalActions(int PlayerID, int Turn)
	{
		CActionTurn localTurn = new CActionTurn(PlayerID, Turn);
		localTurn.mHash = _world.GetWorldHash(); // Hash calculated for current _simTickCount
		localTurn.CopyActionBuffer(_localActionBuffer);
		_localActionBuffer.Clear();
		_actionTurns.Add(localTurn);

		if (CGame.Net != null)
			CGame.Net.SendRequest(new CSendMsgTurn(localTurn));
	}
}

public class CThreadEvent
{
	public enum EType
	{
		T_NONE,
		T_PAUSE,
		T_RESUME,
		T_END,
		T_FAILED,
		T_TICK_STATE,
		T_ACTION
	}

	public EType mType;
	public object mData;

	public CThreadEvent(EType Type)
	{
		mType = Type;
		mData = null;
	}

	public CThreadEvent(EType Type, object Data)
	{
		mType = Type;
		mData = Data;
	}
}

public class CThreadEventBuffer
{
	private Queue<CThreadEvent> _events = new Queue<CThreadEvent>();

	public void PushEvent(CThreadEvent Event)
	{
		lock (_events)
		{
			_events.Enqueue(Event);
		}
	}

	public CThreadEvent PopEvent()
	{
		lock (_events)
		{
			if (_events.Count > 0)
				return _events.Dequeue();

			return null;
		}
	}
}