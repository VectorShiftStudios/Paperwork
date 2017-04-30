using System;
using System.Collections.Generic;
using UnityEngine;

public class CDebug
{
	private struct SLine
	{
		public Color mColor;
		public Vector3 mStartPos;
		public Vector3 mEndPos;
	}

	private struct SQuad
	{
		public Color mColor;
		public Vector3 mV0;
		public Vector3 mV1;
		public Vector3 mV2;
		public Vector3 mV3;
	}

	private static Material _matDebug;

	private static SLine[] _lines;
	public static int mLineCount;
	public static int mLineBufferCount;
	public static int mLineDesiredCount;

	private static SLine[] _linesNoD;
	public static int mLineNoDCount;
	public static int mLineNoDBufferCount;
	public static int mLineNoDDesiredCount;

	private static SQuad[] _quads;
	public static int mQuadCount;
	public static int mQuadBufferCount;
	public static int mQuadDesiredCount;

	private static SQuad[] _quadsNoD;
	public static int mQuadNoDCount;
	public static int mQuadNoDBufferCount;
	public static int mQuadNoDDesiredCount;

	// TODO(keith): REMOVE THIS STUFF.
	private static List<Action> _persistentDrawCommands;

	/// <summary>
	/// Initialize debug class.
	/// </summary>
	public static void StaticInit()
	{
		mLineBufferCount = 20000;
		mLineCount = 0;
		_lines = new SLine[mLineBufferCount];

		mLineNoDBufferCount = 20000;
		mLineNoDCount = 0;
		_linesNoD = new SLine[mLineNoDBufferCount];

		mQuadBufferCount = 20000;
		mQuadCount = 0;
		_quads = new SQuad[mQuadBufferCount];

		mQuadNoDBufferCount = 200000;
		mQuadNoDCount = 0;
		_quadsNoD = new SQuad[mQuadNoDBufferCount];

		_matDebug = new Material(CGame.PrimaryResources.DebugLinesShader);
		_matDebug.hideFlags = HideFlags.HideAndDontSave;

		// TODO(keith): REMOVE THIS STUFF.
		_persistentDrawCommands = new List<Action>();
	}

	// TODO(keith): REMOVE THIS STUFF.
	public static void AddDrawCommand(Action DrawCommand)
	{
		_persistentDrawCommands.Add(DrawCommand);
	}

	// TODO(keith): REMOVE THIS STUFF.
	public static void ClearPersistentDrawCommands()
	{
		_persistentDrawCommands.Clear();
	}

	/// <summary>
	/// Render all the debug primitives.
	/// </summary>
	public static void Draw()
	{
		// TODO(keith): REMOVE THIS STUFF.
		for (int i = 0; i < _persistentDrawCommands.Count; ++i)
		{
			_persistentDrawCommands[i]();
		}

		if (mLineCount != 0)
		{
			_matDebug.SetPass(0);
			GL.Begin(GL.LINES);
			for (int i = 0; i < mLineCount; ++i)
			{
				GL.Color(_lines[i].mColor);
				GL.Vertex(_lines[i].mStartPos);
				GL.Vertex(_lines[i].mEndPos);
			}
			GL.End();
		}

		if (mLineNoDCount != 0)
		{
			_matDebug.SetPass(1);
			GL.Begin(GL.LINES);
			for (int i = 0; i < mLineNoDCount; ++i)
			{
				GL.Color(_linesNoD[i].mColor);
				GL.Vertex(_linesNoD[i].mStartPos);
				GL.Vertex(_linesNoD[i].mEndPos);
			}
			GL.End();
		}

		if (mQuadCount != 0)
		{
			_matDebug.SetPass(0);
			GL.Begin(GL.QUADS);
			for (int i = 0; i < mQuadCount; ++i)
			{
				GL.Color(_quads[i].mColor);
				GL.Vertex(_quads[i].mV0);
				GL.Vertex(_quads[i].mV1);
				GL.Vertex(_quads[i].mV2);
				GL.Vertex(_quads[i].mV3);
			}
			GL.End();
		}

		if (mQuadNoDCount != 0)
		{
			_matDebug.SetPass(1);
			GL.Begin(GL.QUADS);
			for (int i = 0; i < mQuadNoDCount; ++i)
			{
				GL.Color(_quadsNoD[i].mColor);
				GL.Vertex(_quadsNoD[i].mV0);
				GL.Vertex(_quadsNoD[i].mV1);
				GL.Vertex(_quadsNoD[i].mV2);
				GL.Vertex(_quadsNoD[i].mV3);
			}
			GL.End();
		}

		mLineCount = 0;
		mLineDesiredCount = 0;

		mLineNoDCount = 0;
		mLineNoDDesiredCount = 0;

		mQuadCount = 0;
		mQuadDesiredCount = 0;

		mQuadNoDCount = 0;
		mQuadNoDDesiredCount = 0;
	}

