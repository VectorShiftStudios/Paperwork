using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public enum EViewDirection
{
	VD_FRONT = 0,
	VD_RIGHT = 1,
	VD_BACK = 2,
	VD_LEFT = 3
};

public class CModelPlane
{
	public class CCorner
	{
		public Vector3 mPosition;
	}

	public class CEdge
	{
		public CBrushAsset[] mBrush = new CBrushAsset[4];
	}

	public string mName;
	public Vector3 mPosition;
	public Vector3 mRotation;
	public CCorner[] mCorner = new CCorner[4];
	public CEdge[] mEdge = new CEdge[4];
	public CBrushAsset mFillBrush;

	// Proceduraly generated:
	public Vector3 mAxisX;
	public Vector3 mAxisY;
	public Vector3 mAxisZ;

	public Vector3 c1;
	public Vector3 c2;
	public Vector3 c3;
	public Vector3 c4;

	public Vector3 perp1;
	public Vector3 perp2;
	public Vector3 perp3;
	public Vector3 perp4;

	public CModelPlane()
	{
		for (int i = 0; i < 4; ++i)
		{
			mCorner[i] = new CCorner();
			mEdge[i] = new CEdge();
		}
	}

	public CModelPlane Clone()
	{
		CModelPlane p = new CModelPlane();

		p.mName = mName;
		p.mPosition = mPosition;
		p.mRotation = mRotation;

		for (int i = 0; i < 4; ++i)
		{
			p.mCorner[i].mPosition = mCorner[i].mPosition;

			for (int j = 0; j < 4; ++j)
			{
				p.mEdge[i].mBrush[j] = mEdge[i].mBrush[j];
			}
		}

		p.mFillBrush = mFillBrush;

		p.mAxisX = mAxisX;
		p.mAxisY = mAxisY;
		p.mAxisZ = mAxisZ;

		p.c1 = c1;
		p.c2 = c2;
		p.c3 = c3;
		p.c4 = c4;

		p.perp1 = perp1;
		p.perp2 = perp2;
		p.perp3 = perp3;
		p.perp4 = perp4;

		return p;
	}

	/// <summary>
	/// Intersect a ray with this plane.
	/// NOTE: This plane must have been through quad generation to fill out axis/perp info.
	/// </summary>
	public bool IntersectRay(Ray R, out Vector3 HitPoint, out float T)
	{
		Vector3 n = mAxisY;

		float numerator = Vector3.Dot(n, (c1 - R.origin));
		float demonenator = Vector3.Dot(n, R.direction);

		if (demonenator != 0.0f)
		{
			float t = numerator / demonenator;
			
			if (t >= 0.0f)
			{	
				T = t;
				HitPoint = R.origin + R.direction * t;

				if (Vector3.Dot(HitPoint - c1, perp1) >= 0.0f &&
					Vector3.Dot(HitPoint - c2, perp2) >= 0.0f &&
					Vector3.Dot(HitPoint - c3, perp3) >= 0.0f &&
					Vector3.Dot(HitPoint - c4, perp4) >= 0.0f)
				{
					return true;
				}
			}
		}

		T = 0.0f;
		HitPoint = Vector3.zero;
		return false;
	}
}

public class CVectorModel
{
	public const string DEFAULT_EDGE_BRUSH = "default_edge_brush";
	public const string DEFAULT_SURFACE_BRUSH = "default_surface_brush";

	public class CQuad
	{
		public Vector3 v1;
		public Vector3 v2;
		public Vector3 v3;
		public Vector3 v4;
		public Vector3 n;
		public Color32 c;

		public void CalculateNormal()
		{
			n = Vector3.Cross(v2 - v1, v4 - v1);
			n.Normalize();
		}
	}

	public List<CModelPlane> mPlanes = new List<CModelPlane>();
	
	public Mesh[] _mesh = new Mesh[4];
	
