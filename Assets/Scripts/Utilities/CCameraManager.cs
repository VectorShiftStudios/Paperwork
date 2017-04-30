using UnityEngine;
using System;
using System.Collections.Generic;

public enum EOrhtoView
{
	OV_TOP,
	OV_BOTTOM,
	OV_FRONT,
	OV_BACK,
	OV_LEFT,
	OV_RIGHT
}

public enum ECameraViewMode
{
	CVM_GAME,
	CVM_ORTHO,
	CVM_ORBIT,
	CVM_FREE
}

public struct SCameraState
{
	public static Quaternion[] mViewRotations = {
			Quaternion.Euler(45, 45, 0),
			Quaternion.Euler(45, 135, 0),
			Quaternion.Euler(45, 225, 0),
			Quaternion.Euler(45, 315, 0)
		};

	public ECameraViewMode mViewMode;
	public EOrhtoView mOrthView;
	public EViewDirection mViewDirection;

	public bool mInteractable;

	public bool mLockedToMap;

	public float mFOV;
	public float mNearClip;
	public float mFarClip;

	public float mLerpSpeed;

	public Vector3 mTargetPosition;
	public Quaternion mTargetRotation;

	public Vector3 mCamLocalPosition;
	public Quaternion mCamLocalRotation;

	public Vector3 mRotation;

	public Color mBackgroundColor;
	
	public void SetViewGame(EViewDirection ViewDirection)
	{
		mViewMode = ECameraViewMode.CVM_GAME;
		mViewDirection = ViewDirection;
		mInteractable = true;
		mFOV = 15;
		mNearClip = 1;
		mFarClip = 500;
		mTargetPosition = Vector3.zero;		
		mTargetRotation = mViewRotations[(int)ViewDirection];
		mCamLocalPosition = new Vector3(0, 0, -65);
		mCamLocalRotation = Quaternion.identity;
		mLerpSpeed = 1.0f;
	}

	public void SetViewOrbit()
	{
		mViewMode = ECameraViewMode.CVM_ORBIT;
		mLerpSpeed = 1.0f;
	}

	public void SetViewFree()
	{
		mViewMode = ECameraViewMode.CVM_FREE;
		mInteractable = true;
		mFOV = 60;
		mNearClip = 0.1f;
		mFarClip = 100;
		mTargetPosition = Vector3.zero;
		mTargetRotation = Quaternion.identity;
		mCamLocalPosition = new Vector3(-5, 5, -5);
		mCamLocalRotation = Quaternion.identity;
		mRotation = new Vector3(45, -45, 0);
		mLerpSpeed = 1.0f;
	}

	public void SetOrthographic(EOrhtoView OrthoView)
	{
		Quaternion[] viewDir = {
			Quaternion.Euler(90, 0, 0),
			Quaternion.Euler(-90, 0, 0),
			Quaternion.Euler(0, 0, 0),
			Quaternion.Euler(0, 180, 0),
			Quaternion.Euler(0, 90, 0),
			Quaternion.Euler(0, 270, 0)
		};

		mViewMode = ECameraViewMode.CVM_ORTHO;
		mOrthView = OrthoView;
		mInteractable = true;
		mFOV = 7;
		mNearClip = 1;
		mFarClip = 200;
		mTargetPosition = Vector3.zero;
		mTargetRotation = viewDir[(int)OrthoView];
		mCamLocalPosition = new Vector3(0, 0, -50);
		mCamLocalRotation = Quaternion.identity;
		mLerpSpeed = 1.0f;
	}
}

/// <summary>
/// Management of the global camera system.
/// </summary>
public class CCameraManager
{
	public Camera mMainCamera;
	public bool mCanZoom;

	private CCamera _camera;
	private Transform _camTrans;
	private SCameraState _camState;

	private bool _rightMouseDown = false;
	private Vector3 _mousePosition = Vector3.zero;

	public CCameraManager()
	{
		mMainCamera = Camera.main;
		_camera = mMainCamera.GetComponent<CCamera>();
		_camTrans = mMainCamera.transform;
	}

	public float GetRotation()
	{
		return _camera.TargetTransform.eulerAngles.y;
	}