	/// <summary>
	/// Push a new line for drawing.
	/// </summary>
	public static void DrawLine(Vector3 Start, Vector3 End, Color Colour, bool Depth = true)
	{
		if (Depth)
		{
			if (mLineCount < mLineBufferCount)
			{
				_lines[mLineCount].mColor = Colour;
				_lines[mLineCount].mStartPos = Start;
				_lines[mLineCount].mEndPos = End;
				++mLineCount;
			}

			++mLineDesiredCount;
		}
		else
		{
			if (mLineNoDCount < mLineNoDBufferCount)
			{
				_linesNoD[mLineNoDCount].mColor = Colour;
				_linesNoD[mLineNoDCount].mStartPos = Start;
				_linesNoD[mLineNoDCount].mEndPos = End;
				++mLineNoDCount;
			}

			++mLineNoDDesiredCount;
		}
	}
	
	/// <summary>
	/// Push a new quad for drawing.
	/// </summary>
	public static void DrawQuad(Vector3 V0, Vector3 V1, Vector3 V2, Vector3 V3, Color Colour, bool Depth = true)
	{
		if (Depth)
		{
			if (mQuadCount < mQuadBufferCount)
			{
				_quads[mQuadCount].mColor = Colour;
				_quads[mQuadCount].mV0 = V0;
				_quads[mQuadCount].mV1 = V1;
				_quads[mQuadCount].mV2 = V2;
				_quads[mQuadCount].mV3 = V3;
				++mQuadCount;
			}

			++mQuadDesiredCount;
		}
		else
		{
			if (mQuadNoDCount < mQuadNoDBufferCount)
			{
				_quadsNoD[mQuadNoDCount].mColor = Colour;
				_quadsNoD[mQuadNoDCount].mV0 = V0;
				_quadsNoD[mQuadNoDCount].mV1 = V1;
				_quadsNoD[mQuadNoDCount].mV2 = V2;
				_quadsNoD[mQuadNoDCount].mV3 = V3;
				++mQuadNoDCount;
			}

			++mQuadNoDDesiredCount;
		}
	}

	/// <summary>
	/// TODO: Do proper triangle drawing! Not Just a collapsed Quad!!
	/// </summary>
	public static void DrawTri(Vector3 V0, Vector3 V1, Vector3 V2, Color Colour, bool Depth = true)
	{
		if (Depth)
		{
			if (mQuadCount < mQuadBufferCount)
			{
				_quads[mQuadCount].mColor = Colour;
				_quads[mQuadCount].mV0 = V0;
				_quads[mQuadCount].mV1 = V1;
				_quads[mQuadCount].mV2 = V2;
				_quads[mQuadCount].mV3 = V2;
				++mQuadCount;
			}

			++mQuadDesiredCount;
		}
		else
		{
			if (mQuadNoDCount < mQuadNoDBufferCount)
			{
				_quadsNoD[mQuadNoDCount].mColor = Colour;
				_quadsNoD[mQuadNoDCount].mV0 = V0;
				_quadsNoD[mQuadNoDCount].mV1 = V1;
				_quadsNoD[mQuadNoDCount].mV2 = V2;
				_quadsNoD[mQuadNoDCount].mV3 = V2;
				++mQuadNoDCount;
			}

			++mQuadNoDDesiredCount;
		}
	}

	public static void DrawThickLine(Vector3 Start, Vector3 End, float Thickness, Color Colour, bool Depth = true)
	{
		Vector3 forward = (End - Start).normalized;
		// TODO: make sure forward is not up.
		Vector3 left = Vector3.Cross(forward, Vector3.up).normalized;

		float halfW = Thickness * 0.5f;

		int segments = (int)Mathf.Ceil((End - Start).magnitude / 1.0f);
		float pos = 0.0f;

		for (int i = 0; i < segments; ++i)
		{
			Vector3 start = Start + forward * pos;
			Vector3 end = start + forward * 0.5f;

			Vector3 V0 = start - left * halfW;
			Vector3 V1 = end - left * halfW;
			Vector3 V2 = end + left * halfW;
			Vector3 V3 = start + left * halfW;

			DrawQuad(V0, V1, V2, V3, Colour, Depth);

			pos += 1.0f;
		}
	}

