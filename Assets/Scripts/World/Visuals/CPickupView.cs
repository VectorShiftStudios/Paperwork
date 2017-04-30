using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPickupView : CStateView, ISelectable
{	
	// State
	public int mID;
	public int mCarrierID;
	public Vector2 mPosition;
	public CItemAsset mContainedItemAsset;

	private GameObject _Gob;
	private Vector3 _currentPos;
	public Bounds mBounds;

	public void CopyInitialState(CPickup Pickup)
	{
		mID = Pickup.mID;
		mContainedItemAsset = Pickup.mContainedItemAsset;
	}

	public void CopyState(CPickup Pickup)
	{
		mPosition = Pickup.mPosition;
		mCarrierID = Pickup.mCarriedByUnitID;
	}

	protected override void _New(CUserSession UserSession)
	{
		// Check actual prefab
		_Gob = CGame.AssetManager.GetAsset<CModelAsset>("pickup").mVectorModel.CreateGameObject();
		_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);
		_Gob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", new Color(150.0f / 255.0f, 150.0f / 255.0f, 150.0f / 255.0f, 1.0f));

		/*
		GameObject sceneText = new GameObject("sceneText");
		sceneText.transform.SetParent(_Gob.transform);

		TextMesh text = sceneText.AddComponent<TextMesh>();
		text.text = mID.ToString();
		text.characterSize = 0.1f;
		text.fontSize = 32;
		text.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		text.anchor = TextAnchor.MiddleCenter;

		//text.font = CGame.ToolkitUI.SceneTextFont;

		sceneText.transform.position = new Vector3(0.0f, 0.26f, 0.0f);
		sceneText.transform.rotation = Quaternion.Euler(90, 0, 0);
		*/
	}

	protected override void _Update(CUserSession UserSession)
	{
		if (mCarrierID != -1 && _Gob.activeSelf)
		{
			_Gob.SetActive(false);
		}
		else if (mCarrierID == -1 && !_Gob.activeSelf)
		{
			_Gob.SetActive(true);
		}

		// Interpolate to simulated position
		Vector3 targetPos = new Vector3(mPosition.x, 0.0f, mPosition.y);
		Vector3 dir = targetPos - _currentPos;
		float distance = dir.magnitude;

		// Teleport to position if we are too far away
		if (distance >= 3.0f)
			_currentPos = targetPos;
		else
			_currentPos = Vector3.Lerp(_currentPos, targetPos, Mathf.Clamp(Time.deltaTime * 10.0f, 0.0f, 1.0f));

		float offset = (float)((mID * 10) % 100);

		_Gob.transform.position = _currentPos + new Vector3(0.0f, 0.25f, 0.0f);
		_Gob.transform.localRotation = Quaternion.Euler(0.0f, offset * 40.0f, 0.0f);
		//_Gob.transform.position = _currentPos + new Vector3(0.0f, Mathf.Sin((Time.time + offset) * 5.0f) * 0.2f + 0.5f, 0.0f);
		//_Gob.transform.localRotation = Quaternion.Euler(0.0f, (Time.time + offset) * 40.0f, 0.0f);

		//CDebug.DrawYSquare(targetPos, 0.8f, Color.green);

		Vector3 pivot = _Gob.transform.position;
		mBounds = new Bounds(pivot, new Vector3(0.5f, 0.5f, 0.5f));
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		//Debug.Log("Destroy VC");

		if (_Gob != null)
			GameObject.Destroy(_Gob);
	}

	ESelectionType ISelectable.GetType()
	{
		return ESelectionType.PICKUP;
	}

	string ISelectable.GetInfo()
	{
		return "Crate of " + mContainedItemAsset.mFriendlyName;
	}

	void ISelectable.PrintInfo()
	{

	}

	int ISelectable.GetID()
	{
		return mID;
	}

	bool ISelectable.IsStillActive()
	{
		if (mCarrierID != -1)
			return false;

		return (mState != EState.DESTROYED);
	}

	Vector3 ISelectable.GetScreenPos()
	{
		return Vector3.zero;
	}

	Vector3 ISelectable.GetVisualPos()
	{
		return _Gob.transform.position;
	}

	CStateView ISelectable.GetStateView()
	{
		return this;
	}

	void ISelectable.Select()
	{
	}

	void ISelectable.Deselect()
	{
	}

	bool ISelectable.Intersect(Ray R, ref float D)
	{
		if (mCarrierID != -1)
			return false;

		return mBounds.IntersectRay(R);
	}

	void ISelectable.GetRenderers(List<Renderer> Renderers)
	{
		Renderers.Add(_Gob.GetComponent<MeshRenderer>());
	}

	void ISelectable.Hover()
	{
	}

	void ISelectable.HoverOut()
	{
	}
}
