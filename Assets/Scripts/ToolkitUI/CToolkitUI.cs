using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

public class CTUITreeViewItem
{
	public GameObject mGob;
	public List<CTUITreeViewItem> mChildren;
	public bool mExpanded;
	public bool mSelected;
	public CTUITreeViewItem mParent;
	public int mIndentLevel;
	public string mText;

	private CTUITreeView _treeview;

	private Action _onClickedAction;
	private Action _onDoubleClickedAction;

	public CTUITreeViewItem(CTUITreeView TreeView, CTUITreeViewItem Parent, Action OnClicked = null, Action OnDoubleClicked = null)
	{	
		_treeview = TreeView;
		mParent = Parent;
		_onClickedAction = OnClicked;
		_onDoubleClickedAction = OnDoubleClicked;

		if (mParent != null)
			mIndentLevel = mParent.mIndentLevel + 1;
		else
			mIndentLevel = 0;
	}

	public void CreateGOB(GameObject TreeViewContent, string Text, int IndentLevel)
	{
		CToolkitUI.ETreeViewItemState state = CToolkitUI.ETreeViewItemState.TVIS_CHILD;

		if (mChildren != null && mChildren.Count > 0)
		{
			if (mExpanded)
				state = CToolkitUI.ETreeViewItemState.TVIS_OPEN;
			else
				state = CToolkitUI.ETreeViewItemState.TVIS_CLOSED;
		}

		mGob = _treeview.mUI.TreeViewAddItem(this, TreeViewContent, Text, IndentLevel, state);
	}

	public CTUITreeViewItem AddItem(string Text, Action OnClickAction = null, Action OnDoubleClickAction = null)
	{
		if (mChildren == null)
			mChildren = new List<CTUITreeViewItem>();

		/*
		int index = 0;
		if (mGob != null)
			index = mGob.GetComponent<RectTransform>().GetSiblingIndex();
		index += mChildren.Count + 1;
		*/

		CTUITreeViewItem item = new CTUITreeViewItem(_treeview, this, OnClickAction, OnDoubleClickAction);
		item.mText = Text;
		//item.CreateGOB(_treeview.mGob, Text, index, mIndentLevel + 1);

		mChildren.Add(item);

		return item;
	}

	public void Remove()
	{
		mParent.RemoveChild(this);
	}

	public void RemoveChild(CTUITreeViewItem Item)
	{
		if (Item.mSelected)
			_treeview.SelectItem(null);

		GameObject.Destroy(Item.mGob);
		mChildren.Remove(Item);
	}

	public void RemoveChildren()
	{
		if (mChildren == null)
			return;

		while (mChildren.Count > 0)
		{
			RemoveChild(mChildren[0]);
		}
	}

	public void RebuildEntireTree()
	{
		_treeview.Rebuild();
	}

	public void Rebuild(GameObject TreeViewContent, int IndentLevel)
	{
		if (mParent != null)
			CreateGOB(TreeViewContent, mText, IndentLevel);

		if (mChildren != null && mExpanded)
			for (int i = 0; i < mChildren.Count; ++i)
				mChildren[i].Rebuild(TreeViewContent, IndentLevel + 1);
	}

	public void OnClicked(TreeViewItem Item)
	{
		if (mChildren == null)
			_treeview.SelectItem(this);
		else
			_treeview.SelectItem(null);

		if (_onClickedAction != null)
			_onClickedAction();
	}

	public void OnDoubleClicked(TreeViewItem Item)
	{
		if (_onDoubleClickedAction != null)
			_onDoubleClickedAction();
	}

	public void Select()
	{
		_treeview.SelectItem(this);
	}
}

public class CTUITreeView
{
	public CToolkitUI mUI;
	public GameObject mGob;
	public CTUITreeViewItem mRootItem;

	public CTUITreeViewItem mSelectedItem;

	public CTUITreeView(CToolkitUI ToolkitUI, GameObject TreeViewGOB)
	{
		mUI = ToolkitUI;
		mGob = TreeViewGOB;
		mRootItem = new CTUITreeViewItem(this, null);
		mRootItem.mExpanded = true;
	}

	/// <summary>
	/// Ugh, try only call this once per frame.
	/// </summary>
	public void Rebuild()
	{
		RectTransform rect = mGob.GetComponent<RectTransform>();

		for (int i = 0; i < rect.childCount; ++i)
			GameObject.Destroy((rect.GetChild(i).gameObject));

		mRootItem.Rebuild(mGob, 0);
	}

	public void SelectItem(CTUITreeViewItem Item)
	{
		if (mSelectedItem != null)
		{
			mSelectedItem.mSelected = false;

			if (mSelectedItem.mGob != null)
				mSelectedItem.mGob.GetComponent<TreeViewItem>().SetDeselected();
		}

		mSelectedItem = Item;

		if (mSelectedItem != null)
		{
			mSelectedItem.mSelected = true;

			if (mSelectedItem.mGob != null)
				mSelectedItem.mGob.GetComponent<TreeViewItem>().SetSelected();
		}
	}
}

/// <summary>
/// Manages Toolkit UI.
/// Interface for create and maintaining UI elements.
/// </summary>
public class CToolkitUI : MonoBehaviour
{
	public enum EButtonHighlight
	{
		NOTHING,
		SELECTED,
		WARNING
	}

