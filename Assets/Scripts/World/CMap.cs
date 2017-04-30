using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public struct STileComponent
{
	// Floor Type:
	// Different tile types. So far we only have one, the 10.

	// Wall Types:
	// >=100 : Door, >=10 : Wall, 0 : No Wall

	public int mType;
	public bool mSolid;
}

/// <summary>
/// Map Tile.
/// </summary>
public class CTile
{
	// Global map solidity
	// Move collision is performed against this map, which is a true representation of real items.
	// FOW calculation is performed against this data.
	// Real items affect this map
	// Doors augment this solidity, all closed doors regardless of locks.

	public Color32 mTint;
	public STileComponent mFloor;
	public STileComponent mWallX;
	public STileComponent mWallZ;

	public int mItem; // ID of item occupying this tile, -1 = none
}

public class CTileInfluence
{
	public float mEfficiency;
	public float mComfort;
	public int mCounter;
}

/// <summary>
/// Tile collision data.
/// </summary>
public class CCollisionTile
{
	// Low Res.
	// Prevents item placement.
	// May be in 1 of 3 states:
	//	  0	= Open
	//	 -1 = Occupied by non-item
	//	>=1	= Occupied by CItemProxy with mOccupied == CItemProxy.mID
	public int mOccupied;

	// Copied from global map, augmented by impassable doors (Locked or owned by other player) and items.
	public bool mSolid;
	public bool mWallXSolid;
	public bool mWallZSolid;

	// Pathfinding. Indicates connection to neighbouring tiles.
	// Need to regenerate mobility map when the above members change.
	public int mMobility;

	public bool IsOccupied
	{
		get { return (mOccupied != 0); }
	}
}

/// <summary>
/// TODO: Can't this just be an SLine?
/// </summary>
public class CVisibilityBlockingSegment
{
	public Vector2 mA;
	public Vector2 mB;

	public CVisibilityBlockingSegment(float Ax, float Ay, float Bx, float By)
	{
		mA = new Vector2(Ax, Ay);
		mB = new Vector2(Bx, By);
	}

	public CVisibilityBlockingSegment(Vector2 A, Vector2 B)
	{
		mA = A;
		mB = B;
	}
}

/// <summary>
/// Room.
/// </summary>
public class CRoom
{
	public int mTileCount = 0;
	public float mPerfMult = 1.0f;
}

public class CMap
{
	private CLevelGenerator _levelGenerator;
	private int _influenceCount = 0;

	public int mWidth;
	public Color mBackgroundColor;
	public CTile[,] mTiles;
	public CCollisionTile[,] mGlobalCollisionTiles;
	public CCollisionTile[][,] mLocalCollisionTiles;
	public CTileInfluence[,] mInfluenceTiles;
	public CNavRectMesh[] mNavMeshes;
	public int[][] _fow;
	
	public List<CVisibilityBlockingSegment> mStaticVisSegments;
	public int mStaticVisSegmentCount;

	public CMap()
	{
		_levelGenerator = new CLevelGenerator(this);
	}

	public void Destroy()
	{
		_levelGenerator.Destroy();
	}

	public void RebuildMesh()
	{
		_levelGenerator.GenerateLevel();
	}

	public GameObject GetLevelGOB()
	{
		return _levelGenerator._gob;
	}

	public void SetFloorAlwaysVisible(bool Value)
	{
		if (_levelGenerator.mFloorAlwaysVisible != Value)
		{
			_levelGenerator.mFloorAlwaysVisible = Value;
			_levelGenerator.GenerateLevel();
		}
	}

	public bool GetFloorAlwaysVisible()
	{
		return _levelGenerator.mFloorAlwaysVisible;
	}

	public void LoadFromAsset(CLevelAsset Asset)
	{
		// TODO: At the moment the asset will create the map.
	}

	private CCollisionTile[,] _CreateCollisionTiles(int Size)
	{
		CCollisionTile[,] tiles = new CCollisionTile[Size, Size];

		for (int iX = 0; iX < Size; ++iX)
			for (int iY = 0; iY < Size; ++iY)
			{
				CCollisionTile tile = tiles[iX, iY] = new CCollisionTile();
				tile.mSolid = false;
				tile.mWallXSolid = false;
				tile.mWallZSolid = false;
				tile.mMobility = 511;
			}

		return tiles;
	}

