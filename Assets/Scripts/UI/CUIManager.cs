using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Info message.
/// </summary>
public class CFadingMessage
{
	public float mTime;
	public GameObject mGameObject;
	public Color mColor;
}

/// <summary>
/// Manages several UI features.
/// </summary>
public class CUIManager
{
	private CToolkitUI _ui;
	private CGameUIStyle _style;
	private CUIResources _uiRes;

	private GameObject _moveRingGob;
	private float _moveRingTime;

	private List<CFadingMessage> _fadingMessages = new List<CFadingMessage>();
	private List<CInterfaceComponent> _interfaces = new List<CInterfaceComponent>();

	public GameObject gameUI;
	public GameObject underlayLayer;
	public GameObject primaryLayer;
	public GameObject overlayLayer;
	public GameObject cutsceneLayer;
	public GameObject fadeOverLayer;
	public GameObject contextMenuLayer;
	public GameObject mMenuLayer;

	public CContextMenu mContextMenu;
	public CTooltip mTooltip;

	public GameObject mErrorMessageBox;

	public bool mMenuShowing;

	public bool mShowUI;
	public CanvasGroup mPrimaryLayerGroup;
	public CanvasGroup mCutsceneLayerGroup;
	private float _uiFadeTarget;
	private float _uiFadeLerp;

	private AudioClip _queuedMusicTrack;