	public static void DrawLine(Vector3 Start, Vector3 End)
	{
		DrawLine(Start, End, Color.white);
	}

	public static void DrawCircle(Vector3 Up, Vector3 Origin, float Radius, Color C, bool Depth = true)
	{
		int resolution = 32;
		float interval = (Mathf.PI * 2.0f) / resolution;
		Vector3 up = Up;
		Vector3 forward = new Vector3(up.y, up.z, up.x);
		Vector3 left = Vector3.Cross(up, forward);

		/*
		DrawLine(Origin, Origin + up, Color.red);
		DrawLine(Origin, Origin + forward, Color.green);
		DrawLine(Origin, Origin + left, Color.blue);
		*/

		// TODO: Get points then convert to correct axis basis?

		for (int i = 0; i < resolution; ++i)
		{
			Vector3 p1 = new Vector3(Mathf.Sin((i + 0) * interval) * Radius, 0.0f, Mathf.Cos((i + 0) * interval) * Radius);
			Vector3 p2 = new Vector3(Mathf.Sin((i + 1) * interval) * Radius, 0.0f, Mathf.Cos((i + 1) * interval) * Radius);

			DrawLine(p1 + Origin, p2 + Origin, C, Depth);
		}
	}

	public static void DrawYLine(Vector3 Origin, float Size, Color C, bool Depth = true)
	{
		DrawLine(Origin, Origin + new Vector3(0, Size, 0), C, Depth);
	}

	public static void DrawYSquare(Vector3 Origin, float Size, Color C, bool Depth = true)
	{
		float halfSize = Size * 0.5f;
		
		Vector3 p1 = Origin + new Vector3(-halfSize, 0.0f, -halfSize);
		Vector3 p2 = Origin + new Vector3(+halfSize, 0.0f, -halfSize);
		Vector3 p3 = Origin + new Vector3(+halfSize, 0.0f, +halfSize);
		Vector3 p4 = Origin + new Vector3(-halfSize, 0.0f, +halfSize);

		DrawLine(p1, p2, C, Depth);
		DrawLine(p2, p3, C, Depth);
		DrawLine(p3, p4, C, Depth);
		DrawLine(p4, p1, C, Depth);
	}

	public static void DrawYRect(Rect Rect, Color C, bool Depth = true)
	{
		Vector3 p1 = new Vector3(Rect.min.x, 0.0f, Rect.min.y);
		Vector3 p2 = new Vector3(Rect.max.x, 0.0f, Rect.min.y);
		Vector3 p3 = new Vector3(Rect.max.x, 0.0f, Rect.max.y);
		Vector3 p4 = new Vector3(Rect.min.x, 0.0f, Rect.max.y);

		DrawLine(p1, p2, C, Depth);
		DrawLine(p2, p3, C, Depth);
		DrawLine(p3, p4, C, Depth);
		DrawLine(p4, p1, C, Depth);
	}

	public static void DrawYRect(Vector3 Origin, float Width, float Length, Color C, bool Depth = true)
	{
		float halfWidth = Width * 0.5f;
		float halfLength = Length * 0.5f;

		Vector3 p1 = Origin + new Vector3(-halfWidth, 0.0f, -halfLength);
		Vector3 p2 = Origin + new Vector3(+halfWidth, 0.0f, -halfLength);
		Vector3 p3 = Origin + new Vector3(+halfWidth, 0.0f, +halfLength);
		Vector3 p4 = Origin + new Vector3(-halfWidth, 0.0f, +halfLength);

		DrawLine(p1, p2, C, Depth);
		DrawLine(p2, p3, C, Depth);
		DrawLine(p3, p4, C, Depth);
		DrawLine(p4, p1, C, Depth);
	}

	public static void DrawYRectQuad(Vector3 Origin, float Width, float Length, Color C, bool Depth = true)
	{
		float halfWidth = Width * 0.5f;
		float halfLength = Length * 0.5f;

		Vector3 p1 = Origin + new Vector3(-halfWidth, 0.0f, -halfLength);
		Vector3 p2 = Origin + new Vector3(+halfWidth, 0.0f, -halfLength);
		Vector3 p3 = Origin + new Vector3(+halfWidth, 0.0f, +halfLength);
		Vector3 p4 = Origin + new Vector3(-halfWidth, 0.0f, +halfLength);

		DrawQuad(p1, p2, p3, p4, C, Depth);
	}