	public delegate List<string> ComboBoxDataDelegate();

	// Unity References
	public GameObject Canvas;
	public RectTransform CanvasTransform;
	public Texture SceneViewRT;

	// TODO: Encapsulate style in own data structure that can act as an asset?
	// Style
	public Font DefaultFont = null;
	public int DefaultFontSize = 11;

	public Font HeadingFont = null;
	public int HeadingFontSize = 14;

	public Font SceneTextFont = null;

	public Color PrimaryBackground;
	public Color WindowPanelBackground;
	public Color WindowBackground;

	public Color SliderDefault;

	public int TreeViewDepthSpacing = 26;
	public Color TreeViewItemHighlight;

	public Color FoldOutTitleBackground;
	public Color FoldOutTitleHighlight;
	public Color FoldOutBackground;

	public Color ToolbarButtonHighlight;
	public Color ToolbarButtonWarning;

	public Sprite BrushImage;
	public Sprite ModelImage;
	public Sprite SheetImage;
	public Sprite ItemImage;
	public Sprite LevelImage;
	public Sprite CompanyImage;
	public Sprite FoldOutArrowClosedImage;
	public Sprite FoldOutArrowOpenImage;
	public Sprite SaveImage;
	public Sprite UndoImage;
	public Sprite RedoImage;

	public Sprite IndicatorBkg;
	public Sprite IndicatorNoDesk;

	public enum ETreeViewItemState
	{
		TVIS_CHILD,
		TVIS_OPEN,
		TVIS_CLOSED
	}

	public enum ETextStyle
	{
		TS_DEFAULT,
		TS_HEADING
	}

	public void SetTransform(GameObject Element, float X, float Y)
	{
		RectTransform rect = Element.GetComponent<RectTransform>();
		rect.anchoredPosition = new Vector2(X, Y);
	}

	public void SetTransform(GameObject Element, float X, float Y, float Width, float Height)
	{
		RectTransform rect = Element.GetComponent<RectTransform>();
		rect.anchoredPosition = new Vector2(X, Y);
		rect.sizeDelta = new Vector2(Width, Height);
	}

	public void SetAnchors(GameObject Element, Vector2 AnchorMin, Vector2 AnchorMax, Vector2 Pivot)
	{
		RectTransform rect = Element.GetComponent<RectTransform>();
		rect.anchorMin = AnchorMin;
		rect.anchorMax = AnchorMax;
		rect.pivot = Pivot;
	}

	public GameObject CreateElement(GameObject Parent, string Name = "element")
	{
		Type[] comps = { typeof(RectTransform) };
		GameObject gob = new GameObject(Name, comps);

		if (Parent != null)
			gob.transform.SetParent(Parent.GetComponent<RectTransform>());

		RectTransform rect = gob.GetComponent<RectTransform>();
		// TODO: Some things might not want their pivot set here?
		rect.pivot = new Vector2(0.0f, 1.0f);
		rect.anchorMin = new Vector2(0.0f, 1.0f);
		rect.anchorMax = new Vector2(0.0f, 1.0f);

		return gob;
	}

	public GameObject CreateTextElement(GameObject Parent, string TextString, string Name = "text", ETextStyle Style = ETextStyle.TS_DEFAULT)
	{
		GameObject gob = CreateElement(Parent, Name);
		Text text = gob.AddComponent<Text>();

		if (Style == ETextStyle.TS_HEADING)
		{
			text.font = HeadingFont;
			text.fontSize = HeadingFontSize;
		}
		else
		{
			text.font = DefaultFont;
			text.fontSize = DefaultFontSize;
		}

		text.text = TextString;

		return gob;
	}

	public GameObject CreateTextElement(GameObject Parent, string TextString, Font TextFont, int TextSize, Color TextColor, string Name = "text")
	{
		GameObject gob = CreateElement(Parent, Name);
		Text text = gob.AddComponent<Text>();

		text.font = TextFont;
		text.fontSize = TextSize;
		text.text = TextString;

		return gob;
	}

	public GameObject CreateButton(GameObject Parent, string Text, Action OnClicked = null)
	{
		GameObject gob = CreateElement(Parent, "button");
		AddHorizontalLayout(gob);
		gob.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

		Image image = gob.AddComponent<Image>();

		Button button = gob.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = image;
		button.navigation = buttonNav;

		if (OnClicked != null)
			button.onClick.AddListener(() => OnClicked());

		if (Text != "")
		{
			GameObject text = CreateTextElement(gob, Text);
			text.GetComponent<Text>().font = HeadingFont;
			text.GetComponent<Text>().color = Color.black;
		}

		return gob;
	}

	public GameObject CreateToolbarButton(GameObject Parent, string Text, Sprite Icon, Action OnClicked = null)
	{
		GameObject gob = CreateElement(Parent, "button");
		AddVerticalLayout(gob);
		gob.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
		gob.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);
		AddLayout(gob, -1, 48, -1, -1);

		Image btnBackground = gob.AddComponent<Image>();

		Button button = gob.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = btnBackground;
		button.navigation = buttonNav;

		if (OnClicked != null)
			button.onClick.AddListener(() => OnClicked());

