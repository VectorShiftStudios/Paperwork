using System;
using System.Collections.Generic;

public class CRandomStream
{
	private System.Random _rand;

	public CRandomStream()
	{
		_rand = new System.Random();
	}

	public CRandomStream(int Seed)
	{
		SetSeed(Seed);
	}

	public void SetSeed(int Value)
	{
		_rand = new System.Random(Value);
	}

	public int GetSeed()
	{
		return 0;
	}

	public float GetNextFloat()
	{
		return (float)_rand.NextDouble();
	}

	public int GetNextInt(int Max)
	{
		return _rand.Next(Max);
	}
}
