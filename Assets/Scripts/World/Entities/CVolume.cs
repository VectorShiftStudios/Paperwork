using System;
using System.Collections.Generic;
using UnityEngine;

public class CVolume : CEntity
{
	public Vector3 mPosition;
	public Vector3 mSize;
	public Bounds mBounds;

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.VOLUME;
	}

	public void SetPosition(Vector3 Position, Vector3 Size)
	{
		mPosition = Position;
		mSize = Size;
		CalcBounds();
	}

	public void CalcBounds()
	{
		mBounds = new Bounds(mPosition, mSize);
	}
}