	public static void DrawYRectQuad(Rect Rect, Color C, bool Depth = true)
	{
		Vector3 p1 = new Vector3(Rect.xMin, 0.0f, Rect.yMin);
		Vector3 p2 = new Vector3(Rect.xMax, 0.0f, Rect.yMin);
		Vector3 p3 = new Vector3(Rect.xMax, 0.0f, Rect.yMax);
		Vector3 p4 = new Vector3(Rect.xMin, 0.0f, Rect.yMax);

		DrawQuad(p1, p2, p3, p4, C, Depth);
	}

	public static void DrawZRect(Vector3 Origin, float Width, float Height, Color C)
	{
		Vector3 p1 = Origin + new Vector3(0.0f, 0.0f, 0.0f);
		Vector3 p2 = Origin + new Vector3(0.0f, Height, 0.0f);
		Vector3 p3 = Origin + new Vector3(Width, Height, 0.0f);
		Vector3 p4 = Origin + new Vector3(Width, 0.0f, 0.0f);

		DrawLine(p1, p2, C);
		DrawLine(p2, p3, C);
		DrawLine(p3, p4, C);
		DrawLine(p4, p1, C);
	}

	public static void DrawXRect(Vector3 Origin, float Width, float Height, Color C)
	{
		Vector3 p1 = Origin + new Vector3(0.0f, 0.0f, 0.0f);
		Vector3 p2 = Origin + new Vector3(0.0f, Height, 0.0f);
		Vector3 p3 = Origin + new Vector3(0.0f, Height, Width);
		Vector3 p4 = Origin + new Vector3(0.0f, 0.0f, Width);

		DrawLine(p1, p2, C);
		DrawLine(p2, p3, C);
		DrawLine(p3, p4, C);
		DrawLine(p4, p1, C);
	}
	
	public static void DrawBounds(Bounds B, Color C, bool Depth = true)
	{
		Vector3 min = B.min;
		Vector3 max = B.max;

		Vector3 p1 = min;
		Vector3 p2 = new Vector3(min.x, min.y, max.z);
		Vector3 p3 = new Vector3(max.x, min.y, max.z);
		Vector3 p4 = new Vector3(max.x, min.y, min.z);

		Vector3 p5 = new Vector3(min.x, max.y, min.z);
		Vector3 p6 = new Vector3(min.x, max.y, max.z);
		Vector3 p7 = B.max;
		Vector3 p8 = new Vector3(max.x, max.y, min.z);

		DrawLine(p1, p2, C, Depth);
		DrawLine(p2, p3, C, Depth);
		DrawLine(p3, p4, C, Depth);
		DrawLine(p4, p1, C, Depth);

		DrawLine(p5, p6, C, Depth);
		DrawLine(p6, p7, C, Depth);
		DrawLine(p7, p8, C, Depth);
		DrawLine(p8, p5, C, Depth);

		DrawLine(p1, p5, C, Depth);
		DrawLine(p2, p6, C, Depth);
		DrawLine(p3, p7, C, Depth);
		DrawLine(p4, p8, C, Depth);
	}

	public static void DrawBorderQuads(Vector3 Origin, Vector2 Size, Color C, bool Depth = true)
	{
		float oW = 0.1f;

		Size *= 0.5f;

		Vector3 p1 = Origin + new Vector3(-Size.x, 0.0f, -Size.y);
		Vector3 p2 = Origin + new Vector3(-Size.x, 0.0f, Size.y);
		Vector3 p3 = Origin + new Vector3(Size.x, 0.0f, Size.y);
		Vector3 p4 = Origin + new Vector3(Size.x, 0.0f, -Size.y);

		Vector3 p1p2 = (p2 - p1) * 0.5f + p1;
		Vector3 p2p3 = (p3 - p2) * 0.5f + p2;
		Vector3 p3p4 = (p4 - p3) * 0.5f + p3;
		Vector3 p4p1 = (p1 - p4) * 0.5f + p4;

		Size *= 2.0f;

		CDebug.DrawYRectQuad(p1p2, oW, Size.y - oW, C, Depth);
		CDebug.DrawYRectQuad(p2p3, Size.x + oW, oW, C, Depth);
		CDebug.DrawYRectQuad(p3p4, oW, Size.y - oW, C, Depth);
		CDebug.DrawYRectQuad(p4p1, Size.x + oW, oW, C, Depth);
	}
}