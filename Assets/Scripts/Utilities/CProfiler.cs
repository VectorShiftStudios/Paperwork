using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CProfiler
{
	public const int MAX_ENTRIES = 1024 * 8;

	public enum EID
	{
		I_NONE,
		I_POP,
		I_BEGIN_PRIMARY,
		I_BEGIN_SIM,
		I_END,
		I_TICK,
		I_INPUT,
		I_NET,
		I_GAME_SESSION,
		I_GAME_SESSION_INPUT,
		I_RENDER_TICK,
		I_WORLD_RENDER_TICK,
		I_WORLD_PHYSICS,
		I_WORLD_PLAYERS,
		I_WORLD_ENTITIES,
		I_WORLD_ENTITY,
		I_WORLD_FOW,
		I_WORLD_INFLUENCE,
		I_WORLD_ITEM_VIS,
		COUNT
	}

	public static Color[] Colors;

	public struct SEntry
	{
		public EID mID;
		public long mTime;
	}

	// Per frame buffer
	public SEntry[] WriteBuffer = new SEntry[MAX_ENTRIES];
	public int WriteCount = 0;

	static CProfiler()
	{
		System.Random r = new System.Random(0);

		Colors = new Color[(int)EID.COUNT];

		for (int i = 0; i < (int)EID.COUNT; ++i)
			Colors[i] = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

		// Override colors here
		//Colors[(int)EID.I_BEGIN] = Color.blue;
	}

	public void Clear()
	{	
		WriteCount = 0;
	}

	public void Push(EID ID)
	{
		WriteBuffer[WriteCount].mID = ID;
		WriteBuffer[WriteCount].mTime = Stopwatch.GetTimestamp();
		++WriteCount;
	}

	public void Pop()
	{
		WriteBuffer[WriteCount].mID = EID.I_POP;
		WriteBuffer[WriteCount].mTime = Stopwatch.GetTimestamp();
		++WriteCount;
	}
}
