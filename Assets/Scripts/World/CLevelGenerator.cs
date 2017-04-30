using System;
using System.Collections.Generic;
using UnityEngine;

public class CLevelGenerator
{	
	private CMap _map;
	private GameObject _gobFloor;
	private GameObject _gobWalls;
	private Mesh _meshFloor;
	private Mesh _meshWalls;

	public GameObject _gob;
	public bool mFloorAlwaysVisible = false;

	public CLevelGenerator(CMap Map)
	{
		_map = Map;
	}

	public void Destroy()
	{
		if (_meshFloor) GameObject.Destroy(_meshFloor);
		if (_meshWalls) GameObject.Destroy(_meshWalls);
	}

	public void GenerateLevel()
	{
		int width = _map.mWidth;
		int height = _map.mWidth;
		CTile[,] tiles = _map.mTiles;

		Destroy();

		Transform oldParent = null;
		if (_gob != null)
		{
			oldParent = _gob.transform.parent;
			GameObject.Destroy(_gob);
		}

		_gob = new GameObject("level mesh");

		if (oldParent != null)
			_gob.transform.SetParent(oldParent);

		CModelAsset doorFrame = CGame.AssetManager.GetAsset<CModelAsset>("default_door_frame");

		for (int iX = 0; iX < width; ++iX)
			for (int iY = 0; iY < height; ++iY)
			{
				CTile tile = tiles[iX, iY];

				if (tile.mWallX.mType >= 100)
				{
					GameObject door = doorFrame.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
					door.transform.position = new Vector3(iX + 0.5f, 0.0f, iY);
					door.transform.rotation = CUtility.FacingTable[2];
					door.transform.parent = _gob.transform;
				}
				else if (tile.mWallX.mType >= 10)
				{
				}

				if (tile.mWallZ.mType >= 100)
				{
					GameObject door = doorFrame.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
					door.transform.position = new Vector3(iX, 0.0f, iY + 0.5f);
					door.transform.rotation = CUtility.FacingTable[1];
					door.transform.parent = _gob.transform;
				}
				else if (tile.mWallZ.mType >= 10)
				{
				}					
			}
		
		// Floor Mesh
		_gobFloor = new GameObject("floorTiles");
		_gobFloor.transform.parent = _gob.transform;
		_meshFloor = _gobFloor.AddComponent<MeshFilter>().mesh = new Mesh();

		if (mFloorAlwaysVisible)
			_gobFloor.AddComponent<MeshRenderer>().material = CGame.WorldResources.FloorVisibleMat;
		else
			_gobFloor.AddComponent<MeshRenderer>().material = CGame.WorldResources.FloorMat;

		// Count Flat Tiles
		int tileCount = width * height;

		int[] tris = new int[tileCount * 6];
		Vector3[] verts = new Vector3[tileCount * 4];
		Vector2[] uvs = new Vector2[tileCount * 4];
		Vector4[] tanList = new Vector4[tileCount * 4];
		Color32[] cols = new Color32[tileCount * 4];
		int t = 0;
		int v = 0;
				
		for (int iX = 0; iX < width; ++iX)
			for (int iY = 0; iY < height; ++iY)
			{
				verts[v + 0] = new Vector3(iX + 0.0f, 0.0f, iY + 0.0f);
				verts[v + 1] = new Vector3(iX + 0.0f, 0.0f, iY + 1.0f);
				verts[v + 2] = new Vector3(iX + 1.0f, 0.0f, iY + 1.0f);
				verts[v + 3] = new Vector3(iX + 1.0f, 0.0f, iY + 0.0f);

				float texScale = 0.5f;

				uvs[v + 0] = new Vector2(iX * texScale, iY * texScale);
				uvs[v + 1] = new Vector2(iX * texScale, (iY + 1) * texScale);
				uvs[v + 2] = new Vector2((iX + 1) * texScale, (iY + 1) * texScale);
				uvs[v + 3] = new Vector2((iX + 1) * texScale, iY * texScale);

				cols[v + 0] = tiles[iX, iY].mTint;
				cols[v + 1] = tiles[iX, iY].mTint;
				cols[v + 2] = tiles[iX, iY].mTint;
				cols[v + 3] = tiles[iX, iY].mTint;

				tanList[v + 0] = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
				tanList[v + 1] = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
				tanList[v + 2] = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
				tanList[v + 3] = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);

				tris[t + 0] = v + 0;
				tris[t + 1] = v + 1;
				tris[t + 2] = v + 2;
				tris[t + 3] = v + 0;
				tris[t + 4] = v + 2;
				tris[t + 5] = v + 3;

				v += 4;
				t += 6;				
			}

