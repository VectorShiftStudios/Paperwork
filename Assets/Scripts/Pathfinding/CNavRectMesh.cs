using System;
using System.Collections.Generic;
using UnityEngine;

public class CNavRectPortal
{
	public Vector2 mA;
	public Vector2 mB;
	public Vector2 mCentre;
	public CNavRect mRectA;
	public CNavRect mRectB;

	public CNavRectPortal(Vector2 A, Vector2 B, CNavRect RectA, CNavRect RectB)
	{
		mA = A;
		mB = B;
		// NOTE: The centre is at half size for the pathfinder.
		mCentre = ((B - A) * 0.5f + A) * 0.5f;
		mRectA = RectA;
		mRectB = RectB;
	}
}

public class CNavRectEdge
{
	public Vector2 mA;
	public Vector2 mB;
	public CNavRect mRect;

	public CNavRectEdge(Vector2 A, Vector2 B, CNavRect Rect)
	{
		mA = A;
		mB = B;
		mRect = Rect;
	}
}

public class CNavRect
{
	public int mIndex;
	public int mFlags;
	public int mRoomId;
	public Rect mRect;
	public List<CNavRectPortal> mPortals = new List<CNavRectPortal>();
}

public class CNavRectMesh
{
	public List<CNavRect> mRects = new List<CNavRect>();
	public List<CNavRectEdge> mEdges = new List<CNavRectEdge>();
	public List<CNavRectPortal> mPortals = new List<CNavRectPortal>();
	public List<CNavRect>[,] mBuckets = new List<CNavRect>[7, 7];
	public CNavRect[,] mRectLookup;
	public List<CNavRectEdge>[] mXAxisEdges;
	public List<CNavRectEdge>[] mYAxisEdges;
	public bool mDirty = true;

	public CNavRectMesh(CMap Map)
	{
		for (int iX = 0; iX < 7; ++iX)
			for (int iY = 0; iY < 7; ++iY)
				mBuckets[iX, iY] = new List<CNavRect>();

		mRectLookup = new CNavRect[Map.mWidth * 2, Map.mWidth * 2];
		mXAxisEdges = new List<CNavRectEdge>[Map.mWidth * 2 + 1];
		mYAxisEdges = new List<CNavRectEdge>[Map.mWidth * 2 + 1];

		for (int i = 0; i < Map.mWidth * 2 + 1; ++i)
		{
			mXAxisEdges[i] = new List<CNavRectEdge>();
			mYAxisEdges[i] = new List<CNavRectEdge>();
		}
	}

	private bool _CanExpandX(CNavRect[,] TileState, CCollisionTile[,] Tiles, int X, int Y, int Size, int Type)
	{
		for (int y = Y; y < Size + Y; ++y)
		{
			if (TileState[X + 1, y] != null)
				return false;

			//if (Tiles[X + 1, y].mOccupied + 10000 * (Tiles[X + 1, y].mSolid ? 1 : 0) != Type)
			if ((Tiles[X + 1, y].mOccupied + 10000) * (Tiles[X + 1, y].mSolid ? 1 : 0) != Type)
					return false;

			if (Tiles[X + 1, y].mWallZSolid)
				return false;

			if (y < Y + Size - 1)
			{
				if (Tiles[X + 1, y + 1].mWallXSolid)
					return false;
			}
		}

		return true;
	}

	private bool _CanExpandY(CNavRect[,] TileState, CCollisionTile[,] Tiles, int X, int Y, int Size, int Type)
	{
		for (int x = X; x < Size + X; ++x)
		{
			if (TileState[x, Y + 1] != null)
				return false;

			//if (Tiles[x, Y + 1].mOccupied + 10000 * (Tiles[x, Y + 1].mSolid ? 1 : 0) != Type)
			if ((Tiles[x, Y + 1].mOccupied + 10000) * (Tiles[x, Y + 1].mSolid ? 1 : 0) != Type)
				return false;

			if (Tiles[x, Y + 1].mWallXSolid)
				return false;

			if (x < X + Size - 1)
			{
				if (Tiles[x + 1, Y + 1].mWallZSolid)
					return false;
			}
		}

		return true;
	}

