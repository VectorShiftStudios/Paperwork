using System;
using System.Collections.Generic;
using UnityEngine;

public enum ESoundClip
{
	NONE,
	SOUND_1
}

/// <summary>
/// Event that is queued during simulation then executed on the main thread when possible.
/// </summary>
public class CTransientEvent
{
	public enum EType
	{
		NONE,
		SOUND,
		UI_SOUND,
		ACTION,
		EFFECT,
		MESSAGE,
		PAYDAY,
		NOTIFY
	}

	public EType mType;
	public int mViewerFlags;
	public int mGameTick;

	public int mData;
	public Vector3 mPosition;
	public string mMessage;

	public CTransientEvent(int GameTick, int ViewerFlags)
	{
		mType = EType.NONE;
		mGameTick = GameTick;
		mViewerFlags = ViewerFlags;
	}

	public void SetEffect(Vector3 Position)
	{
		mType = EType.EFFECT;
		mPosition = Position;
	}

	public void SetSound(Vector3 Position, int SoundID)
	{
		mType = EType.SOUND;
		mData = SoundID;
		mPosition = Position;
	}

	public void SetUISound(Vector3 Position, int SoundID)
	{
		mType = EType.UI_SOUND;
		mData = SoundID;
		mPosition = Position;
	}

	public void SetNotify(string ItemName)
	{
		mType = EType.NOTIFY;
		mMessage = ItemName;
	}
}