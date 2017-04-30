using UnityEngine;
using System.Collections;

public class ModelFacer : MonoBehaviour
{
	private CVectorModel _model;
	private int _currentDirection = -1;
	private MeshFilter _meshFilter;

	/// <summary>
	/// Check orientation and assign correct mesh.
	/// </summary>
	public void Update()
	{
		int rot = (int)(CGame.CameraManager.GetRotation() - transform.eulerAngles.y);

		if (rot < 0) rot = 360 + rot;
		else if (rot >= 360) rot = rot - 360;

		int direction = (int)(rot / 90.0f);

		if (_currentDirection != direction)
		{
			_meshFilter.mesh = _model._mesh[direction];
			_currentDirection = direction;
		}
	}

	public void Init(CVectorModel Model, MeshFilter Filter)
	{
		_model = Model;
		_meshFilter = Filter;

		Update();
	}
}
