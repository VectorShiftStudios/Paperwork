using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CInterfaceComponent
{
	public bool mActive = false;

	protected CUIManager _context;
	
	public virtual void Init(CUIManager Context)
	{
		_context = Context;
	}

	public virtual void Destory()
	{
	}

	public virtual void Update()
	{
	}

	public void SetActive(bool Active)
	{
		if (mActive && !Active)
			Hide();
		else if (!mActive && Active)
			Show();
		
		mActive = Active;
	}

	public virtual void Show()
	{
	}

	public virtual void Hide()
	{
	}
}

public class CGameUI : CInterfaceComponent
{
	
}

public class CMainMenuUI : CInterfaceComponent
{
	private float _time = 0.0f;
	private GameObject _base;
	private GameObject _splashBase;
	private GameObject logo;

	public override void Init(CUIManager Context)
	{
		base.Init(Context);

		CToolkitUI ui = CGame.ToolkitUI;
		
		_base = ui.CreateElement(CGame.UIManager.mMenuLayer, "mainMenuBase");
		_base.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		ui.SetRectFillParent(_base);

		logo = ui.CreateElement(_base, "logo");
		Image logoImage = logo.AddComponent<Image>();
		logoImage.sprite = CGame.PrimaryResources.Sprites[1];
		//logoImage.SetNativeSize();
		logoImage.preserveAspect = true;
		logoImage.color = new Color(1.0f, 1.0f, 1.0f);
		//logo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
		logo.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.0f);
		logo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
		logo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
		logo.GetComponent<RectTransform>().sizeDelta = new Vector2(256, 256);
		logo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
		
		_splashBase = ui.CreateElement(CGame.UIManager.mMenuLayer, "splashBase");
		_splashBase.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.0f);
		ui.SetRectFillParent(_splashBase);

		CGame.UIManager.PlayMusic(0);
	}

	public override void Destory()
	{
		base.Destory();

		GameObject.Destroy(_base);

		if (_splashBase != null)
			GameObject.Destroy(_splashBase);
	}

	private int _phase = 0;

	public override void Update()
	{
		base.Update();

		_time += Time.deltaTime;

		if (_phase == 0)
		{
			if (_time >= 2.0f)
			{
				_phase = 1;
				_time = 0.0f;
			}
		}

		if (_phase == 1)
		{
			_splashBase.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, _time * 2.0f);

			if (_time >= 0.5f)
			{	
				_phase = 2;
				_time = 0.0f;
				GameObject.Destroy(logo);
				_ShowMainMenu();
			}
		}

		if (_phase == 2)
		{
			_splashBase.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1.0f - (_time * 2.0f));

			if (_time >= 0.5f)
			{
				_phase = 3;
				_time = 0.0f;
				GameObject.Destroy(_splashBase);
				_splashBase = null;
			}
		}
	}

	private void _ShowLevelSelect()
	{
		// Create Level select stuff
	}

	private void _ShowMainMenu()
	{
		// Create main menu stuff
		CToolkitUI ui = CGame.ToolkitUI;

		CJSONParser json = new CJSONParser();
		CJSONValue levelArray = json.Parse(CGame.DataDirectory + "campaign.txt");
		
		GameObject button;

		if (levelArray != null)
		{
			int levelCount = 0;

			for (int i = 0; i < levelArray.GetCount(); ++i)
			{
				CJSONValue level = levelArray[i];

				string levelName = level.GetString("name", "unknown");
				string assetName = level.GetString("asset", "unknown");
				bool playable = level.GetBool("playable");
				bool visible = level.GetBool("visible");

				if (visible)
				{
					button = ui.CreateMenuButton(_base, levelName + " (" + assetName + ".pwa)", () =>
					{
						CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[15]);
						_PlayLevel(assetName);
					}, 1, playable);
					ui.SetTransform(button, 50, -100 - (levelCount++ * 30), 512, 50);
				}
			}
		}
		
		/*
		button = CreateButton(_base, "Sample (cutscene)", null);
		ui.SetTransform(button, 50, -150, 256, 50);
		*/

		/*
		button = CreateButton(_base, "Multiplayer", null);
		ui.SetTransform(button, 50, -200, 256, 50);

		button = CreateButton(_base, "Options", null);
		ui.SetTransform(button, 50, -250, 256, 50);
		*/
		
		button = ui.CreateMenuButton(_base, "Quit", _Exit);
		ui.SetTransform(button, 50, -30, 256, 50);
	}

	private void _PlayLevel(string AssetName)
	{
		CGameSession.CStartParams startParams = new CGameSession.CStartParams();
		startParams.mPlayType = CGameSession.EPlayType.SINGLE;
		startParams.mUserPlayerIndex = 0;
		startParams.mLevelName = AssetName;

		if (CGame.Game.StartGameSession(startParams))
			_context.RemoveInterface(this);
	}

	private void _Exit()
	{
		CGame.Game.ExitApplication();
	}
}