		_meshFloor.vertices = verts;
		_meshFloor.uv = uvs;
		_meshFloor.triangles = tris;
		_meshFloor.tangents = tanList;
		_meshFloor.colors32 = cols;
		_meshFloor.RecalculateNormals();

		_CreateWallMesh();
	}

	private void _CreateWallMesh()
	{
		int width = _map.mWidth;
		int height = _map.mWidth;
		CTile[,] tiles = _map.mTiles;

		// Wall Mesh
		_gobWalls = new GameObject("walls");
		_gobWalls.transform.parent = _gob.transform;
		MeshFilter wallsMeshFilter = _gobWalls.AddComponent<MeshFilter>();
		wallsMeshFilter.mesh = new Mesh();
		MeshRenderer wallsMeshRenderer = _gobWalls.AddComponent<MeshRenderer>();
		wallsMeshRenderer.material = CGame.WorldResources.VectorMat;
		//wallsMeshRenderer.castShadows = false;
		
		_meshWalls = _gobWalls.GetComponent<MeshFilter>().mesh;
		_meshWalls.Clear();

		int quadCount = 0;

		for (int iX = 1; iX < width - 1; ++iX)
			for (int iY = 1; iY < height - 1; ++iY)
			{
				// X
				if (tiles[iX, iY].mWallX.mType >= 100)
				{
					// normal door
				}
				else if (tiles[iX, iY].mWallX.mType >= 10)
					++quadCount;

				// Z
				if (tiles[iX, iY].mWallZ.mType >= 100)
				{

				}
				else if (tiles[iX, iY].mWallZ.mType >= 10)
					++quadCount;
			}

		Vector3[] verts = new Vector3[quadCount * 4];
		Color32[] cols = new Color32[quadCount * 4];
		int[] tris = new int[quadCount * 6];

		int v = 0;
		int t = 0;

		float basicLineThickness = 0.3f;

		for (int iX = 1; iX < width - 1; ++iX)
			for (int iY = 1; iY < height - 1; ++iY)
			{
				// X
				if (tiles[iX, iY].mWallX.mType >= 100)
				{
					// normal door
				}
				else if (tiles[iX, iY].mWallX.mType >= 10)
				{
					verts[v + 0] = new Vector3(iX, 0.0f, iY);
					verts[v + 1] = new Vector3(iX, basicLineThickness, iY);
					verts[v + 2] = new Vector3(iX + 1, basicLineThickness, iY);
					verts[v + 3] = new Vector3(iX + 1, 0.0f, iY);

					tris[t + 0] = v + 0;
					tris[t + 1] = v + 1;
					tris[t + 2] = v + 2;
					tris[t + 3] = v + 0;
					tris[t + 4] = v + 2;
					tris[t + 5] = v + 3;

					cols[v + 0] = new Color32(230, 230, 230, 255);
					cols[v + 1] = new Color32(230, 230, 230, 255);
					cols[v + 2] = new Color32(230, 230, 230, 255);
					cols[v + 3] = new Color32(230, 230, 230, 255);
					
					v += 4;
					t += 6;
				}

				// Z
				if (tiles[iX, iY].mWallZ.mType >= 100)
				{
					// normal door
				}
				else if (tiles[iX, iY].mWallZ.mType >= 10)
				{
					verts[v + 0] = new Vector3(iX, 0.0f, iY);
					verts[v + 1] = new Vector3(iX, basicLineThickness, iY);
					verts[v + 2] = new Vector3(iX, basicLineThickness, iY + 1);
					verts[v + 3] = new Vector3(iX, 0.0f, iY + 1);

					tris[t + 0] = v + 3;
					tris[t + 1] = v + 2;
					tris[t + 2] = v + 1;
					tris[t + 3] = v + 3;
					tris[t + 4] = v + 1;
					tris[t + 5] = v + 0;

					cols[v + 0] = new Color32(230, 230, 230, 255);
					cols[v + 1] = new Color32(230, 230, 230, 255);
					cols[v + 2] = new Color32(230, 230, 230, 255);
					cols[v + 3] = new Color32(230, 230, 230, 255);

					v += 4;
					t += 6;
				}
			}

		_meshWalls.vertices = verts;
		_meshWalls.colors32 = cols;
		_meshWalls.triangles = tris;
	}
}
