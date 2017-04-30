using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CProfilerManager
{
	public const int MAX_STACK_DEPTH = 128;
	public const int MAX_RENDER_BLOCKS = 4096;
	public const int MAX_PROFILER_SAMPLES = CProfiler.MAX_ENTRIES * 2;

	public struct SProfileBlockEntry
	{
		public long mStartTime;
		public long mEndTime;
		public int mThreadIndex;
		public int mStackIndex;
		public CProfiler.EID mID;
	}
	
	private long _startTicks;
	private int[] _stackList = new int[MAX_STACK_DEPTH];

	private long _safeRangeStart;
	private long _safeRangeEnd;
	private SProfileBlockEntry[] _blocks = new SProfileBlockEntry[MAX_RENDER_BLOCKS];
	private int _blockCount = 0;
	private int _thread1MaxStack;
	private int _thread2MaxStack;

	// Shared Data, lock this CProfilerManager on access.
	private CProfiler.SEntry[] _profilerSamples = new CProfiler.SEntry[MAX_PROFILER_SAMPLES];
	private int _nextProfilerSample;
	private long _markedRangeStart;
	private long _markedRangeEnd;

	public void Init()
	{
		_startTicks = Stopwatch.GetTimestamp();

		for (int i = 0; i < MAX_PROFILER_SAMPLES; ++i)
		{
			_profilerSamples[i].mID = CProfiler.EID.I_NONE;
			_profilerSamples[i].mTime = 0;
		}
	}

	/// <summary>
	/// Add a balanced stack of profile data.
	/// </summary>
	public void AddProfiledData(CProfiler Profiler, long MarkTick)
	{
		// TODO: Ensure balanced stack from each thread contribution?
		lock (this)
		{
			//UnityEngine.Debug.LogError("Samples: " + Profiler.WriteCount);

			if (Profiler.WriteCount > MAX_PROFILER_SAMPLES)
			{
				UnityEngine.Debug.LogError("Tried to add " + Profiler.WriteCount + " samples. Max: " + MAX_PROFILER_SAMPLES);
			}
			else
			{
				//UnityEngine.Debug.Log("Copy Profiler: " + Profiler.WriteCount);
				for (int i = 0; i < Profiler.WriteCount; ++i)
				{
					int sampleIndex = _nextProfilerSample % MAX_PROFILER_SAMPLES;
					_profilerSamples[sampleIndex].mID = Profiler.WriteBuffer[i].mID;
					_profilerSamples[sampleIndex].mTime = Profiler.WriteBuffer[i].mTime;
					++_nextProfilerSample;

					//UnityEngine.Debug.Log(i + " " + Profiler.WriteBuffer[i].mID + " " + (Profiler.WriteBuffer[i].mTime - _startTicks));
				}
			}

			if (MarkTick != -1)
			{
				//_markedRangeStart = MarkTick - (Stopwatch.Frequency / 20) * 19 - 20000;
				_markedRangeStart = MarkTick;
				_markedRangeEnd = MarkTick + Stopwatch.Frequency / 20;
				//UnityEngine.Debug.Log("MarkTick: " + (_markedTick - _startTicks));
			}
		}
	}

	public double GetSecondsFromTicks(long Ticks)
	{
		return (double)(Ticks) / (double)Stopwatch.Frequency;
	}

	public void GenerateSampleSnapshot()
	{
		// TODO: Check for unbalanced stack??

		lock (this)
		{
			long updateStartTick = Stopwatch.GetTimestamp();

			_safeRangeStart = _markedRangeStart;
			_safeRangeEnd = _markedRangeEnd;

			_thread1MaxStack = 0;
			_thread2MaxStack = 0;

			int maxStack = 0;
			int stackListIndex = 0;
			int threadFrameStack = 0;			
			_blockCount = 0;

			for (int i = _nextProfilerSample; i < _nextProfilerSample + MAX_PROFILER_SAMPLES; ++i)
			{
				int sampleIndex = i % MAX_PROFILER_SAMPLES;
				CProfiler.SEntry sample = _profilerSamples[sampleIndex];

				// run while begin samples are within range, or end of samples
				if (threadFrameStack == 0)
				{
					if (sample.mID == CProfiler.EID.I_BEGIN_PRIMARY)
					{
						if (sample.mTime >= _markedRangeStart && sample.mTime < _markedRangeEnd)
						{
							//UnityEngine.Debug.Log("Start " + GetSecondsFromTicks(sample.mTime));
							threadFrameStack = 1;
							--i;
						}
					}
					else if (sample.mID == CProfiler.EID.I_BEGIN_SIM)
					{
						if (sample.mTime >= _markedRangeStart)
						{
							//UnityEngine.Debug.Log("Start " + GetSecondsFromTicks(sample.mTime));
							threadFrameStack = 2;
							--i;
						}
					}
				}
				else
				{
					// Add blocks
					if (sample.mID != CProfiler.EID.I_POP)
					{
						// Push
						_stackList[stackListIndex] = _blockCount;
						_blocks[_blockCount].mStartTime = sample.mTime;
						_blocks[_blockCount].mID = sample.mID;
						_blocks[_blockCount].mStackIndex = stackListIndex;
						_blocks[_blockCount].mThreadIndex = threadFrameStack;
						++_blockCount;
						++stackListIndex;

						if (stackListIndex > maxStack) maxStack = stackListIndex;					
					}
					else
					{
						// Pop
						--stackListIndex;
						_blocks[_stackList[stackListIndex]].mEndTime = sample.mTime;

						if (stackListIndex == 0)
						{
							if (threadFrameStack == 1) _thread1MaxStack = maxStack;
							else if (threadFrameStack == 2) _thread2MaxStack = maxStack;
							threadFrameStack = 0;
							maxStack = 0;
						}
					}
				}				
			}

			long updateTicks = Stopwatch.GetTimestamp() - updateStartTick;
			UnityEngine.Debug.Log("Profiler Update: " + ((double)updateTicks / ((double)Stopwatch.Frequency / 1000.0)));
		}
	}

	long _tickRangeModifier = 0;
	long _tickOffset = 0;

	public void OnGUI()
	{
		float profilerWidth = Screen.width - 10.0f;
		float posX = 5.0f;
		float posY = 5.0f;
		float barHeight = 20.0f;
		long tickRange = (_safeRangeEnd + _tickRangeModifier) - _safeRangeStart;
		long tickStart = _safeRangeStart + _tickOffset;
		double totalTime = GetSecondsFromTicks(tickRange) * 1000.0;
		double ticksToPixels = (double)profilerWidth / (double)tickRange;

		GUI.color = new Color(0.3f, 0.3f, 0.3f);
		GUI.DrawTexture(new Rect(posX, posY, profilerWidth, 35), CGame.UIResources.DebugBlock);
		GUI.DrawTexture(new Rect(posX, posY + 35, profilerWidth, _thread1MaxStack * barHeight), CGame.UIResources.DebugBlock);
		GUI.DrawTexture(new Rect(posX, posY + 35 + _thread1MaxStack * barHeight, profilerWidth, _thread2MaxStack * barHeight), CGame.UIResources.DebugBlock);
		GUI.color = new Color(0.8f, 0.8f, 0.8f);
		GUI.Label(new Rect(posX, posY, 500, 20), "Profiler " + _blockCount + " " + tickStart + " " + _nextProfilerSample + "(" + _nextProfilerSample % MAX_PROFILER_SAMPLES + ") " + _thread1MaxStack + " " + _thread2MaxStack);

		int labelCount = (int)(profilerWidth / 100.0f) + 1;
		for (int i = 0; i < labelCount; ++i)
		{
			int intTime = (int)((i * (100.0f / profilerWidth) * totalTime) * 1000);
			float labelTime = (float)intTime / 1000.0f;

			GUI.Label(new Rect(posX + i * 100, posY + 20, 50, 20), labelTime.ToString());
		}

		for (int i = 0; i < _blockCount; ++i)
		{
			double secs = GetSecondsFromTicks(_blocks[i].mEndTime - _blocks[i].mStartTime);

			long s = _blocks[i].mStartTime - tickStart;
			long e = _blocks[i].mEndTime - tickStart;

			float start = (float)((double)s * ticksToPixels);
			float end = (float)((double)e * ticksToPixels);

			if (end >= 0 && start <= profilerWidth)
			{
				if (start < 0) start = 0;
				if (end > profilerWidth) end = profilerWidth;

				float width = end - start;
				if (width < 1) width = 1;

				float offsetY = 0.0f;
				if (_blocks[i].mThreadIndex == 1)
				{
					GUI.color = Color.cyan;
					offsetY = 0.0f;
				}
				else if (_blocks[i].mThreadIndex == 2)
				{
					GUI.color = Color.red;
					offsetY = _thread1MaxStack * barHeight;
				}

				// GUI.color = CProfiler.Colors[(int)_blocks[i].mID];
				GUI.DrawTexture(new Rect(posX + start, posY + 35 + _blocks[i].mStackIndex * barHeight + offsetY, width, barHeight), CGame.UIResources.DebugBlock2);

				if (width > 50)
				{
					GUI.color = Color.white;
					GUI.Label(new Rect(posX + start, posY + 35 + _blocks[i].mStackIndex * barHeight + offsetY, width, barHeight), _blocks[i].mID + " " + secs * 1000.0);
				}
			}
		}

		GUI.color = Color.white;
		if (GUI.Button(new Rect(posX + profilerWidth - 100, posY + 2, 80, 20), "Snapshot"))
			GenerateSampleSnapshot();

		// Scale
		if (GUI.Button(new Rect(posX + profilerWidth - 160, posY + 2, 50, 20), "+ ms"))
			_tickRangeModifier -= Stopwatch.Frequency / 1000;

		if (GUI.Button(new Rect(posX + profilerWidth - 210, posY + 2, 50, 20), "- ms"))
			_tickRangeModifier += Stopwatch.Frequency / 1000;

		if (GUI.Button(new Rect(posX + profilerWidth - 260, posY + 2, 50, 20), "+ us"))
			_tickRangeModifier -= Stopwatch.Frequency / 10000;

		if (GUI.Button(new Rect(posX + profilerWidth - 310, posY + 2, 50, 20), "- us"))
			_tickRangeModifier += Stopwatch.Frequency / 10000;

		if (GUI.Button(new Rect(posX + profilerWidth - 360, posY + 2, 50, 20), "+ ns"))
			_tickRangeModifier -= Stopwatch.Frequency / 1000000;

		if (GUI.Button(new Rect(posX + profilerWidth - 410, posY + 2, 50, 20), "- ns"))
			_tickRangeModifier += Stopwatch.Frequency / 1000000;

		// Range
		if (GUI.Button(new Rect(posX + profilerWidth - 470, posY + 2, 50, 20), "> ms"))
			_tickOffset += Stopwatch.Frequency / 1000;

		if (GUI.Button(new Rect(posX + profilerWidth - 520, posY + 2, 50, 20), "< ms"))
			_tickOffset -= Stopwatch.Frequency / 1000;

		if (GUI.Button(new Rect(posX + profilerWidth - 570, posY + 2, 50, 20), "> us"))
			_tickOffset += Stopwatch.Frequency / 10000;

		if (GUI.Button(new Rect(posX + profilerWidth - 620, posY + 2, 50, 20), "< us"))
			_tickOffset -= Stopwatch.Frequency / 10000;

		if (GUI.Button(new Rect(posX + profilerWidth - 670, posY + 2, 50, 20), "> ns"))
			_tickOffset += Stopwatch.Frequency / 1000000;

		if (GUI.Button(new Rect(posX + profilerWidth - 720, posY + 2, 50, 20), "< ns"))
			_tickOffset -= Stopwatch.Frequency / 1000000;
	}
}