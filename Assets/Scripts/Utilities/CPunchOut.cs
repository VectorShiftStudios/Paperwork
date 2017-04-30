using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class CPunchOut
{
	public class CSightSegment
	{
		public int mSegID;
		public float mAngle;
		public Vector2 mStart;
		public Vector2 mEnd;
	}

	public static GameObject Create(out Mesh PunchOutMesh)
	{
		GameObject punchOut = new GameObject();
		MeshRenderer meshRenderer = punchOut.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = punchOut.AddComponent<MeshFilter>();
		PunchOutMesh = new Mesh();
		PunchOutMesh.MarkDynamic();
		meshFilter.mesh = PunchOutMesh;
		meshRenderer.material = CGame.PrimaryResources.PunchOutMat;

		return punchOut;
	}

	public static void UpdateMesh(Mesh PunchOutMesh, CUserWorldView WorldView, Vector2 Position, float Radius)
	{
		List<CSightSegment> segs = GenerateLOSPoints(WorldView, Position, 8.0f);

		int resolution = segs.Count;
		int vertCount = resolution + 1;
		int triCount = resolution;
		int indexCount = triCount * 3;

		Vector3[] verts = new Vector3[vertCount];
		int[] tris = new int[indexCount];

		verts[0] = Vector3.zero;

		for (int i = 0; i < resolution; ++i)
		{
			verts[i + 1] = new Vector3(segs[i].mEnd.x - Position.x, 0.0f, segs[i].mEnd.y - Position.y);

			tris[i * 3 + 0] = 0;
			tris[i * 3 + 1] = i + 1;
			tris[i * 3 + 2] = i + 2;

			if (i == resolution - 1)
				tris[i * 3 + 2] = 1;
		}

		PunchOutMesh.Clear(true);
		PunchOutMesh.vertices = verts;
		PunchOutMesh.triangles = tris;
	}

	/// <summary>
	/// Get a list of points for building a line-of-site punch out.
	/// </summary>
	public static List<CSightSegment> GenerateLOSPoints(CUserWorldView WorldView, Vector2 StartPos, float Radius)
	{
		Vector2 start = StartPos;

		if (CGame.VarShowVisLines.mValue)
		{
			CDebug.DrawCircle(Vector3.up, StartPos.ToWorldVec3(), Radius, Color.red, false);
			CDebug.DrawYRectQuad(start.ToWorldVec3(), 0.25f, 0.25f, new Color(1.0f, 0.4f, 0.2f, 0.4f), false);
		}

		List<CVisibilityBlockingSegment> culledSegs = new List<CVisibilityBlockingSegment>();
		List<CVisibilityBlockingSegment> visSegs = WorldView.GetVisSegments();

		// Get wall segments that intersect view radius.
		// TODO: Speed up with acceleration structure.
		for (int i = 0; i < visSegs.Count; ++i)
		{
			CVisibilityBlockingSegment seg = visSegs[i];

			Vector2 hitA;
			Vector2 hitB;
			bool OpenA, OpenB;
			if (CIntersections.LineVsCircle(seg.mA, seg.mB, StartPos, Radius, out hitA, out hitB, out OpenA, out OpenB))
			{
				culledSegs.Add(new CVisibilityBlockingSegment(hitA, hitB));

				if (CGame.VarShowVisLines.mValue)
					CDebug.DrawLine(hitA.ToWorldVec3(), hitB.ToWorldVec3(), Color.black, false);
			}
		}

		List<CSightSegment> lineSegs = new List<CSightSegment>();

		if (culledSegs.Count == 0)
		{
			// TODO: Could precalculate this and store it, but this only helps improve best case.
			int count = 46;
			float interval = (Mathf.PI * 2.0f) / count;

			for (int i = 0; i < count; ++i)
			{
				CSightSegment seg = new CSightSegment();
				lineSegs.Add(seg);
				seg.mEnd = new Vector2(Mathf.Sin(i * interval) * Radius, Mathf.Cos(i * interval) * Radius) + StartPos;
			}
		}
		else
		{
			// Find closest intersection points when casting a ray to each segment vertex.
			// TODO: There are still redundant casts to shared segment verts.
			for (int i = 0; i < culledSegs.Count; ++i)
			{
				CVisibilityBlockingSegment seg = culledSegs[i];

				float offset = 0.001f;
				Vector2 shift;

				float angle = Mathf.Atan2((seg.mA - start).x, (seg.mA - start).y);

				shift.x = Mathf.Sin(angle - offset) * Radius + start.x;
				shift.y = Mathf.Cos(angle - offset) * Radius + start.y;
				lineSegs.Add(_IntersectLOSRayWithLines(culledSegs, start, shift, Radius));

				shift.x = Mathf.Sin(angle + offset) * Radius + start.x;
				shift.y = Mathf.Cos(angle + offset) * Radius + start.y;
				lineSegs.Add(_IntersectLOSRayWithLines(culledSegs, start, shift, Radius));

				angle = Mathf.Atan2((seg.mB - start).x, (seg.mB - start).y);

				shift.x = Mathf.Sin(angle - offset) * Radius + start.x;
				shift.y = Mathf.Cos(angle - offset) * Radius + start.y;
				lineSegs.Add(_IntersectLOSRayWithLines(culledSegs, start, shift, Radius));

				shift.x = Mathf.Sin(angle + offset) * Radius + start.x;
				shift.y = Mathf.Cos(angle + offset) * Radius + start.y;
				lineSegs.Add(_IntersectLOSRayWithLines(culledSegs, start, shift, Radius));

				if (CGame.VarShowVisLines.mValue)
				{
					//CDebug.DrawYRectQuad(seg.mA.ToWorldVec3(), 0.25f, 0.25f, new Color(1.0f, 0.4f, 0.2f, 0.4f), false);
					//CDebug.DrawYRectQuad(seg.mB.ToWorldVec3(), 0.25f, 0.25f, new Color(0.2f, 0.4f, 1.0f, 0.4f), false);
					CDebug.DrawLine(seg.mA.ToWorldVec3(), seg.mB.ToWorldVec3(), Color.red, false);
				}
			}

			// Sort intersections by angle.
			lineSegs.Sort((x, y) =>
			{
				if (x.mAngle > y.mAngle) return 1;
				if (x.mAngle < y.mAngle) return -1;
				return 0;
			});

			// Cull redundant intersections.
			for (int i = 0; i < lineSegs.Count; ++i)
			{
				int s1 = lineSegs[i].mSegID;

				if (s1 == -1)
					continue;

				int s2 = lineSegs[(i + 1) % lineSegs.Count].mSegID;
				int s3 = lineSegs[(i + 2) % lineSegs.Count].mSegID;

				if (s1 == s2 && s1 == s3)
				{
					lineSegs.RemoveAt((i + 1) % lineSegs.Count);
					--i;
				}
			}

			// Regenerate circular caps.
			for (int i = 0; i < lineSegs.Count; ++i)
			{
				CSightSegment s1 = lineSegs[i];
				CSightSegment s2 = lineSegs[(i + 1) % lineSegs.Count];

				if (s1.mSegID == -1 && s2.mSegID == -1)
				{
					float angleD = s2.mAngle - s1.mAngle;

					if (i == lineSegs.Count - 1)
						angleD = s2.mAngle + Mathf.PI * 2 - s1.mAngle;

					int count = (int)(angleD / 0.1f);
					float interval = angleD / count;

					for (int j = 1; j < count; ++j)
					{
						float newAngle = s1.mAngle + interval * j;
						Vector2 shift;
						shift.x = Mathf.Sin(newAngle) * Radius + s1.mStart.x;
						shift.y = Mathf.Cos(newAngle) * Radius + s1.mStart.y;

						CSightSegment s = new CSightSegment();
						s.mStart = s1.mStart;
						s.mEnd = shift;

						lineSegs.Insert(i + 1, s);
						++i;
					}

					if (CGame.VarShowVisLines.mValue)
						CDebug.DrawLine(s1.mEnd.ToWorldVec3(), s2.mEnd.ToWorldVec3(), Color.white, false);
				}
			}

			if (CGame.VarShowVisLines.mValue)
			{
				for (int i = 0; i < lineSegs.Count; ++i)
				{
					CSightSegment s = lineSegs[i];

					if (s.mSegID != -1)
					{
						CDebug.DrawLine(s.mStart.ToWorldVec3(), s.mEnd.ToWorldVec3(), Color.green, false);
					}
					else
					{
						CDebug.DrawYRectQuad(s.mEnd.ToWorldVec3(), 0.25f, 0.25f, Color.magenta, false);
						CDebug.DrawLine(s.mStart.ToWorldVec3(), s.mEnd.ToWorldVec3(), Color.blue, false);
					}
				}
			}
		}

		return lineSegs;
	}

	/// <summary>
	/// Intersect a line-of-sight line segment with a list of line segments.
	/// </summary>
	private static CSightSegment _IntersectLOSRayWithLines(List<CVisibilityBlockingSegment> Segs, Vector2 Start, Vector2 End, float Radius)
	{
		CSightSegment result = new CSightSegment();

		Vector2 la = Start;
		Vector2 lb = End;
		Vector2 ld = (lb - la).normalized;
		float minT = (lb - la).magnitude;

		if (minT > Radius)
		{
			minT = Radius;
			lb = la + ld * minT;
		}

		// TODO: Not sure if this epsilon is needed.
		minT += 0.01f;

		Vector2 hitPoint = Vector2.zero;
		int segID = -1;

		for (int i = 0; i < Segs.Count; ++i)
		{
			CVisibilityBlockingSegment seg = Segs[i];
			float t = 0.0f;
			Vector2 hp;

			if (CIntersections.LineVsLine(la, lb, seg.mA, seg.mB, out t, out hp))
			{
				if (t >= 0 && t <= minT)
				{
					hitPoint = hp;
					minT = t;
					segID = i;
				}
			}
		}

		result.mAngle = Mathf.Atan2(ld.x, ld.y);
		result.mStart = la;
		result.mEnd = lb;
		result.mSegID = segID;

		if (segID != -1)
		{
			float len = (hitPoint - la).magnitude;
			result.mEnd = la + ld * len;
		}

		return result;
	}
}