	public void Update(CInputState InputState)
	{
		// Perspective zooming:
		//_zoomLevel -= Input.mouseScrollDelta.y;
		//_targetPos.y = _zoomLevel;

		// Orthographic zooming:
		//_zoomLevel -= Input.mouseScrollDelta.y * _zoomLevel * 0.1f;
		//_zoomLevel = Mathf.Clamp(_zoomLevel, 20.0f, 100.0f);
		////transform.localPosition = new Vector3(0, 0, -_zoomLevel);

		Vector3 keyVec = Vector3.zero;
		Vector3 upVec = Vector3.zero;
		Vector3 rightVec = Vector3.zero;
		
		float time = Time.deltaTime * 15.0f;
		time = Mathf.Clamp(time, 0.0f, 1.0f);

		float scrollSpeed = 1.0f;

		if (_camState.mInteractable)
		{
			if (_camState.mViewMode == ECameraViewMode.CVM_GAME)
			{	
				upVec = _camTrans.forward;
				upVec.y = 0.0f;
				upVec.Normalize();
				rightVec = new Vector3(upVec.z, 0, -upVec.x);

				Vector3 camPos = _camState.mCamLocalPosition;

				// TODO: This prevents zooming while in the toolkit...
				//if (!InputState.mOverUI)
				if (mCanZoom)
					camPos.z += Input.mouseScrollDelta.y * 5.0f;

				camPos.z = Mathf.Clamp(camPos.z, -500, -20);
				_camState.mCamLocalPosition = camPos;
				scrollSpeed = 25.0f;

				if (InputState.GetCommand("camForward").mDown) keyVec += upVec;
				if (InputState.GetCommand("camBackward").mDown) keyVec -= upVec;
				if (InputState.GetCommand("camLeft").mDown) keyVec -= rightVec;
				if (InputState.GetCommand("camRight").mDown) keyVec += rightVec;
				keyVec.Normalize();

				int newViewDir = (int)_camState.mViewDirection;
				if (InputState.GetCommand("camRotateLeft").mPressed) newViewDir = (newViewDir + 1) % 4;
				if (InputState.GetCommand("camRotateRight").mPressed)
				{
					if (newViewDir == 0)
						newViewDir = 3;
					else
						--newViewDir;
				}

				_camState.mViewDirection = (EViewDirection)newViewDir;

				Vector3 targetPos = _camState.mTargetPosition + keyVec * Time.deltaTime * scrollSpeed;

				if (_camState.mLockedToMap)
				{
					targetPos.x = Mathf.Clamp(targetPos.x, 0.0f, 100.0f);
					targetPos.z = Mathf.Clamp(targetPos.z, 0.0f, 100.0f);
				}

				_camState.mTargetPosition = targetPos;
				_camera.TargetTransform.rotation = Quaternion.Slerp(_camera.TargetTransform.rotation, SCameraState.mViewRotations[(int)_camState.mViewDirection], 2.5f * Time.deltaTime);
				//_camera.TargetTransform.rotation = Quaternion.RotateTowards(_camera.TargetTransform.rotation, SCameraState.mViewRotations[(int)_camState.mViewDirection], 180.0f * Time.deltaTime);
			}
			else if (_camState.mViewMode == ECameraViewMode.CVM_ORTHO)
			{
				if (_camState.mOrthView == EOrhtoView.OV_TOP)
				{
					upVec = _camTrans.up;
					rightVec = _camTrans.right;
				}
				else if (_camState.mOrthView == EOrhtoView.OV_FRONT)
				{
					upVec = _camTrans.up;
					rightVec = _camTrans.right;
				}
				else if (_camState.mOrthView == EOrhtoView.OV_LEFT)
				{
					upVec = _camTrans.up;
					rightVec = _camTrans.right;
				}

				_camState.mFOV -= Input.mouseScrollDelta.y * 0.3f;
				_camState.mFOV = Mathf.Clamp(_camState.mFOV, 1.0f, 20.0f);
				mMainCamera.orthographicSize = Mathf.Lerp(mMainCamera.orthographicSize, _camState.mFOV, time);
				scrollSpeed = 10.0f;

				if (Input.GetKey(KeyCode.W)) keyVec += upVec;
				if (Input.GetKey(KeyCode.S)) keyVec -= upVec;
				if (Input.GetKey(KeyCode.A)) keyVec -= rightVec;
				if (Input.GetKey(KeyCode.D)) keyVec += rightVec;
				keyVec.Normalize();

				_camState.mTargetPosition += keyVec * Time.deltaTime * scrollSpeed;
			}
			else if (_camState.mViewMode == ECameraViewMode.CVM_FREE)
			{
				if (_rightMouseDown)
				{
					upVec = _camTrans.forward;
					rightVec = _camTrans.right;

					if (Input.GetKey(KeyCode.W)) keyVec += upVec;
					if (Input.GetKey(KeyCode.S)) keyVec -= upVec;
					if (Input.GetKey(KeyCode.A)) keyVec -= rightVec;
					if (Input.GetKey(KeyCode.D)) keyVec += rightVec;
					if (Input.GetKey(KeyCode.Q)) keyVec += _camTrans.up;
					if (Input.GetKey(KeyCode.E)) keyVec -= _camTrans.up;
					keyVec.Normalize();

					_camState.mCamLocalPosition += keyVec * Time.deltaTime * 5.0f;

					Vector3 mouseDelta = Input.mousePosition - _mousePosition;
					_mousePosition = Input.mousePosition;

					_camState.mRotation += mouseDelta * 0.3f;
				}

				_camState.mCamLocalRotation = Quaternion.AngleAxis(_camState.mRotation.x, Vector3.up) * Quaternion.AngleAxis(_camState.mRotation.y, Vector3.left);
				_camTrans.rotation = _camState.mCamLocalRotation;// Quaternion.Slerp(_camTrans.rotation, _camState.mCamLocalRotation, time);
			}
		}

		_camera.TargetTransform.position = Vector3.Lerp(_camera.TargetTransform.position, _camState.mTargetPosition, time * _camState.mLerpSpeed);
		_camTrans.localPosition = Vector3.Lerp(_camTrans.localPosition, _camState.mCamLocalPosition, time * _camState.mLerpSpeed);
	}

