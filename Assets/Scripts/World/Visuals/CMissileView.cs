using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CMissileView : CStateView
{	
	// State
	public int mID;
	public int mOwnerID;
	public Vector3 mPosition;
	public Vector3 mVelocity;
	public int mDead;
	private GameObject _Gob;
	private Vector3 _currentPos;

	public void CopyInitialState(CMissile Missile)
	{
		mID = Missile.mID;
		mOwnerID = Missile.mOwner;
	}

	public void CopyState(CMissile Missile)
	{
		mPosition = Missile.mRealPosition;
		mVelocity = Missile.mVelocity;
		mDead = Missile.mDead;
	}

	protected override void _New(CUserSession UserSession)
	{
		_Gob = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[8]);
		_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);
	}

	protected override void _Update(CUserSession UserSession)
	{
		// Interpolate to simulated position
		Vector3 targetPos = mPosition;// new Vector3(mPosition.x, 0.0f, mPosition.y);
		Vector3 dir = targetPos - _currentPos;
		float distance = dir.magnitude;

		// Teleport to position if we are too far away
		if (distance >= 3.0f)
			_currentPos = targetPos;
		else
			_currentPos = Vector3.Lerp(_currentPos, targetPos, Mathf.Clamp(Time.deltaTime * 10.0f, 0.0f, 1.0f));

		float offset = (float)((mID * 10) % 100);

		_Gob.transform.position = _currentPos;
		// TODO: Make sure velocity doesn't = 0
		_Gob.transform.localRotation = Quaternion.LookRotation(mVelocity);

		if (mDead != -1)
			_Gob.transform.GetChild(0).gameObject.SetActive(false);
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		if (_Gob != null)
		{
			//_Gob.transform.GetChild(0).gameObject.SetActive(false);
			GameObject.Destroy(_Gob, 1.0f);
			_Gob = null;
		}
	}
}