	public void Init(int Width, Color BackgroundColor)
	{
        mWidth = Width;
		mBackgroundColor = BackgroundColor;
		int colSize = Width * 2;

		mGlobalCollisionTiles = _CreateCollisionTiles(colSize);

		mLocalCollisionTiles = new CCollisionTile[CWorld.MAX_PLAYERS][,];
		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
			mLocalCollisionTiles[i] = _CreateCollisionTiles(colSize);

		mTiles = new CTile[mWidth, mWidth];
		mInfluenceTiles = new CTileInfluence[mWidth, mWidth];
		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mWidth; ++iY)
			{
				CTile tile = mTiles[iX, iY] = new CTile();
				tile.mFloor.mType = 10;
				tile.mWallX.mType = 0;
				tile.mWallZ.mType = 0;
				tile.mTint = new Color32(128, 128, 128, 255);
				tile.mItem = -1;

				CTileInfluence inf = mInfluenceTiles[iX, iY] = new CTileInfluence();
				inf.mComfort = 0;
				inf.mEfficiency = 0;
			}
		}

		mNavMeshes = new CNavRectMesh[CWorld.MAX_PLAYERS];
		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
			mNavMeshes[i] = new CNavRectMesh(this);

		InitFOW();
	}

	public void Serialize(BinaryWriter W)
	{
		// TODO: Take code from asset serialization.

		/*
		W.Write(mWidth);
		// Tiles
		for (int iX = 0; iX < mWidth; ++iX)
			for (int iY = 0; iY < mWidth; ++iY)
			{
				W.Write(mTiles[iX, iY].mFloor.mType);
				W.Write(mTiles[iX, iY].mWallX.mType);
				W.Write(mTiles[iX, iY].mWallZ.mType);
				W.Write(mTiles[iX, iY].mVisibility);
				W.Write(mTiles[iX, iY].mTint.r);
				W.Write(mTiles[iX, iY].mTint.g);
				W.Write(mTiles[iX, iY].mTint.b);
			}
		*/
	}

	public void Deserialize(BinaryReader R)
	{
		// TODO: Take code from asset deserialization.

		/*
		int w = R.ReadInt32();
		Init(w);
		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mWidth; ++iY)
			{
				CTile tile = mTiles[iX, iY];
				tile.mFloor.mType = R.ReadInt32();
				tile.mWallX.mType = R.ReadInt32();
				tile.mWallZ.mType = R.ReadInt32();
				tile.mVisibility = R.ReadInt32();
				tile.mTint.r = R.ReadByte();
				tile.mTint.g = R.ReadByte();
				tile.mTint.b = R.ReadByte();

				if (tile.mWallX.mType >= 10 && tile.mWallX.mType < 100)
				{
					tile.mWallX.mSolid = true;
					tile.mWallX.mBlocksVision = true;
				}

				if (tile.mWallZ.mType >= 10 && tile.mWallZ.mType < 100)
				{
					tile.mWallZ.mSolid = true;
					tile.mWallZ.mBlocksVision = true;
				}

				// TODO: What tile type is less than 10?
				if (tile.mFloor.mType != 10)
					Debug.LogError("Loaded non 10 tile!");

				for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
				{
					mLocalCollisionTiles[i][iX, iY].mWallXSolid = !tile.mWallX.mSolid;
					mLocalCollisionTiles[i][iX, iY].mWallZSolid = !tile.mWallZ.mSolid;
				}
			}
		}
		*/
	}

	/// <summary>
	/// Rebuild the global and local collision maps.
	/// </summary>
	public void GenerateStaticWallCollisions()
	{
		int colSize = mWidth * 2;

		// Build global collision map.
		for (int iX = 0; iX < mWidth; ++iX)
			for (int iY = 0; iY < mWidth; ++iY)
			{
				CTile tile = mTiles[iX, iY];
				
				if (tile.mWallX.mType >= 10 && tile.mWallX.mType < 100)
				{
					tile.mWallX.mSolid = true;
					mGlobalCollisionTiles[iX * 2 + 0, iY * 2].mWallXSolid = true;
					mGlobalCollisionTiles[iX * 2 + 1, iY * 2].mWallXSolid = true;
				}

				if (tile.mWallZ.mType >= 10 && tile.mWallZ.mType < 100)
				{
					tile.mWallZ.mSolid = true;
					mGlobalCollisionTiles[iX * 2, iY * 2 + 0].mWallZSolid = true;
					mGlobalCollisionTiles[iX * 2, iY * 2 + 1].mWallZSolid = true;
				}
			}
		
		// Copy permanent global collision data to local.
		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
			for (int iX = 0; iX < colSize; ++iX)
				for (int iY = 0; iY < colSize; ++iY)
				{
					mLocalCollisionTiles[i][iX, iY].mWallXSolid = mGlobalCollisionTiles[iX, iY].mWallXSolid;
					mLocalCollisionTiles[i][iX, iY].mWallZSolid = mGlobalCollisionTiles[iX, iY].mWallZSolid;
				}

		// Place Safety barrier
		for (int iX = 0; iX < colSize; ++iX)
			for (int iY = 0; iY < colSize; ++iY)
			{				
				if (iX < 2 || iX >= colSize - 2 || iY < 2 || iY >= colSize - 2)
				{
					mGlobalCollisionTiles[iX, iY].mSolid = true;
					mGlobalCollisionTiles[iX, iY].mOccupied = -1;

					for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
					{
						mLocalCollisionTiles[i][iX, iY].mSolid = true;
						mLocalCollisionTiles[i][iX, iY].mOccupied = -1;
					}
				}
			}
	}

	/// <summary>
	/// Generate a static list of segments (edges/verts) that represent the walls.
	/// </summary>
	public void GenerateVisibilitySegments()
	{
		mStaticVisSegments = new List<CVisibilityBlockingSegment>();
		int[,] tiles = new int[mWidth, mWidth];

		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mWidth; ++iY)
			{
				if ((tiles[iX, iY] & 1) == 0)
				{
					int endPoint = iX;
					for (int i = iX; i < mWidth - iX; ++i)
					{
						if (mTiles[i, iY].mWallX.mSolid)
						{
							tiles[i, iY] |= 1;
							endPoint = i + 1;
						}
						else
						{
							break;
						}
					}

					if (endPoint != iX)
						mStaticVisSegments.Add(new CVisibilityBlockingSegment(iX, iY, endPoint, iY));
				}

				if ((tiles[iX, iY] & 2) == 0)
				{
					int endPoint = iY;
					for (int i = iY; i < mWidth - iY; ++i)
					{
						if (mTiles[iX, i].mWallZ.mSolid)
						{
							tiles[iX, i] |= 2;
							endPoint = i + 1;
						}
						else
						{
							break;
						}
					}

					if (endPoint != iY)
						mStaticVisSegments.Add(new CVisibilityBlockingSegment(iX, iY, iX, endPoint));
				}
			}
		}

		mStaticVisSegmentCount = mStaticVisSegments.Count;
	}

	/// <summary>
	/// Make sure we only have the initial static segments in the visibility list.
	/// </summary>
	public void TrimStaticVisSegments()
	{
		if (mStaticVisSegments.Count > mStaticVisSegmentCount)
			mStaticVisSegments.RemoveRange(mStaticVisSegmentCount, mStaticVisSegments.Count - mStaticVisSegmentCount);
	}

	/// <summary>
	/// Rebuild mobility flags for the specified area (Collision tile space).
	/// </summary>
	private void _RebuildMobility(int PlayerID, Rect Area)
	{
		// 3 x 3 array
		// (X + 1) + ((Z + 1) * 3)
		// -1, -1 = 0
		// -1, +0 = 3
		// -1, +1 = 6

		// -0, -1 = 1
		// -0, +0 = 4
		// -0, +1 = 7

		// +1, -1 = 2
		// +1, +0 = 5
		// +1, +1 = 8

		// 9 Movability bits
		// 8 7 6 5 4 3 2 1 0
		// - - - - - - - - -
		//  0 | 1 | 2
		// --- --- ---
		//  3 | 4 | 5
		// --- --- ---
		//  6 | 7 | 8

		// Get cell extents
		int xMin = (int)(Area.xMin - 0.5f);
		int yMin = (int)(Area.yMin - 0.5f);
		int xMax = (int)(Area.xMax + 0.5f);
		int yMax = (int)(Area.yMax + 0.5f);

		// Make sure cells stay within map bounds
		xMin = Mathf.Max(xMin, 1);
		yMin = Mathf.Max(yMin, 1);
		xMax = Mathf.Min(xMax, 198);
		yMax = Mathf.Min(yMax, 198);

		CCollisionTile[,] tiles = mLocalCollisionTiles[PlayerID];
		
		for (int iX = xMin; iX <= xMax; ++iX)
			for (int iZ = yMin; iZ <= yMax; ++iZ)
			{
				int mobility = 511;

				if (tiles[iX, iZ].mSolid)
				{
					tiles[iX, iZ].mMobility = mobility;
					continue;
				}

				// Surrounding tiles
				bool t0 = tiles[iX - 1, iZ + 1].mSolid;
				bool t1 = tiles[iX - 0, iZ + 1].mSolid;
				bool t2 = tiles[iX + 1, iZ + 1].mSolid;
				bool t3 = tiles[iX - 1, iZ + 0].mSolid;				
				bool t5 = tiles[iX + 1, iZ + 0].mSolid;
				bool t6 = tiles[iX - 1, iZ - 1].mSolid;
				bool t7 = tiles[iX - 0, iZ - 1].mSolid;
				bool t8 = tiles[iX + 1, iZ - 1].mSolid;

				// Surrounding walls
				bool w01 = tiles[iX + 0, iZ + 1].mWallZSolid;
				bool w12 = tiles[iX + 1, iZ + 1].mWallZSolid;

				bool w34 = tiles[iX + 0, iZ + 0].mWallZSolid;
				bool w45 = tiles[iX + 1, iZ + 0].mWallZSolid;

				bool w67 = tiles[iX + 0, iZ - 1].mWallZSolid;
				bool w78 = tiles[iX + 1, iZ - 1].mWallZSolid;

				bool w03 = tiles[iX - 1, iZ + 1].mWallXSolid;
				bool w36 = tiles[iX - 1, iZ + 0].mWallXSolid;

				bool w14 = tiles[iX + 0, iZ + 1].mWallXSolid;
				bool w47 = tiles[iX + 0, iZ + 0].mWallXSolid;

				bool w25 = tiles[iX + 1, iZ + 1].mWallXSolid;
				bool w58 = tiles[iX + 1, iZ + 0].mWallXSolid;

				if (t3 || t0 || t1 || w34 || w03 || w01 || w14)
					mobility &= ~(1 << 0);

				if (t1 || w14)
					mobility &= ~(1 << 1);

				if (t1 || t2 || t5 || w12 || w45 || w14 || w25)
					mobility &= ~(1 << 2);

				if (t3 || w34)
					mobility &= ~(1 << 3);

				if (t5 || w45)
					mobility &= ~(1 << 5);

				if (t6 || t3 || t7 || w34 || w67 || w36 || w47)
					mobility &= ~(1 << 6);

				if (t7 || w47)
					mobility &= ~(1 << 7);

				if (t8 || t7 || t5 || w45 || w78 || w47 || w58)
					mobility &= ~(1 << 8);

				tiles[iX, iZ].mMobility = mobility;
			}
	}

	/// <summary>
	/// Rebuild collision information for the entire map and all players.
	/// </summary>
	public void CollisionModified()
	{	
		Rect entireMap = new Rect(0.0f, 0.0f, mWidth * 2, mWidth * 2);

		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
			CollisionModified(i, entireMap);
	}

	/// <summary>
	/// Must be called after a player's local collision map has changed.
	/// </summary>
	public void CollisionModified(int PlayerID, Rect ModifiedArea)
	{
		// NOTE: ModifedArea is in collision tile space already
		_RebuildMobility(PlayerID, ModifiedArea);
		mNavMeshes[PlayerID].mDirty = true;
	}

	public Color GetTileColor(int X, int Y)
	{
		Color32 tint = mTiles[X, Y].mTint;
		return  new Color(tint.r / 255.0f, tint.g / 255.0f, tint.b / 255.0f, 1.0f);
	}

	public void DrawGrid()
	{
		float cellCount = mWidth * 2;
		float cellSize = 0.5f;
		float totalSize = cellCount * cellSize;
		//Color color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
		//Color color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

		Color color = new Color(0.22f, 0.22f, 0.22f, 0.25f);
		Color tileColor = new Color(0.27f, 0.27f, 0.27f, 0.5f);
		Color axisLineColor = new Color(0.34f, 0.34f, 0.34f, 1.0f);

		for (int i = 0; i < cellCount + 1; ++i)
		{
			Color c = color;

			if (i % 2 == 0)
				c = tileColor;

			CDebug.DrawLine(new Vector3(i * cellSize, 0.01f, 0.0f), new Vector3(i * cellSize, 0.01f, totalSize), c);
			CDebug.DrawLine(new Vector3(0.0f, 0.01f, i * cellSize), new Vector3(totalSize, 0.01f, i * cellSize), c);
		}
	}

	/// <summary>
	/// Debug view the solidity and mobility maps.
	/// </summary>
	public void DebugDrawMobility(int PlayerID)
	{
		int colSize = mWidth * 2;

		Color c = Color.yellow;

		for (int iX = 0; iX < colSize; ++iX)
			for (int iY = 0; iY < colSize; ++iY)
			{
				//if (!mLocalCollisionTiles[PlayerID][iX, iZ].mSolid)
					//CDebug.DrawYSquare(new Vector3(iX + 0.5f, 0.1f, iZ + 0.5f), 0.8f, Color.red);

				int mov = mLocalCollisionTiles[PlayerID][iX, iY].mMobility;

				if ((mov & (1 << 0)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.16f) * 0.5f, 0.0f, (iY + 0.82f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 1)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.49f) * 0.5f, 0.0f, (iY + 0.82f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 2)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.82f) * 0.5f, 0.0f, (iY + 0.82f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 3)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.16f) * 0.5f, 0.0f, (iY + 0.49f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 5)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.82f) * 0.5f, 0.0f, (iY + 0.49f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 6)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.16f) * 0.5f, 0.0f, (iY + 0.16f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 7)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.49f) * 0.5f, 0.0f, (iY + 0.16f) * 0.5f), 0.1f, 0.1f, c, false);
				if ((mov & (1 << 8)) == 0) CDebug.DrawYRectQuad(new Vector3((iX + 0.82f) * 0.5f, 0.0f, (iY + 0.16f) * 0.5f), 0.1f, 0.1f, c, false);
			}
	}

	public void DebugDrawCollision(CCollisionTile[,] Tiles, bool Walls = true)
	{
		int colSize = mWidth * 2;

		for (int iX = 0; iX < colSize; ++iX)
			for (int iY = 0; iY < colSize; ++iY)
			{
				CCollisionTile tile = Tiles[iX, iY];

				if (tile.mSolid)
					CDebug.DrawYRectQuad(new Vector3(iX * 0.5f + 0.25f, 0.0f, iY * 0.5f + 0.25f), 0.5f, 0.5f, new Color(1, 0, 0, 0.3f), false);

				if (Walls)
				{
					if (tile.mWallXSolid)
						CDebug.DrawLine(new Vector3(iX * 0.5f, 0.0f, iY * 0.5f), new Vector3(iX * 0.5f + 0.5f, 0.0f, iY * 0.5f), Color.red, false);

					if (tile.mWallZSolid)
						CDebug.DrawLine(new Vector3(iX * 0.5f, 0.0f, iY * 0.5f), new Vector3(iX * 0.5f, 0.0f, iY * 0.5f + 0.5f), Color.red, false);
				}
			}
	}

	public void DebugDrawOccupancy(CCollisionTile[,] Tiles)
	{
		int colSize = mWidth * 2;

		for (int iX = 0; iX < colSize; ++iX)
			for (int iY = 0; iY < colSize; ++iY)
			{
				CCollisionTile tile = Tiles[iX, iY];

				if (tile.IsOccupied)
					CDebug.DrawYRectQuad(new Vector3(iX * 0.5f + 0.25f, 0.0f, iY * 0.5f + 0.25f), 0.5f, 0.5f, new Color(1.0f, 0.5f, 0.0f, 0.3f), false);
			}
	}

	public void DebugDrawTileInfo()
	{
		//DebugDrawCollision(mGlobalCollisionTiles, false);
		DebugDrawOccupancy(mLocalCollisionTiles[0]);

		for (int iX = 0; iX < mWidth; ++iX)
		{
			for (int iY = 0; iY < mWidth; ++iY)
			{
				if (mTiles[iX, iY].mItem != -1)
					CDebug.DrawYRectQuad(new Vector3(iX + 0.5f, 0.0f, iY + 0.5f), 0.8f, 0.8f, new Color(0.1f, 0.3f, 1.0f, 0.3f), false);

				if (mTiles[iX, iY].mWallX.mSolid)
					CDebug.DrawYRectQuad(new Vector3(iX + 0.5f, 0.0f, iY), 1.0f, 0.1f, new Color(1.0f, 0.3f, 0.1f, 0.6f), false);
				//CDebug.DrawLine(new Vector3(iX, 0.0f, iY), new Vector3(iX + 1.0f, 0.0f, iY), Color.red, false);

				if (mTiles[iX, iY].mWallZ.mSolid)
					CDebug.DrawYRectQuad(new Vector3(iX, 0.0f, iY + 0.5f), 0.1f, 1.0f, new Color(1.0f, 0.3f, 0.1f, 0.6f), false);
				//CDebug.DrawLine(new Vector3(iX, 0.0f, iY), new Vector3(iX, 0.0f, iY + 1.0f), Color.red, false);

				//if (mTiles[iX, iZ].mWallX.mBlocksVision)
				//CDebug.DrawLine(new Vector3(iX, 0.2f, iZ), new Vector3(iX + 1.0f, 0.2f, iZ), Color.blue, false);

				//if (mTiles[iX, iZ].mWallZ.mBlocksVision)
				//CDebug.DrawLine(new Vector3(iX, 0.2f, iZ), new Vector3(iX, 0.2f, iZ + 1.0f), Color.blue, false);

				//if (mTiles[iX, iZ].mFloor.mSolid)
				//CDebug.DrawYSquare(new Vector3(iX + 0.5f, 0.0f, iZ + 0.5f), 0.8f, Color.red, false);


				/*int mov = mTileViews[Level][iX, iZ].mMobility;

				if ((mov & (1 << 0)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.16f, 0.0f, iZ + 0.82f), 0.25f, Color.yellow);
				if ((mov & (1 << 1)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.49f, 0.0f, iZ + 0.82f), 0.25f, Color.yellow);
				if ((mov & (1 << 2)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.82f, 0.0f, iZ + 0.82f), 0.25f, Color.yellow);
				if ((mov & (1 << 3)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.16f, 0.0f, iZ + 0.49f), 0.25f, Color.yellow);
				if ((mov & (1 << 5)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.82f, 0.0f, iZ + 0.49f), 0.25f, Color.yellow);
				if ((mov & (1 << 6)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.16f, 0.0f, iZ + 0.16f), 0.25f, Color.yellow);
				if ((mov & (1 << 7)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.49f, 0.0f, iZ + 0.16f), 0.25f, Color.yellow);
				if ((mov & (1 << 8)) == 0) CDebug.DrawYSquare(new Vector3(iX + 0.82f, 0.0f, iZ + 0.16f), 0.25f, Color.yellow);
				*/
			}
		}
	}

	public void DebugDrawTileInfluence()
	{
		Color A = new Color(0.0f, 1.0f, 0.0f, 0.5f);
		Color B = new Color(1.0f, 0.0f, 0.0f, 0.5f);

		Color C = new Color(1.0f, 0.0f, 1.0f, 0.5f);
		Color D = new Color(0.0f, 0.0f, 1.0f, 0.5f);

		for (int iX = 0; iX < mWidth; ++iX)
			for (int iY = 0; iY < mWidth; ++iY)
			{
				if (mInfluenceTiles[iX, iY].mComfort != 0)
				{
					float t = (mInfluenceTiles[iX, iY].mComfort + 20) / 40.0f;
					Color ct = Color.Lerp(A, B, t);
					ct.a = Mathf.Abs(t * 2.0f - 1.0f);
					CDebug.DrawYRectQuad(new Vector3(iX + 0.5f, 0.01f, iY + 0.5f), 0.8f, 0.8f, ct, true);
				}

				if (mInfluenceTiles[iX, iY].mEfficiency != 0)
				{
					float t = (mInfluenceTiles[iX, iY].mEfficiency + 20) / 40.0f;
					Color ct = Color.Lerp(C, D, t);
					ct.a = Mathf.Abs(t * 2.0f - 1.0f);
					CDebug.DrawYRectQuad(new Vector3(iX + 0.5f, 0.02f, iY + 0.5f), 0.4f, 0.4f, ct, true);
				}
			}
	}

	public void InitFOW()
	{
		_fow = new int[CWorld.MAX_PLAYERS][];

		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
		{
			_fow[i] = new int[mWidth * mWidth];
		}
	}

	/// <summary>
	/// Clear FOW table.
	/// </summary>
	public void ResetFOW()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_FOW);

		for (int p = 0; p < CWorld.MAX_PLAYERS; ++p)
		{
			if (p == 0 && CGame.VarNoFow.mValue)
			{
				for (int i = 0; i < mWidth * mWidth; ++i)
					_fow[p][i] = 255;
			}
			else
			{
				Array.Clear(_fow[p], 0, _fow[p].Length);
			}
		}

		CGame.SimThreadProfiler.Pop();
	}

	/// <summary>
	/// Clear LOS in FOW.
	/// </summary>
	public void InjectLOS(int PlayerID, float X, float Y)
	{
		Vector2 pos = new Vector2(X, Y);
		int x = (int)X;
		int y = (int)Y;

		_fow[PlayerID][y * mWidth + x] = 255;

		// Raycast to boundaries
		//Stopwatch combineWatch = new Stopwatch();
		//combineWatch.Start();

		//UnityEngine.Debug.Log("Start");

		//Vector3 nPos = new Vector3((int)pos.x, 0, (int)pos.z);
		Vector2 nPos = new Vector3(pos.x, pos.y);

		int segments = 50;
		float radius = 8;
		float pi2 = Mathf.PI * 2.0f;
		Vector3 prevTarget = Vector3.zero;

		int segs = 0;

		for (int i = 0; i < segments; ++i)
		{
			Vector2 target = nPos;
			target.x += Mathf.Sin((1.0f / segments) * (i + 0) * pi2 + Mathf.PI * 0.25f) * radius;
			target.y += Mathf.Cos((1.0f / segments) * (i + 0) * pi2 + Mathf.PI * 0.25f) * radius;

			//target = new Vector3((int)target.x, (int)target.y, (int)target.z);

			//if (prevTarget == target)
			//continue;

			++segs;

			prevTarget = target;

			//Vector3 target = new Vector3(rayX, 0, nPos.z + 8);

			CRayGridQuery2D rq = new CRayGridQuery2D(nPos, (target - nPos).normalized);

			//UnityEngine.Debug.Log(pos + " " + target + " " + (target - pos).normalized);

			int rX = 0;
			int rY = 0;
			int tX = (int)target.x;
			int tY = (int)target.y;
			int max = 50;
			int dX = 0;
			int dY = 0;

			while (max-- > 0)
			{
				rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);

				if (rX < 1 || rY < 1 || rX >= 99 || rY >= 99)
					break;

				// Distance
				float distX = nPos.x - rX;
				float distY = nPos.y - rY;
				float d = Mathf.Sqrt(distX * distX + distY * distY);
				d = 10 - d;
				d = Mathf.Clamp(d, 0, 8);
				int col = _fow[PlayerID][(rY) * mWidth + (rX)];

				if ((31 * d) > col)
					_fow[PlayerID][(rY) * mWidth + (rX)] = 255;

				if (mTiles[rX, rY].mFloor.mSolid)
					break;

				if ((dX > 0) && (mTiles[rX + 1, rY].mWallZ.mSolid)) break;
				else if ((dX < 0) && (mTiles[rX, rY].mWallZ.mSolid)) break;
				else if ((dY > 0) && (mTiles[rX, rY + 1].mWallX.mSolid)) break;
				else if ((dY < 0) && (mTiles[rX, rY].mWallX.mSolid)) break;

				if (rX == tX && rY == tY)
					break;
			}
		}

		//combineWatch.Stop();
		//UnityEngine.Debug.LogWarning("Trace Time: " + combineWatch.Elapsed.TotalMilliseconds + "ms" + "Segs: " + segs);
	}

	/// <summary>
	/// Reset influence tiles.
	/// </summary>
	public void ResetInfluence()
	{
		CGame.SimThreadProfiler.Push(CProfiler.EID.I_WORLD_INFLUENCE);

		_influenceCount = 0;

		for (int iY = 0; iY < mWidth; ++iY)
		{
			for (int iX = 0; iX < mWidth; ++iX)
			{
				mInfluenceTiles[iX, iY].mComfort = 0;
				mInfluenceTiles[iX, iY].mEfficiency = 0;
				mInfluenceTiles[iX, iY].mCounter = 0;
			}
		}

		CGame.SimThreadProfiler.Pop();
	}

	/// <summary>
	/// Inject item influence.
	/// </summary>
	public void InjectInfluence(float X, float Y, float Comfort, float Efficiency, float Radius)
	{
		Vector2 pos = new Vector2(X, Y);
		int x = (int)X;
		int y = (int)Y;

		++_influenceCount;
		mInfluenceTiles[x, y].mComfort = Comfort;
		mInfluenceTiles[x, y].mEfficiency = Efficiency;

		Vector2 nPos = new Vector3(pos.x, pos.y);

		int segments = 50;
		float pi2 = Mathf.PI * 2.0f;
		
		for (int i = 0; i < segments; ++i)
		{
			Vector2 target = nPos;
			target.x += Mathf.Sin((1.0f / segments) * (i + 0) * pi2 + Mathf.PI * 0.25f) * Radius;
			target.y += Mathf.Cos((1.0f / segments) * (i + 0) * pi2 + Mathf.PI * 0.25f) * Radius;
		
			CRayGridQuery2D rq = new CRayGridQuery2D(nPos, (target - nPos).normalized);
			
			int rX = 0;
			int rY = 0;
			int tX = (int)target.x;
			int tY = (int)target.y;
			int max = 50;
			int dX = 0;
			int dY = 0;

			while (max-- > 0)
			{
				rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);

				if (rX < 1 || rY < 1 || rX >= 99 || rY >= 99)
					break;

				// Distance
				float distX = nPos.x - rX;
				float distY = nPos.y - rY;
				float d = Mathf.Sqrt(distX * distX + distY * distY);
				d = Radius - d;
				d = Mathf.Clamp(d, 0, Radius);

				if (mInfluenceTiles[rX, rY].mCounter < _influenceCount)
				{
					mInfluenceTiles[rX, rY].mComfort += (d / Radius * Comfort);
					mInfluenceTiles[rX, rY].mEfficiency += (d / Radius * Efficiency);
					mInfluenceTiles[rX, rY].mCounter = _influenceCount;
				}

				if ((dX > 0) && (mTiles[rX + 1, rY].mWallZ.mType != 0)) break;
				else if ((dX < 0) && (mTiles[rX, rY].mWallZ.mType != 0)) break;
				else if ((dY > 0) && (mTiles[rX, rY + 1].mWallX.mType != 0)) break;
				else if ((dY < 0) && (mTiles[rX, rY].mWallX.mType != 0)) break;

				if (rX == tX && rY == tY)
					break;
			}
		}
	}

	/// <summary>
	/// Update the next dirty navmesh. 
	/// Only updates a single mesh each call unless RebuildAllImmediate is true.
	/// </summary>
	public void UpdateNavMeshes(bool RebuildAllImmediate = false)
	{
		// TODO: Perf improves: Build each player on a different thread?
		// Only update areas that have changed (would no longer be a dirty update, or dirty on buckets)
		
		for (int i = 0; i < mNavMeshes.Length; ++i)
		{
			if (mNavMeshes[i].mDirty)
			{
				mNavMeshes[i].Generate(this, i);
				mNavMeshes[i].mDirty = false;

				if (!RebuildAllImmediate)
					return;
			}
		}
	}
	
	/// <summary>
	/// Attempt to Move from Start to Dest with collision detection and response.
	/// </summary>
	public Vector2 Move(Vector2 Start, Vector2 Dest, float Radius)
	{
		// TODO: Verify that we don't collide more than a single tile (boundry, radius) away
		// Fastest move speed is 5m/s, which gives us 0.25m per tick, which is half a collision tile.

		// Convert to collision grid resolution.
		Start *= 2.0f;
		Dest *= 2.0f;

		Radius *= 2;

		Vector2 final = Dest;
		Vector2 Dir = Dest - Start;
		int sX = (int)Start.x;
		int sY = (int)Start.y;

		float boundry = 0.2f;
		float hboundry = boundry * 0.5f;

		CCollisionTile[,] tiles = mGlobalCollisionTiles;

		// X Movement
		if (Dir.x > 0.0f)
		{
			if ((int)(final.x + Radius + hboundry) == sX + 1)
			{
				// Middle				
				if (tiles[sX + 1, sY].mSolid || tiles[sX + 1, sY].mWallZSolid)
				{
					final.x = sX + 1.0f - Radius - boundry;
				}
				else
				{
					// Top
					if ((int)(Start.y + Radius + hboundry) == sY + 1)
						if (tiles[sX + 1, sY + 1].mSolid || tiles[sX + 1, sY + 1].mWallZSolid || tiles[sX + 1, sY + 1].mWallXSolid)
							final.x = sX + 1.0f - Radius - boundry;

					// Bottom
					if ((int)(Start.y - Radius - hboundry) == sY - 1)
						if (tiles[sX + 1, sY - 1].mSolid || tiles[sX + 1, sY - 1].mWallZSolid || tiles[sX + 1, sY].mWallXSolid)
							final.x = sX + 1.0f - Radius - boundry;
				}
			}

		}
		else if (Dir.x < 0.0f)
		{
			if ((int)(final.x - Radius - hboundry) == sX - 1)
			{
				// Middle				
				if (tiles[sX - 1, sY].mSolid || tiles[sX, sY].mWallZSolid)
				{
					final.x = sX + Radius + boundry;
				}
				else
				{
					// Top
					if ((int)(Start.y + Radius + hboundry) == sY + 1)
						if (tiles[sX - 1, sY + 1].mSolid || tiles[sX, sY + 1].mWallZSolid || tiles[sX - 1, sY + 1].mWallXSolid)
							final.x = sX + Radius + boundry;

					// Bottom
					if ((int)(Start.y - Radius - hboundry) == sY - 1)
						if (tiles[sX - 1, sY - 1].mSolid || tiles[sX, sY - 1].mWallZSolid || tiles[sX - 1, sY].mWallXSolid)
							final.x = sX + Radius + boundry;
				}
			}
		}

		// Y Movement
		Start.x = final.x;
		sX = (int)Start.x;
		sY = (int)Start.y;

		if (Dir.y > 0.0f)
		{
			if ((int)(final.y + Radius + hboundry) == sY + 1)
			{
				// Middle				
				if (tiles[sX, sY + 1].mSolid || tiles[sX, sY + 1].mWallXSolid)
				{
					final.y = sY + 1.0f - Radius - boundry;
				}
				else
				{
					// Right
					if ((int)(Start.x + Radius + hboundry) == sX + 1)
						if (tiles[sX + 1, sY + 1].mSolid || tiles[sX + 1, sY + 1].mWallZSolid || tiles[sX + 1, sY + 1].mWallXSolid)
							final.y = sY + 1.0f - Radius - boundry;

					// Left
					if ((int)(Start.x - Radius - hboundry) == sX - 1)
						if (tiles[sX - 1, sY + 1].mSolid || tiles[sX, sY + 1].mWallZSolid || tiles[sX - 1, sY + 1].mWallXSolid)
							final.y = sY + 1.0f - Radius - boundry;
				}
			}
		}
		else if (Dir.y < 0.0f)
		{
			if ((int)(final.y - Radius - hboundry) == sY - 1)
			{
				// Middle				
				if (tiles[sX, sY - 1].mSolid || tiles[sX, sY].mWallXSolid)
				{
					final.y = sY + Radius + boundry;
				}
				else
				{
					// Right
					if ((int)(Start.x + Radius + hboundry) == sX + 1)
						if (tiles[sX + 1, sY - 1].mSolid || tiles[sX + 1, sY - 1].mWallZSolid || tiles[sX + 1, sY].mWallXSolid)
							final.y = sY + Radius + boundry;

					// Left
					if ((int)(Start.x - Radius - hboundry) == sX - 1)
						if (tiles[sX - 1, sY - 1].mSolid || tiles[sX, sY - 1].mWallZSolid || tiles[sX - 1, sY].mWallXSolid)
							final.y = sY + Radius + boundry;
				}
			}
		}

		return final * 0.5f;
	}

	/// <summary>
	/// Simply check if a move would have resulted in a collision
	/// </summary>
	public bool Collide(Vector2 Start, Vector2 Dest, float Radius)
	{
		// Convert to collision grid resolution.
		Start *= 2.0f;
		Dest *= 2.0f;

		Vector2 final = Dest;
		Vector2 Dir = Dest - Start;
		int sX = (int)Start.x;
		int sY = (int)Start.y;

		float boundry = 0.1f;

		CCollisionTile[,] tiles = mGlobalCollisionTiles;

		// X Movement
		if (Dir.x > 0.0f)
		{
			if ((int)(final.x + Radius + boundry) == sX + 1)
			{
				// Middle				
				if (tiles[sX + 1, sY].mSolid || tiles[sX + 1, sY].mWallZSolid)
				{
					return true;
				}
				else
				{
					// Top
					if ((int)(Start.y + Radius + boundry) == sY + 1)
						if (tiles[sX + 1, sY + 1].mSolid || tiles[sX + 1, sY + 1].mWallZSolid || tiles[sX + 1, sY + 1].mWallXSolid)
							return true;

					// Bottom
					if ((int)(Start.y - Radius - boundry) == sY - 1)
						if (tiles[sX + 1, sY - 1].mSolid || tiles[sX + 1, sY - 1].mWallZSolid || tiles[sX + 1, sY].mWallXSolid)
							return true;
				}
			}

		}
		else if (Dir.x < 0.0f)
		{
			if ((int)(final.x - Radius - boundry) == sX - 1)
			{
				// Middle				
				if (tiles[sX - 1, sY].mSolid || tiles[sX, sY].mWallZSolid)
				{
					return true;
				}
				else
				{
					// Top
					if ((int)(Start.y + Radius + boundry) == sY + 1)
						if (tiles[sX - 1, sY + 1].mSolid || tiles[sX, sY + 1].mWallZSolid || tiles[sX - 1, sY + 1].mWallXSolid)
							return true;

					// Bottom
					if ((int)(Start.y - Radius - boundry) == sY - 1)
						if (tiles[sX - 1, sY - 1].mSolid || tiles[sX, sY - 1].mWallZSolid || tiles[sX - 1, sY].mWallXSolid)
							return true;
				}
			}
		}

		// Y Movement
		Start.x = final.x;
		sX = (int)Start.x;
		sY = (int)Start.y;

		if (Dir.y > 0.0f)
		{
			if ((int)(final.y + Radius + boundry) == sY + 1)
			{
				// Middle				
				if (tiles[sX, sY + 1].mSolid || tiles[sX, sY + 1].mWallXSolid)
				{
					return true;
				}
				else
				{
					// Right
					if ((int)(Start.x + Radius + boundry) == sX + 1)
						if (tiles[sX + 1, sY + 1].mSolid || tiles[sX + 1, sY + 1].mWallZSolid || tiles[sX + 1, sY + 1].mWallXSolid)
							return true;

					// Left
					if ((int)(Start.x - Radius - boundry) == sX - 1)
						if (tiles[sX - 1, sY + 1].mSolid || tiles[sX, sY + 1].mWallZSolid || tiles[sX - 1, sY + 1].mWallXSolid)
							return true;
				}
			}
		}
		else if (Dir.y < 0.0f)
		{
			if ((int)(final.y - Radius - boundry) == sY - 1)
			{
				// Middle
				if (tiles[sX, sY - 1].mSolid || tiles[sX, sY].mWallXSolid)
				{
					return true;
				}
				else
				{
					// Right
					if ((int)(Start.x + Radius + boundry) == sX + 1)
						if (tiles[sX + 1, sY - 1].mSolid || tiles[sX + 1, sY - 1].mWallZSolid || tiles[sX + 1, sY].mWallXSolid)
							return true;

					// Left
					if ((int)(Start.x - Radius - boundry) == sX - 1)
						if (tiles[sX - 1, sY - 1].mSolid || tiles[sX, sY - 1].mWallZSolid || tiles[sX - 1, sY].mWallXSolid)
							return true;
				}
			}
		}

		return false;
	}

	public bool IsTileVisible(int PlayerID, int X, int Y)
	{
		if (_fow[PlayerID][(Y) * mWidth + (X)] != 0)
			return true;

		return false;
	}

	public bool IsTileReachable(int PlayerID, IntVec2 Start, IntVec2 Dir)
	{
		int bit = (Dir.X + 1) + ((-Dir.Y + 1) * 3);

		if ((mLocalCollisionTiles[PlayerID][Start.X, Start.Y].mMobility & (1 << bit)) > 0)
			return true;

		return false;
	}

	public bool IsTileSolid(int PlayerID, IntVec2 Tile)
	{
		return mLocalCollisionTiles[PlayerID][Tile.X, Tile.Y].mSolid;
	}

	public bool IsTileItem(int PlayerID, IntVec2 Tile)
	{
		return mLocalCollisionTiles[PlayerID][Tile.X, Tile.Y].mOccupied > 0;
	}

	public bool IsTileInBounds(IntVec2 Tile)
	{
		int colSize = mWidth * 2;

		return (Tile.X >= 0 && Tile.X < colSize && Tile.Y >= 0 && Tile.Y < colSize);
	}

	/// <summary>
	/// Check if there is LOS to a point from a point.
	/// This is a visibility check, does not care about floor solidity.
	/// </summary>	
	public bool IsPointVisible(Vector2 Start, Vector2 Point, Vector2 Direction)
	{
		CRayGridQuery2D rq = new CRayGridQuery2D(Start, Direction);

		int rX = 0;
		int rY = 0;
		int tX = (int)Point.x;
		int tY = (int)Point.y;
		int max = 1000;
		int dX = 0;
		int dY = 0;

		while (max-- > 0)
		{
			rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);

			if (rX <= 0 || rX >= mWidth - 1 || rY <= 0 || rY >= mWidth - 1)
				return false;
			
			if ((dX > 0) && (mTiles[rX + 1, rY].mWallZ.mSolid)) return false;
			else if ((dX < 0) && (mTiles[rX, rY].mWallZ.mSolid)) return false;
			else if ((dY > 0) && (mTiles[rX, rY + 1].mWallX.mSolid)) return false;
			else if ((dY < 0) && (mTiles[rX, rY].mWallX.mSolid)) return false;

			if (rX == tX && rY == tY)
				return true;
		}

		Debug.LogError("IsPointVisible: Used too many steps in LOS query.");
		return false;
	}

	/// <summary>
	/// Check if there is LOS to a point within specified range.
	/// </summary>
	public bool IsPointInRangeVisible(Vector2 Start, Vector2 Point, float Distance)
	{
		Vector2 dist = Point - Start;
		float sqrLen = dist.sqrMagnitude;

		if (sqrLen <= Distance * Distance)
		{
			dist.Normalize();
			return IsPointVisible(Start, Point, dist);
		}

		return false;
	}

	public bool TraceNodes(int PlayerID, Vector2 A, Vector2 B)
	{
		A *= 2.0f;
		B *= 2.0f;

		Vector2 dir = (B - A).normalized;
		CRayGridQuery2D rq = new CRayGridQuery2D(A, dir);

		int rX = 0;
		int rY = 0;
		int tX = (int)B.x;
		int tY = (int)B.y;
		int dX = 0;
		int dY = 0;
		int sX = (int)A.x;
		int sY = (int)A.y;

		bool los = true;

		int maxSteps = 1000;
		while ((rX != tX || rY != tY) && --maxSteps > 0)
		{
			int bit = (dX + 1) + ((-dY + 1) * 3);
			if ((mLocalCollisionTiles[PlayerID][sX, sY].mMobility & (1 << bit)) == 0)
			{
				los = false;
				break;
			}
			
			rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);
			sX = rX;
			sY = rY;
		}

		if (maxSteps == 0)
			Debug.LogWarning("Path Trace MAX STEPS " + A + " " + B + " " + dir);

		return los;
	}

	public bool TraceNodesDebug(int PlayerID, Vector2 A, Vector2 B)
	{
		A *= 2.0f;
		B *= 2.0f;

		Vector2 dir = (B - A).normalized;
		CRayGridQuery2D rq = new CRayGridQuery2D(A, dir);

		int rX = 0;
		int rY = 0;
		int tX = (int)B.x;
		int tY = (int)B.y;
		int dX = 0;
		int dY = 0;
		int sX = (int)A.x;
		int sY = (int)A.y;

		bool los = true;

		int maxSteps = 1000;
		while ((rX != tX || rY != tY) && --maxSteps > 0)
		//while (--maxSteps > 0)
		{
			CDebug.DrawYSquare(new Vector3(sX * 0.5f + 0.25f, 0.0f, sY * 0.5f + 0.25f), 0.5f, Color.black, false);
			CDebug.DrawYSquare(new Vector3(sX * 0.5f + 0.25f + dX * 0.2f, 0.0f, sY * 0.5f + 0.25f + dY * 0.2f), 0.1f, Color.red, false);
			
			//Debug.DrawLine(new Vector3(rX + 0.5f, 0.1f, rY + 0.5f), new Vector3(rX + 0.5f, 1.0f, rY + 0.5f), Color.magenta, 1.0f);			


			int bit = (dX + 1) + ((-dY + 1) * 3);

			if ((mLocalCollisionTiles[PlayerID][sX, sY].mMobility & (1 << bit)) == 0)
			{
				los = false;
				break;
			}

			rq.GetNextCell(ref rX, ref rY, ref dX, ref dY);



			sX = rX;
			sY = rY;


			/*
			if (CGame.mSingleton.mWorld.mTiles[rX, rY].mSolid != 0)
			{
				los = false;
				break;
			}
			*/
		}

		if (los)
		{
			//CDebug.DrawYSquare(new Vector3(tX * 0.5f + 0.25f, 0.0f, tY * 0.5f + 0.25f), 0.5f, Color.black, false);
		}

		if (maxSteps == 0)
			Debug.LogWarning("Path Trace MAX STEPS " + A + " " + B + " " + dir);

		/*
		if (los)
			Debug.DrawLine(new Vector3(A.x, 0.1f, A.y), new Vector3(B.x, 0.1f, B.y), Color.green, 5.0f);
		else
			Debug.DrawLine(new Vector3(A.x, 0.1f, A.y), new Vector3(B.x, 0.1f, B.y), Color.red, 5.0f);
		*/

		return los;
	}

	public void SetTileRebuild(int Type, int Value, int X, int Y)
	{
		if (Type == 1)
			mTiles[X, Y].mWallX.mType = Value;
		else if (Type == 2)
			mTiles[X, Y].mWallZ.mType = Value;
		else
			mTiles[X, Y].mFloor.mType = Value;

		RebuildMesh();
	}
}

