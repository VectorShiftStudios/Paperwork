using System;
using System.Collections.Generic;
using UnityEngine;

public class CNavEdge
{
	public Vector2 mVertA;
	public Vector2 mVertB;

	public CNavEdge(Vector2 A, Vector2 B)
	{
		mVertA = A;
		mVertB = B;
	}

	public static bool operator ==(CNavEdge LHS, CNavEdge RHS)
	{
		return (LHS.mVertA == RHS.mVertA && LHS.mVertB == RHS.mVertB) || (LHS.mVertA == RHS.mVertB && LHS.mVertB == RHS.mVertA);
	}

	public static bool operator !=(CNavEdge LHS, CNavEdge RHS)
	{
		return !(LHS == RHS);
	}
}

public class CNavTri
{
	public Vector2 p1;
	public Vector2 p2;
	public Vector2 p3;

	public CNavEdge mEdgeA;
	public CNavEdge mEdgeB;
	public CNavEdge mEdgeC;
	
	public CNavTri(Vector2 A, Vector2 B, Vector2 C)
	{
		p1 = A;
		p2 = B;
		p3 = C;

		mEdgeA = new CNavEdge(A, B);
		mEdgeB = new CNavEdge(B, C);
		mEdgeC = new CNavEdge(C, A);
	}

	public static bool operator ==(CNavTri LHS, CNavTri RHS)
	{
		return (LHS.p1 == RHS.p1) || (LHS.p1 == RHS.p2) || (LHS.p1 == RHS.p3) &&
			(LHS.p2 == RHS.p1) || (LHS.p2 == RHS.p2) || (LHS.p2 == RHS.p3) &&
			(LHS.p3 == RHS.p1) || (LHS.p3 == RHS.p2) || (LHS.p3 == RHS.p3);
	}

	public static bool operator !=(CNavTri LHS, CNavTri RHS)
	{
		return !(LHS == RHS);
	}

	public bool HasVert(Vector2 Vert)
	{
		return p1 == Vert || p2 == Vert || p3 == Vert;
	}

	public bool VertInCircumCircle(Vector2 v)
	{
		float ab = Vector2.Dot(p1, p1);
		float cd = Vector2.Dot(p2, p2);
		float ef = Vector2.Dot(p3, p3);

		float circum_x = (ab * (p3.y - p2.y) + cd * (p1.y - p3.y) + ef * (p2.y - p1.y)) / (p1.x * (p3.y - p2.y) + p2.x * (p1.y - p3.y) + p3.x * (p2.y - p1.y)) / 2.0f;
		float circum_y = (ab * (p3.x - p2.x) + cd * (p1.x - p3.x) + ef * (p2.x - p1.x)) / (p1.y * (p3.x - p2.x) + p2.y * (p1.x - p3.x) + p3.y * (p2.x - p1.x)) / 2.0f;
		//float circum_radius = sqrtf(((p1.x - circum_x) * (p1.x - circum_x)) + ((p1.y - circum_y) * (p1.y - circum_y)));
		//float dist = sqrtf(((v.x - circum_x) * (v.x - circum_x)) + ((v.y - circum_y) * (v.y - circum_y)));
		//return dist <= circum_radius;

		float circum_radius = Mathf.Sqrt(((p1.x - circum_x) * (p1.x - circum_x)) + ((p1.y - circum_y) * (p1.y - circum_y)));
		float dist = Mathf.Sqrt(((v.x - circum_x) * (v.x - circum_x)) + ((v.y - circum_y) * (v.y - circum_y)));

		return dist <= circum_radius;
	}
}

public class CNavMesh
{
	public List<CNavTri> mTris = new List<CNavTri>();

	public CNavMesh(List<CVisibilityBlockingSegment> Segs)
	{
		Generate(Segs);
	}

	public void Generate(List<CVisibilityBlockingSegment> Segs)
	{
		// Create the nav mesh based on map info, walls and solid tiles.
		// Walls and tiles need to be lines segments. Also, any metadata segments need to be added.
		// Segment end verts constrained to grid.

		// A* takes unit radius into account, only matters when vert not surrounded by 4 traversable tris.

		// Insert surrounding points? How will this interact with edge padding tiles?

		// Constrained edges must not intersect existing verts.

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		mTris.Add(new CNavTri(new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100)));
		mTris.Add(new CNavTri(new Vector2(0, 0), new Vector2(100, 100), new Vector2(100, 0)));

