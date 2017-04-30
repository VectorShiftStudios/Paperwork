using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates icons at runtime.
/// </summary>
public class CIconBuilder
{
	public RenderTexture mIconTexture;

	private int _updates = 0;
	private bool _deferRender = false;

	private int _texSize = 512;
	private int _iconSize = 54;

	/// <summary>
	/// Create textures and allocate item icon UVs.
	/// </summary>
	public void Init()
	{
		mIconTexture = new RenderTexture(_texSize, _texSize, 16);
		mIconTexture.useMipMap = false;
		mIconTexture.antiAliasing = 8;
		mIconTexture.Create();

		RebuildItemIcons(false);
	}

	/// <summary>
	/// Manage timing for icon rendering.	
	/// </summary>
	public void Update()
	{
		// NOTE: We can't render before Unity has rendered the first frame. This is potentially due to state we can set up.
		if (_deferRender && _updates > 0)
		{
			// Render item thumbnails
			RebuildItemIcons(true);
			_deferRender = false;
		}

		++_updates;
	}
	
	/// <summary>
	/// Render each item asset into a thumbnail.
	/// </summary>
	public void RebuildItemIcons(bool ImmediateRender)
	{
		if (!ImmediateRender)
			_deferRender = true;

		List<CItemAsset> items = CGame.AssetManager.GetAllItemAssets();

		int iconsPerRow = _texSize / _iconSize;

		if (ImmediateRender)
		{
			Graphics.SetRenderTarget(mIconTexture);
			GL.Viewport(new Rect(0, 0, _texSize, _texSize));
			GL.Clear(true, true, new Color(1.0f, 1.0f, 1.0f, 0.0f));
		}

		for (int i = 0; i < items.Count; ++i)
		{
			Rect pixelRect = new Rect((i % iconsPerRow) * _iconSize, (i / iconsPerRow) * _iconSize, _iconSize, _iconSize);

			if (ImmediateRender)
				_RenderItemIcon(pixelRect, items[i]);

			pixelRect.y += _iconSize;
			pixelRect.height = -pixelRect.height;

			pixelRect.x /= _texSize;
			pixelRect.y /= _texSize;
			pixelRect.width /= _texSize;
			pixelRect.height /= _texSize;
			items[i].mIconRect = pixelRect;
			items[i].mIconTexture = mIconTexture;
		}
	}

	private void _RenderItemIcon(Rect ViewportRect, CItemAsset Item)
	{
		GL.Viewport(ViewportRect);
		//GL.Clear(true, true, new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f));

		Matrix4x4 projMat = Matrix4x4.Perspective(5.0f, 1, 1.0f, 100.0f);

		Matrix4x4 viewMat = Matrix4x4.TRS(new Vector3(Item.mIconCameraPostion.x, Item.mIconCameraPostion.y, Item.mIconCameraPostion.z - 30), Quaternion.identity, Vector3.one);
		viewMat *= Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.AngleAxis(-40.0f, Vector3.left), Vector3.one);
		viewMat *= Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.AngleAxis(225.0f, Vector3.up), Vector3.one);

		Matrix4x4 viewProjMat = projMat * viewMat;

		if (Item.mPMAsset != null)
		{
			Mesh m = Item.mPMAsset.mVectorModel.GetSharedMesh(EViewDirection.VD_FRONT);
			Matrix4x4 modelMat = Matrix4x4.TRS(Item.mPMPosition, Quaternion.Euler(Item.mPMRotation), Vector3.one);

			CGame.PrimaryResources.VecMat.SetMatrix("worldMat", viewProjMat * modelMat);
			CGame.PrimaryResources.VecMat.SetPass(0);
			Graphics.DrawMeshNow(m, Matrix4x4.zero);
		}

		if (Item.mSMAsset != null)
		{
			Mesh m = Item.mSMAsset.mVectorModel.GetSharedMesh(EViewDirection.VD_FRONT);
			Matrix4x4 modelMat = Matrix4x4.TRS(Item.mSMPosition, Quaternion.Euler(Item.mSMRotation), Vector3.one);

			CGame.PrimaryResources.VecMat.SetMatrix("worldMat", viewProjMat * modelMat);
			CGame.PrimaryResources.VecMat.SetPass(0);
			Graphics.DrawMeshNow(m, Matrix4x4.zero);
		}
	}
}