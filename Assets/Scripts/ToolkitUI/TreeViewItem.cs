using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class TreeViewItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
{
	public Image BackingImage;
	public Color HighlightColor;
	public Color SelectedColor;
	public CTUITreeViewItem mItem;

	public void OnPointerDown(PointerEventData EventData)
	{
		// TODO: let the CTUITreeViewItem handle what happens on click?
		// Then this just becomes a dumb input component.
		if (mItem.mChildren != null)
		{
			mItem.mExpanded = !mItem.mExpanded;
			mItem.RebuildEntireTree();
		}

		mItem.OnClicked(this);
	}

	public void OnPointerEnter(PointerEventData EventData)
	{
		if (!mItem.mSelected)
			BackingImage.color = HighlightColor;
	}

	public void OnPointerExit(PointerEventData EventData)
	{
		if (!mItem.mSelected)
			BackingImage.color = new Color(0, 0, 0, 0);
	}

	public void OnPointerClick(PointerEventData EventData)
	{
		if (EventData.clickCount == 2)
		{
			mItem.OnDoubleClicked(this);
		}
	}

	void Start ()
	{
		if (mItem.mSelected)
			BackingImage.color = SelectedColor;
		else
			BackingImage.color = new Color(0, 0, 0, 0);
	}

	public void SetSelected()
	{
		BackingImage.color = SelectedColor;
	}

	public void SetDeselected()
	{
		BackingImage.color = new Color(0, 0, 0, 0); ;
	}
}
