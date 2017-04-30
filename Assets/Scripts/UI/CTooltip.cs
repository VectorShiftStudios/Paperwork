using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
	 
public class CTooltip
{
	private GameObject _tooltip;
	private bool _tooltipOverGame;

	private CToolkitUI _ui;
	private CGameUIStyle _style;

	public CTooltip(CToolkitUI Toolkit, CGameUIStyle Style, GameObject Root)
	{
		_ui = Toolkit;
		_style = Style;

		_tooltipOverGame = false;
		_tooltip = _ui.CreateElement(Root, "tooltip");
		_ui.AddImage(_tooltip, _style.TooltipBackground);
		_ui.SetTransform(_tooltip, 0, 0, 256, 256);
		_ui.AddVerticalLayout(_tooltip, new RectOffset(3, 3, 3, 3), 3);
		_tooltip.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
		_tooltip.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
		ContentSizeFitter fitter = _tooltip.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		float width = 150.0f;

		GameObject title = _ui.CreateTextElement(_tooltip, "Title", "title", CToolkitUI.ETextStyle.TS_HEADING);
		_ui.AddLayout(title, width, -1, -1, -1);

		GameObject details = _ui.CreateTextElement(_tooltip, "Sub-title", "subtitle", CToolkitUI.ETextStyle.TS_DEFAULT);
		_ui.AddLayout(details, width, -1, -1, -1);

		GameObject desc = _ui.CreateTextElement(_tooltip, "Description text that can be longer.", "description", CToolkitUI.ETextStyle.TS_HEADING);
		_ui.AddLayout(desc, width, -1, -1, -1);

		Hide(false);
	}


	/// <summary>
	/// Set details of the tooltip and show it.
	/// </summary>
	public void Set(string Title, string Details, string Description, Vector2 Position, Vector2 Pivot, bool OverGame)
	{
		_tooltipOverGame = OverGame;
		_tooltip.GetComponent<RectTransform>().anchoredPosition = Position;
		_tooltip.GetComponent<RectTransform>().pivot = Pivot;

		_tooltip.transform.GetChild(0).GetComponent<Text>().text = Title;

		if (Details == "")
		{
			_tooltip.transform.GetChild(1).gameObject.SetActive(false);
		}
		else
		{
			_tooltip.transform.GetChild(1).gameObject.SetActive(true);
			_tooltip.transform.GetChild(1).GetComponent<Text>().text = Details;
		}

		if (Description == "")
		{
			_tooltip.transform.GetChild(2).gameObject.SetActive(false);
		}
		else
		{
			_tooltip.transform.GetChild(2).gameObject.SetActive(true);
			_tooltip.transform.GetChild(2).GetComponent<Text>().text = Description;
		}

		_tooltip.SetActive(true);
	}

	/// <summary>
	/// Hide tooltip.
	/// </summary>
	public void Hide(bool OverGame)
	{
		if (_tooltipOverGame == OverGame)
			_tooltip.SetActive(false);
	}
}