	public void Generate(CMap Map, int PlayerId)
	{
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		mRects.Clear();
		mEdges.Clear();
		mPortals.Clear();

		Array.Clear(mRectLookup, 0, mRectLookup.Length);

		for (int i = 0; i < Map.mWidth * 2 + 1; ++i)
		{
			mXAxisEdges[i].Clear();
			mYAxisEdges[i].Clear();
		}

		for (int iX = 0; iX < 7; ++iX)
			for (int iY = 0; iY < 7; ++iY)
				mBuckets[iX, iY].Clear();
		
		CCollisionTile[,] tileMap = Map.mLocalCollisionTiles[PlayerId];

		double setupTime = sw.Elapsed.TotalMilliseconds;

		// Touch all cells and expand rects
		for (int iY = 0; iY < Map.mWidth * 2; ++iY)
			for (int iX = 0; iX < Map.mWidth * 2; ++iX)
			{
				CCollisionTile tile = tileMap[iX, iY];
				
				if (mRectLookup[iX, iY] == null && tile.mOccupied >= 0)
				{
					bool expandX = true;
					bool expandY = true;
					int sizeX = 1;
					int sizeY = 1;
					//int type = tile.mOccupied + 10000 * (tile.mSolid ? 1 : 0);
					int type = (tile.mOccupied + 10000) * (tile.mSolid ? 1 : 0);

					while (expandX || expandY)
					{
						// Check expansion in X
						if (expandX)
							expandX = _CanExpandX(mRectLookup, tileMap, iX + sizeX - 1, iY, sizeY, type);

						// Check expansion in Y
						if (expandY)
							expandY = _CanExpandY(mRectLookup, tileMap, iX, iY + sizeY - 1, sizeX, type);
						
						// Check corner expansion if we do both
						if (expandX && expandY)
						{
							// If we fail here, then prefer expand X.
							if (mRectLookup[iX + sizeX, iY + sizeY] != null ||
								//tileMap[iX + sizeX, iY + sizeY].mOccupied + 10000 * (tileMap[iX + sizeX, iY + sizeY].mSolid ? 1 : 0) != type ||
								(tileMap[iX + sizeX, iY + sizeY].mOccupied + 10000) * (tileMap[iX + sizeX, iY + sizeY].mSolid ? 1 : 0) != type ||
								tileMap[iX + sizeX, iY + sizeY].mWallXSolid ||
								tileMap[iX + sizeX, iY + sizeY].mWallZSolid)
									expandY = false;
						}

						if ((iX + sizeX) % 32 == 0) expandX = false;
						if ((iY + sizeY) % 32 == 0) expandY = false;

						if (expandX) ++sizeX;
						if (expandY) ++sizeY;
					}
					
					// Add nav rect.
					CNavRect r = new CNavRect();
					r.mIndex = mRects.Count;
					r.mRect = new Rect(iX, iY, sizeX, sizeY);
					r.mFlags = type;
					r.mRoomId = -1;
					mRects.Add(r);
					mBuckets[iX / 32, iY / 32].Add(r);

					// Clear expanded area
					for (int jX = iX; jX < iX + sizeX; ++jX)
						for (int jY = iY; jY < iY + sizeY; ++jY)
						{
							mRectLookup[jX, jY] = r;
						}
				}
			}

		double expansionTime = sw.Elapsed.TotalMilliseconds;

		// Go through each rect and determine accesible edges.
		for (int i = 0; i < mRects.Count; ++i)
		{
			Rect r = mRects[i].mRect;

			//-----------------------------------------------------------------------------
			// Bottom X
			//-----------------------------------------------------------------------------
			int start = (int)r.xMin;
			int run = 0;
			int y = (int)r.y;
			int x = (int)r.x;

			for (int j = start; j < (int)r.xMax; ++j)
			{	
				if (tileMap[j, y].mWallXSolid)
				{
					if (run != 0)
					{
						// Add run to this point
						CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
						mEdges.Add(e);
						mXAxisEdges[y].Add(e);
						run = 0;
					}

					start = j + 1;
				}
				else
				{
					if (run != 0 && tileMap[j, y - 1].mWallZSolid)
					{
						// Add run to this point and start with 1 already in run
						CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
						mEdges.Add(e);
						mXAxisEdges[y].Add(e);

						start = j;
						run = 0;
					}

					++run;
				}
			}

			// Add final non-blocked edge.
			if (run != 0)
			{
				CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
				mEdges.Add(e);
				mXAxisEdges[y].Add(e);
			}

			//-----------------------------------------------------------------------------
			// Top X
			//-----------------------------------------------------------------------------
			start = (int)r.xMin;
			run = 0;
			y = (int)r.yMax;
			x = (int)r.x;
			
			for (int j = start; j < (int)r.xMax; ++j)
			{
				if (tileMap[j, y].mWallXSolid)
				{
					if (run != 0)
					{
						// Add run to this point
						CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
						mEdges.Add(e);
						mXAxisEdges[y].Add(e);

						run = 0;
					}

					start = j + 1;
				}
				else
				{
					if (run != 0 && tileMap[j, y].mWallZSolid)
					{
						// Add run to this point and start with 1 already in run
						CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
						mEdges.Add(e);
						mXAxisEdges[y].Add(e);

						start = j;
						run = 0;
					}

					++run;
				}
			}

			// Add final non-blocked edge.
			if (run != 0)
			{
				CNavRectEdge e = new CNavRectEdge(new Vector2(start, y), new Vector2(start + run, y), mRects[i]);
				mEdges.Add(e);
				mXAxisEdges[y].Add(e);
			}

			//-----------------------------------------------------------------------------
			// Left Y
			//-----------------------------------------------------------------------------
			start = (int)r.yMin;
			run = 0;
			y = (int)r.y;
			x = (int)r.x;

			for (int j = start; j < (int)r.yMax; ++j)
			{
				if (tileMap[x, j].mWallZSolid)
				{
					if (run != 0)
					{
						// Add run to this point
						CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
						mEdges.Add(e);
						mYAxisEdges[x].Add(e);

						run = 0;
					}

					start = j + 1;
				}
				else
				{
					if (run != 0 && tileMap[x - 1, j].mWallXSolid)
					{
						// Add run to this point and start with 1 already in run
						CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
						mEdges.Add(e);
						mYAxisEdges[x].Add(e);

						start = j;
						run = 0;
					}

					++run;
				}
			}

			// Add final non-blocked edge.
			if (run != 0)
			{
				CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
				mEdges.Add(e);
				mYAxisEdges[x].Add(e);
			}

			//-----------------------------------------------------------------------------
			// Right Y
			//-----------------------------------------------------------------------------
			start = (int)r.yMin;
			run = 0;
			y = (int)r.y;
			x = (int)r.xMax;

			for (int j = start; j < (int)r.yMax; ++j)
			{
				if (tileMap[x, j].mWallZSolid)
				{
					if (run != 0)
					{
						// Add run to this point
						CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
						mEdges.Add(e);
						mYAxisEdges[x].Add(e);

						run = 0;
					}

					start = j + 1;
				}
				else
				{
					if (run != 0 && tileMap[x, j].mWallXSolid)
					{
						// Add run to this point and start with 1 already in run
						CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
						mEdges.Add(e);
						mYAxisEdges[x].Add(e);

						start = j;
						run = 0;
					}

					++run;
				}
			}

			// Add final non-blocked edge.
			if (run != 0)
			{
				CNavRectEdge e = new CNavRectEdge(new Vector2(x, start), new Vector2(x, start + run), mRects[i]);
				mEdges.Add(e);
				mYAxisEdges[x].Add(e);
			}
		}

		double edgesTime = sw.Elapsed.TotalMilliseconds;

		// Generate portals.
		for (int k = 0; k < Map.mWidth * 2 + 1; ++k)
		{
			List<CNavRectEdge> edges = mXAxisEdges[k];

			for (int i = 0; i < edges.Count; ++i)
			{
				for (int j = i + 1; j < edges.Count; ++j)
				{
					float min = Mathf.Max(edges[i].mA.x, edges[j].mA.x);
					float max = Mathf.Min(edges[i].mB.x, edges[j].mB.x);

					if (min < max)
					{
						CNavRectPortal p = new CNavRectPortal(new Vector2(min, edges[i].mA.y), new Vector2(max, edges[i].mA.y), edges[i].mRect, edges[j].mRect);
						mPortals.Add(p);
						edges[i].mRect.mPortals.Add(p);
						edges[j].mRect.mPortals.Add(p);
					}
				}
			}

			edges = mYAxisEdges[k];

			for (int i = 0; i < edges.Count; ++i)
			{
				for (int j = i + 1; j < edges.Count; ++j)
				{
					float min = Mathf.Max(edges[i].mA.y, edges[j].mA.y);
					float max = Mathf.Min(edges[i].mB.y, edges[j].mB.y);

					if (min < max)
					{
						CNavRectPortal p = new CNavRectPortal(new Vector2(edges[i].mA.x, min), new Vector2(edges[i].mA.x, max), edges[i].mRect, edges[j].mRect);
						mPortals.Add(p);
						edges[i].mRect.mPortals.Add(p);
						edges[j].mRect.mPortals.Add(p);
					}
				}
			}
		}

		double portalsTime = sw.Elapsed.TotalMilliseconds;

		// Generate room IDs.
		int roomCounter = 1;
		List<CNavRect> rectStack = new List<CNavRect>();
		int rectScan = 0;

		while (rectScan < mRects.Count)
		{
			if (mRects[rectScan].mRoomId == -1)
			{
				if (mRects[rectScan].mFlags >= 10000)
				{
					mRects[rectScan].mRoomId = 0;
				}
				else
				{
					rectStack.Add(mRects[rectScan]);
					mRects[rectScan].mRoomId = roomCounter;

					while (rectStack.Count > 0)
					{
						CNavRect rect = rectStack[0];
						rectStack.RemoveAt(0);

						for (int i = 0; i < rect.mPortals.Count; ++i)
						{
							CNavRectPortal p = rect.mPortals[i];

							if (p.mRectA.mFlags < 10000 && p.mRectA.mRoomId == -1)
							{
								rectStack.Add(p.mRectA);
								p.mRectA.mRoomId = roomCounter;
							}

							if (p.mRectB.mFlags < 10000 && p.mRectB.mRoomId == -1)
							{
								rectStack.Add(p.mRectB);
								p.mRectB.mRoomId = roomCounter;
							}
						}
					}
				}

				++roomCounter;
			}

			++rectScan;
		}

		double roomTime = sw.Elapsed.TotalMilliseconds;

		sw.Stop();
		Debug.Log("NavRect: " + sw.Elapsed.TotalMilliseconds.ToString("0.000") +
			"ms Setup: " + setupTime.ToString("0.000") +
			"ms Rects(" + mRects.Count + "): " + (expansionTime - setupTime).ToString("0.000") +
			"ms Edges(" + mEdges.Count + "): " + (edgesTime - expansionTime).ToString("0.000") +
			"ms Portals(" + mPortals.Count + "): " + (portalsTime - edgesTime).ToString("0.000") +
			"ms Rooms: " + (roomTime - portalsTime).ToString("0.000") + "ms");
	}