		//*
		List<Vector2> addedVerts = new List<Vector2>();

		for (int i = 0; i < Segs.Count; ++i)
		{
			Vector2 v = Segs[i].mA;

			bool found = false;
			for (int j = 0; j < addedVerts.Count; ++j)
			{
				if (addedVerts[j] == v)
				{
					found = true;
					break;
				}
			}
			
			if (!found)
			{
				InsertVert(v);
				addedVerts.Add(v);
			}

			v = Segs[i].mB;

			found = false;
			for (int j = 0; j < addedVerts.Count; ++j)
			{
				if (addedVerts[j] == v)
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				InsertVert(v);
				addedVerts.Add(v);
			}
		}


		InsertVert(new Vector2(10, 17));
		InsertVert(new Vector2(10, 16));
		InsertVert(new Vector2(10.5f, 16));
		InsertVert(new Vector2(10.5f, 15.5f));
		InsertVert(new Vector2(11.5f, 15.5f));
		InsertVert(new Vector2(11.5f, 16));
		InsertVert(new Vector2(12, 16));

		//*/

		/*
		for (int i = 0; i < 100; ++i)
		{
			InsertVert(new Vector2(UnityEngine.Random.value * 100.0f, UnityEngine.Random.value * 100.0f));
		}
		//*/

		/*
		InsertVert(new Vector2(10, 30));
		InsertVert(new Vector2(20, 30));
		InsertVert(new Vector2(20, 40));
		InsertVert(new Vector2(10, 40));

		//*/

		// Build edge links

		sw.Stop();
		Debug.Log("NavMesh: " + sw.Elapsed.TotalMilliseconds + "ms Tris: " + mTris.Count);
	}

	public void InsertVert(Vector2 Vert)
	{
		List<CNavEdge> polygon = new List<CNavEdge>();

		// Go through all triangles and if this point falls within the circum circle, then remove the triangle, and store a list of edges.
		for (int i = 0; i < mTris.Count; ++i)
		{
			CNavTri t = mTris[i];

			if (t.VertInCircumCircle(Vert))
			{
				polygon.Add(t.mEdgeA);
				polygon.Add(t.mEdgeB);
				polygon.Add(t.mEdgeC);

				mTris.RemoveAt(i);
				--i;
			}
		}

		// Remove internally shared edges.
		// TODO: Does this have to be n^2?
		for (int i = 0; i < polygon.Count; ++i)
		{
			for (int j = 0; j < polygon.Count; ++j)
			{
				if (i == j)
					continue;

				if (polygon[i] == polygon[j])
				{
					if (i < j)
					{
						polygon.RemoveAt(j);
						polygon.RemoveAt(i);
					}
					else
					{
						polygon.RemoveAt(i);
						polygon.RemoveAt(j);
					}

					--i;
					--j;
					break;
				}
			}
		}

		// Reconstruct triangles within polygon using new vert as centre.
		for (int i = 0; i < polygon.Count; ++i)
		{
			mTris.Add(new CNavTri(polygon[i].mVertA, polygon[i].mVertB, Vert));
		}
	}

	public void DebugDraw()
	{
		// How to draw wire of mesh?

		for (int i = 0; i < mTris.Count; ++i)
		{
			Color c = Color.green;

			CNavTri tri = mTris[i];
			CDebug.DrawTri(tri.p1.ToWorldVec3(), tri.p2.ToWorldVec3(), tri.p3.ToWorldVec3(), new Color(c.r, c.g, c.b, 0.3f), false);
			CDebug.DrawLine(tri.p1.ToWorldVec3(), tri.p2.ToWorldVec3(), c, false);
			CDebug.DrawLine(tri.p2.ToWorldVec3(), tri.p3.ToWorldVec3(), c, false);
			CDebug.DrawLine(tri.p3.ToWorldVec3(), tri.p1.ToWorldVec3(), c, false);

			CDebug.DrawYRectQuad(tri.p1.ToWorldVec3(), 0.1f, 0.1f, Color.white, false);
			CDebug.DrawYRectQuad(tri.p2.ToWorldVec3(), 0.1f, 0.1f, Color.white, false);
			CDebug.DrawYRectQuad(tri.p3.ToWorldVec3(), 0.1f, 0.1f, Color.white, false);
		}
	}
}
