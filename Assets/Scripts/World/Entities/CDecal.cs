using System;
using System.Collections.Generic;
using UnityEngine;

public struct SDecalInfo
{
	public CDecal.EDecalType mType;
	public CDecal.EDecalVis mVis;
	public string mText;
	public int mVisualId; // Font or Sprite
	public Color mColor;
	public Vector3 mPosition;
	public Vector2 mSize;
	public Quaternion mRotation;
}

public class CDecal : CEntity
{
	public enum EDecalType
	{
		IMAGE,
		TEXT,
	}

	public enum EDecalVis
	{
		ALWAYS,
		FOW,
		LOS
	}

	public SDecalInfo mInfo;

	public CDecalView mStateView = null;

	public override void Init(CWorld World)
	{	
		base.Init(World);
		mType = EType.DECAL;
	}

	public override void Destroy()
	{
		base.Destroy();
	}

	public void SetInfo(SDecalInfo Info)
	{
		mInfo = Info;
	}
}