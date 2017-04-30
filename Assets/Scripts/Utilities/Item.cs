using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
	private Material _sharedMat;

	public void Init(Material SharedMat)
	{
		_sharedMat = SharedMat;
	}

	public void SetSurfaceColor(Color C)
	{
		_sharedMat.SetColor("_FloorColor", C);
	}

	void OnDestroy()
	{
		if (_sharedMat != null)
			GameObject.Destroy(_sharedMat);
	}
}
