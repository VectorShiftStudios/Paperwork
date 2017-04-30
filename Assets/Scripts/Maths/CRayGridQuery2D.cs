using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DDA Line Algorithm.
/// </summary>
public class CRayGridQuery2D
{
	int _x;
	int _y;	

	int _stepX;
	int _stepY;	

	int _boundX;
	int _boundY;	

	Vector2 _tMax;
	Vector2 _tDelta;

	/// <summary>
	/// Construct a ray grid query
	/// Ray must be in cell co-ords
	/// </summary>
	public CRayGridQuery2D(Vector2 Start, Vector2 Direction)
	{
		_x = (int)Start.x;
		_y = (int)Start.y;

		_stepX = Math.Sign(Direction.x);
		_stepY = Math.Sign(Direction.y);

		_boundX = _x + (_stepX > 0 ? 1 : 0);
		_boundY = _y + (_stepY > 0 ? 1 : 0);

		// TODO: Start should be cast to int here??		
		_tMax = new Vector2(
			(_boundX - Start.x) / Direction.x,
			(_boundY - Start.y) / Direction.y);

		//if (Single.IsNaN(_tMax.x)) _tMax.x = Single.PositiveInfinity;
		//if (Single.IsNaN(_tMax.y)) _tMax.y = Single.PositiveInfinity;

		_tDelta = new Vector2(
			_stepX / Direction.x,
			_stepY / Direction.y);

		if (Direction.x == 0)
		{
			_tDelta.x = Single.PositiveInfinity;
			_tMax.x = Single.PositiveInfinity;
		}
		if (Direction.y == 0)
		{
			_tDelta.y = Single.PositiveInfinity;
			_tMax.y = Single.PositiveInfinity;
		}

		//if (Single.IsNaN(_tDelta.x)) _tDelta.x = Single.PositiveInfinity;
		//if (Single.IsNaN(_tDelta.y)) _tDelta.y = Single.PositiveInfinity;
	}

	public void GetNextCell(ref int X, ref int Y)
	{
		//return current cell
		X = _x;
		Y = _y;

		//step to next cell
		if (_tMax.x < _tMax.y)
		{
			_x += _stepX;
			_tMax.x += _tDelta.x;
		}
		else
		{
			_y += _stepY;
			_tMax.y += _tDelta.y;
		}		
	}
		
	public void GetNextCell(ref int X, ref int Y, ref int DX, ref int DY)
	{
		//return current cell
		X = _x;
		Y = _y;		

		DX = 0;
		DY = 0;		

		//step to next cell
		if (_tMax.x < _tMax.y)
		{
			_x += _stepX;
			_tMax.x += _tDelta.x;
			DX = _stepX;
		}
		else
		{
			_y += _stepY;
			_tMax.y += _tDelta.y;
			DY = _stepY;
		}		
	}	
}
