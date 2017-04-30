using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCursor : MonoBehaviour
{
	public Material PrimaryMat;

	private Mesh _mesh;
	private MeshRenderer _renderer;

	private float _t;
	public float interp;

	private Vector3[] verts = new Vector3[4 * 4 + 4];
	private Color32[] cols = new Color32[4 * 4 + 4];
	private Vector2[] uvs = new Vector2[4 * 4 + 4];
	private int[] inds = new int[4 * 2 * 3 + 2 * 3];

	void Start()
	{
		_mesh = new Mesh();
		_renderer = gameObject.AddComponent<MeshRenderer>();
		gameObject.AddComponent<MeshFilter>().mesh = _mesh;
		_renderer.material = PrimaryMat;

		float scale = 0.75f;
		transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
	}

	public void Show()
	{
		if (_mesh == null)
			return;

		Update();
	}
	
	void Update()
	{
		Vector3 dir = CGame.CameraManager.mMainCamera.transform.forward;
		dir.y = 0.0f;
		dir.Normalize();

		Quaternion rot = Quaternion.LookRotation(dir);
		transform.rotation = rot;
		transform.RotateAround(Vector3.up, -45.0f * Mathf.PI / 180.0f); 

		float hw = 0.1f;
		float h = 0.5f;

		verts[0] = new Vector3(h - hw, 0, -h);
		verts[1] = new Vector3(h, 0, -h);
		verts[2] = new Vector3(h, 0, 0);
		verts[3] = new Vector3(h - hw, 0, 0);

		cols[0] = new Color32(255, 255, 255, 64);
		cols[1] = new Color32(255, 255, 255, 64);
		cols[2] = new Color32(255, 255, 255, 32);
		cols[3] = new Color32(255, 255, 255, 32);

		verts[4] = new Vector3(-h, 0, -h);
		verts[5] = new Vector3(-h + hw, 0, -h);
		verts[6] = new Vector3(-h + hw, 0, h);
		verts[7] = new Vector3(-h, 0, h);

		cols[4] = new Color32(255, 255, 255, 128);
		cols[5] = new Color32(255, 255, 255, 128);
		cols[6] = new Color32(255, 255, 255, 192);
		cols[7] = new Color32(255, 255, 255, 192);

		verts[8] = new Vector3(h, 0, -h);
		verts[9] = new Vector3(h, 0, -h + hw);
		verts[10] = new Vector3(-h, 0, -h + hw);
		verts[11] = new Vector3(-h, 0, -h);

		cols[8] = new Color32(255, 255, 255, 64);
		cols[9] = new Color32(255, 255, 255, 64);
		cols[10] = new Color32(255, 255, 255, 128);
		cols[11] = new Color32(255, 255, 255, 128);

		verts[12] = new Vector3(0, 0, h - hw);
		verts[13] = new Vector3(0, 0, h);
		verts[14] = new Vector3(-h, 0, h);
		verts[15] = new Vector3(-h, 0, h - hw);

		cols[12] = new Color32(255, 255, 255, 224);
		cols[13] = new Color32(255, 255, 255, 224);
		cols[14] = new Color32(255, 255, 255, 192);
		cols[15] = new Color32(255, 255, 255, 192);

		inds[0] = 0;
		inds[1] = 1;
		inds[2] = 2;
		inds[3] = 0;
		inds[4] = 2;
		inds[5] = 3;

		inds[6] = 4;
		inds[7] = 5;
		inds[8] = 6;
		inds[9] = 4;
		inds[10] = 6;
		inds[11] = 7;

		inds[12] = 8;
		inds[13] = 9;
		inds[14] = 10;
		inds[15] = 8;
		inds[16] = 10;
		inds[17] = 11;

		inds[18] = 12;
		inds[19] = 13;
		inds[20] = 14;
		inds[21] = 12;
		inds[22] = 14;
		inds[23] = 15;

		float ahw = 0.66f;

		float scale = CGame.WorldResources.MoveRingResponseCurve.Evaluate(1.0f - interp);
		float ay = scale;

		float rs = 0.5f;
		verts[16] = new Vector3(-ahw * rs, ay, ahw * rs);
		verts[17] = new Vector3(-ahw * rs, ahw * 2.0f + ay, ahw * rs);
		verts[18] = new Vector3(ahw * rs, ahw * 2.0f + ay, -ahw * rs);
		verts[19] = new Vector3(ahw * rs, ay, -ahw * rs);

		uvs[16] = new Vector2(0, 0);
		uvs[17] = new Vector2(0, 1);
		uvs[18] = new Vector2(1, 1);
		uvs[19] = new Vector2(1, 0);

		cols[16] = new Color32(0, 255, 255, 255);
		cols[17] = new Color32(0, 255, 255, 255);
		cols[18] = new Color32(0, 255, 255, 255);
		cols[19] = new Color32(0, 255, 255, 255);

		inds[24] = 16;
		inds[25] = 17;
		inds[26] = 18;
		inds[27] = 16;
		inds[28] = 18;
		inds[29] = 19;

		_mesh.vertices = verts;
		_mesh.colors32 = cols;
		_mesh.uv = uvs;
		_mesh.triangles = inds;

		float ot = (interp - 0.8f) * 5.0f;
		ot = Mathf.Clamp01(ot);

		_renderer.material.SetFloat("_Fade", 1.0f - ot);
	}
}