		ColorBlock cols = new ColorBlock();
		cols.highlightedColor = ToolbarButtonHighlight;
		cols.normalColor = new Color(ToolbarButtonHighlight.r, ToolbarButtonHighlight.g, ToolbarButtonHighlight.b, 0.0f);
		cols.pressedColor = new Color(ToolbarButtonHighlight.r * 0.8f, ToolbarButtonHighlight.g * 0.8f, ToolbarButtonHighlight.b * 0.8f, ToolbarButtonHighlight.a);
		cols.colorMultiplier = 1.0f;
		button.colors = cols;

		GameObject icon = CreateElement(gob, "image");
		AddLayout(icon, 26, 26, -1, -1);
		Image iconImage = icon.AddComponent<Image>();
		iconImage.sprite = Icon;

		if (Text != "")
		{
			GameObject text = CreateTextElement(gob, Text);
			//text.GetComponent<Text>().font = HeadingFont;
		}

		return gob;
	}

	public void SetToolbarButtonHighlight(GameObject Button, EButtonHighlight Mode)
	{
		Button button = Button.GetComponent<Button>();
		ColorBlock colors = button.colors;

		if (Mode == EButtonHighlight.NOTHING)
			colors.normalColor = new Color(ToolbarButtonHighlight.r, ToolbarButtonHighlight.g, ToolbarButtonHighlight.b, 0.0f);
		else if (Mode == EButtonHighlight.SELECTED)
			colors.normalColor = ToolbarButtonHighlight;
		else if (Mode == EButtonHighlight.WARNING)
			colors.normalColor = ToolbarButtonWarning;

		button.colors = colors;
	}

	public void SetToolbarButtonEnabled(GameObject Button, bool Enabled)
	{
		Button button = Button.GetComponent<Button>();
		button.interactable = Enabled;

		if (Enabled)
		{
			Button.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, 1);
			Button.transform.GetChild(1).GetComponent<Text>().color = new Color(1, 1, 1, 1);
		}
		else
		{
			Button.transform.GetChild(0).GetComponent<Image>().color = FoldOutBackground;
			Button.transform.GetChild(1).GetComponent<Text>().color = FoldOutBackground;
		}
	}

	public GameObject CreateToolbarSeparator(GameObject Parent)
	{
		GameObject gob = CreateElement(Parent, "separator");
		AddLayout(gob, 2, -1, -1, 1.0f);

		Image img = gob.AddComponent<Image>();
		img.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);

		return gob;
	}

	public GameObject CreateInputElement(GameObject Parent, int Mode, Action<String> OnEdited = null)
	{
		GameObject gob = CreateElement(Parent, "input");
		AddLayout(gob, -1, 20, 1.0f, -1);

		InputField input = gob.AddComponent<InputField>();

		AddImage(gob, Color.white);

		GameObject text = CreateTextElement(gob, "");
		Text t = text.GetComponent<Text>();
		t.font = HeadingFont;
		t.color = Color.black;
		t.supportRichText = false;
		t.alignment = TextAnchor.MiddleLeft;
		SetRectFillParent(text);
		
		input.textComponent = text.GetComponent<Text>();

		if (OnEdited != null)
			input.onEndEdit.AddListener((string inputString) => OnEdited(inputString));

		if (Mode == 1)
		{
			input.contentType = InputField.ContentType.DecimalNumber;
			input.text = "0.0";
		}
		if (Mode == 3)
		{
			input.contentType = InputField.ContentType.IntegerNumber;
			input.text = "0";
		}
		else
		{
			input.contentType = InputField.ContentType.Standard;
			input.text = "(Not Implemented)";
		}

		return gob;
	}

	public GameObject CreateScrollView(GameObject Parent, out GameObject Content, bool ConstrainContentWidth = false, bool InGame = false)
	{
		GameObject scrollView = CreateElement(Parent, "scrollView");
		ScrollRect scrollRect = scrollView.AddComponent<CBetterScrollRect>();
		AddImage(scrollView, new Color(0, 0, 0, 0));
		
		GameObject viewport = CreateElement(scrollView, "viewport");
		SetRectFillParent(viewport);
		viewport.AddComponent<RectMask2D>();

		float scrollBarWidth = 10.0f;

		// Horizontal
		GameObject horzScroll = CreateScrollBar(scrollView, false, InGame);
		RectTransform rect = horzScroll.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0, 0);
		rect.anchorMax = new Vector2(1, 0);
		rect.anchoredPosition = new Vector2(0, scrollBarWidth);
		rect.sizeDelta = new Vector2(0.0f, scrollBarWidth);

		// Vertical
		GameObject vertScroll = CreateScrollBar(scrollView, false, InGame);
		vertScroll.GetComponent<Scrollbar>().direction = Scrollbar.Direction.BottomToTop;
		rect = vertScroll.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(1, 0);
		rect.anchorMax = new Vector2(1, 1);
		rect.anchoredPosition = new Vector2(-scrollBarWidth, 0);
		rect.sizeDelta = new Vector2(scrollBarWidth, 0.0f);

		GameObject content = CreateElement(viewport, "content");
		RectTransform contentRect = content.GetComponent<RectTransform>();
		contentRect.anchorMin = new Vector2(0.0f, 1.0f);
		contentRect.anchorMax = new Vector2(1.0f, 1.0f);
		contentRect.pivot = new Vector2(0.0f, 1.0f);
		contentRect.anchoredPosition = Vector3.zero;
		contentRect.sizeDelta = new Vector2(0.0f, 0.0f);

		content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		if (!ConstrainContentWidth)
			content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

		AddVerticalLayout(content);
		
		scrollRect.viewport = viewport.GetComponent<RectTransform>();
		scrollRect.content = contentRect;
		scrollRect.horizontalScrollbar = horzScroll.GetComponent<Scrollbar>();
		scrollRect.verticalScrollbar = vertScroll.GetComponent<Scrollbar>();

		// TODO: Can't do this becuase of bug in Unity 5.3.X:
		//scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
		//scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
		scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

		scrollRect.verticalScrollbarSpacing = 5.0f;
		scrollRect.horizontalScrollbarSpacing = 5.0f;
		scrollRect.scrollSensitivity = 30.0f;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.inertia = false;
		
		Content = content;
		return scrollView;
	}

	public GameObject CreateScrollBar(GameObject Parent, bool Vertical, bool InGame = false)
	{
		GameObject scrollbar = CreateElement(Parent, "scrollBar");
		Scrollbar sb = scrollbar.AddComponent<Scrollbar>();

		if (InGame)
			AddImage(scrollbar, CGame.GameUIStyle.ThemeColorB);

		GameObject slideArea = CreateElement(scrollbar, "slideArea");
		SetRectFillParent(slideArea);
		
		// Depending on orientation is how this works?
		GameObject handle = CreateElement(slideArea, "handle");
		RectTransform rect = handle.GetComponent<RectTransform>();
		rect.pivot = new Vector2(0.0f, 1.0f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;

		if (InGame)
			AddImage(handle, CGame.GameUIStyle.ThemeColorC);
		else
			AddImage(handle, SliderDefault);

		sb.handleRect = handle.GetComponent<RectTransform>();

		return scrollbar;
	}
	
	public GameObject CreateTreeView(GameObject Parent, out CTUITreeView TreeView)
	{
		GameObject gob = CreateElement(Parent, "treeView");
		AddVerticalLayout(gob);
		gob.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(2, 2, 2, 2);
		
		TreeView = new CTUITreeView(this, gob);

		return gob;
	}

	public GameObject TreeViewAddItem(CTUITreeViewItem Item, GameObject Parent, string Text, int IndentLevel, ETreeViewItemState State)
	{
		GameObject item = CreateElement(Parent, "item");
		AddHorizontalLayout(item);
		item.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(2, 2, 2, 2);
		item.GetComponent<HorizontalLayoutGroup>().spacing = 4;
		item.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

		AddLayout(item, -1, 20, 1.0f, -1);
		//item.GetComponent<RectTransform>().SetSiblingIndex(Index);

		TreeViewItem tvi = item.AddComponent<TreeViewItem>();
		tvi.BackingImage = item.AddComponent<Image>();
		tvi.HighlightColor = TreeViewItemHighlight;
		tvi.SelectedColor = ToolbarButtonHighlight;
		tvi.mItem = Item;

		if (IndentLevel > 1)
		{
			GameObject spacer = CreateElement(item, "spacer");
			AddLayout(spacer, TreeViewDepthSpacing * (IndentLevel - 1), -1, -1, -1);
		}

		if (State == ETreeViewItemState.TVIS_CLOSED)
		{
			GameObject arrowImg = CreateElement(item, "arrow");
			AddImage(arrowImg, Color.white);
			arrowImg.GetComponent<Image>().sprite = FoldOutArrowClosedImage;
			AddLayout(arrowImg, 12, 12, -1, -1);
		}
		else if(State == ETreeViewItemState.TVIS_OPEN)
		{
			GameObject arrowImg = CreateElement(item, "arrow");
			AddImage(arrowImg, Color.white);
			arrowImg.GetComponent<Image>().sprite = FoldOutArrowOpenImage;
			AddLayout(arrowImg, 12, 12, -1, -1);
		}

		GameObject gob = CreateTextElement(item, Text, "tvItem");

		return item;
	}

	private int _ParseInt(string Text)
	{
		int result = 0;
		int.TryParse(Text, out result);
		return result;
	}

	private float _ParseFloat(string Text)
	{
		float result = 0.0f;
		float.TryParse(Text, out result);
		return result;
	}

	public GameObject CreateStringEditor(GameObject Parent, string Value, Action<string> OnEdited = null)
	{
		GameObject editor = CreateInputElement(Parent, 0, OnEdited);
		InputField input = editor.GetComponent<InputField>();

		input.text = Value;

		return editor;
	}

	public GameObject CreateComboBox(GameObject Parent, string DefaultText, ComboBoxDataDelegate DataDelegate, Action<string> OnEdited = null)
	{
		GameObject gob = CreateElement(Parent, "comboBox");
		AddHorizontalLayout(gob);

		Image image = gob.AddComponent<Image>();

		Button button = gob.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = image;
		button.navigation = buttonNav;

		GameObject text = CreateTextElement(gob, "selectedName");
		AddLayout(text, -1, 20, 1, -1);
		Text t = text.GetComponent<Text>();
		t.font = HeadingFont;
		t.color = Color.black;
		t.horizontalOverflow = HorizontalWrapMode.Overflow;
		t.alignment = TextAnchor.MiddleLeft;
		t.text = DefaultText;

		GameObject arrow = CreateElement(gob);
		AddLayout(arrow, 20, 20, -1, -1);

		GameObject arrowImg = CreateElement(arrow);
		AddImage(arrowImg, WindowBackground);
		arrowImg.GetComponent<Image>().sprite = FoldOutArrowOpenImage;
		SetRectFillParent(arrowImg, 3.0f);

		button.onClick.AddListener(() =>
		{
			List<string> data = DataDelegate();

			GameObject blocker = CreateElement(Canvas, "blocker");
			blocker.transform.SetAsLastSibling();
			SetRectFillParent(blocker);
			AddImage(blocker, new Color(0, 0, 0, 0));

			CUIEventHandler blockerClickHandler = blocker.AddComponent<CUIEventHandler>();
			blockerClickHandler.mAction = () =>
			{
				GameObject.Destroy(blocker);
			};
			
			GameObject listBack = CreateElement(blocker, "comboList");
			listBack.transform.SetAsLastSibling();
			listBack.GetComponent<RectTransform>().anchorMin = Vector2.zero;
			listBack.GetComponent<RectTransform>().anchorMax = Vector2.zero;
			// TODO: make this pixel perfect
			SetTransform(listBack, gob.GetComponent<RectTransform>().position.x, gob.GetComponent<RectTransform>().position.y - 20, gob.GetComponent<RectTransform>().sizeDelta.x, 100);
			AddImage(listBack, WindowBackground);
			//listBack.AddComponent<LayoutElement>().minHeight = 20;

			GameObject scrollContent;
			GameObject scrollView = CreateScrollView(listBack, out scrollContent, true);
			SetRectFillParent(scrollView);

			for (int i = 0; i < data.Count; ++i)
			{
				string entryName = data[i];

				GameObject entry = CreateElement(scrollContent, "test");
				AddLayout(entry, -1, 20, 1, -1);
				AddImage(entry, Color.blue);
				AddHorizontalLayout(entry);
				entry.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

				ContextMenuItem contextItem = entry.AddComponent<ContextMenuItem>();
				contextItem.BackingImage = entry.GetComponent<Image>();
				contextItem.HighlightColor = ToolbarButtonHighlight;
				contextItem.SelectedColor = ToolbarButtonHighlight;
				contextItem.SetAction(() =>
				{
					GameObject.Destroy(blocker);
					if (OnEdited != null)
					{
						t.text = entryName;
						OnEdited(entryName);						
					}
				});

				GameObject entryText = CreateTextElement(entry, "test");
				//AddLayout(entryText, -1, 20, 1, -1);			
				Text entryTextText = entryText.GetComponent<Text>();
				entryTextText.font = HeadingFont;
				entryTextText.color = Color.white;
				entryTextText.horizontalOverflow = HorizontalWrapMode.Overflow;
				entryTextText.text = entryName;
			}
		});
		
		return gob;
	}

	public GameObject CreateColorEditor(GameObject Parent, Color Value, Action<Color> OnEdited)
	{
		GameObject hl = CreateElement(Parent, "horz");
		AddLayout(hl, -1, -1, 1.0f, -1);
		AddHorizontalLayout(hl);
		hl.GetComponent<HorizontalLayoutGroup>().spacing = 4.0f;

		//CreateTextElement(hl, "R");
		InputField i1 = CreateInputElement(hl, 1).GetComponent<InputField>();
		//CreateTextElement(hl, "G");
		InputField i2 = CreateInputElement(hl, 1).GetComponent<InputField>();
		//CreateTextElement(hl, "B");
		InputField i3 = CreateInputElement(hl, 1).GetComponent<InputField>();
		//CreateTextElement(hl, "A");
		//InputField i4 = CreateInputElement(hl, 1).GetComponent<InputField>();

		GameObject prev = CreateElement(hl, "preview");
		prev.AddComponent<Image>().color = new Color(Value.r, Value.g, Value.b);
		AddLayout(prev, 32, 20, -1, -1);

		i1.text = (Value.r * 255.0f).ToString();
		i2.text = (Value.g * 255.0f).ToString();
		i3.text = (Value.b * 255.0f).ToString();
		//i4.text = (Value.a * 255.0f).ToString();

		i1.onEndEdit.AddListener((string text) => { OnEditColorEditor(hl, OnEdited); });
		i2.onEndEdit.AddListener((string text) => { OnEditColorEditor(hl, OnEdited); });
		i3.onEndEdit.AddListener((string text) => { OnEditColorEditor(hl, OnEdited); });
		//i4.onEndEdit.AddListener((string text) => { OnEditColorEditor(hl, OnEdited); });

		return hl;
	}

	public void OnEditColorEditor(GameObject Editor, Action<Color> OnEdited)
	{
		InputField i1 = Editor.transform.GetChild(0).GetComponent<InputField>();
		InputField i2 = Editor.transform.GetChild(1).GetComponent<InputField>();
		InputField i3 = Editor.transform.GetChild(2).GetComponent<InputField>();
		//InputField i4 = Editor.transform.GetChild(7).GetComponent<InputField>();
		Image prev = Editor.transform.GetChild(3).GetComponent<Image>();

		float f1 = _ParseFloat(i1.text);
		float f2 = _ParseFloat(i2.text);
		float f3 = _ParseFloat(i3.text);
		//float f4 = _ParseFloat(i4.text);

		f1 = Mathf.Clamp(f1, 0.0f, 255.0f);
		f2 = Mathf.Clamp(f2, 0.0f, 255.0f);
		f3 = Mathf.Clamp(f3, 0.0f, 255.0f);
		//f4 = Mathf.Clamp(f4, 0.0f, 255.0f);

		i1.text = f1.ToString();
		i2.text = f2.ToString();
		i3.text = f3.ToString();
		//i4.text = f4.ToString();

		Color col = new Color(f1 / 255.0f, f2 / 255.0f, f3 / 255.0f, 1.0f);
		prev.color = col;
		OnEdited(col);
	}

	public void SetColorEditorValue(GameObject Editor, Color Value)
	{
		InputField i1 = Editor.transform.GetChild(0).GetComponent<InputField>();
		InputField i2 = Editor.transform.GetChild(1).GetComponent<InputField>();
		InputField i3 = Editor.transform.GetChild(2).GetComponent<InputField>();

		Editor.transform.GetChild(3).GetComponent<Image>().color = new Color(Value.r, Value.g, Value.b);

		i1.text = (Value.r * 255.0f).ToString();
		i2.text = (Value.g * 255.0f).ToString();
		i3.text = (Value.b * 255.0f).ToString();
	}

	public GameObject CreateVector3Editor(GameObject Parent, Vector3 Value, Action<Vector3> OnEdited)
	{
		GameObject hl = CreateElement(Parent, "horz");
		AddLayout(hl, -1, -1, 1.0f, -1);
		AddHorizontalLayout(hl);
		hl.GetComponent<HorizontalLayoutGroup>().spacing = 4.0f;

		CreateTextElement(hl, "X");
		InputField i1 = CreateInputElement(hl, 1).GetComponent<InputField>();
		CreateTextElement(hl, "Y");
		InputField i2 = CreateInputElement(hl, 1).GetComponent<InputField>();
		CreateTextElement(hl, "Z");
		InputField i3 = CreateInputElement(hl, 1).GetComponent<InputField>();

		i1.text = Value.x.ToString();
		i2.text = Value.y.ToString();
		i3.text = Value.z.ToString();

		i1.onEndEdit.AddListener((string text) => { OnEdited(new Vector3(_ParseFloat(i1.text), _ParseFloat(i2.text), _ParseFloat(i3.text))); });
		i2.onEndEdit.AddListener((string text) => { OnEdited(new Vector3(_ParseFloat(i1.text), _ParseFloat(i2.text), _ParseFloat(i3.text))); });
		i3.onEndEdit.AddListener((string text) => { OnEdited(new Vector3(_ParseFloat(i1.text), _ParseFloat(i2.text), _ParseFloat(i3.text))); });

		return hl;
	}

	public void ModifyVector3Editor(GameObject Editor, Vector3 Value)
	{
		Editor.transform.GetChild(1).GetComponent<InputField>().text = Value.x.ToString();
		Editor.transform.GetChild(3).GetComponent<InputField>().text = Value.y.ToString();
		Editor.transform.GetChild(5).GetComponent<InputField>().text = Value.z.ToString();
	}

	public GameObject CreateIntEditor(GameObject Parent, int Value, Action<int> OnEdited)
	{
		GameObject gob = CreateInputElement(Parent, 3);
		InputField input = gob.GetComponent<InputField>();

		input.text = Value.ToString();

		if (OnEdited != null)
			input.onEndEdit.AddListener((string text) => { OnEdited(_ParseInt(input.text)); });

		return gob;
	}

	public GameObject CreateBoolEditor(GameObject Parent, bool Value, Action<bool> OnEdited)
	{
		GameObject gob = CreateElement(Parent, "toggleInput");
		AddLayout(gob, 20, 20, -1, -1);
		AddImage(gob, Color.white);
		
		GameObject checkmark = CreateElement(gob, "mark");
		SetTransform(checkmark, 0, 0, 20, 20);
		AddImage(checkmark, Color.black);
		Image checkImg = checkmark.GetComponent<Image>();
		checkImg.sprite = CGame.GameUIStyle.Sprites[18];

		Toggle toggle = gob.AddComponent<Toggle>();
		toggle.targetGraphic = gob.GetComponent<Image>();
		toggle.graphic = checkImg;
		toggle.isOn = Value;
		Navigation nav = new Navigation();
		nav.mode = Navigation.Mode.None;
		toggle.navigation = nav;
		
		if (OnEdited != null)
			toggle.onValueChanged.AddListener((value) => OnEdited(value));

		return gob;
	}

	public GameObject CreateFloatEditor(GameObject Parent, float Value, Action<float> OnEdited)
	{
		GameObject gob = CreateInputElement(Parent, 1);
		InputField input = gob.GetComponent<InputField>();

		input.text = Value.ToString();

		if (OnEdited != null)
			input.onEndEdit.AddListener((string text) => { OnEdited(_ParseFloat(input.text)); });

		return gob;
	}

	public void ModifyFloatEditor(GameObject Editor, float Value)
	{
		Editor.GetComponent<InputField>().text = Value.ToString();
	}

	public GameObject CreateVector2Editor(GameObject Parent, Vector2 Value, Action<Vector2> OnEdited)
	{
		GameObject hl = CreateElement(Parent, "horz");
		AddLayout(hl, -1, -1, 1.0f, -1);
		AddHorizontalLayout(hl);
		hl.GetComponent<HorizontalLayoutGroup>().spacing = 4.0f;

		CreateTextElement(hl, "X");
		InputField i1 = CreateInputElement(hl, 1).GetComponent<InputField>();
		CreateTextElement(hl, "Y");
		InputField i2 = CreateInputElement(hl, 1).GetComponent<InputField>();
		
		i1.text = Value.x.ToString();
		i2.text = Value.y.ToString();
		
		i1.onEndEdit.AddListener((string text) => { OnEdited(new Vector2(_ParseFloat(i1.text), _ParseFloat(i2.text))); });
		i2.onEndEdit.AddListener((string text) => { OnEdited(new Vector2(_ParseFloat(i1.text), _ParseFloat(i2.text))); });
		
		return hl;
	}

	public GameObject CreateFieldElement(GameObject Parent, string Title, out GameObject Editor)
	{
		GameObject field = CreateElement(Parent, "field");
		AddHorizontalLayout(field);
		field.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

		GameObject left = CreateElement(field, "left");
		AddVerticalLayout(left);
		LayoutElement layout = left.AddComponent<LayoutElement>();
		layout.minWidth = 0.0f;
		layout.preferredWidth = 0.0f;
		layout.flexibleWidth = 1.0f;

		CreateTextElement(left, Title);

		GameObject right = CreateElement(field, "right");
		AddVerticalLayout(right);
		layout = right.AddComponent<LayoutElement>();
		layout.minWidth = 0.0f;
		layout.preferredWidth = 0.0f;
		layout.flexibleWidth = 1.5f;

		Editor = right;
		return field;
	}

	public GameObject CreateFoldOut(GameObject Parent, string Title, out GameObject Content, bool Expanded = true)
	{
		GameObject foldOut = CreateElement(Parent, "foldOut");
		AddVerticalLayout(foldOut);

		GameObject title = CreateElement(foldOut, "title");
		AddHorizontalLayout(title);
		title.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
		title.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 0, 0);
		title.GetComponent<HorizontalLayoutGroup>().spacing = 4;
		AddLayout(title, -1, 20, 1.0f, -1);
		AddImage(title, FoldOutTitleBackground);

		GameObject arrowImg = CreateElement(title, "arrow");
		AddImage(arrowImg, Color.white);
		arrowImg.GetComponent<Image>().sprite = FoldOutArrowOpenImage;
		AddLayout(arrowImg, 12, 12, -1, -1);

		CreateTextElement(title, Title);

		GameObject content = CreateElement(foldOut, "content");
		AddImage(content, FoldOutBackground);
		AddVerticalLayout(content);
		content.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);
		content.GetComponent<VerticalLayoutGroup>().spacing = 4;
		AddLayout(content, -1, -1, 1.0f, -1);

		FoldOutTitle fot = title.AddComponent<FoldOutTitle>();
		fot.Content = content;
		fot.HighlightColor = FoldOutTitleHighlight;
		fot.BackgroundColor = FoldOutTitleBackground;
		fot.BackingImage = title.GetComponent<Image>();
		fot.ArrowImage = arrowImg.GetComponent<Image>();
		fot.ArrowOpen = FoldOutArrowOpenImage;
		fot.ArrowClosed = FoldOutArrowClosedImage;
		fot.SetFold(Expanded);

		Content = content;
		return foldOut;
	}

	public void SetRectFillParent(GameObject Element, float Padding = 0.0f)
	{
		RectTransform rect = Element.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.0f, 1.0f);
		//rect.anchoredPosition = Vector2.zero;
		//rect.sizeDelta = Vector2.zero;
		
		rect.anchoredPosition = new Vector2(Padding, -Padding);
		rect.sizeDelta = new Vector2(-Padding * 2, -Padding * 2);
	}

	/*
	public void SetRectPadding(GameObject Element, Vector4 Rect)
	{
		RectTransform rect = Element.GetComponent<RectTransform>();		
		//rect.sizeDelta = new Vector2(Rect.z, Rect.w);
	}
	*/

	public VerticalLayoutGroup AddVerticalLayout(GameObject Element, RectOffset Padding = null, int Spacing = 0)
	{
		VerticalLayoutGroup layout = Element.AddComponent<VerticalLayoutGroup>();
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		if (Padding != null)
			layout.padding = Padding;

		layout.spacing = Spacing;

		return layout;
	}

	public void AddHorizontalLayout(GameObject Element, RectOffset Padding = null, int Spacing = 0)
	{
		HorizontalLayoutGroup layout = Element.AddComponent<HorizontalLayoutGroup>();
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		if (Padding != null)
			layout.padding = Padding;

		layout.spacing = Spacing;
	}

	public void AddLayout(GameObject Element, float width, float height, float flexWidth, float flexHeight)
	{
		// TODO: We check if a layout already exists, but that could be costly?
		LayoutElement l = Element.GetComponent<LayoutElement>();

		if (l == null)
			l = Element.AddComponent<LayoutElement>();

		if (width >= 0.0f)
		{
			l.minWidth = width;
			l.preferredWidth = width;
		}

		if (height >= 0.0f)
		{
			l.minHeight = height;
			l.preferredHeight = height;
		}

		if (flexWidth >= 0.0f)
		{
			l.flexibleWidth = flexWidth;
		}

		if (flexHeight >= 0.0f)
		{
			l.flexibleHeight = flexHeight;
		}
	}

	public void AddImage(GameObject Element, Color Colour)
	{
		Element.AddComponent<Image>().color = Colour;
	}
	
	/// <summary>
	/// A window panel sits on a primary window.
	/// </summary>
	public GameObject CreateWindowPanel(GameObject Parent, out GameObject Content, string WindowTitle)
	{
		GameObject wp = CreateElement(Parent, "windowPanel");
		AddVerticalLayout(wp);

		GameObject tabView2 = CreateTabView(wp);
		CreateWindowTab(tabView2, WindowTitle);

		GameObject s1Main = CreateElement(wp);
		AddVerticalLayout(s1Main);
		AddLayout(s1Main, -1, -1, 1.0f, 1.0f);
		AddImage(s1Main, WindowPanelBackground);
		s1Main.GetComponent<VerticalLayoutGroup>().spacing = 4;
		s1Main.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);

		Content = s1Main;
		return wp;
	}

	public GameObject CreateTabView(GameObject Parent)
	{
		GameObject gob = CreateElement(Parent, "tabView");
		LayoutElement layout = gob.AddComponent<LayoutElement>();
		layout.flexibleWidth = 1.0f;
		layout.flexibleHeight = 0.0f;
		layout.minHeight = 20.0f;
		layout.preferredHeight = 20.0f;
		AddHorizontalLayout(gob);
		gob.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerLeft;
		gob.GetComponent<HorizontalLayoutGroup>().spacing = 8;
		
		return gob;
	}

	public GameObject CreateWindowTab(GameObject TabViewParent, string TabText, Action OnClick = null, bool Active = true, bool GlobalTab = false)
	{
		GameObject tab = CreateElement(TabViewParent, "tab");
		AddHorizontalLayout(tab);
		tab.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 0, 0);
		tab.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

		float inactiveScale = 0.7f;

		if (GlobalTab)
		{
			if (Active)
				AddImage(tab, WindowBackground);
			else
				AddImage(tab, new Color(WindowBackground.r * inactiveScale, WindowBackground.g * inactiveScale, WindowBackground.b * inactiveScale, WindowBackground.a));
		}
		else
		{
			if (Active)
				AddImage(tab, WindowPanelBackground);
			else
				AddImage(tab, new Color(WindowPanelBackground.r * inactiveScale, WindowPanelBackground.g * inactiveScale, WindowPanelBackground.b * inactiveScale, WindowPanelBackground.a));
		}

		tab.AddComponent<LayoutElement>();
		tab.GetComponent<LayoutElement>().minWidth = 128.0f;
		tab.GetComponent<LayoutElement>().flexibleHeight = 1.0f;
		CreateTextElement(tab, TabText);

		if (OnClick != null)
		{
			Button btn = tab.AddComponent<Button>();
			btn.onClick.AddListener(() => OnClick());
		}

		return tab;
	}

	public void SetTab(GameObject Tab, bool Active, bool GlobalTab = false)
	{
		float inactiveScale = 0.7f;

		if (GlobalTab)
		{
			if (Active)
				Tab.GetComponent<Image>().color = WindowBackground;
			else
				Tab.GetComponent<Image>().color = new Color(WindowBackground.r * inactiveScale, WindowBackground.g * inactiveScale, WindowBackground.b * inactiveScale, WindowBackground.a);
		}
		else
		{
			if (Active)
				Tab.GetComponent<Image>().color = WindowPanelBackground;
			else
				Tab.GetComponent<Image>().color = new Color(WindowPanelBackground.r * inactiveScale, WindowPanelBackground.g * inactiveScale, WindowPanelBackground.b * inactiveScale, WindowPanelBackground.a);
		}
	}

	public GameObject CreateMenuButton(GameObject Parent, string Text, Action OnClicked = null, int Type = 0, bool Enabled = true)
	{
		GameObject gob = CreateElement(Parent, "button");
		AddHorizontalLayout(gob);
		gob.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
		gob.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(20, 0, 0, 0);

		//Image image = gob.AddComponent<Image>();

		Button button = gob.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.navigation = buttonNav;
		ColorBlock cb = new ColorBlock();
		cb.normalColor = Color.white;
		cb.pressedColor = Color.white;
		cb.highlightedColor = CGame.GameUIStyle.ThemeColorA;
		cb.disabledColor = CGame.GameUIStyle.ThemeColorB;
		cb.colorMultiplier = 1.0f;
		button.colors = cb;

		if (!Enabled)
			button.interactable = false;

		if (OnClicked != null)
			button.onClick.AddListener(() => OnClicked());

		if (Text != "")
		{
			GameObject text = CreateTextElement(gob, Text);


			if (Type == 0)
			{
				text.GetComponent<Text>().font = HeadingFont;
				text.GetComponent<Text>().fontSize = 24;
			}
			else
			{
				text.GetComponent<Text>().font = DefaultFont;
				text.GetComponent<Text>().fontSize = 18;
			}

			text.GetComponent<Text>().color = Color.white;

			button.targetGraphic = text.GetComponent<Text>();
		}

		return gob;
	}
}
