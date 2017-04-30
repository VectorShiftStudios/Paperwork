using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class CameraView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	public enum EMouseButton
	{
		LEFT,
		MIDDLE,
		RIGHT
	}

	public Vector2 mDimensions;
	public Vector2 mLocalMousePos;
	private RectTransform _rect;

	public Action<EMouseButton> mOnMouseDown;
	public Action<EMouseButton> mOnMouseUp;

	void Start()
	{
		_rect = GetComponent<RectTransform>();
	}

	void OnRectTransformDimensionsChange()
	{
		if (mDimensions != _rect.sizeDelta)
			mDimensions = _rect.sizeDelta;
	}

	public void Update()
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, Input.mousePosition, null, out mLocalMousePos);
		mLocalMousePos.y = mDimensions.y + mLocalMousePos.y;
	}

	public void OnPointerDown(PointerEventData EventData)
	{
		if (EventData.button == PointerEventData.InputButton.Right)
			CGame.CameraManager.OnRightMouseDown();

		if (mOnMouseDown != null)
		{
			EMouseButton button = EMouseButton.MIDDLE;

			if (EventData.button == PointerEventData.InputButton.Left)
				button = EMouseButton.LEFT;
			else if (EventData.button == PointerEventData.InputButton.Right)
				button = EMouseButton.RIGHT;

			mOnMouseDown(button);
		}
	}

	public void OnPointerUp(PointerEventData EventData)
	{
		if (EventData.button == PointerEventData.InputButton.Right)
			CGame.CameraManager.OnRightMouseUp();

		if (mOnMouseUp != null)
		{
			EMouseButton button = EMouseButton.MIDDLE;

			if (EventData.button == PointerEventData.InputButton.Left)
				button = EMouseButton.LEFT;
			else if (EventData.button == PointerEventData.InputButton.Right)
				button = EMouseButton.RIGHT;

			mOnMouseUp(button);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		CGame.CameraManager.mCanZoom = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		CGame.CameraManager.mCanZoom = false;
	}
}
