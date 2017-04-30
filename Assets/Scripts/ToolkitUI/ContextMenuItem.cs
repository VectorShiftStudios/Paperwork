using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class ContextMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
	public Image BackingImage;
	public Color HighlightColor;
	public Color SelectedColor;

	private bool _selected;
	private Action _clickAction;

	public void SetAction(Action ClickAction)
	{
		_clickAction = ClickAction;
	}
	
	public void OnPointerDown(PointerEventData EventData)
	{
		if (_clickAction != null)
			_clickAction();
	}

	public void OnPointerEnter(PointerEventData EventData)
	{
		if (!_selected)
			BackingImage.color = HighlightColor;
	}

	public void OnPointerExit(PointerEventData EventData)
	{
		if (!_selected)
			BackingImage.color = new Color(0, 0, 0, 0);
	}

	public void OnPointerClick(PointerEventData EventData)
	{	
	}

	void Start ()
	{
		BackingImage.color = new Color(0, 0, 0, 0);
	}

	public void SetSelected()
	{
		_selected = true;
		BackingImage.color = SelectedColor;
	}

	public void SetDeselected()
	{
		_selected = false;
		BackingImage.color = new Color(0, 0, 0, 0); ;
	}
}