	public void UpdateInput()
	{
		// Depnding on the mode, tying in with overall UI
	}

	public void OnRightMouseDown()
	{
		_rightMouseDown = true;
		_mousePosition = Input.mousePosition;
	}

	public void OnRightMouseUp()
	{
		_rightMouseDown = false;
	}

	public SCameraState GetCamState()
	{
		return _camState;
	}

	public void SetBackgroundColor(Color Colour)
	{
		_camState.mBackgroundColor = Colour;
		mMainCamera.backgroundColor = Colour;
	}

	public void SetCamState(SCameraState State)
	{
		_camState = State;
		mMainCamera.backgroundColor = State.mBackgroundColor;
		_camera.TargetTransform.position = State.mTargetPosition;
		_camera.TargetTransform.rotation = State.mTargetRotation;
		_camera.transform.localPosition = State.mCamLocalPosition;
		_camera.transform.localRotation = State.mCamLocalRotation;

		mMainCamera.nearClipPlane = State.mNearClip;
		mMainCamera.farClipPlane = State.mFarClip;

		if (State.mViewMode == ECameraViewMode.CVM_ORTHO)
		{	
			mMainCamera.orthographic = true;
			mMainCamera.orthographicSize = State.mFOV;
		}
		else
		{
			mMainCamera.orthographic = false;
			mMainCamera.fieldOfView = State.mFOV;
		}		
	}

	public void SetInteractable(bool Value)
	{
		_camState.mInteractable = Value;
	}

	public void SetTargetPosition(Vector3 Position, bool Immediate = false, float Zoom = 0.0f)
	{
		_camState.mTargetPosition = Position;

		if (Immediate)
			_camera.TargetTransform.position = Position;

		if (Zoom != 0.0f)
			SetZoom(Zoom);
	}

	public void SetLerpSpeed(float Speed)
	{
		_camState.mLerpSpeed = Speed;
	}

	public float GetZoom()
	{
		return -_camTrans.localPosition.z;
	}

	public void SetZoom(float Level, bool Immediate = false)
	{
		Vector3 newPos = _camTrans.localPosition;
		newPos.z = -Level;
		_camState.mCamLocalPosition = newPos;

		if (Immediate)
			_camTrans.localPosition = newPos;
	}

	public Vector3 GetCamTargetPosition()
	{
		return _camera.TargetTransform.position;
	}

	public void Shake()
	{
		Vector3 pos = _camera.TargetTransform.position;
		pos.y += 0.25f;
		_camera.TargetTransform.position = pos;
	}
}
