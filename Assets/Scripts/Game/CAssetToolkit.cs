using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class CAssetToolkitWindow
{
	public CAssetToolkit mToolkit;
	public CToolkitUI mUI;
	public GameObject mPrimaryContent;
	public GameObject mWindowTab;
	public string mAssetName;

	public virtual string GetTabName()
	{
		return "(No Name)";
	}

	public virtual void Init(CAssetToolkit Toolkit)
	{
		mToolkit = Toolkit;
		mUI = Toolkit.mUI;
	}

	public virtual void Update(CInputState InputState) { }
	public virtual void Destroy() { }
	public virtual void Show() { }
	public virtual void Hide() { }

	// TODO: This ewwwwwwwwwwwwwwww.
	public virtual void ViewportResize() { }
}

public class CAssetToolkit
{
	// Base UI
	public CToolkitUI mUI;
	public GameObject mBaseUIGob;
	public GameObject mGlobalTabs;
	public GameObject mPrimaryContent;

	private List<CAssetToolkitWindow> _windows = new List<CAssetToolkitWindow>();
	private CAssetToolkitWindow _activeWindow;

	public RenderTexture mPrimaryRT;

	private Vector2 _primaryRTSize;
	private CameraView _camView;

	public void Init(CToolkitUI UI)
	{
		QualitySettings.vSyncCount = 0;

		mUI = UI;

		mPrimaryRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
		Camera.main.targetTexture = mPrimaryRT;

		CreateBaseUI();
		CreateWindow(new CAssetEditor());
		ShowWindow(_windows[0]);
	}

	public void Destroy()
	{
		// TODO: Clear RT on camera
		// TODO: Restore V-Sync
	}

	public void Update(CInputState InputState)
	{
		_UpdateCamView();

		if (_activeWindow != null)
			_activeWindow.Update(InputState);
	}

	private void _UpdateCamView(bool ForceResize = false)
	{
		if (_camView != null)
		{
			if (_primaryRTSize != _camView.mDimensions || ForceResize)
			{
				_primaryRTSize = _camView.mDimensions;
				Camera.main.targetTexture = null;
				RenderTexture.ReleaseTemporary(mPrimaryRT);
				//mPrimaryRT = RenderTexture.GetTemporary((int)_primaryRTSize.x, (int)_primaryRTSize.y, 24);
				mPrimaryRT = RenderTexture.GetTemporary((int)_primaryRTSize.x, (int)_primaryRTSize.y, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
				Camera.main.targetTexture = mPrimaryRT;
				_activeWindow.ViewportResize();
				Debug.Log("Size Update " + _primaryRTSize);
			}
		}
	}

	public void SetCameraView(CameraView CamView)
	{
		_camView = CamView;
		_UpdateCamView(true);
	}

	public void CreateBaseUI()
	{
		GameObject b = mUI.CreateElement(mUI.Canvas, "uiBase");
		b.transform.SetAsFirstSibling();
		mUI.SetRectFillParent(b);
		mUI.AddImage(b, mUI.PrimaryBackground);

		GameObject vl = mUI.CreateElement(b, "vertLayout");
		mUI.SetRectFillParent(vl);
		mUI.AddVerticalLayout(vl);

		GameObject sp1 = mUI.CreateElement(vl, "spacer");
		mUI.AddLayout(sp1, -1, 10, 1.0f, -1);

		mGlobalTabs = mUI.CreateTabView(vl);

		mPrimaryContent = mUI.CreateElement(vl, "primaryWindow");
		mUI.AddLayout(mPrimaryContent, -1, -1, 1.0f, 1.0f);
		mUI.AddImage(mPrimaryContent, mUI.WindowBackground);
		mUI.AddVerticalLayout(mPrimaryContent);
		mPrimaryContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);
		mPrimaryContent.GetComponent<VerticalLayoutGroup>().spacing = 4;
	}

	public void CreateWindow(CAssetToolkitWindow Window)
	{
		Window.Init(this);
		Window.mWindowTab = mUI.CreateWindowTab(mGlobalTabs, Window.GetTabName(), () => ShowWindow(Window), false, true);
		_windows.Add(Window);
	}

	public void ShowWindow(CAssetToolkitWindow Window)
	{
		if (_activeWindow != null)
		{
			_activeWindow.mPrimaryContent.SetActive(false);
			mUI.SetTab(_activeWindow.mWindowTab, false, true);
			_activeWindow.Hide();
			_camView = null;
		}

		_activeWindow = Window;

		if (_activeWindow != null)
		{
			_activeWindow.mPrimaryContent.SetActive(true);
			mUI.SetTab(_activeWindow.mWindowTab, true, true);
			_activeWindow.Show();
			EventSystem.current.SetSelectedGameObject(null);
		}
	}

	public void EditCharacter()
	{
		for (int i = 0; i < _windows.Count; ++i)
		{
			if (_windows[i].mAssetName == "%%Character")
			{
				ShowWindow(_windows[i]);
				return;
			}
		}

		CAssetToolkitWindow window = new CCharacterEditor();
		CreateWindow(window);
		ShowWindow(window);
	}

	public void EditAsset(string AssetName)
	{
		for (int i = 0; i < _windows.Count; ++i)
		{
			if (_windows[i].mAssetName == AssetName)
			{
				ShowWindow(_windows[i]);
				return;
			}
		}

		CAssetDeclaration decl = CGame.AssetManager.GetDeclaration(AssetName);
		CAssetToolkitWindow window = null;

		if (decl.mType == EAssetType.AT_BRUSH) window = new CBrushEditor(AssetName);
		else if (decl.mType == EAssetType.AT_MODEL) window = new CModelEditor(AssetName);
		else if (decl.mType == EAssetType.AT_LEVEL) window = new CLevelEditor(AssetName);
		else if (decl.mType == EAssetType.AT_ITEM) window = new CItemEditor(AssetName);

		if (window != null)
		{
			CreateWindow(window);
			ShowWindow(window);
		}
		else
		{
			Debug.LogError("No editor for asset (" + AssetName + ") of type " + decl.mType);
		}
	}
}