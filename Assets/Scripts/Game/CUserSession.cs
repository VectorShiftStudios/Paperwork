using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CContractInTray
{
	public GameObject mGob;
	public GameObject mPaperRoot;
	public CContractView mContract;
	public Transform mTimerBar;
	public int mUndistributedStacks;
	public int mUncompletedStacks;
}

public class CNotifyStackIcon
{
	public GameObject mGob;
	public int mHeight;
	public float mTargetY;
	public float mCurrentY;
	public Transform mTimerBar;

	public bool mAccepted;
	public float mFadeOut;
}

public class CEmployeeEntry
{
	public GameObject mGob;
	public RectTransform mStaminaBar;
	public RectTransform mLevelBar;
	public RectTransform mStressBar;
}

/// <summary>
/// Pumps turns into Game Session based on user input.
/// Manages all UI state for user.
/// </summary>
public class CUserSession
{
	private enum EInteractionMode
	{
		NORMAL,
		PLACE_ITEM,
		CONTEXT_MENU //??
	}

	private CGameSession _gameSession;
	private CUserWorldView _worldView;
	
	public int mPlayerIndex;
	public GameObject mPrimaryScene;

	public bool mUserInteraction;

	private CToolkitUI _ui;
	private CGameUIStyle _style;

	private GameObject _uiRoot;
	private GameObject _uiMenuRoot;
	private Text _moneyText;
	private GameObject _buyItemRoot;
	private GameObject _optionsMenu;
	private GameObject _employeeMenu;
	private GameObject _empListContent;
	private GameObject _uiNotifyList;
	private GameObject _uiOwnedList;
	private GameObject _paydayTimer;
	private GameObject _newResumeBlip;

	private int _newResumes;
	private List<CContractInTray> _contractTray;
	private List<CNotifyStackIcon> _notifyStack;
	private List<CEmployeeEntry> _employeeEntries;

	private CEntityPlacer _placementEntity;
	private CContractView _contractPlacement;
	private int _contractCursorCount = 0;
	private GameObject _contractCursor;

	private ISelectable _hovered = null;
	private ISelectable _selected = null;

