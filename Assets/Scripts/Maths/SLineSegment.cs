using UnityEngine;
using System;
using System.Collections.Generic;

public struct SLine
{
	Vector2 mA;
	Vector2 mB;

	/*
	public SLineSegment()
	{
		mA = Vector2.zero;
		mB = Vector2.zero;
	}
	*/

	public SLine(Vector2 A, Vector2 B)
	{
		mA = A;
		mB = B;
	}

	public SLine(float Ax, float Ay, float Bx, float By)
	{
		mA = new Vector2(Ax, Ay);
		mB = new Vector2(Bx, By);
	}
}
