using System;
using System.Collections.Generic;
using UnityEngine;

public class CEntityPlacer
{
	public enum EPlaceType
	{
		ITEM,
		UNIT,
		PICKUP,
		VOLUME,
		DECAL
	}

	public delegate bool PlacementDelegate(CEntityPlacer Placer);

	public EPlaceType mType;

	public CItemAsset mAsset;
	public Vector3 mPosition;
	public int mRotation;
	public int mX;
	public int mY;

	private GameObject _gob;
	private bool _placeable;
	private bool _visible;
	private PlacementDelegate _placeDelegate;

	public CEntityPlacer(string AssetName, Transform Parent, PlacementDelegate PlacementDelegate)
	{	
		mAsset = CGame.AssetManager.GetAsset<CItemAsset>(AssetName);
		if (mAsset == null)
		{
			Debug.LogError("Can't asset to place: " + AssetName);
			return;
		}

		_Init(EPlaceType.ITEM, Parent, PlacementDelegate);
	}

	public CEntityPlacer(EPlaceType Type, Transform Parent, PlacementDelegate PlacementDelegate)
	{
		_Init(Type, Parent, PlacementDelegate);
	}

	private void _Init(EPlaceType Type, Transform Parent, PlacementDelegate PlacementDelegate)
	{
		mType = Type;
		_placeable = false;
		_placeDelegate = PlacementDelegate;

		if (mType == EPlaceType.ITEM)
		{
			_gob = mAsset.CreateVisuals(EViewDirection.VD_FRONT);
			_gob.transform.SetParent(Parent);
			CItemView.SetItemSurfaceColour(mAsset.mItemType, _gob, CGame.COLOR_BLUEPRRINT);
		}
		else if (mType == EPlaceType.UNIT)
		{
			mRotation = 225;
			_gob = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7] as GameObject);
			_gob.transform.SetParent(Parent);
			_gob.transform.rotation = Quaternion.AngleAxis(mRotation, Vector3.up);
		}
	}

	public void Destroy()
	{
		if (_gob != null)
		{
			GameObject.Destroy(_gob);
		}
	}

	public void SetVisible(bool Visible)
	{
		if (_visible != Visible)
		{
			_visible = Visible;
			_gob.SetActive(_visible);
		}
	}

	public void Rotate()
	{
		if (mType == EPlaceType.ITEM)
		{
			++mRotation;

			if (mRotation == 4)
				mRotation = 0;

			_gob.transform.rotation = CItem.RotationTable[mRotation];
		}
		else if (mType == EPlaceType.UNIT)
		{
			mRotation += 45;

			if (mRotation == 360)
				mRotation = 0;

			_gob.transform.rotation = Quaternion.AngleAxis(mRotation, Vector3.up);
		}
	}
	
	public void Update()
	{
		bool newPlaceable = IsPlaceable();

		if (_placeable != newPlaceable)
		{
			_placeable = newPlaceable;

			if (mType == EPlaceType.ITEM)
			{
				if (newPlaceable)
					CItemView.SetItemSurfaceColour(mAsset.mItemType, _gob, CGame.COLOR_BLUEPRRINT);
				else
					CItemView.SetItemSurfaceColour(mAsset.mItemType, _gob, new Color(1.0f, 0.1f, 0.1f));
			}
		}

		if (mType == EPlaceType.ITEM)
		{
			Color c = CGame.COLOR_BLUEPRRINT;
			c.a = 0.4f;

			if (!_placeable)
				c = new Color(1.0f, 0.1f, 0.1f, 0.4f);

			Vector2 midOffset = CItem.RotationAxisTable[mRotation].Transform(new Vector2(mAsset.mWidth, mAsset.mLength));
			float yOff = 0.001f;
			float oW = 0.1f;
			// Draw an outline.

			Vector3 p1 = mPosition + new Vector3(0, yOff, 0);
			Vector3 p2 = mPosition + new Vector3(0, yOff, midOffset.y);
			Vector3 p3 = mPosition + new Vector3(midOffset.x, yOff, midOffset.y);
			Vector3 p4 = mPosition + new Vector3(midOffset.x, yOff, 0);

			Vector3 p1p2 = (p2 - p1) * 0.5f + p1;
			Vector3 p2p3 = (p3 - p2) * 0.5f + p2;
			Vector3 p3p4 = (p4 - p3) * 0.5f + p3;
			Vector3 p4p1 = (p1 - p4) * 0.5f + p4;

			CDebug.DrawYRectQuad(p1p2, oW, Mathf.Abs(midOffset.y) - oW, c);
			CDebug.DrawYRectQuad(p2p3, Mathf.Abs(midOffset.x) + oW, oW, c);
			CDebug.DrawYRectQuad(p3p4, oW, Mathf.Abs(midOffset.y) - oW, c);
			CDebug.DrawYRectQuad(p4p1, Mathf.Abs(midOffset.x) + oW, oW, c);
		}
		else if (mType == EPlaceType.UNIT)
		{
			CDebug.DrawYRectQuad(mPosition, 0.25f, 0.25f, CGame.COLOR_BLUEPRRINT, false);
		}
		else if (mType == EPlaceType.VOLUME || mType == EPlaceType.DECAL)
		{
			CDebug.DrawBorderQuads(mPosition, new Vector2(1, 1), CGame.COLOR_BLUEPRRINT, false);
		}
	}

	public void SetPosition(Vector3 Pos)
	{
		if (mType == EPlaceType.ITEM)
		{
			Vector2 midOffset = CItem.RotationAxisTable[mRotation].Transform(new Vector2(mAsset.mWidth * 0.5f, mAsset.mLength * 0.5f));
			Vector2 pos = new Vector2(Pos.x, Pos.z) - midOffset;
			mPosition = new Vector3((int)(pos.x + 0.5f), 0, (int)(pos.y + 0.5f));
			_gob.transform.position = mPosition;

			Vector2 placePos = new Vector2(mPosition.x, mPosition.z) - CItem.PivotRelativeToTile[mRotation];
			mX = (int)placePos.x;
			mY = (int)placePos.y;
		}
		else if (mType == EPlaceType.UNIT)
		{
			mPosition = Pos;
			_gob.transform.position = Pos;
		}
		else if (mType == EPlaceType.VOLUME)
		{
			mPosition = Pos;
		}
		else if (mType == EPlaceType.DECAL)
		{
			mPosition = Pos;
		}
	}

	public bool IsPlaceable()
	{
		if (_placeDelegate == null)
			return true;
				 
		return _placeDelegate(this);
	}
}