	public void Serialize(BinaryWriter W)
	{
		W.Write(mPlanes.Count);

		// Build brush index table
		List<CBrushAsset> _brushTable = new List<CBrushAsset>();

		for (int i = 0; i < mPlanes.Count; ++i)
		{
			CBrushAsset brush = mPlanes[i].mFillBrush;
			bool found = false;

			if (brush != null)
			{
				for (int t = 0; t < _brushTable.Count; ++t)
				{
					if (_brushTable[t] == brush)
					{
						found = true;
						break;
					}
				}

				if (!found)
					_brushTable.Add(brush);
			}

			for (int e = 0; e < 4; ++e)
			{
				for (int b = 0; b < 4; ++b)
				{
					brush = mPlanes[i].mEdge[e].mBrush[b];
					found = false;

					if (brush != null)
					{
						for (int t = 0; t < _brushTable.Count; ++t)
						{
							if (_brushTable[t] == brush)
							{
								found = true;
								break;
							}
						}

						if (!found)
							_brushTable.Add(brush);
					}
				}
			}
		}

		W.Write(_brushTable.Count);
		for (int t = 0; t < _brushTable.Count; ++t)
		{
			W.Write(_brushTable[t].mName);
		}

		for (int i = 0; i < mPlanes.Count; ++i)
		{
			CModelPlane p = mPlanes[i];
			W.Write(p.mName);

			W.Write(p.mPosition.x);
			W.Write(p.mPosition.y);
			W.Write(p.mPosition.z);

			W.Write(p.mRotation.x);
			W.Write(p.mRotation.y);
			W.Write(p.mRotation.z);

			CBrushAsset brush = p.mFillBrush;

			if (brush == null)
			{
				W.Write((int)-1);
			}
			else
			{
				for (int t = 0; t < _brushTable.Count; ++t)
				{
					if (_brushTable[t] == brush)
					{
						W.Write(t);
						break;
					}
				}
			}

			for (int c = 0; c < 4; ++c)
			{
				W.Write(p.mCorner[c].mPosition.x);
				W.Write(p.mCorner[c].mPosition.y);
				W.Write(p.mCorner[c].mPosition.z);
			}

			for (int e = 0; e < 4; ++e)
			{
				for (int b = 0; b < 4; ++b)
				{
					brush = p.mEdge[e].mBrush[b];

					if (brush == null)
					{
						W.Write((int)-1);
					}
					else
					{
						for (int t = 0; t < _brushTable.Count; ++t)
						{
							if (_brushTable[t] == brush)
							{
								W.Write(t);
								break;
							}
						}
					}
				}
			}
		}
	}
	
	public void Deserialize(BinaryReader R, int Version)
	{
		mPlanes.Clear();

		if (Version == 2)
		{
			int planeCount = R.ReadInt32();
			
			for (int i = 0; i < planeCount; ++i)
			{
				CModelPlane p = new CModelPlane();

				p.mName = R.ReadString();
				p.mPosition = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());
				p.mRotation = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());

				for (int c = 0; c < 4; ++c)
				{
					p.mCorner[c].mPosition = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());
				}