	public void DebugDraw()
	{
		List<CNavRect> rects = mRects;
		//List<CNavRect> rects = mBuckets[1,1];

		for (int i = 0; i < rects.Count; ++i)
		{	
			float red = ((rects[i].mRoomId * 30) % 255) / 255.0f;
			float green = ((rects[i].mRoomId * 60) % 255) / 255.0f;
			float blue = ((rects[i].mRoomId * 90) % 255) / 255.0f;
			Color c = new Color(red, green, blue, 0.5f);
			//Color c = Color.green;

			if (rects[i].mFlags > 10000)
				c = Color.red;
			//else if (rects[i].mFlags != 0)
				//c = Color.cyan;
			
			Rect r = rects[i].mRect;
			Rect rect = new Rect(r.x * 0.5f, r.y * 0.5f, r.width * 0.5f, r.height * 0.5f);
			Vector3 rc = rect.center.ToWorldVec3();

			CDebug.DrawYRectQuad(rc, rect.width, rect.height, new Color(c.r, c.g, c.b, 0.3f), false);
			CDebug.DrawYRect(rect.center.ToWorldVec3(), rect.width, rect.height, c, false);
			
			for (int j = 0; j < rects[i].mPortals.Count; ++j)
			{
				CNavRectPortal p = rects[i].mPortals[j];
				Vector3 a = p.mA.ToWorldVec3() * 0.5f;
				Vector3 b = p.mB.ToWorldVec3() * 0.5f;
				Vector3 pc = (b - a) * 0.5f + a;

				CDebug.DrawLine(pc, rc, Color.yellow, false);
			}
		}

		for (int i = 0; i < mPortals.Count; ++i)
		{
			Vector3 a = mPortals[i].mA.ToWorldVec3() * 0.5f;
			Vector3 b = mPortals[i].mB.ToWorldVec3() * 0.5f;
			Vector3 c = (b - a) * 0.5f + a;

			CDebug.DrawYRectQuad(c, 0.05f, 0.05f, Color.black, false);
			CDebug.DrawLine(a, b, Color.black, false);

			//CDebug.DrawYRectQuad(a, 0.05f, 0.05f, Color.white, false);
			//CDebug.DrawYRectQuad(b, 0.05f, 0.05f, Color.white, false);
		}

		/*
		for (int i = 0; i < mEdges.Count; ++i)
		{
			Vector3 a = Vector3.zero;
			Vector3 b = Vector3.zero;

			if (mEdges[i].mType == 0)
			{
				a = mEdges[i].mA.ToWorldVec3() * 0.5f + new Vector3(0.04f, 0, 0.04f);
				b = mEdges[i].mB.ToWorldVec3() * 0.5f + new Vector3(-0.04f, 0, 0.04f);
			}
			else if (mEdges[i].mType == 1)
			{
				a = mEdges[i].mA.ToWorldVec3() * 0.5f + new Vector3(0.04f, 0, -0.04f);
				b = mEdges[i].mB.ToWorldVec3() * 0.5f + new Vector3(-0.04f, 0, -0.04f);
			}
			else if (mEdges[i].mType == 2)
			{
				a = mEdges[i].mA.ToWorldVec3() * 0.5f + new Vector3(0.04f, 0, 0.04f);
				b = mEdges[i].mB.ToWorldVec3() * 0.5f + new Vector3(0.04f, 0, -0.04f);
			}
			else if (mEdges[i].mType == 3)
			{
				a = mEdges[i].mA.ToWorldVec3() * 0.5f + new Vector3(-0.04f, 0, 0.04f);
				b = mEdges[i].mB.ToWorldVec3() * 0.5f + new Vector3(-0.04f, 0, -0.04f);
			}

			CDebug.DrawLine(a, b, Color.red, false);
			CDebug.DrawYRectQuad(a, 0.05f, 0.05f, Color.white, false);
			CDebug.DrawYRectQuad(b, 0.05f, 0.05f, Color.white, false);
		}
		//*/
	}
}
