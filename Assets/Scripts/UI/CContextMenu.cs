using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CContextMenu
{
	private GameObject _root;
	private GameObject _contextMenu;

	private CToolkitUI _ui;
	private CGameUIStyle _style;

	public bool mShowing;

	public CContextMenu(CToolkitUI Toolkit, CGameUIStyle Style, GameObject Root)
	{
		_ui = Toolkit;
		_style = Style;

		_root = _ui.CreateElement(Root, "contextMenuRoot");
		_ui.AddImage(_root, new Color(0, 0, 0, 0.0f));
		_ui.SetRectFillParent(_root);
		_root.AddComponent<CUIContextMenuOverlay>();

		_contextMenu = _ui.CreateElement(_root, "contextMenu");
		_ui.AddImage(_contextMenu, _style.TooltipBackground);
		_ui.SetTransform(_contextMenu, 0, 0, 128, 256);
		_ui.AddVerticalLayout(_contextMenu, new RectOffset(3, 3, 3, 3), 3);
		_contextMenu.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
		_contextMenu.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);

		ContentSizeFitter fitter = _contextMenu.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Hide();
	}

	/// <summary>
	/// Add an item to the context menu.
	/// </summary>
	public void AddItem(string Text, Action OnClicked = null)
	{
		GameObject item = _ui.CreateElement(_contextMenu, "item");
		_ui.AddLayout(item, 128.0f, 20.0f, -1, -1);
		_ui.AddImage(item, Color.white);
		_ui.AddHorizontalLayout(item);
		item.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

		GameObject title = _ui.CreateTextElement(item, Text, "item", CToolkitUI.ETextStyle.TS_HEADING);
		//_ui.AddLayout(title, 128.0f, -1, -1, -1);

		ColorBlock cb = new ColorBlock();
		cb.normalColor = _style.ThemeColorB;
		cb.highlightedColor = _style.ThemeColorA;
		cb.colorMultiplier = 1.0f;
		
		Button button = item.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = item.GetComponent<Image>();
		button.navigation = buttonNav;
		button.colors = cb;

		if (OnClicked != null)
			button.onClick.AddListener(() =>
			{
				OnClicked();
				Hide();
			});
	}

	/// <summary>
	/// Add category text to the context menu.
	/// </summary>
	public void AddCategory(string Text)
	{
		GameObject title = _ui.CreateTextElement(_contextMenu, Text, "itemCat", CToolkitUI.ETextStyle.TS_DEFAULT);
		_ui.AddLayout(title, 128.0f, -1, -1, -1);
	}

	/// <summary>
	/// Remove all content.
	/// </summary>
	public void Clear()
	{
		CUtility.DestroyChildren(_contextMenu);
	}

	/// <summary>
	/// Show new context menu.
	/// </summary>
	public void Show(Vector2 Position, Vector2 Pivot)
	{
		_contextMenu.GetComponent<RectTransform>().anchoredPosition = Position;
		_contextMenu.GetComponent<RectTransform>().pivot = Pivot;

		_root.SetActive(true);
		mShowing = true;
	}

	/// <summary>
	/// Hide context menu.
	/// </summary>
	public void Hide()
	{
		_root.SetActive(false);
		mShowing = false;
	}
}
