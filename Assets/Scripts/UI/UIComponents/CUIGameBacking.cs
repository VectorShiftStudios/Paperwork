using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class CUIGameBacking : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	private bool _showingTip = false;

	private bool _mouseOver = false;
	private bool _mouseEnter = false;	

	void Update()
	{
		// TODO: Check constantly if we have released the pointer over this

		//Debug.Log("Over " + EventSystem.current.game .IsPointerOverGameObject());

		if (_mouseOver && !_mouseEnter)
		{

		}
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData EventData)
	{
		_mouseOver = true;

		Debug.Log("Over UI");
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData EventData)
	{
		Debug.Log("Exit UI");
	}

	void IPointerDownHandler.OnPointerDown(PointerEventData EventData)
	{
		Debug.Log("Pointer Down");
	}

	void IPointerUpHandler.OnPointerUp(PointerEventData EventData)
	{
		Debug.Log("Pointer Up");
	}
}