				for (int e = 0; e < 4; ++e)
				{
					for (int b = 0; b < 4; ++b)
					{
						R.ReadInt32();
						p.mEdge[e].mBrush[b] = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_EDGE_BRUSH);
					}
				}

				R.ReadInt32();
				p.mFillBrush = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_SURFACE_BRUSH);

				mPlanes.Add(p);
			}
		}
		else if (Version == 3)
		{
			int planeCount = R.ReadInt32();
			int brushTableCount = R.ReadInt32();

			string[] brushTable = new string[brushTableCount];
			for (int i = 0; i < brushTableCount; ++i)
			{
				brushTable[i] = R.ReadString();
			}

			for (int i = 0; i < planeCount; ++i)
			{
				CModelPlane p = new CModelPlane();

				p.mName = R.ReadString();
				p.mPosition = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());
				p.mRotation = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());

				p.mFillBrush = null;
				int brushID = R.ReadInt32();

				if (brushID != -1)
				{
					CBrushAsset tryBrush = CGame.AssetManager.GetAsset<CBrushAsset>(brushTable[brushID]);

					if (tryBrush == null)
						tryBrush = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_SURFACE_BRUSH);

					p.mFillBrush = tryBrush;
				}

				for (int c = 0; c < 4; ++c)
				{
					p.mCorner[c].mPosition = new Vector3(R.ReadSingle(), R.ReadSingle(), R.ReadSingle());
				}

				for (int e = 0; e < 4; ++e)
				{
					for (int b = 0; b < 4; ++b)
					{
						p.mEdge[e].mBrush[b] = null;

						brushID = R.ReadInt32();

						if (brushID != -1)
						{
							CBrushAsset tryBrush = CGame.AssetManager.GetAsset<CBrushAsset>(brushTable[brushID]);

							if (tryBrush == null)
								tryBrush = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_EDGE_BRUSH);

							p.mEdge[e].mBrush[b] = tryBrush;
						}
					}
				}

				mPlanes.Add(p);
			}
		}
	}

	public void RebuildEverything()
	{
		for (int v = 0; v < 4; ++v)
		{
			List<CQuad> _quads = new List<CQuad>();

			for (int i = 0; i < mPlanes.Count; ++i)
			{
				CModelPlane p = mPlanes[i];

				Quaternion o = Quaternion.Euler(p.mRotation);
				Matrix4x4 m = Matrix4x4.TRS(p.mPosition, o, Vector3.one);
				p.mAxisX = m.MultiplyVector(Vector3.right);
				p.mAxisY = m.MultiplyVector(Vector3.up);
				p.mAxisZ = m.MultiplyVector(Vector3.forward);

				Vector3 c1 = m.MultiplyPoint(p.mCorner[0].mPosition);
				Vector3 c2 = m.MultiplyPoint(p.mCorner[1].mPosition);
				Vector3 c3 = m.MultiplyPoint(p.mCorner[2].mPosition);
				Vector3 c4 = m.MultiplyPoint(p.mCorner[3].mPosition);
				p.c1 = c1; p.c2 = c2; p.c3 = c3; p.c4 = c4;

				Vector3 normal = p.mAxisY;

				Vector3 c1c2 = (c2 - c1).normalized;
				Vector3 c2c3 = (c3 - c2).normalized;
				Vector3 c3c4 = (c4 - c3).normalized;
				Vector3 c4c1 = (c1 - c4).normalized;

				Vector3 perp1 = Vector3.Cross(normal, c1c2);
				Vector3 perp2 = Vector3.Cross(normal, c2c3);
				Vector3 perp3 = Vector3.Cross(normal, c3c4);
				Vector3 perp4 = Vector3.Cross(normal, c4c1);
				p.perp1 = perp1; p.perp2 = perp2; p.perp3 = perp3; p.perp4 = perp4;

				Vector3 ie1_1 = Vector3.zero;
				Vector3 ie1_2 = Vector3.zero;
				Vector3 ie2_1 = Vector3.zero;
				Vector3 ie2_2 = Vector3.zero;
				Vector3 ie3_1 = Vector3.zero;
				Vector3 ie3_2 = Vector3.zero;
				Vector3 ie4_1 = Vector3.zero;
				Vector3 ie4_2 = Vector3.zero;

				if (p.mEdge[0].mBrush[v] != null)
				{
					CBrushAsset brush = p.mEdge[0].mBrush[v];
					ie1_1 = -c2c3 * (brush.mWeight / Vector3.Dot(-c2c3, perp1));
					ie1_2 = c4c1 * (brush.mWeight / Vector3.Dot(c4c1, perp1));
					CQuad q = new CQuad();
					q.c = brush.mColor;
					q.v1 = c1;
					q.v2 = c2;
					q.v3 = c2 + ie1_1;
					q.v4 = c1 + ie1_2;
					q.n = normal;

					_quads.Add(q);
				}

				if (p.mEdge[1].mBrush[v] != null)
				{
					CBrushAsset brush = p.mEdge[1].mBrush[v];
					ie2_1 = c1c2 * (brush.mWeight / Vector3.Dot(c1c2, perp2));
					ie2_2 = -c3c4 * (brush.mWeight / Vector3.Dot(-c3c4, perp2));
					CQuad q = new CQuad();
					q.c = brush.mColor;
					q.v1 = c2 + ie2_1;
					q.v2 = c2;
					q.v3 = c3;
					q.v4 = c3 + ie2_2;
					q.n = normal;

					_quads.Add(q);
				}

				if (p.mEdge[2].mBrush[v] != null)
				{
					CBrushAsset brush = p.mEdge[2].mBrush[v];
					ie3_1 = -c4c1 * (brush.mWeight / Vector3.Dot(-c4c1, perp3));
					ie3_2 = c2c3 * (brush.mWeight / Vector3.Dot(c2c3, perp3));
					CQuad q = new CQuad();
					q.c = brush.mColor;
					q.v1 = c4 + ie3_1;
					q.v2 = c3 + ie3_2;
					q.v3 = c3;
					q.v4 = c4;
					q.n = normal;

					_quads.Add(q);
				}

				if (p.mEdge[3].mBrush[v] != null)
				{
					CBrushAsset brush = p.mEdge[3].mBrush[v];
					ie4_1 = -c1c2 * (brush.mWeight / Vector3.Dot(-c1c2, perp4));
					ie4_2 = c3c4 * (brush.mWeight / Vector3.Dot(c3c4, perp4));
					CQuad q = new CQuad();
					q.c = brush.mColor;
					q.v1 = c1;
					q.v2 = c1 + ie4_1;
					q.v3 = c4 + ie4_2;
					q.v4 = c4;
					q.n = normal;

					_quads.Add(q);
				}

				if (p.mFillBrush != null)
				{
					CQuad q = new CQuad();
					q.c = p.mFillBrush.mColor;
					q.v1 = c1 + ie1_2 + ie4_1; 
					q.v2 = c2 + ie1_1 + ie2_1;
					q.v3 = c3 + ie2_2 + ie3_2;
					q.v4 = c4 + ie3_1 + ie4_2;
					q.n = normal;

					_quads.Add(q);
				}
			}

			if (_mesh[v] == null)
				_mesh[v] = new Mesh();
			else
				_mesh[v].Clear(true);

			if (_quads.Count != 0)
			{
				Vector3[] verts = new Vector3[_quads.Count * 4];
				Color32[] cols = new Color32[_quads.Count * 4];
				int[] tris = new int[_quads.Count * 6];

				for (int i = 0; i < _quads.Count; ++i)
				{
					CQuad q = _quads[i];

					verts[i * 4 + 0] = q.v1;
					verts[i * 4 + 1] = q.v2;
					verts[i * 4 + 2] = q.v3;
					verts[i * 4 + 3] = q.v4;

					tris[i * 6 + 0] = i * 4 + 0;
					tris[i * 6 + 1] = i * 4 + 1;
					tris[i * 6 + 2] = i * 4 + 2;
					tris[i * 6 + 3] = i * 4 + 0;
					tris[i * 6 + 4] = i * 4 + 2;
					tris[i * 6 + 5] = i * 4 + 3;

					cols[i * 4 + 0] = q.c;
					cols[i * 4 + 1] = q.c;
					cols[i * 4 + 2] = q.c;
					cols[i * 4 + 3] = q.c;
				}

				_mesh[v].vertices = verts;
				_mesh[v].colors32 = cols;
				_mesh[v].triangles = tris;
			}
		}
	}

	public Mesh GetSharedMesh(EViewDirection ViewDirection)
	{
		return _mesh[(int)ViewDirection];
	}

	public GameObject CreateGameObject(EViewDirection ViewDirection, Material SharedMat = null)
	{
		GameObject gob = new GameObject("vectorModel");
		MeshRenderer meshRenderer = gob.AddComponent<MeshRenderer>();

		if (SharedMat != null)
			meshRenderer.sharedMaterial = SharedMat;
		else
			meshRenderer.material = CGame.WorldResources.VectorMat;

		MeshFilter meshFilter = gob.AddComponent<MeshFilter>();
		meshFilter.mesh = _mesh[(int)ViewDirection];

		return gob;
	}

	public GameObject CreateGameObject(Material SharedMat = null)
	{
		GameObject gob = new GameObject("vectorModel");
		MeshRenderer meshRenderer = gob.AddComponent<MeshRenderer>();

		if (SharedMat != null)
			meshRenderer.sharedMaterial = SharedMat;
		else
			meshRenderer.material = CGame.WorldResources.VectorMat;

		MeshFilter meshFilter = gob.AddComponent<MeshFilter>();
		ModelFacer facer = gob.AddComponent<ModelFacer>();
		facer.Init(this, meshFilter);

		return gob;
	}

	public bool IntersectRay(Ray R, Matrix4x4 Transform, out float T)
	{
		T = float.MaxValue;
		Vector3 planeHitPoint = Vector3.zero;
		int hitPlaneID = -1;
		bool hit = false;

		Ray r = new Ray(Transform.MultiplyPoint(R.origin), Transform.MultiplyVector(R.direction));

		for (int i = 0; i < mPlanes.Count; ++i)
		{
			CModelPlane p = mPlanes[i];
			Vector3 hitPos;
			float t;
			if (p.IntersectRay(r, out hitPos, out t))
			{				
				if (t < T)
				{
					hit = true;
					T = t;
					planeHitPoint = hitPos;
					hitPlaneID = i;
				}
			}
		}

		return hit;
	}
}
