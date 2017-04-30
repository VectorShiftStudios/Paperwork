using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User's view of some state information in the simulation.
/// The source info in the simulation is copied to the view.
/// </summary>
public class CStateView
{
	public enum EState
	{
		NEW,
		UPDATING,
		DESTROYED,
		WAITING
	}

	public EState mState;
	protected int _playerIndex;
	protected CUserWorldView _worldView;

	public void Init(int PlayerIndex, CUserWorldView WorldView)
	{
		_playerIndex = PlayerIndex;
		_worldView = WorldView;
	}

	public bool Update(CUserSession UserSession)
	{
		if (mState == EState.NEW)
		{
			_New(UserSession);
			mState = EState.UPDATING;
		}
				
		if (mState == EState.UPDATING)
		{
			_Update(UserSession);
			return true;
		}

		if (mState == EState.WAITING)
		{
			_Destroy(UserSession);
			mState = EState.DESTROYED;
			return false;
		}

		return true;
	}

	protected virtual void _New(CUserSession UserSession) { }
	protected virtual void _Update(CUserSession UserSession) { }
	protected virtual void _Destroy(CUserSession UserSession) { }

	public virtual Transform GetTransform() 
	{ 
		return null;
	}

	public virtual void Select() { }
	public virtual void Deselect() { }

	public virtual void DrawDebugPrims() { }
}