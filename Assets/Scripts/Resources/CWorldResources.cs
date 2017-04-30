using System;
using System.Collections.Generic;
using UnityEngine;

public class CWorldResources : MonoBehaviour
{
	// TODO: Blend with primary resources?

	public Material FloorMat;
	public Material FloorVisibleMat;
	public Material VectorMat;
	public Material ItemGhostMat;

	public Material TileIconSolid;
	public Material TileIconTrigger;
	
	public GameObject MoveRingPrefab;
	public AnimationCurve MoveRingResponseCurve;

	public GameObject PadlockPrefab;

	public Font DecalFontA;
	public Material DecalMat;
	public Material DecalFOWMat;
	public Material DecalLOSMat;
}
