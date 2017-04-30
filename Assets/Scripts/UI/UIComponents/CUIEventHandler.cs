using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class CUIEventHandler : MonoBehaviour, IPointerDownHandler
{
	public Action mAction;

	public void OnPointerDown(PointerEventData EventData)
	{
		mAction();
	}
}