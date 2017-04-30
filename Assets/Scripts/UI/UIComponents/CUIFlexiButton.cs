using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CUIFlexiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
	public delegate void ButtonEventDelegate(int Param);

	private bool _mouseLeftDown;
	private bool _mouseRightDown;
	private ButtonEventDelegate mLeftClickEvent;
	private ButtonEventDelegate mRightClickEvent;
	private int _param;

	public void SetEvents(ButtonEventDelegate LeftClick, ButtonEventDelegate RightClick, int Param)
	{
		mLeftClickEvent = LeftClick;
		mRightClickEvent = RightClick;
		_param = Param;
	}

	public void OnPointerEnter(PointerEventData EventData)
	{
		//Debug.Log("Mouse in");
	}

	public void OnPointerExit(PointerEventData EventData)
	{
		//Debug.Log("Mouse out");
	}

	public void OnPointerClick(PointerEventData EventData)
	{
		//Debug.Log("Mouse click: " + EventData.button);

		if (EventData.button == PointerEventData.InputButton.Left)
		{
			if (mLeftClickEvent != null)
				mLeftClickEvent(_param);

		}
		else if (EventData.button == PointerEventData.InputButton.Right)
		{
			if (mRightClickEvent != null)
				mRightClickEvent(_param);
		}
	}

	public void OnPointerDown(PointerEventData EventData)
	{
		//Debug.Log("Mouse down");
	}

	public void OnPointerUp(PointerEventData EventData)
	{
		//Debug.Log("Mouse up");
	}
}