	public CUserSession(CGameSession GameSession, CUserWorldView WorldView)
	{	
		_gameSession = GameSession;
		_worldView = WorldView;
		mPlayerIndex = GameSession.mUserPlayerIndex;
		mPrimaryScene = GameSession.mPrimaryScene;
		mUserInteraction = true;
		_ui = CGame.ToolkitUI;
		_style = CGame.GameUIStyle;

		_placementEntity = null;
		_contractPlacement = null;
		
		//--------------------------------------------------------------------
		// Game UI
		//--------------------------------------------------------------------
		_uiRoot = _ui.CreateElement(CGame.UIManager.primaryLayer, "GameUIRoot");
		_ui.SetRectFillParent(_uiRoot);

		_uiMenuRoot = _ui.CreateElement(CGame.UIManager.mMenuLayer, "MenuUIRoot");
		_ui.SetRectFillParent(_uiMenuRoot);

		/*
		// TODO: Maybe have this for debug purposes??
		GameObject rtTest = _ui.CreateElement(_uiRoot, "img");
		_ui.SetTransform(rtTest, 10, -60, 512, 512);
		RawImage ii = rtTest.AddComponent<RawImage>();
		ii.texture = CGame.ProcIconTexture;
		ii.uvRect = new Rect(0, 1, 1, -1);
		*/

		//--------------------------------------------------------------------
		// Options Menu
		//--------------------------------------------------------------------
		_optionsMenu = _ui.CreateElement(_uiMenuRoot, "OptionsWindow");
		_optionsMenu.SetActive(false);
		_ui.AddImage(_optionsMenu, _style.TooltipBackground);
		_ui.SetAnchors(_optionsMenu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		_ui.SetTransform(_optionsMenu, 0, 0, 300, 256);

		GameObject optsTitle = _ui.CreateElement(_optionsMenu);
		_ui.AddImage(optsTitle, _style.ThemeColorC);
		_ui.SetAnchors(optsTitle, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
		_ui.SetTransform(optsTitle, 0, 0, 0, 26);

		GameObject optsTitleText = _ui.CreateElement(optsTitle);
		_ui.SetAnchors(optsTitleText, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
		_ui.SetTransform(optsTitleText, 5, 0, -10, 0);
		Text optsTitleTextComp = optsTitleText.AddComponent<Text>();
		optsTitleTextComp.text = "Options";
		optsTitleTextComp.font = _style.FontA;
		optsTitleTextComp.alignment = TextAnchor.MiddleLeft;

		GameObject button = _ui.CreateMenuButton(_optionsMenu, "Continue", () => {
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
			ToggleOptionsMenu();
			//_PlayLevel();
		});
		_ui.SetTransform(button, 50, -50, 256, 50);

		button = _ui.CreateMenuButton(_optionsMenu, "Main Menu", () => {
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
			CGame.Game.TerminateGameSession(true);			
		});
		_ui.SetTransform(button, 50, -100, 256, 50);

		button = _ui.CreateMenuButton(_optionsMenu, "Exit", () => {
			CGame.Game.ExitApplication();
		});
		_ui.SetTransform(button, 50, -150, 256, 50);

		//--------------------------------------------------------------------
		// Top Bar
		//--------------------------------------------------------------------
		CreateTopBarIcon(_uiRoot, new Vector2(10 + 54 * 0, -10), 0);
		CreateTopBarIcon(_uiRoot, new Vector2(10 + 54 * 1, -10), 1);
		CreateTopBarIcon(_uiRoot, new Vector2(10 + 54 * 2, -10), 2);
		CreateTopBarIcon(_uiRoot, new Vector2(10 + 54 * 3, -10), 3);
		CreateTopBarIcon(_uiRoot, new Vector2(10 + 54 * 4, -10), 4);

		//--------------------------------------------------------------------
		// Bottom Bar
		//--------------------------------------------------------------------
		GameObject primStats = _ui.CreateElement(_uiRoot, "primStats");
		_ui.AddImage(primStats, _style.ThemeColorA);
		_ui.SetAnchors(primStats, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(primStats, 10, 10, 222, 54);
		
		GameObject money = _ui.CreateTextElement(primStats, "105,000", _style.FontA, 30, _style.ThemeColorE, "money");
		_moneyText = money.GetComponent<Text>();
		_moneyText.alignment = TextAnchor.MiddleRight;
		_ui.SetAnchors(money, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(money, 0, 0, 165, 54);

		GameObject moneyImg = _ui.CreateElement(money, "img");
		_ui.SetTransform(moneyImg, 0, 0, 54, 54);
		_ui.SetAnchors(moneyImg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0));
		moneyImg.AddComponent<Image>().sprite = _style.Sprites[13];

		_paydayTimer = _ui.CreateElement(money, "paydayTimer");
		_ui.AddImage(_paydayTimer, _style.ThemeColorE);
		_ui.SetAnchors(_paydayTimer, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(_paydayTimer, 0, 0, 222, 6);

		CreateInternHireIcon(_uiRoot, new Vector2(10, 56 + 10));

		GameObject bbarBack = _ui.CreateElement(_uiRoot, "bottomBarBacking");
		_ui.AddImage(bbarBack, _style.ThemeColorB);
		_ui.SetAnchors(bbarBack, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(bbarBack, 235, 10, 54 * 6, 54);
		
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 0, 10), 0);
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 1, 10), 1);
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 2, 10), 2);
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 3, 10), 3);
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 4, 10), 4);
		CreateCatIcon(_uiRoot, new Vector2(235 + 54 * 5, 10), 5);

		_buyItemRoot = _ui.CreateElement(_uiRoot, "buyItemRoot");
		_ui.SetRectFillParent(_buyItemRoot);

		//--------------------------------------------------------------------
		// Employee Window
		//--------------------------------------------------------------------
		_employeeEntries = new List<CEmployeeEntry>();
		_newResumes = 0;

		GameObject empWindow = _ui.CreateElement(_uiRoot, "empWindow");
		empWindow.SetActive(false);
		_employeeMenu = empWindow;
		//_ui.AddImage(empWindow, _style.ThemeColorA);
		_ui.SetAnchors(empWindow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		_ui.SetTransform(empWindow, 0, 0, 840, 500);

		GameObject empWinTitle = _ui.CreateElement(empWindow);
		_ui.AddImage(empWinTitle, _style.ThemeColorC);
		_ui.SetAnchors(empWinTitle, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, 1.0f));
		_ui.SetTransform(empWinTitle, 0, 0, 840, 26);

		GameObject empWinTitleText = _ui.CreateElement(empWinTitle);
		Text empWinTitleTextComp = empWinTitleText.AddComponent<Text>();
		empWinTitleTextComp.text = "Employees";
		empWinTitleTextComp.font = _style.FontA;
		empWinTitleTextComp.alignment = TextAnchor.MiddleLeft;
		_ui.SetTransform(empWinTitleText, 5, 0, 200, 26);

		CreateEmpWinCatIcon(empWindow, new Vector2(0, -28), "All");
		CreateEmpWinCatIcon(empWindow, new Vector2(0, -28 - 54 - 2), "New");
		CreateEmpWinCatIcon(empWindow, new Vector2(0, -28 - 54 - 2 - 54 - 2), "Hired");

		GameObject empWinListBack = _ui.CreateElement(empWindow, "list");
		_ui.AddImage(empWinListBack, _style.TooltipBackground);
		_ui.SetTransform(empWinListBack, 54 + 2, -28, 840 - 54 - 2, 500 - 28);

		GameObject empWinListSideBar = _ui.CreateElement(empWinListBack, "listBackingSide");
		_ui.AddImage(empWinListSideBar, _style.ThemeColorB);
		_ui.SetTransform(empWinListSideBar, 0, 0, 54, 500 - 28);

		// TODO: Header for table

		GameObject empWinListContent;
		GameObject empWinListScrollView = _ui.CreateScrollView(empWinListBack, out empWinListContent, false, true);
		_ui.SetTransform(empWinListScrollView, 0, -28, 784, 500 - 28 - 28);
		_empListContent = empWinListContent;

		//--------------------------------------------------------------------
		// Notify Stack UI
		//--------------------------------------------------------------------
		_notifyStack = new List<CNotifyStackIcon>();

		//CreateNotifyStackIcon(1);
		//CreateNotifyStackIcon(2, null, CGame.AssetManager.GetAsset<CItemAsset>("item_couch_test"));

		//--------------------------------------------------------------------
		// Contract Tray UI
		//--------------------------------------------------------------------
		_contractTray = new List<CContractInTray>();

		//--------------------------------------------------------------------
		// Dummy UI
		//--------------------------------------------------------------------
		//GameObject iv =_ui.CreateElement(_uiRoot, "iconsView");
		//_ui.SetTransform(iv, 0, 0, 512, 512);
		//iv.AddComponent<RawImage>().texture = CGame.ProcIconTexture;		

		_uiNotifyList = _ui.CreateElement(_uiRoot);
		_ui.SetTransform(_uiNotifyList, 5, -120, 256, 512);
		_ui.AddVerticalLayout(_uiNotifyList);

		_uiOwnedList = _ui.CreateElement(_uiRoot);
		_ui.SetTransform(_uiOwnedList, -5, -50, 256, 512);
		_ui.SetAnchors(_uiOwnedList, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
		_ui.AddVerticalLayout(_uiOwnedList);
		//--------------------------------------------------------------------

		// Actions Temp
		//_gameSession.PushUserAction(new CUserAction(CUserAction.EType.T_RETURN_CONTRACT_PAPERS, ID, 0, 0));
	}

	public void Destroy()
	{
		// TODO: Aggregate all this into a single object.
		// TODO: Determine what else we need to destroy here.
		GameObject.Destroy(_uiRoot);
		_uiRoot = null;
		GameObject.Destroy(_uiMenuRoot);
		_uiMenuRoot = null;
	}

	public GameObject CreateEmpWinCatIcon(GameObject Root, Vector2 Position, string Text)
	{
		GameObject catButton = _ui.CreateElement(Root, "cat" + Text);
		_ui.AddImage(catButton, _style.ThemeColorB);
		_ui.SetTransform(catButton, Position.x, Position.y, 54, 54);

		GameObject catText = _ui.CreateElement(catButton);
		_ui.SetRectFillParent(catText);
		Text catTextComp = catText.AddComponent<Text>();
		catTextComp.font = _style.FontA;
		catTextComp.text = Text;
		catTextComp.alignment = TextAnchor.MiddleCenter;

		return catButton;
	}

	public CEmployeeEntry CreateEmpEntry(GameObject Root, SUnitBasicStats Stats, bool mIntern, bool Resume, Action OnClick = null)
	{
		CEmployeeEntry result = new CEmployeeEntry();

		GameObject entry = _ui.CreateElement(Root);
		_ui.AddImage(entry, Color.white);
		_ui.SetTransform(entry, 0, 0, 784, 54);
		_ui.AddLayout(entry, 784, 54, 0.0f, 0.0f);
		result.mGob = entry;

		ColorBlock cb = new ColorBlock();
		cb.normalColor = new Color(0, 0, 0, 0);
		cb.highlightedColor = _style.ThemeColorB;
		cb.pressedColor = _style.TooltipBackground;
		cb.colorMultiplier = 1.0f;

		Button button = entry.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = entry.GetComponent<Image>();
		button.navigation = buttonNav;
		button.colors = cb;

		if (OnClick != null)
			button.onClick.AddListener(() => {
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
				OnClick();
			});

		GameObject nameText = _ui.CreateElement(entry);
		_ui.SetTransform(nameText, 60, 0, 200, 54);
		Text nameTextComp = nameText.AddComponent<Text>();
		nameTextComp.font = _style.FontA;
		nameTextComp.text = Stats.mName;
		nameTextComp.alignment = TextAnchor.MiddleLeft;

		GameObject rankText = _ui.CreateElement(entry);
		_ui.SetTransform(rankText, 200, 0, 200, 54);
		Text rankTextComp = rankText.AddComponent<Text>();
		rankTextComp.font = _style.FontA;
		rankTextComp.text = CGame.AssetManager.mUnitRules.GetTier(Stats.mTier).mTitle;
		rankTextComp.alignment = TextAnchor.MiddleLeft;

		if (Resume)
		{
			GameObject newText = _ui.CreateElement(entry);
			_ui.SetTransform(newText, 0, 0, 54, 54);
			Text newTextComp = newText.AddComponent<Text>();
			newTextComp.font = _style.FontA;
			newTextComp.text = "+";
			newTextComp.alignment = TextAnchor.MiddleCenter;
			newTextComp.fontSize = 20;
		}
		else
		{
			GameObject iconImg = _ui.CreateElement(entry, "img");
			_ui.SetTransform(iconImg, 0, 0, 54, 54);
			iconImg.AddComponent<Image>().sprite = _style.Sprites[6];

			GameObject lvlText = _ui.CreateElement(entry);
			_ui.SetTransform(lvlText, 340, 0, 200, 54);
			Text textComp = lvlText.AddComponent<Text>();
			textComp.font = _style.FontA;
			textComp.text = Stats.mLevel.ToString();
			textComp.alignment = TextAnchor.MiddleLeft;

			GameObject staminaBar = _ui.CreateElement(entry);
			_ui.SetTransform(staminaBar, 500, -20, 54, 14);
			_ui.AddImage(staminaBar, _style.ThemeColorC);

			GameObject staminaBarFill = _ui.CreateElement(staminaBar);
			_ui.SetTransform(staminaBarFill, 0, 0, 54, 14);
			_ui.AddImage(staminaBarFill, _style.ThemeColorA);

			result.mStaminaBar = staminaBarFill.GetComponent<RectTransform>();
		}

		return result;
	}

	public GameObject CreateCatIcon(GameObject Root, Vector2 Position, int Index)
	{
		GameObject icon = _ui.CreateElement(Root, "icon");
		_ui.AddImage(icon, Color.white);
		_ui.SetAnchors(icon, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

		GameObject iconImg = _ui.CreateElement(icon, "img");
		_ui.SetRectFillParent(iconImg, 0);
		iconImg.AddComponent<Image>().sprite = _style.Sprites[7 + Index];

		ColorBlock cb = new ColorBlock();
		cb.normalColor = _style.ThemeColorC;
		cb.highlightedColor = _style.ThemeColorA;
		cb.colorMultiplier = 1.0f;

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;
		button.colors = cb;
		button.onClick.AddListener(() => { ChangeItemCategory(Index); });

		icon.AddComponent<CUIRolloverSound>().mSoundIndex = 12;

		string CatText = "";
		string CatDescr = "Category Description";

		if (Index == 0) CatText = "Workspaces";
		else if (Index == 1) CatText = "Storage";
		else if (Index == 2) CatText = "Relaxation";
		else if (Index == 3) CatText = "Sustenance";
		else if (Index == 4) CatText = "Decoration";
		else if (Index == 5) CatText = "Security";

		icon.AddComponent<CUITooltipGenerator>().SetDetails(CatText, CatDescr, "", new Vector2(0, 56), Vector2.zero);

		/*
		GameObject iconTag = _ui.CreateElement(icon, "iconTag");
		_ui.AddImage(iconTag, _style.ThemeColorA);
		_ui.SetAnchors(iconTag, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(iconTag, 0, 0, 54, 6);
		*/

		return icon;
	}

	public GameObject CreateTopBarIcon(GameObject Root, Vector2 Position, int Index)
	{
		GameObject icon = _ui.CreateElement(Root, "icon");
		_ui.AddImage(icon, Color.white);
		_ui.SetAnchors(icon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
		_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

		GameObject iconImg = _ui.CreateElement(icon, "img");
		_ui.SetRectFillParent(iconImg, 0);
		iconImg.AddComponent<Image>().sprite = _style.Sprites[2 + Index];
		iconImg.GetComponent<Image>().color = _style.ThemeColorB;

		ColorBlock cb = new ColorBlock();
		cb.normalColor = _style.ThemeColorE;
		cb.highlightedColor = _style.ThemeColorA;
		cb.colorMultiplier = 1.0f;

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;
		button.colors = cb;
		//button.onClick.AddListener(() => { ChangeItemCategory(Index); });

		icon.AddComponent<CUIRolloverSound>().mSoundIndex = 12;

		string CatText = "";
		string CatDescr = "Category Description";

		if (Index == 0)
		{
			CatText = "Options Menu";

			button.onClick.AddListener(() =>
			{
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
				ToggleOptionsMenu();
			});
		}
		else if (Index == 1) CatText = "Graphs";
		else if (Index == 2) CatText = "Policies";
		else if (Index == 3) CatText = "Map";
		else if (Index == 4)
		{
			CatText = "Employees";

			_newResumeBlip = _ui.CreateElement(icon, "blip");
			_ui.SetAnchors(_newResumeBlip, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
			_ui.SetTransform(_newResumeBlip, 0, 0, 16, 16);
			Image blipImgComp = _newResumeBlip.AddComponent<Image>();
			blipImgComp.sprite = _style.Sprites[17];
			blipImgComp.color = _style.ThemeColorA;

			button.onClick.AddListener(() =>
			{
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
				ToggleEmployeeMenu();
			});
		}

		icon.AddComponent<CUITooltipGenerator>().SetDetails(CatText, CatDescr, "", new Vector2(0, -56), new Vector2(0, 1));

		return icon;
	}

	public CNotifyStackIcon CreateNotifyIcon(GameObject Root, Vector2 Position, int Type, CContractView Contract, CItemAsset ItemAsset)
	{
		CNotifyStackIcon stackIcon = new CNotifyStackIcon();
		stackIcon.mAccepted = false;
		stackIcon.mFadeOut = 1.0f;

		GameObject icon = _ui.CreateElement(Root, "notifyIcon");
		_ui.AddImage(icon, Color.white);
		_ui.SetAnchors(icon, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
		
		ColorBlock cb = new ColorBlock();

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;

		CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[16]);

		if (Type == 0)
		{
			int contractCount = Contract.mStackCount;
			int addHeight = (contractCount - 1) * 7;
			if (addHeight < 0) addHeight = 0;
			_ui.SetTransform(icon, Position.x, Position.y, 54, 54 + addHeight);

			cb.normalColor = _style.ThemeColorC;
			cb.highlightedColor = _style.ThemeColorA;
			cb.colorMultiplier = 1.0f;

			for (int i = 0; i < contractCount; ++i)
			{
				GameObject img1 = _ui.CreateElement(icon, "img");
								
				if (i == contractCount - 1)
					img1.AddComponent<Image>().sprite = _style.Sprites[15];
				else
					img1.AddComponent<Image>().sprite = _style.Sprites[16];

				img1.GetComponent<Image>().raycastTarget = false;
				_ui.SetAnchors(img1, Vector2.zero, Vector2.zero, Vector2.zero);
				_ui.SetTransform(img1, 0, i * 7 + 5, 54, 54);
			}

			string contractDetailText = Contract.mCompanyName +
				"\nTier " + Contract.mTier +
				"\n\nValue: " + Contract.mValue +
				"\nPenalty: " + Contract.mPenalty +
				"\nDeadline: " + Contract.mDeadline;

			icon.AddComponent<CUITooltipGenerator>().SetDetails(Contract.mName, contractDetailText, "", new Vector2(-56, 0), new Vector2(1, 0));

			button.onClick.AddListener(() =>
			{
				_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ACCEPT_CONTRACT, Contract.mID, 0, 0));
				GameObject.Destroy(button);
				icon.GetComponent<Image>().color = _worldView.mPlayerViews[mPlayerIndex].mColor;
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[20]);
				//Contract.mNotifyIcon = null;
				//DestoryNotifyStackIcon(stackIcon);
			});

			GameObject timerBar = _ui.CreateElement(icon, "timer");
			_ui.AddImage(timerBar, _style.ThemeColorA);
			_ui.SetAnchors(timerBar, Vector2.zero, Vector2.zero, Vector2.zero);
			_ui.SetTransform(timerBar, 0, 0, 54, 6);
			stackIcon.mTimerBar = timerBar.transform;
		}
		else if (Type == 1)
		{
			_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

			cb.normalColor = Color.white;
			cb.highlightedColor = _style.ThemeColorA;
			cb.colorMultiplier = 1.0f;

			GameObject iconImg = _ui.CreateElement(icon, "img");
			_ui.SetTransform(iconImg, 0, 0, 54, 54);
			iconImg.AddComponent<Image>().sprite = _style.Sprites[6];
			iconImg.GetComponent<Image>().color = _style.ThemeColorC;

			icon.AddComponent<CUITooltipGenerator>().SetDetails("4 New Employees Available", "", "", new Vector2(-56, 0), new Vector2(1, 0));

			button.onClick.AddListener(() =>
			{
				DestoryNotifyStackIcon(stackIcon);
			});
		}
		else if (Type == 2)
		{
			_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

			cb.normalColor = Color.white;
			cb.highlightedColor = _style.ThemeColorA;
			cb.colorMultiplier = 1.0f;

			GameObject iconImg = _ui.CreateElement(icon, "img");
			_ui.SetTransform(iconImg, 0, 0, 54, 54);
			RawImage ii = iconImg.AddComponent<RawImage>();
			ii.texture = ItemAsset.mIconTexture;
			ii.uvRect = ItemAsset.mIconRect;
			ii.color = _style.ThemeColorC;

			icon.AddComponent<CUITooltipGenerator>().SetDetails("New Item", "", ItemAsset.mFriendlyName + " now available!", new Vector2(-56, 0), new Vector2(1, 0));

			button.onClick.AddListener(() =>
			{
				DestoryNotifyStackIcon(stackIcon);
			});
		}

		icon.AddComponent<CUIRolloverSound>().mSoundIndex = 12;
		button.colors = cb;
		stackIcon.mGob = icon;

		return stackIcon;
	}

	public CNotifyStackIcon CreateNotifyStackIcon(int Type, CContractView Contract = null, CItemAsset ItemAsset = null)
	{
		CNotifyStackIcon icon = CreateNotifyIcon(_uiRoot, new Vector2(0, 0), Type, Contract, ItemAsset);

		icon.mHeight = 54;
		icon.mCurrentY = 1000;

		if (Type == 0)
		{
			int addHeight = (Contract.mStackCount - 1) * 7;
			if (addHeight < 0) addHeight = 0;
			icon.mHeight = 54 + addHeight;
		}

		_ui.SetTransform(icon.mGob, -10, (int)icon.mCurrentY);

		_notifyStack.Add(icon);

		return icon;
	}

	public void DestoryNotifyStackIcon(CNotifyStackIcon Icon)
	{
		GameObject.Destroy(Icon.mGob);
		_notifyStack.Remove(Icon);
	}

	private void _UpdateNotifyStackIcons()
	{
		int stackPos = 10;

		// Work out stack icon position
		for (int i = 0; i < _notifyStack.Count; ++i)
		{
			if (!_notifyStack[i].mAccepted)
			{
				_notifyStack[i].mTargetY = stackPos;
				stackPos += 2 + _notifyStack[i].mHeight;
			}
		}

		bool movingY = false;

		for (int i = 0; i < _notifyStack.Count; ++i)
		{
			var icon = _notifyStack[i];
			float x = icon.mGob.GetComponent<RectTransform>().anchoredPosition.x;

			if (icon.mAccepted)
			{
				if (x > -66)
				{
					x -= Time.deltaTime * 500.0f;

					if (x <= -66)
					{
						x = -66;
					}
				}

				icon.mFadeOut -= Time.deltaTime;

				if (icon.mFadeOut <= 0.0f)
				{
					DestoryNotifyStackIcon(icon);
					--i;
					continue;
				}
				else
				{
					icon.mGob.GetComponent<CanvasGroup>().alpha = icon.mFadeOut;
				}
			}
			else if (!movingY && icon.mCurrentY != icon.mTargetY)
			{	
				movingY = true;

				icon.mCurrentY -= Time.deltaTime * 1000.0f;

				if (icon.mCurrentY <= icon.mTargetY)
				{
					icon.mCurrentY = icon.mTargetY;
					CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
				}
			}

			_ui.SetTransform(icon.mGob, x, (int)icon.mCurrentY);
		}
	}

	public void ToggleOptionsMenu()
	{
		_optionsMenu.SetActive(!_optionsMenu.activeSelf);
		CGame.UIManager.mMenuShowing = _optionsMenu.activeSelf;
	}

	public void ToggleEmployeeMenu()
	{
		_employeeMenu.SetActive(!_employeeMenu.activeSelf);
	}

	public void ChangeItemCategory(int Index)
	{
		CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[13]);

		CUtility.DestroyChildren(_buyItemRoot);
		
		/*
		GameObject itemBarBack = _ui.CreateElement(_buyItemRoot, "itemBuyBar");
		_ui.AddImage(itemBarBack, _style.ThemeColorB);
		_ui.SetAnchors(itemBarBack, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(itemBarBack, 235 + 2 + 54 * 6, 10, 54 * 4, 54);
		*/

		List<CItemAsset> items = CGame.AssetManager.GetAllItemAssets();

		EItemType keepType = EItemType.NONE;

		if (Index == 0) keepType = EItemType.DESK;
		else if (Index == 1) keepType = EItemType.SAFE;
		else if (Index == 2) keepType = EItemType.REST;
		else if (Index == 3) keepType = EItemType.FOOD;
		else if (Index == 4) keepType = EItemType.DECO;
		else if (Index == 5) keepType = EItemType.CAMERA;

		for (int i = 0; i < items.Count; ++i)
		{
			if (Index == 5)
			{
				if (items[i].mItemType != EItemType.CAMERA && items[i].mItemType != EItemType.DOOR)
				{
					items.RemoveAt(i);
					--i;
				}
			}
			else
			{
				if (items[i].mItemType != keepType)
				{
					items.RemoveAt(i);
					--i;
				}
			}
		}

		int posX = 0;

		for (int i = 0; i < items.Count; ++i)
		{
			for (int j = 0; j < _worldView.mPlayerViews[mPlayerIndex].mAvailableItems.Length; ++j)
			{
				if (_worldView.mPlayerViews[mPlayerIndex].mAvailableItems[j] == items[i].mName)
				{
					CreateItemIcon(_buyItemRoot, new Vector2(561 + posX, 10), items[i]);
					posX += 54;
					break;
				}
			}
		}
	}
		
	public GameObject CreateItemIcon(GameObject Root, Vector2 Position, CItemAsset Asset)
	{
		GameObject icon = _ui.CreateElement(Root, "icon");
		_ui.AddImage(icon, _style.ThemeColorB);
		_ui.SetAnchors(icon, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

		GameObject iconImg = _ui.CreateElement(icon, "img");
		_ui.SetRectFillParent(iconImg, 0);
		RawImage ii = iconImg.AddComponent<RawImage>();
		ii.texture = Asset.mIconTexture;
		ii.uvRect = Asset.mIconRect;

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;
		button.onClick.AddListener(() =>
		{
			EnterPlacementMode(Asset.mName);
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
		});

		icon.AddComponent<CUIRolloverSound>().mSoundIndex = 14;

		icon.AddComponent<CUITooltipGenerator>().SetDetails(Asset.mFriendlyName, "Does something", "Cost " + Asset.mCost + "\n" + Asset.mFlavourText, new Vector2(0, 56), Vector2.zero);

		/*
		GameObject iconTag = _ui.CreateElement(icon, "iconTag");
		_ui.AddImage(iconTag, _style.ThemeColorA);
		_ui.SetAnchors(iconTag, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(iconTag, 0, 0, 54, 6);
		*/

		return icon;
	}

	public GameObject CreateInternHireIcon(GameObject Root, Vector2 Position)
	{
		GameObject icon = _ui.CreateElement(Root, "icon");
		_ui.AddImage(icon, _style.ThemeColorB);
		_ui.SetAnchors(icon, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(icon, Position.x, Position.y, 25, 25);

		GameObject iconImg = _ui.CreateElement(icon, "img");
		_ui.SetRectFillParent(iconImg, 2);
		iconImg.AddComponent<Image>().sprite = _style.Sprites[0];

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;
		button.onClick.AddListener(() =>
		{
			_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ENSLAVE_INTERN, 0, 0, 0));
		});

		icon.AddComponent<CUITooltipGenerator>().SetDetails("Hire Intern", "'You do not lead by hitting people over the head — that’s assault, not leadership.' – Dwight Eisenhower", "", new Vector2(0, 27), Vector2.zero);

		/*
		GameObject iconTag = _ui.CreateElement(icon, "iconTag");
		_ui.AddImage(iconTag, _style.ThemeColorA);
		_ui.SetAnchors(iconTag, Vector2.zero, Vector2.zero, Vector2.zero);
		_ui.SetTransform(iconTag, 0, 0, 54, 6);
		*/

		return icon;
	}

	private void _CreateContractCursor(int Count)
	{
		if (_contractCursor != null)
			GameObject.Destroy(_contractCursor);

		_contractCursor = new GameObject("contractCursor");
		_contractCursor.transform.SetParent(mPrimaryScene.transform);
		CModelAsset paperModel = CGame.AssetManager.GetAsset<CModelAsset>("default_contract_paper");

		for (int i = 0; i < Count; ++i)
		{
			GameObject paperGob = paperModel.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
			paperGob.transform.SetParent(_contractCursor.transform);
			paperGob.transform.localPosition = new Vector3(0, i * 0.15f, 0);

			paperGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", _style.ThemeColorA);
		}

		_contractCursorCount = Count;
	}

	/// <summary>
	/// Enter contract placement mode.
	/// </summary>
	public void EnterContractPlacementMode(CContractView ContractView)
	{
		if (_placementEntity != null)
		{
			_placementEntity.Destroy();
			_placementEntity = null;
		}

		_contractPlacement = ContractView;
		_contractCursorCount = 0;
		_CreateContractCursor(ContractView.mUndistributedStacks);
	}
	
	public void ExitContractPlacementMode()
	{
		_contractPlacement = null;

		if (_contractCursor != null)
			GameObject.Destroy(_contractCursor);
	}
	
	/// <summary>
	/// Enter item placement mode.
	/// </summary>
	public void EnterPlacementMode(string AssetName)
	{
		ExitContractPlacementMode();

		if (_placementEntity != null)
			_placementEntity.Destroy();

		_placementEntity = new CEntityPlacer(AssetName, mPrimaryScene.transform, (placer) =>
		{
			return CItem.IsPlaceable(_worldView, placer.mAsset, placer.mX, placer.mY, placer.mRotation);
		});
	}

	public void OnEmployeeAdded(CUnitView Unit)
	{
		Unit.mUIEmployeeEntry = CreateEmpEntry(_empListContent, Unit.mStats, Unit.mIntern, false);
	}

	public void OnEmployeeRemoved(CUnitView Unit)
	{	
		GameObject.Destroy(Unit.mUIEmployeeEntry.mGob);
	}

	/// <summary>
	/// Add a resume to the UI.
	/// </summary>
	public void OnResumeAdded(CResumeView Resume)
	{
		++_newResumes;

		Resume.mUIEmployeeEntry = CreateEmpEntry(_empListContent, Resume.mStats, false, true, () => {
			Resume.mUIEmployeeEntry.mGob.SetActive(false);
			_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ACCEPT_RESUME, Resume.mID, 0, 0));
		});

		/*
		Resume.mUIElement = _ui.CreateTextElement(_uiNotifyList, "Resume: " + Resume.mStats.mName);
		_ui.AddLayout(Resume.mUIElement, -1, 16, 1, -1);

		Button button = Resume.mUIElement.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.navigation = buttonNav;

		button.onClick.AddListener(() =>
		{
			Resume.mUIElement.GetComponent<CUITooltipGenerator>().HideTooltip();
			Resume.mUIElement.SetActive(false);
			_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ACCEPT_RESUME, Resume.mID, 0, 0));
		});

		Resume.mUIElement.AddComponent<CUITooltipGenerator>().SetDetails(Resume.mStats.mName, "Hire Me", "", new Vector2(128, 0), new Vector2(0, 1));
		*/
	}

	/// <summary>
	/// Remove a resume from the UI.
	/// </summary>
	public void OnResumeRemoved(CResumeView Resume)
	{
		--_newResumes;
		GameObject.Destroy(Resume.mUIEmployeeEntry.mGob);
	}

	public void OnContractAdded(CContractView Contract)
	{
		/*
		Contract.mUIElement = _ui.CreateTextElement(_uiNotifyList, "Contract: " + Contract.mName + " " + Contract.mID);
		_ui.AddLayout(Contract.mUIElement, -1, 16, 1, -1);

		Button button = Contract.mUIElement.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.navigation = buttonNav;

		button.onClick.AddListener(() =>
		{
			Contract.mUIElement.SetActive(false);
			_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ACCEPT_CONTRACT, Contract.mID, 0, 0));
		});
		*/

		// TODO: If this contract already belongs to us, then add to tray.

		if (Contract.mOwner == -1)
			Contract.mNotifyIcon = CreateNotifyStackIcon(0, Contract);
		else if (Contract.mOwner == mPlayerIndex)
			AddContractToTray(Contract);
	}

	public void OnContractRemoved(CContractView Contract)
	{
		// TODO: It is possible that we already removed the notify icon, but the contract also wants to terminated itself.
		if (Contract.mNotifyIcon != null)
			DestoryNotifyStackIcon(Contract.mNotifyIcon);

		if (Contract.mContractInTray != null)
			DestroyContractInTray(Contract.mContractInTray);
	}

	public void OnContractChangedOwner(CContractView Contract)
	{
		// Colour contract with owner color?

		GameObject.Destroy(Contract.mNotifyIcon.mGob.GetComponent<Button>());
		Contract.mNotifyIcon.mGob.GetComponent<Image>().color = _worldView.mPlayerViews[Contract.mOwner].mColor;
		Contract.mNotifyIcon.mGob.AddComponent<CanvasGroup>();
		Contract.mNotifyIcon.mAccepted = true;
		Contract.mNotifyIcon = null;

		if (Contract.mOwner == mPlayerIndex)
		{
			AddContractToTray(Contract);
		}

		/*
		if (Contract.mOwner == _playerIndex)
		{
			if (Contract.mUIElement == null)
				OnContractAdded(Contract);

			Contract.mUIElement.transform.SetParent(_uiOwnedList.transform);
			Contract.mUIElement.SetActive(true);

			Contract.mUIElement.GetComponent<Button>().onClick.RemoveAllListeners();
			Contract.mUIElement.GetComponent<Button>().onClick.AddListener(() =>
			{	
				EnterContractPlacementMode(Contract);
			});
		}
		else
		{
			Debug.LogError("Can't be viewing a contract that has another ID!!??");
		}
		*/
	}

	public void DestroyContractInTray(CContractInTray ContractInTray)
	{
		GameObject.Destroy(ContractInTray.mGob);
		_contractTray.Remove(ContractInTray);
	}

	/// <summary>
	/// Generate a contract in tray and add it to be managed.
	/// </summary>
	public void AddContractToTray(CContractView Contract)
	{
		Vector2 Position = new Vector2(10, 10);

		GameObject icon = _ui.CreateElement(_uiRoot, "contract");
		_ui.AddImage(icon, Color.white);
		_ui.SetAnchors(icon, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));

		ColorBlock cb = new ColorBlock();

		Button button = icon.AddComponent<Button>();
		Navigation buttonNav = new Navigation();
		buttonNav.mode = Navigation.Mode.None;
		button.targetGraphic = icon.GetComponent<Image>();
		button.navigation = buttonNav;

		_ui.SetTransform(icon, Position.x, Position.y, 54, 54);

		cb.normalColor = _style.TooltipBackground;
		cb.highlightedColor = _style.ThemeColorC;
		cb.colorMultiplier = 1.0f;

		GameObject paperRoot = _ui.CreateElement(icon, "paperRoot");
		_ui.SetRectFillParent(paperRoot);

		string contractDetailText = Contract.mCompanyName +
			"\nTier " + Contract.mTier +
			"\n\nValue: " + Contract.mValue +
			"\nPenalty: " + Contract.mPenalty +
			"\nDeadline: " + Contract.mDeadline;

		icon.AddComponent<CUITooltipGenerator>().SetDetails(Contract.mName, contractDetailText, "", new Vector2(-56, 0), new Vector2(1, 0));

		button.onClick.AddListener(() =>
		{
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
			EnterContractPlacementMode(Contract);
		});

		GameObject timerBarBack = _ui.CreateElement(icon, "timerBack");
		_ui.AddImage(timerBarBack, _style.ThemeColorC);
		_ui.SetAnchors(timerBarBack, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
		_ui.SetTransform(timerBarBack, 0, 0, 54, 6);

		GameObject timerBar = _ui.CreateElement(icon, "timer");
		_ui.AddImage(timerBar, _style.ThemeColorE);
		_ui.SetAnchors(timerBar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
		_ui.SetTransform(timerBar, 0, 0, 54, 6);

		icon.AddComponent<CUIRolloverSound>().mSoundIndex = 12;
		button.colors = cb;

		CContractInTray contractInTray = new CContractInTray();
		contractInTray.mUndistributedStacks = -1;
		contractInTray.mUncompletedStacks = -1;
		contractInTray.mGob = icon;
		contractInTray.mPaperRoot = paperRoot;
		contractInTray.mTimerBar = timerBar.transform;
		contractInTray.mContract = Contract;
		_contractTray.Add(contractInTray);

		Contract.mContractInTray = contractInTray;
	}

	public void UpdateContractInTray(CContractInTray Contract)
	{
		if (Contract.mUndistributedStacks != Contract.mContract.mUndistributedStacks || Contract.mUncompletedStacks != Contract.mContract.mUncompletedStacks)
		{
			Contract.mUndistributedStacks = Contract.mContract.mUndistributedStacks;
			Contract.mUncompletedStacks = Contract.mContract.mUncompletedStacks;

			int contractCount = Contract.mUncompletedStacks;
			int addHeight = (contractCount - 1) * 7;
			if (addHeight < 0) addHeight = 0;
			Contract.mGob.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54 + addHeight);
			
			CUtility.DestroyChildren(Contract.mPaperRoot);

			for (int i = 0; i < Contract.mUncompletedStacks; ++i)
			{
				GameObject img1 = _ui.CreateElement(Contract.mPaperRoot, "img");
				Color spriteCol = _style.ThemeColorA;

				if (i >= Contract.mUncompletedStacks - Contract.mUndistributedStacks)
					spriteCol = _style.ThemeColorE;

				if (i < Contract.mUncompletedStacks - 1)
					img1.AddComponent<Image>().sprite = _style.Sprites[16];
				else
					img1.AddComponent<Image>().sprite = _style.Sprites[15];

				img1.GetComponent<Image>().color = spriteCol;

				img1.GetComponent<Image>().raycastTarget = false;
				_ui.SetAnchors(img1, Vector2.zero, Vector2.zero, Vector2.zero);
				_ui.SetTransform(img1, 0, i * 7 + 5, 54, 54);
			}
		}
	}

	/// <summary>
	/// Rebuild UI from scratch by looking at World View State.
	/// Used when swapping player index in the world view, or when loading a game.
	/// </summary>
	public void RebuildPrimaryUI()
	{
		// TODO: This doesn't account for contracts that we already own?

		// Clear UI to defaults
		CUtility.DestroyChildren(_uiNotifyList);

		// Rebuild UI from state views
		for (int i = 0; i < _worldView.mStateViews.Count; ++i)
		{
			if (_worldView.mStateViews[i].GetType() == typeof(CResumeView))
				OnResumeAdded(_worldView.mStateViews[i] as CResumeView);
			else if (_worldView.mStateViews[i].GetType() == typeof(CContractView))
				OnContractAdded(_worldView.mStateViews[i] as CContractView);
		}
	}

	/// <summary>
	/// Set the current hovered.
	/// </summary>
	public void SetHovered(ISelectable Selectable)
	{
		if (_hovered != null && _hovered.IsStillActive())
		{
			_hovered.HoverOut();
		}

		_hovered = Selectable;

		if (_hovered != null)
		{
			_hovered.Hover();
		}
	}

	/// <summary>
	/// Set the current selectable.
	/// </summary>
	public void SetSelected(ISelectable Selectable)
	{
		if (_selected != null && _selected.IsStillActive())
			_selected.Deselect();

		_selected = Selectable;

		if (_selected != null)
		{
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[5]);
			_selected.Select();
		}
	}

	public bool IsSelectedMyUnit()
	{
		return (_selected != null && _selected.GetType() == ESelectionType.UNIT && ((CUnitView)_selected).mOwner == mPlayerIndex);
	}

	public bool IsSelectedMyWorker()
	{
		return (_selected != null && _selected.GetType() == ESelectionType.UNIT && !((CUnitView)_selected).mIntern && ((CUnitView)_selected).mOwner == mPlayerIndex);
	}

	/// <summary>
	/// Called every render frame.
	/// </summary>
	public void Update(CInputState InputState)
	{
		if (mUserInteraction)
		{
			Ray r = CGame.PrimaryResources.PrimaryCamera.ScreenPointToRay(InputState.mMousePosition);

			// This only happens in modes that require selecting the floor
			// Intersect mouse with floor		
			Plane floorPlane = new Plane(Vector3.up, 0.0f);
			Vector3 mouseFloorPos = Vector3.zero;
			float t = 0.0f;
			bool mouseHitFloor = floorPlane.Raycast(r, out t);
			if (mouseHitFloor)
			{
				mouseFloorPos = r.direction * t + r.origin;
			}

			//CGame.PrimaryResources.Particles.transform.position = mouseFloorPos;

			SetHovered(null);
			// Sanity check for currenlty selected.
			if (_selected != null && !_selected.IsStillActive())
				_selected = null;

			if (InputState.GetCommand("focusOnSpawn").mDown)
			{
				Vector3 spawnPos = _worldView.mPlayerViews[mPlayerIndex].mSpawnPos.ToWorldVec3();
				CGame.CameraManager.SetTargetPosition(spawnPos);
			}

			if (CGame.UIManager.mContextMenu.mShowing)
			{
				// Context Menu Mode

				if (InputState.IsAnyKeyDown())
					CGame.UIManager.mContextMenu.Hide();
			}
			else if (_placementEntity != null)
			{
				// Placement Mode

				SetSelected(null);

				if (InputState.GetCommand("escape").mDown)
				{
					CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[19]);
					_placementEntity.Destroy();
					_placementEntity = null;
				}
				else
				{
					if (InputState.mOverUI)
					{
						_placementEntity.SetVisible(false);
					}
					else
					{
						_placementEntity.SetVisible(true);

						if (InputState.mMouseRightUp)
						{
							CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[19]);
							_placementEntity.Destroy();
							_placementEntity = null;
						}
						else if (InputState.mMouseLeftDown)
						{
							// TODO: Make sure we can afford this thing.
							if (_placementEntity.IsPlaceable())
							{
								CGame.CameraManager.Shake();
								CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[11]);
								_gameSession.PushUserAction(new CUserAction(CUserAction.EType.PLACE_OBJECT, 0, _placementEntity.mX | (_placementEntity.mRotation << 16), _placementEntity.mY, _placementEntity.mAsset.mName));
							}

							if (!InputState.GetCommand("itemPlaceRepeat").mDown)
							{
								_placementEntity.Destroy();
								_placementEntity = null;
							}
						}
						else if (InputState.GetCommand("itemPlaceRotate").mPressed)
						{
							CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[18]);
							_placementEntity.Rotate();
						}

						if (_placementEntity != null)
						{
							_placementEntity.SetPosition(mouseFloorPos);
							_placementEntity.Update();
						}
					}
				}
			}
			else if (_contractPlacement != null)
			{
				// Contract Place Mode

				SetSelected(null);

				if (_contractPlacement.mState == CStateView.EState.DESTROYED || InputState.GetCommand("escape").mDown || _contractPlacement.mUndistributedStacks == 0)
				{
					ExitContractPlacementMode();
				}
				else
				{
					if (_contractCursorCount != _contractPlacement.mUndistributedStacks)
					{
						_CreateContractCursor(_contractPlacement.mUndistributedStacks);
					}

					if (InputState.mOverUI)
					{
						_contractCursor.SetActive(false);
					}
					else
					{
						_contractCursor.SetActive(true);

						Plane p = new Plane(Vector3.up, -2.0f);
						float pT = 0.0f;
						if (p.Raycast(r, out pT))
						{
							Vector3 pHitPos = r.direction * pT + r.origin;
							_contractCursor.transform.position = pHitPos + new Vector3(0, 0.5f, 0);
						}

						SetHovered(_worldView.GetSelectableDesk(r));

						if (_hovered != null)
						{
							CItemView itemView = _hovered as CItemView;

							if (itemView != null && itemView.mAsset.mItemType == EItemType.DESK)
							{
								if (InputState.mMouseLeftDown)
								{
									_gameSession.PushUserAction(new CUserAction(CUserAction.EType.DISTRIBUTE_CONTRACT, _contractPlacement.mID, itemView.mProxyID, 0));
								}
							}
						}

						if (InputState.mMouseRightDown)
						{
							ExitContractPlacementMode();
						}
					}
				}
			}
			else
			{
				// Normal Mode

				if (InputState.mOverUI)
				{

				}
				else
				{
					// TODO: Could change based on the action we want to perform?
					SetHovered(_worldView.GetSelectable(r));

					// Context menu interaction depending on what we have selected and what we are right clicking on.
					if (_hovered != null)
					{
						if (InputState.mMouseRightDown)
						{
							// Get interactions based on the hovered/selected pair.
							bool showContextMenu = false;
							Vector2 tooltipPos = CGame.UIManager.ConvertScreenSpaceToUISpace(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
							CGame.UIManager.mContextMenu.Clear();

							if (_hovered.GetType() == ESelectionType.ITEM)
							{
								CItemView item = _hovered as CItemView;

								if (item.mBlueprint)
								{
									CGame.UIManager.mContextMenu.AddCategory("Blueprint");
									CGame.UIManager.mContextMenu.AddItem("Cancel", () =>
									{
										_gameSession.PushUserAction(new CUserAction(CUserAction.EType.CANCEL_BLUEPRINT, item.mItemID, 0, 0));
									});

									showContextMenu = true;
								}
								else
								{
									if (IsSelectedMyUnit())
									{
										CGame.UIManager.mContextMenu.AddCategory("Staff");
										CGame.UIManager.mContextMenu.AddItem("Attack", () =>
										{
											_gameSession.PushUserAction(new CUserAction(CUserAction.EType.FORCE_ATTACK, item.mProxyID, 0, 0));
										});

										showContextMenu = true;
									}

									if (item.mAsset.mItemType == EItemType.DESK)
									{
										if (IsSelectedMyWorker())
										{
											CGame.UIManager.mContextMenu.AddCategory("Worker");
											CGame.UIManager.mContextMenu.AddItem("Assign Desk", () =>
											{
												_gameSession.PushUserAction(new CUserAction(CUserAction.EType.ASSIGN_WORKSPACE, item.mProxyID, 0, 0));
											});

											showContextMenu = true;
										}
									}

									if (item.mAsset.mItemType == EItemType.DOOR && item.mOwnerID == mPlayerIndex)
									{
										CGame.UIManager.mContextMenu.AddCategory("Door");

										if (item.mLocked)
										{
											CGame.UIManager.mContextMenu.AddItem("Unlock", () =>
											{
												_gameSession.PushUserAction(new CUserAction(CUserAction.EType.LOCK_ITEM, item.mProxyID, 0, 0));
											});
										}
										else
										{
											CGame.UIManager.mContextMenu.AddItem("Lock", () =>
											{
												_gameSession.PushUserAction(new CUserAction(CUserAction.EType.LOCK_ITEM, item.mProxyID, 1, 0));
											});
										}

										showContextMenu = true;
									}
								}
							}
							else if (_hovered.GetType() == ESelectionType.UNIT)
							{
								CUnitView unit = _hovered as CUnitView;

								if (unit.mOwner == mPlayerIndex)
								{
									CGame.UIManager.mContextMenu.AddCategory("Staff");
									CGame.UIManager.mContextMenu.AddItem("Fire", () =>
									{
										_gameSession.PushUserAction(new CUserAction(CUserAction.EType.FIRE_EMPLOYEE, unit.mID, 0, 0));
									});

									CGame.UIManager.mContextMenu.AddItem("Bonus", () =>
									{
										_gameSession.PushUserAction(new CUserAction(CUserAction.EType.BONUS, unit.mID, 0, 0));
									});

									CGame.UIManager.mContextMenu.AddItem("Raise", () =>
									{
										_gameSession.PushUserAction(new CUserAction(CUserAction.EType.RAISE, unit.mID, 0, 0));
									});

									CGame.UIManager.mContextMenu.AddItem("Promote", () =>
									{
										_gameSession.PushUserAction(new CUserAction(CUserAction.EType.PROMOTE, unit.mID, 0, 0));
									});
									
									showContextMenu = true;
								}
							}
							else if (_hovered.GetType() == ESelectionType.PICKUP)
							{

							}

							if (showContextMenu)
								CGame.UIManager.mContextMenu.Show(tooltipPos + new Vector2(20.0f, 20.0f), Vector2.zero);
						}
					}
					else
					{
						if (InputState.mMouseRightDown)
						{
							if (IsSelectedMyUnit())
							{
								CGame.UIManager.PlayMoveRing(mouseFloorPos);
								CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[5]);
								// TODO: Can move to blocked location, pathfinder should solve this problem.
								_gameSession.PushUserAction(new CUserAction(CUserAction.EType.MOVE_UNIT, 0, (int)(mouseFloorPos.x * 1000.0f), (int)(mouseFloorPos.z * 1000.0f)));
							}
						}
					}

					// Check if we are selecting something.
					// TODO: Region drag select.
					if (InputState.mMouseLeftDown)
					{
						SetSelected(_hovered);

						if (_selected != null && _selected.GetType() == ESelectionType.UNIT)
						{
							_gameSession.PushUserAction(new CUserAction(CUserAction.EType.SELECT_ENTITY, _selected.GetID(), 0, 0));
						}
						else
						{
							_gameSession.PushUserAction(new CUserAction(CUserAction.EType.SELECT_ENTITY, 0, 0, 0));
						}
					}
				}
			}

			// Sanity check the hovered/selected.
			if (_selected != null && !_selected.IsStillActive())
				SetSelected(null);

			if (_hovered != null && !_hovered.IsStillActive())
				SetHovered(null);

			if (_hovered != null)
			{
				Vector3 elementWorldPos = Input.mousePosition;
				Vector2 tooltipPos = CGame.UIManager.ConvertScreenSpaceToUISpace(new Vector2(elementWorldPos.x, elementWorldPos.y));
				tooltipPos.x += 20.0f;
				tooltipPos.y -= 20.0f;

				if (_contractPlacement != null)
				{
					if (_hovered.GetType() == ESelectionType.ITEM && ((CItemView)_hovered).mAsset.mItemType == EItemType.DESK)
					{
						CItemView v = _hovered as CItemView;

						CGame.UIManager.mTooltip.Set(_contractPlacement.mName + " " + _contractPlacement.mID, "Distribute Contract", "Assign a slot of the contract to this desk", tooltipPos, new Vector2(0, 0), true);
					}
				}
				else
				{
					if (_hovered.GetType() == ESelectionType.UNIT)
					{
						CUnitView v = _hovered as CUnitView;

						string infoText = "";
						infoText += "Tier " + v.mStats.mTier + " (" + v.mStats.mLevel + ")\n";
						infoText += "Stam " + v.mStamina + "/" + v.mStats.mMaxStamina + "\n";
						infoText += "Strs " + v.mStress + "/" + v.mStats.mMaxStress + "\n";
						infoText += "Idl " + v.mWorkIdleTimer + "/" + v.mWorkIdle + "\n";
						infoText += "Hngr " + v.mHunger + "/" + v.mStats.mMaxHunger + " (" + v.mStats.mHungerRate + ")\n";
						infoText += "Exp " + v.mStats.mExperience + "/" + v.mStats.mRequiredXP;
						infoText += "Prm " + v.mPromotionCounter;
						infoText += "\nPapr " + v.mCarriedPapers;
						infoText += "\nAnim " + v.mActionAnim + "\nActn " + v.mAction + "\nPhas " + v.mActionPhase;

						CTierInfo tier = CGame.AssetManager.mUnitRules.GetTier(v.mStats.mTier);

						CGame.UIManager.mTooltip.Set(_hovered.GetInfo(), tier.mTitle, infoText, tooltipPos, new Vector2(0, 0), true);
					}
					else if (_hovered.GetType() == ESelectionType.ITEM)
					{
						CItemView v = _hovered as CItemView;

						string infoText = "This item is waiting to be built";

						if (!v.mBlueprint)
						{
							infoText = "Dura " + v.mDurability + "/" + v.mMaxDurability;

							if (v.mAsset.mItemType == EItemType.SAFE)
							{
								infoText += "\nValue " + v.mValue;
							}
							else if (v.mAsset.mItemType == EItemType.DESK)
							{
								//infoText += "\nPapers " + v.mCompletedPapers + "/" + v.mMaxCompletedPapers;
							}
						}

						CGame.UIManager.mTooltip.Set(_hovered.GetInfo(), "Game World Entity", infoText, tooltipPos, new Vector2(0, 0), true);
					}
					else if (_hovered.GetType() == ESelectionType.PICKUP)
					{
						CGame.UIManager.mTooltip.Set(_hovered.GetInfo(), "Game World Entity", "", tooltipPos, new Vector2(0, 0), true);
					}
				}
			}
			else
			{
				CGame.UIManager.mTooltip.Hide(true);
			}

			// Submit selection highlights for rendering.
			List<Renderer> renderers = new List<Renderer>();
			List<Renderer> transRenderers = new List<Renderer>();

			if (_hovered != null)
				_hovered.GetRenderers(renderers);

			if (_selected != null)
				_selected.GetRenderers(renderers);

			for (int i = 0; i < _worldView.mStateViews.Count; ++i)
			{
				CItemView view = _worldView.mStateViews[i] as CItemView;

				if (view != null)
				{
					if (view.mBlueprint)
					{
						((ISelectable)view).GetRenderers(transRenderers);
					}
				}
			}

			CGame.CameraManager.mMainCamera.GetComponent<CCamera>().SetRenderEffects(renderers, transRenderers);

			if (_hovered != null && _hovered.GetType() == ESelectionType.UNIT)
			{
				CUnitView unitView = _hovered as CUnitView;

				if (unitView.mAssignedDeskID != 0)
				{
					CItemView deskView = _worldView.GetItemView(unitView.mAssignedDeskID);

					if (deskView != null)
					{
						Vector3 endPos = deskView.mBounds.center;
						endPos.y = 0.0f;
						Vector3 offset = new Vector3(0, 0.1f, 0);
						CDebug.DrawThickLine(_hovered.GetVisualPos() + offset, endPos + offset, 0.15f, new Color(0.3f, 0.3f, 0.3f, 1.0f), true);
					}
				}
			}
		}
		
		// Update UI stuff from the world state.
		_moneyText.text = _worldView.mPlayerViews[mPlayerIndex].mMoney.ToString();
		_ui.SetTransform(_paydayTimer, 0, 0, 222.0f * _worldView.mPaydayTimerNormalised, 6);

		if (_newResumes != 0)
		{
			Color blipCol = _style.ThemeColorA;
			blipCol.a = Mathf.Sin(Time.time * 10.0f) * 0.5f + 0.5f;
			_newResumeBlip.GetComponent<Image>().color = blipCol;
			_newResumeBlip.SetActive(true);
		}
		else
		{
			_newResumeBlip.SetActive(false);
		}
		
		// TODO: Pull timer data from contract views for notify timer bar? (Rather than push from contract views)
		_UpdateNotifyStackIcons();

		// Update contracts in tray
		int trayX = -10 - 56 - 56;
		for (int i = 0; i < _contractTray.Count; ++i)
		{
			// TODO: Move all of this to update function.
			CContractInTray c = _contractTray[i];

			_ui.SetTransform(c.mGob, trayX, 10);
			trayX -= 56;

			float normalizedTimerBarScale = ((c.mContract.mAcceptedTime + c.mContract.mDeadline) - _worldView.mGameTick) / (float)c.mContract.mDeadline;
			c.mTimerBar.localScale = new Vector3(normalizedTimerBarScale, 1.0f, 1.0f);

			UpdateContractInTray(c);
		}


		if (InputState.GetCommand("openOptions").mPressed)
		{
			ToggleOptionsMenu();
		}

		/*
		CGame.UIManager.mPlayerStatWorkerCap = _world.mPopCap;
		CGame.UIManager.mPlayerStatInternCap = _world.mInternCap;
		CGame.UIManager.mPlayerStatInterns = _world.GetPlayerInterCount(_currentPlayer);
		CGame.UIManager.mPlayerStatWorkers = _world.GetPlayerWorkerCount(_currentPlayer);
		*/
	}
}