	public CUIManager(CToolkitUI Toolkit, CGameUIStyle Style)
	{
		_ui = Toolkit;
		_style = Style;
		_uiRes = CGame.UIResources;

		gameUI = _ui.CreateElement(_ui.Canvas, "gameUI");
		_ui.SetRectFillParent(gameUI);
		gameUI.transform.SetAsFirstSibling();

		underlayLayer = _ui.CreateElement(gameUI, "underlayLayer");
		_ui.SetRectFillParent(underlayLayer);

		CanvasGroup cgroup = underlayLayer.AddComponent<CanvasGroup>();
		cgroup.blocksRaycasts = false;

		primaryLayer = _ui.CreateElement(gameUI, "primaryLayer");
		mPrimaryLayerGroup = primaryLayer.AddComponent<CanvasGroup>();
		mPrimaryLayerGroup.alpha = 1.0f;
		_ui.SetRectFillParent(primaryLayer);

		overlayLayer = _ui.CreateElement(gameUI, "overlayLayer");
		_ui.SetRectFillParent(overlayLayer);
		
		cgroup = overlayLayer.AddComponent<CanvasGroup>();
		cgroup.blocksRaycasts = false;

		cutsceneLayer = _ui.CreateElement(gameUI, "cutsceneLayer");
		_ui.SetRectFillParent(cutsceneLayer);
		GameObject letterBox = _ui.CreateElement(cutsceneLayer);
		_ui.SetAnchors(letterBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
		_ui.AddImage(letterBox, _style.ThemeColorC);
		_ui.SetTransform(letterBox, 0, 0, 0, 54);

		letterBox = _ui.CreateElement(cutsceneLayer);
		_ui.SetAnchors(letterBox, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0));
		_ui.AddImage(letterBox, _style.ThemeColorC);
		_ui.SetTransform(letterBox, 0, 0, 0, 54);

		mCutsceneLayerGroup = cutsceneLayer.AddComponent<CanvasGroup>();
		mCutsceneLayerGroup.alpha = 0.0f;
		mCutsceneLayerGroup.blocksRaycasts = false;

		/*
		fadeOverLayer = _ui.CreateElement(gameUI, "fadeOverLayer");
		_ui.SetRectFillParent(fadeOverLayer);
		_ui.AddImage(fadeOverLayer, Color.black);
		*/

		contextMenuLayer = _ui.CreateElement(gameUI, "contextMenuLayer");
		_ui.SetRectFillParent(contextMenuLayer);

		mMenuLayer = _ui.CreateElement(gameUI, "menuLayer");
		_ui.SetRectFillParent(mMenuLayer);

		GameObject productText = _ui.CreateTextElement(overlayLayer, CGame.PRODUCT_NAME + " (" + CGame.VERSION_MAJOR + "." + CGame.VERSION_MINOR + ") " + CGame.VERSION_NAME, "productText", CToolkitUI.ETextStyle.TS_HEADING);
		_ui.SetAnchors(productText, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
		_ui.SetTransform(productText, 5, 0, -10, 20);
		productText.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

		mContextMenu = new CContextMenu(Toolkit, Style, contextMenuLayer);
		mTooltip = new CTooltip(Toolkit, Style, overlayLayer);

		SetupForGameSession();
	}

	public void SetupForGameSession()
	{
		mMenuShowing = false;
		mShowUI = true;
		mPrimaryLayerGroup.alpha = 1.0f;
		mCutsceneLayerGroup.alpha = 0.0f;
		_uiFadeTarget = 1.0f;
		_uiFadeLerp = 1.0f;
	}

	public void AddInterface(CInterfaceComponent Interface)
	{
		Interface.Init(this);
		_interfaces.Add(Interface);
	}

	public void RemoveInterface(CInterfaceComponent Interface)
	{
		Interface.Destory();
		_interfaces.Remove(Interface);
	}

	public void ToggleUIActive()
	{
		gameUI.SetActive(!gameUI.activeSelf);
	}
	
	/// <summary>
	/// Play a sound.
	/// </summary>
	public void PlaySound(AudioClip Clip)
	{
		_uiRes.UIAudioSource.PlayOneShot(Clip);
	}
	
	/// <summary>
	/// Queue a music track to play after fading out current track.
	/// Set the current track ID.
	/// </summary>
	public void PlayMusic(int TrackID)
	{
		AudioClip[] tracks = CGame.PrimaryResources.MusicClips;
		TrackID = Mathf.Clamp(TrackID, 0, tracks.Length);
		_queuedMusicTrack = tracks[TrackID];
	}
	
	public void PlayMoveRing(Vector3 Position)
	{
		if (_moveRingGob == null)
		{
			_moveRingGob = GameObject.Instantiate(CGame.WorldResources.MoveRingPrefab) as GameObject;
		}

		_moveRingGob.transform.position = Position + new Vector3(0.0f, 0.02f, 0.0f);
		_moveRingTime = 1.0f;
		_moveRingGob.GetComponent<MoveCursor>().interp = _moveRingTime;
		_moveRingGob.GetComponent<MoveCursor>().Show();
		_moveRingGob.SetActive(true);
	}

	private void _UpdateMoveRing()
	{
		if (_moveRingTime > 0.0f)
		{
			_moveRingTime -= Time.deltaTime;
			_moveRingGob.GetComponent<MoveCursor>().interp = _moveRingTime;

			if (_moveRingTime <= 0.0f)
			{
				_moveRingGob.SetActive(false);
			}
			else
			{
				//_moveRingGob.transform.rotation = Quaternion.AngleAxis(_moveRingTime * 180.0f, Vector3.up);
				//float scale = CGame.WorldResources.MoveRingResponseCurve.Evaluate(1.0f - _moveRingTime);
				//_moveRingGob.transform.localScale = new Vector3(scale, scale, scale);
				//_moveRingGob.GetComponent<MeshRenderer>().material.SetColor("_MainColor", new Color(1.0f, 1.0f, 1.0f, _moveRingTime));
			}
		}
	}
	
	public Vector2 ConvertScreenSpaceToUISpace(Vector2 Position)
	{
		Position.x = Position.x * (_uiRes.CanvasTransform.rect.width / (float)Screen.width);
		Position.y = Position.y * (_uiRes.CanvasTransform.rect.height / (float)Screen.height);

		return Position;
	}

	public float GetScaledScreenWidth()
	{
		return _uiRes.PrimaryCanvasScaler.referenceResolution.x;
	}

	/// <summary>
	/// Called every render frame.
	/// </summary>
	public void Update()
	{
		for (int i = 0; i < _interfaces.Count; ++i)
			_interfaces[i].Update();

		_UpdateMessages();
		_UpdateMoveRing();
		//int internCost = internCount * 100;
		//_ui.UIMain.SetStatus(mWorld.mPlayers[mWorld.mCurrentPlayer].mMoney, workerCount, mWorld.mLevelDefinition.mPopCap, mWorld.mGameTick, availContracts, availResumes, internCost);

		float fadeTarget = _uiFadeTarget;

		if (mMenuShowing)
			fadeTarget = 0.0f;
		else if (mShowUI)
			fadeTarget = 1.0f;
		else
			fadeTarget = 0.0f;

		if (fadeTarget > _uiFadeLerp)
		{
			_uiFadeLerp += Time.deltaTime;
			if (_uiFadeLerp > fadeTarget) _uiFadeLerp = fadeTarget;
		}
		else if (fadeTarget < _uiFadeLerp)
		{
			_uiFadeLerp -= Time.deltaTime;
			if (_uiFadeLerp < fadeTarget) _uiFadeLerp = fadeTarget;
		}

		float alpha = Mathf.Clamp01(_uiFadeLerp * 2.0f - 1.0f);
		mPrimaryLayerGroup.alpha = alpha;
		mCutsceneLayerGroup.alpha = 1.0f - alpha;

		if (fadeTarget < 1.0f)
			mPrimaryLayerGroup.blocksRaycasts = false;
		else
			mPrimaryLayerGroup.blocksRaycasts = true;

		if (_queuedMusicTrack != null && _uiRes.UIMusicSource.clip != _queuedMusicTrack)
		{
			_uiRes.UIMusicSource.volume -= Time.deltaTime * 0.75f;

			if (_uiRes.UIMusicSource.volume <= 0.0f || _uiRes.UIMusicSource.clip == null)
			{
				_uiRes.UIMusicSource.volume = 1.0f;
				_uiRes.UIMusicSource.clip = _queuedMusicTrack;
				_uiRes.UIMusicSource.Play();
			}
		}
	}

	public void DisplayMessage(string Text, Color Colour)
	{
		// TODO: Let message instantiate itself properly.

		CFadingMessage msg = new CFadingMessage();
		_fadingMessages.Add(msg);
	}

	private void _UpdateMessages()
	{
		float posY = -50.0f;

		for (int i = 0; i < _fadingMessages.Count; ++i)
		{
			float t = _fadingMessages[i].mTime;			
			_fadingMessages[i].mTime -= Time.deltaTime;

			if (t <= 0.0f)
			{
				GameObject.Destroy(_fadingMessages[i].mGameObject);
				_fadingMessages.RemoveAt(i);
				--i;
			}
			else
			{				
				Color colorDest = _fadingMessages[i].mColor;
				colorDest.a = 0.0f;
				float lerp = 1.0f;

				if (t <= 0.5f)
					lerp = t * 2.0f;

				//_fadingMessages[i].mGameObject.transform.localPosition = Vector3.Lerp(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, -150.0f, 0.0f), t);
				_fadingMessages[i].mGameObject.GetComponent<Text>().color = Color.Lerp(colorDest, _fadingMessages[i].mColor, lerp);
				_fadingMessages[i].mGameObject.transform.localPosition = new Vector3(0.0f, posY, 0.0f);
				posY -= 30.0f;
			}
		}
	}

	public void HideErrorMesssage()
	{
		if (mErrorMessageBox != null)
			GameObject.Destroy(mErrorMessageBox);
	}

	public void ShowErrorMessage(string Message, string Title)
	{
		if (mErrorMessageBox != null)
			GameObject.Destroy(mErrorMessageBox);

		mErrorMessageBox = _ui.CreateElement(mMenuLayer, "ErrorMessage");
		//errorMsg.SetActive(false);
		_ui.AddImage(mErrorMessageBox, _style.TooltipBackground);
		_ui.SetAnchors(mErrorMessageBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		_ui.SetTransform(mErrorMessageBox, 0, 0, 600, 256);
		_ui.AddVerticalLayout(mErrorMessageBox).childForceExpandWidth = true;
		mErrorMessageBox.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		GameObject titleBar = _ui.CreateElement(mErrorMessageBox, "TitleBar");
		_ui.AddImage(titleBar, _style.ThemeColorC);
		_ui.SetAnchors(titleBar, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
		_ui.AddLayout(titleBar, -1, 30, -1, -1);
		//_ui.SetTransform(optsTitle, 0, 0, 0, 26);

		GameObject titleText = _ui.CreateElement(titleBar);
		_ui.SetAnchors(titleText, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
		_ui.SetTransform(titleText, 5, 0, -10, 0);
		Text optsTitleTextComp = titleText.AddComponent<Text>();
		optsTitleTextComp.text = Title;
		optsTitleTextComp.font = _style.FontA;
		optsTitleTextComp.alignment = TextAnchor.MiddleLeft;


		GameObject messageBody = _ui.CreateElement(mErrorMessageBox, "MsgBorder");
		_ui.AddVerticalLayout(messageBody, new RectOffset(10, 10, 10, 10));

		GameObject message = _ui.CreateElement(messageBody);
		//_ui.SetAnchors(optsTitleText, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
		//_ui.SetTransform(optsTitleText, 5, 0, -10, 0);
		Text messageTextComp = message.AddComponent<Text>();
		messageTextComp.text = Message;
		messageTextComp.font = _style.FontA;
		//messageTextComp.alignment = TextAnchor.MiddleLeft;

		// TODO: This message should be removed when session terminated with other game related UI.

		/*
		GameObject button = _ui.CreateMenuButton(errorMsg, "Exit", () => {
			GameObject.Destroy(errorMsg);
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
			CGame.Game.TerminateGameSession();
			CGame.UIManager.AddInterface(new CMainMenuUI());
		});
		_ui.SetTransform(button, 50, -100, 256, 50);
		*/

		GameObject button = _ui.CreateButton(mErrorMessageBox, "Close", () => {
			GameObject.Destroy(mErrorMessageBox);
			mErrorMessageBox = null;
			CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
		});
		_ui.AddLayout(button, -1, 20, 1.0f, -1);
	}
}