using System;
using System.Collections.Generic;
using UnityEngine;

public class CPrimaryResources : MonoBehaviour
{
	public Camera PrimaryCamera;

	public Shader DebugLinesShader;
	public Shader DebugLinesNoDepthShader;

	public Material EdgeBlitMat;
	public Material HighlightMat;

	public Material TranslucentBlitMat;
	public Material TranslucentModelMat;

	public Material FlatMat;
	public Material VecMat;

	public Material PunchOutMat;
	public Material TieMat;

	public Texture UnitTexTie;
	public Texture UnitTexTieArmband;
	public Texture UnitTexTieSling;

	public AudioClip[] AudioClips;
	public AudioClip[] MusicClips;
	public GameObject[] Prefabs;
	public Sprite[] Sprites;
	public Texture[] Decals;

	public GameObject Particles;

	public Texture GetDecalImage(int Id)
	{
		if (Id >= 0 && Id < Decals.Length)
			return Decals[Id];

		return Decals[0];
	}
}
