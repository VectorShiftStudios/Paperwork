using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CCharacterEditor : CAssetToolkitWindow
{
	private GameObject _sceneGraph;
	private GameObject _viewport;
	private SCameraState _camState;
	
	private GameObject _tbbSave;
	private GameObject _tbbShowGrid;

	private GameObject _facingAngleEditor;

	private GameObject _toolPanelContent;
	private GameObject _propViewContent;

	private Text _viewInfoText;

	private bool _showGrid = true;
	
	private bool _viewportMouseDown;
	private bool _prevMouseDown;
	private Vector2 _prevMousePos;

	private GameObject _unit;
	private Animator _animator;

	private string _animName = "";
	private float _animSpeed = 1.0f;
	private float _facingAngle = 180.0f;

	public CCharacterEditor()
	{
		mAssetName = "%%Character";
	}

	public override void Init(CAssetToolkit Toolkit)
	{
		base.Init(Toolkit);

		mPrimaryContent = mUI.CreateElement(Toolkit.mPrimaryContent, "modelEditor");
		mUI.AddLayout(mPrimaryContent, -1, -1, 1.0f, 1.0f);
		mUI.AddVerticalLayout(mPrimaryContent);
		mPrimaryContent.GetComponent<VerticalLayoutGroup>().spacing = 4.0f;

		GameObject toolbar = mUI.CreateElement(mPrimaryContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, 0.0f);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		_tbbShowGrid = mUI.CreateToolbarButton(toolbar, "Show Grid", mUI.BrushImage, () =>
		{
			_showGrid = !_showGrid;
			mUI.SetToolbarButtonHighlight(_tbbShowGrid, _showGrid ? CToolkitUI.EButtonHighlight.SELECTED : CToolkitUI.EButtonHighlight.NOTHING);
		});
		mUI.SetToolbarButtonHighlight(_tbbShowGrid, CToolkitUI.EButtonHighlight.SELECTED);

		mUI.CreateToolbarButton(toolbar, "Screenshot", mUI.LevelImage, () => { Application.CaptureScreenshot("shot.png"); });

		GameObject hSplit = mUI.CreateElement(mPrimaryContent, "horzLayout");
		mUI.AddLayout(hSplit, -1, -1, 1.0f, 1.0f);
		mUI.AddHorizontalLayout(hSplit);
		hSplit.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		GameObject w1 = mUI.CreateElement(hSplit, "split1");
		mUI.AddLayout(w1, 256, -1, 0.0f, 1.0f);
		mUI.AddVerticalLayout(w1);
		//AddImage(w1, Color.red);

		GameObject w2 = mUI.CreateElement(hSplit, "split2");
		mUI.AddLayout(w2, -1, -1, 0.60f, 1.0f);
		mUI.AddVerticalLayout(w2);
		//AddImage(w2, Color.green);

		GameObject w3 = mUI.CreateElement(hSplit, "split3");
		mUI.AddLayout(w3, 300, -1, 0.0f, 1.0f);
		mUI.AddVerticalLayout(w3);

		// Split 1
		GameObject w1p = mUI.CreateWindowPanel(w1, out _toolPanelContent, "Tools");
		mUI.AddLayout(w1p, -1, -1, 1.0f, 1.0f);

		_animName = Animation.GetDefaultAnimation();
		
		GameObject editor;
		
		mUI.CreateFieldElement(_toolPanelContent, "Speed", out editor);
		mUI.CreateFloatEditor(editor, _animSpeed, (float Value) => { _animSpeed = Value; _animator.speed = Value; });

		mUI.CreateFieldElement(_toolPanelContent, "Facing Angle", out editor);
		_facingAngleEditor = mUI.CreateFloatEditor(editor, _facingAngle, (float Value) => 
		{
			_facingAngle = Value;
			_unit.transform.rotation = Quaternion.AngleAxis(_facingAngle, Vector3.up);
		});

		mUI.CreateTextElement(_toolPanelContent, "(Click and drag in viewport to rotate)");

		//mUI.CreateFieldElement(_toolPanelContent, "Animation", out editor);
		//mUI.CreateComboBox(editor, _animName, _GetAnimsComboData, (string Name) => { _SetAnim(Name); });

		mUI.CreateTextElement(_toolPanelContent, "Animation", "test", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject scrollContent;
		GameObject scrollV1 = mUI.CreateScrollView(_toolPanelContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);

		CTUITreeView treeView;
		GameObject treeViewGob = mUI.CreateTreeView(scrollContent, out treeView);
		mUI.AddLayout(treeViewGob, -1, -1, 1.0f, 1.0f);

		for (int i = 0; i < Animation.FBXEntries.Length; ++i)
		{
			int localI = i;
			CTUITreeViewItem item = treeView.mRootItem.AddItem(Animation.FBXEntries[i].mName, () =>
			{
				_SetAnim(Animation.FBXEntries[localI].mName);
			});
		}

		treeView.Rebuild();

		// Split 2
		GameObject w2pContent;
		GameObject w2p = mUI.CreateWindowPanel(w2, out w2pContent, "Item View");
		mUI.AddLayout(w2p, -1, -1, 1.0f, 1.0f);

		_viewport = mUI.CreateElement(w2pContent, "itemView");
		_viewport.AddComponent<CameraView>();
		_viewport.AddComponent<RawImage>().texture = mToolkit.mPrimaryRT;
		_viewport.GetComponent<CameraView>().mOnMouseDown = _OnViewportMouseDown;
		_viewport.GetComponent<CameraView>().mOnMouseUp = _OnViewportMouseUp;
		mUI.AddLayout(_viewport, -1, -1, 1.0f, 1.0f);
		mUI.AddVerticalLayout(_viewport, new RectOffset(4, 4, 4, 4));

		_viewInfoText = mUI.CreateTextElement(_viewport, "Testing Text", "", CToolkitUI.ETextStyle.TS_HEADING).GetComponent<Text>();

		// Split 3
		GameObject w3p = mUI.CreateWindowPanel(w3, out _propViewContent, "Properties");
		mUI.AddLayout(w3p, -1, -1, 1.0f, 1.0f);

		// World Scene
		_sceneGraph = new GameObject("charEditRoot");
		_sceneGraph.SetActive(false);

		_camState.mBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1.0f);
		_camState.SetViewGame(EViewDirection.VD_FRONT);

		_unit = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7] as GameObject);
		_unit.transform.SetParent(_sceneGraph.transform);
		_unit.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
		_unit.transform.rotation = Quaternion.AngleAxis(_facingAngle, Vector3.up);

		_animator = _unit.transform.GetChild(0).GetComponent<Animator>();
	}

	public override void ViewportResize()
	{
		_viewport.GetComponent<RawImage>().texture = mToolkit.mPrimaryRT;
	}

	public override void Show()
	{
		mToolkit.SetCameraView(_viewport.GetComponent<CameraView>());
		CGame.CameraManager.SetCamState(_camState);
		_sceneGraph.SetActive(true);

		_SetAnim(_animName);
	}

	public override void Hide()
	{
		_camState = CGame.CameraManager.GetCamState();
		_sceneGraph.SetActive(false);
	}

	public void DrawGrid(int Width)
	{
		float halfWidth = Width / 2.0f;

		Color color = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		Color axisLineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);

		for (int i = 0; i < Width + 1; ++i)
			CDebug.DrawLine(new Vector3(i - halfWidth - 0.5f, 0.0f, -halfWidth - 0.5f), new Vector3(i - halfWidth - 0.5f, 0.0f, halfWidth - 0.5f), color);

		for (int i = 0; i < Width + 1; ++i)
			CDebug.DrawLine(new Vector3(-halfWidth - 0.5f, 0.0f, i - halfWidth - 0.5f), new Vector3(halfWidth - 0.5f, 0.0f, i - halfWidth - 0.5f), color);

		CDebug.DrawLine(new Vector3(0.0f, 0.0f, -halfWidth - 0.5f), new Vector3(0.0f, 0.0f, halfWidth - 0.5f), axisLineColor);
		CDebug.DrawLine(new Vector3(-halfWidth - 0.5f, 0.0f, 0.0f), new Vector3(halfWidth - 0.5f, 0.0f, 0.0f), axisLineColor);
	}

	private void _ModifyAsset()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.WARNING);
	}

	private void _Save()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.NOTHING);
	}

	public override void Destroy()
	{
	}

	private void _OnViewportMouseDown(CameraView.EMouseButton Button)
	{
		_prevMousePos = _viewport.GetComponent<CameraView>().mLocalMousePos;
		_viewportMouseDown = true;
	}

	private void _OnViewportMouseUp(CameraView.EMouseButton Button)
	{
		_viewportMouseDown = false;
	}

	private Ray _GetViewportMouseRay()
	{
		Vector2 m = _viewport.GetComponent<CameraView>().mLocalMousePos;
		return Camera.main.ScreenPointToRay(m);
	}

	private bool _IntersectFloor(Ray R, out Vector3 HitPoint)
	{
		Vector3 n = Vector3.up;

		float numerator = Vector3.Dot(n, -R.origin);
		float demonenator = Vector3.Dot(n, R.direction);

		if (demonenator != 0.0f)
		{
			float t = numerator / demonenator;

			if (t >= 0.0f)
			{
				HitPoint = R.origin + R.direction * t;
				return true;
			}
		}

		HitPoint = Vector3.zero;
		return false;
	}

	public override void Update(CInputState InputState)
	{
		if (_showGrid)
			DrawGrid(10);

		AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

		if (!state.loop && state.normalizedTime >= 1.0f)
		{
			_animator.Play(state.shortNameHash, 0, state.normalizedTime - 1.0f);
		}

		float timeInSeconds = (state.normalizedTime * state.length * _animator.speed);
		float totalTimeInSeconds = state.length * _animator.speed;
		float tick = timeInSeconds * CWorld.TICKS_PER_SECOND;
		float totalTicks = totalTimeInSeconds * CWorld.TICKS_PER_SECOND;

		_viewInfoText.text = "Time: " + timeInSeconds.ToString("0.00") + "/" + totalTimeInSeconds.ToString("0.00");
		_viewInfoText.text += "\nTick: " + tick.ToString("0.0") + "/" + totalTicks.ToString("0.0");

		if (_viewportMouseDown)
		{
			Vector2 viewMousePos = _viewport.GetComponent<CameraView>().mLocalMousePos;
			Vector2 mouseDelta = viewMousePos - _prevMousePos;
			_prevMousePos = viewMousePos;

			_facingAngle -= mouseDelta.x;
			_facingAngle = _facingAngle % 360.0f;
			if (_facingAngle < 0.0f) _facingAngle = 360.0f - _facingAngle;
			mUI.ModifyFloatEditor(_facingAngleEditor, _facingAngle);
			_unit.transform.rotation = Quaternion.AngleAxis(_facingAngle, Vector3.up);
		}
	}

	public override string GetTabName()
	{
		return "Character Editor";
	}

	private List<string> _GetAnimsComboData()
	{
		List<string> data = new List<string>();

		for (int i = 0; i < Animation.FBXEntries.Length; ++i)
			data.Add(Animation.FBXEntries[i].mName);

		return data;
	}

	private void _SetAnim(string Name)
	{
		_animName = Name;
		_animator.Play(Name);
		_animator.speed = _animSpeed;
		
		CUtility.DestroyChildren(_propViewContent);
		
		mUI.CreateTextElement(_propViewContent, Name, "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject foldContent;
		mUI.CreateFoldOut(_propViewContent, "Animation Details", out foldContent);

		GameObject fieldEditor;
		mUI.CreateFieldElement(foldContent, "Name", out fieldEditor); mUI.CreateTextElement(fieldEditor, Name);

		Animation.CAnimationEntry entry = Animation.GetAnimEntry(Name);

		if (entry == null)
		{
			mUI.CreateTextElement(_propViewContent, "No Matching Animation Entry!");
		}
		else
		{
			mUI.CreateFieldElement(foldContent, "Framerate", out fieldEditor); mUI.CreateTextElement(fieldEditor, entry.mFPS.ToString());
			mUI.CreateFieldElement(foldContent, "Start Time", out fieldEditor); mUI.CreateTextElement(fieldEditor, entry.mStartTime.ToString());
			mUI.CreateFieldElement(foldContent, "Duration", out fieldEditor); mUI.CreateTextElement(fieldEditor, entry.mDuration.ToString());
			mUI.CreateFieldElement(foldContent, "Speed Mod", out fieldEditor); mUI.CreateTextElement(fieldEditor, entry.mSpeed.ToString());
		}		
	}
}
