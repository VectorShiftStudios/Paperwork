using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CModelEditor : CAssetToolkitWindow
{
	public enum ETool
	{
		NONE,
		TRANSLATE,
		ROTATE,
		SCALE,
		VERTEX,
		EDGE
	}

	public enum EGizmoHandleID
	{
		NONE,
		TRANSLATE,
		ROTATE,
		SCALE,
		PRIMTIVE,
		VERTEX,
		EDGE
	}

	public struct SGizmoData
	{
		public EGizmoHandleID mGizmoHover;
		public Ray mStartMouseRay;
		public Ray mCurrentMouseRay;
		public Vector2 mStartMousePos;
		public Vector2 mCurrentMousePos;
		public bool mActedOnMouseDown;

		public Vector3 mOrigin;
		public Vector3 mAxis;
		public Vector3 mStartPos;
		public int mCornerID;

		public int mHoverID;
	}
	
	// TODO: Duplicate the asset for isolated editing.
	private CModelAsset _asset;
	private CVectorModel _vectorModel;

	private GameObject _modelGob;

	private GameObject _primProps;
	private GameObject _viewport;

	private GameObject _primPosField;

	private bool _viewportMouseDown = false;
	private CTUITreeViewItem _treeViewPrimItem;
	private GameObject _sceneGraph;
	private GameObject _scaleMan;
	private int _selectedPrimID = -1;

	private bool _localTransform = false;
	private float _snapSpacingTranslate = 0.1f;
	private float _snapSpacingRotate = 15.0f;
	private float _snapSpacingScale = 0.1f;

	private SCameraState _camState;

	private EViewDirection _viewDirection;
	
	private ETool _tool;
	private SGizmoData _gizmoData;

	private string _edgeBrush = "(None)";

	private bool _assetModified = false;
	private GameObject _tbbSave;

	private GameObject _tbbCamGame;
	private GameObject _tbbCamFree;
	private GameObject _tbbCamSide;
	private GameObject _tbbCamTop;
	private GameObject _tbbCamFront;

	private GameObject[] _tbbView = new GameObject[4];
	
	private GameObject _tbbToolMove;
	private GameObject _tbbToolVertex;
	private GameObject _tbbToolEdgePaint;

	private GameObject _tbbShowScale;
	private GameObject _tbbLocal;

	private Color _floorColour = new Color(151.0f / 255.0f, 145.0f / 255.0f, 136.0f / 255.0f);

	public CModelEditor(string AssetName)
	{
		_viewDirection = EViewDirection.VD_FRONT;
		mAssetName = AssetName;
		_asset = CGame.AssetManager.GetAsset<CModelAsset>(AssetName);
		_vectorModel = _asset.mVectorModel;
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

		_tbbSave = mUI.CreateToolbarButton(toolbar, "Save", mUI.SaveImage, _Save);
		//mUI.CreateToolbarButton(toolbar, "Revert", mUI.SaveImage);
		mUI.CreateToolbarSeparator(toolbar);

		_tbbCamGame = mUI.CreateToolbarButton(toolbar, "Game", mUI.CompanyImage, () =>
		{
			_camState.SetViewGame(_viewDirection);
			CGame.CameraManager.SetCamState(_camState);
			mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbCamFree, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamSide, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamTop , CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFront, CToolkitUI.EButtonHighlight.NOTHING);
		});

		_tbbCamFree = mUI.CreateToolbarButton(toolbar, "Free", mUI.CompanyImage, () =>
		{
			_camState = CGame.CameraManager.GetCamState();
			_camState.SetViewFree();
			CGame.CameraManager.SetCamState(_camState);
			mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFree, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbCamSide, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamTop, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFront, CToolkitUI.EButtonHighlight.NOTHING);
		});

		_tbbCamSide = mUI.CreateToolbarButton(toolbar, "Side", mUI.CompanyImage, () =>
		{
			_camState.SetOrthographic(EOrhtoView.OV_LEFT);
			CGame.CameraManager.SetCamState(_camState);
			mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFree, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamSide, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbCamTop, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFront, CToolkitUI.EButtonHighlight.NOTHING);
		});

		_tbbCamTop = mUI.CreateToolbarButton(toolbar, "Top", mUI.CompanyImage, () =>
		{
			_camState.SetOrthographic(EOrhtoView.OV_TOP);
			CGame.CameraManager.SetCamState(_camState);
			mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFree, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamSide, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamTop, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbCamFront, CToolkitUI.EButtonHighlight.NOTHING);
		});

		_tbbCamFront = mUI.CreateToolbarButton(toolbar, "Front", mUI.CompanyImage, () =>
		{
			_camState.SetOrthographic(EOrhtoView.OV_FRONT);
			CGame.CameraManager.SetCamState(_camState);
			mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFree, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamSide, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamTop, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbCamFront, CToolkitUI.EButtonHighlight.SELECTED);
		});
		
		mUI.CreateToolbarSeparator(toolbar);
		_tbbShowScale = mUI.CreateToolbarButton(toolbar, "Show Scale", mUI.BrushImage, _OnShowScale);
		mUI.SetToolbarButtonHighlight(_tbbShowScale, CToolkitUI.EButtonHighlight.SELECTED);
		mUI.CreateToolbarSeparator(toolbar);
		_tbbLocal = mUI.CreateToolbarButton(toolbar, "Local", mUI.SaveImage, _OnGlobalLocalClicked);
		mUI.CreateToolbarSeparator(toolbar);
		_tbbToolMove = mUI.CreateToolbarButton(toolbar, "Move", mUI.SaveImage, () => _SetTool(ETool.TRANSLATE));
		//mUI.CreateToolbarButton(toolbar, "Rotate", mUI.SaveImage, () => _SetTool(ETool.ROTATE));
		//mUI.CreateToolbarButton(toolbar, "Scale", mUI.SaveImage, () => _SetTool(ETool.SCALE));
		_tbbToolVertex = mUI.CreateToolbarButton(toolbar, "Vertex", mUI.SaveImage, () => _SetTool(ETool.VERTEX));
		_tbbToolEdgePaint = mUI.CreateToolbarButton(toolbar, "Edge Paint", mUI.SaveImage, () => _SetTool(ETool.EDGE));

		mUI.SetToolbarButtonHighlight(_tbbToolMove, CToolkitUI.EButtonHighlight.SELECTED);
		
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
		//AddImage(w3, Color.blue);

		// Split 1
		GameObject w1pContent;
		GameObject w1p = mUI.CreateWindowPanel(w1, out w1pContent, "Tools");
		mUI.AddLayout(w1p, -1, -1, 1.0f, 1.0f);
		//mUI.CreateTextElement(w1pContent, "Information", "text", CToolkitUI.ETextStyle.TS_HEADING);
		//mUI.CreateTextElement(w1pContent, "Testing string");

		mUI.CreateTextElement(w1pContent, "View Direction", "text", CToolkitUI.ETextStyle.TS_HEADING);

		toolbar = mUI.CreateElement(w1pContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		_tbbView[0] = mUI.CreateToolbarButton(toolbar, "Front", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_FRONT));
		_tbbView[1] = mUI.CreateToolbarButton(toolbar, "Right", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_RIGHT));
		_tbbView[2] = mUI.CreateToolbarButton(toolbar, "Back", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_BACK));
		_tbbView[3] = mUI.CreateToolbarButton(toolbar, "Left", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_LEFT));
		mUI.SetToolbarButtonHighlight(_tbbView[0], CToolkitUI.EButtonHighlight.SELECTED);

		mUI.CreateTextElement(w1pContent, "Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject editor;
		mUI.CreateFieldElement(w1pContent, "Edge Brush", out editor);
		mUI.CreateComboBox(editor, _edgeBrush, _GetBrushComboData, (string Name) => { _edgeBrush = Name; });
		
		mUI.CreateFieldElement(w1pContent, "Floor Colour", out editor);
		mUI.CreateColorEditor(editor, _floorColour, (Color color) => { _SetFloorColour(color); });
		//mUI.CreateFieldElement(w1pContent, "Translate Snap", out editor); mUI.CreateFloatEditor(editor, _snapSpacingTranslate, (float value) => { _snapSpacingTranslate = value; });
		//mUI.CreateFieldElement(w1pContent, "Rotate Snap", out editor); mUI.CreateFloatEditor(editor, _snapSpacingRotate, (float value) => { _snapSpacingRotate = value; });
		//mUI.CreateFieldElement(w1pContent, "Scale Snap", out editor); mUI.CreateFloatEditor(editor, _snapSpacingScale, (float value) => { _snapSpacingScale = value; });

		mUI.CreateTextElement(w1pContent, "Model Components", "text", CToolkitUI.ETextStyle.TS_HEADING);

		toolbar = mUI.CreateElement(w1pContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		mUI.CreateToolbarButton(toolbar, "Add", mUI.LevelImage, _OnClickAddComponent);
		mUI.CreateToolbarButton(toolbar, "Duplicate", mUI.LevelImage, _OnClickDuplicateComponent);
		mUI.CreateToolbarButton(toolbar, "Remove", mUI.LevelImage, _OnClickRemoveComponent);

		GameObject scrollContent;
		GameObject scrollV1 = mUI.CreateScrollView(w1pContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);

		CTUITreeView treeView;
		GameObject treeViewGob = mUI.CreateTreeView(scrollContent, out treeView);
		mUI.AddLayout(treeViewGob, -1, -1, 1.0f, 1.0f);

		_treeViewPrimItem = treeView.mRootItem.AddItem("Primitives");
		_treeViewPrimItem.mExpanded = true;
		_RebuildComponentTree();

		// Split 2
		GameObject w2pContent;
		GameObject w2p = mUI.CreateWindowPanel(w2, out w2pContent, "Model View");
		mUI.AddLayout(w2p, -1, -1, 1.0f, 1.0f);

		_viewport = mUI.CreateElement(w2pContent, "modelView");
		_viewport.AddComponent<CameraView>();
		_viewport.AddComponent<RawImage>().texture = mToolkit.mPrimaryRT;
		_viewport.GetComponent<CameraView>().mOnMouseDown = _OnViewportMouseDown;
		_viewport.GetComponent<CameraView>().mOnMouseUp = _OnViewportMouseUp;
		mUI.AddLayout(_viewport, -1, -1, 1.0f, 1.0f);

		// Split 3
		GameObject w3pContent;
		GameObject w3p = mUI.CreateWindowPanel(w3, out w3pContent, "Properties");
		mUI.AddLayout(w3p, -1, -1, 1.0f, 1.0f);

		mUI.CreateTextElement(w3pContent, "Selected Component", "text", CToolkitUI.ETextStyle.TS_HEADING);
	
		scrollV1 = mUI.CreateScrollView(w3pContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);
		scrollContent.GetComponent<VerticalLayoutGroup>().spacing = 4;

		_primProps = scrollContent;

		// World Scene
		_sceneGraph = new GameObject("modelEditorRoot");

		_scaleMan = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7] as GameObject);
		_scaleMan.transform.SetParent(_sceneGraph.transform);
		_scaleMan.transform.localPosition = new Vector3(-1.0f, 0.0f, -3.0f);
		_scaleMan.transform.rotation = Quaternion.AngleAxis(225, Vector3.up);

		_sceneGraph.SetActive(false);
		_modelGob = _vectorModel.CreateGameObject(_viewDirection);
		_modelGob.transform.SetParent(_sceneGraph.transform);
		_modelGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", _floorColour);

		_camState.mBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1.0f);
		_camState.SetViewGame(EViewDirection.VD_FRONT);
		mUI.SetToolbarButtonHighlight(_tbbCamGame, CToolkitUI.EButtonHighlight.SELECTED);

		_tool = ETool.TRANSLATE;
	}

	private void _RebuildComponentTree()
	{
		_treeViewPrimItem.RemoveChildren();

		CTUITreeViewItem selectedItem = null;

		for (int i = 0; i < _vectorModel.mPlanes.Count; ++i)
		{
			int guid = i;
			CTUITreeViewItem item = _treeViewPrimItem.AddItem(i + ": " + _vectorModel.mPlanes[i].mName, () => _OnClickPrimitive(guid, false));

			if (_selectedPrimID == i)
				selectedItem = item;
		}
		
		_treeViewPrimItem.RebuildEntireTree();

		if (selectedItem != null)
			selectedItem.Select();
	}

	private void _SetFloorColour(Color FloorColour)
	{
		_floorColour = FloorColour;
		_modelGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", _floorColour);
	}

	private void _ModifyAsset()
	{
		_vectorModel.RebuildEverything();

		if (!_assetModified)
		{
			_assetModified = true;
			mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.WARNING);
		}
	}

	private List<string> _GetBrushComboData()
	{
		List<string> data = CGame.AssetManager.GetAllAssetNames(EAssetType.AT_BRUSH);
		data.Insert(0, "(None)");

		return data;
	}

	private void _Save()
	{
		_assetModified = false;
		_asset.Save();
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.NOTHING);
	}

	public override void ViewportResize()
	{
		_viewport.GetComponent<RawImage>().texture = mToolkit.mPrimaryRT;
	}

	public override void Show()
	{
		mToolkit.SetCameraView(_viewport.GetComponent<CameraView>());
		CGame.CameraManager.SetCamState(_camState);
		_vectorModel.RebuildEverything();
		_sceneGraph.SetActive(true);
	}

	public override void Hide()
	{
		_camState = CGame.CameraManager.GetCamState();
		_sceneGraph.SetActive(false);
	}

	// TODO: This should really be a debug draw utility function.
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

	private void _OnClickEdgeSetView(EViewDirection ViewDirection)
	{
		_viewDirection = ViewDirection;
		_OnClickPrimitive(_selectedPrimID, false);

		for (int i = 0; i < 4; ++i)
		{
			if (i == (int)ViewDirection)
			{
				mUI.SetToolbarButtonHighlight(_tbbView[i], CToolkitUI.EButtonHighlight.SELECTED);
			}
			else
			{
				mUI.SetToolbarButtonHighlight(_tbbView[i], CToolkitUI.EButtonHighlight.NOTHING);
			}
		}
		
		/*
		Quaternion rotation = Quaternion.AngleAxis(0.0f, Vector3.up);
		if (ViewDirection == EViewDirection.VD_RIGHT) rotation = Quaternion.AngleAxis(270.0f, Vector3.up);
		else if (ViewDirection == EViewDirection.VD_BACK) rotation = Quaternion.AngleAxis(180.0f, Vector3.up);
		else if (ViewDirection == EViewDirection.VD_LEFT) rotation = Quaternion.AngleAxis(90.0f, Vector3.up);
		_modelGob.transform.rotation = rotation;
		*/

		GameObject.Destroy(_modelGob);
		_modelGob = _vectorModel.CreateGameObject(ViewDirection);
		_modelGob.transform.SetParent(_sceneGraph.transform);
		_modelGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", _floorColour);

		_camState = CGame.CameraManager.GetCamState();
		if (_camState.mViewMode == ECameraViewMode.CVM_GAME)
		{
			_camState.SetViewGame(ViewDirection);
			CGame.CameraManager.SetCamState(_camState);
		}
	}

	private void _OnViewportMouseDown(CameraView.EMouseButton Button)
	{
		_viewportMouseDown = true;
	}

	private void _OnViewportMouseUp(CameraView.EMouseButton Button)
	{
		_viewportMouseDown = false;
	}

	private Ray GetViewportMouseRay()
	{
		Vector2 m = _viewport.GetComponent<CameraView>().mLocalMousePos;
		return Camera.main.ScreenPointToRay(m);
	}

	// Gizmo Manipulation
	private bool _gizmoHold = false;
	private int _gizmoHoverID = -1;
	private int _gizmoHoldID = -1;
	private Vector3 _gizmoTranslateAxis;
	private Vector3 _gizmoStartPos = Vector3.zero;

	public override void Update(CInputState InputState)
	{
		DrawGrid(10);

		CModelPlane p = null;
		Ray mouseRay = GetViewportMouseRay();
		_gizmoData.mCurrentMouseRay = mouseRay;
		
		if (_selectedPrimID != -1)
		{
			p = _vectorModel.mPlanes[_selectedPrimID];
		}

		if (_viewportMouseDown)
		{
			EdgePaintHandleMouseDown(ref _gizmoData);
			TranslateHandleMouseDown(ref _gizmoData);
			PrimHandleMouseDown(ref _gizmoData);
		}
		else
		{
			PrimHandleDraw(ref _gizmoData);

			_gizmoData.mGizmoHover = EGizmoHandleID.NONE;
			_gizmoData.mStartMouseRay = mouseRay;
			_gizmoData.mActedOnMouseDown = false;
			_gizmoData.mHoverID = -1;

			if (_tool == ETool.EDGE)
			{
				// Primitive agnostic
				EdgePaintHandleUpdate(_vectorModel, ref _gizmoData);
			}
			else if (_tool == ETool.TRANSLATE)
			{
				if (p != null)
				{
					Vector3 axisX = p.mAxisX;
					Vector3 axisY = p.mAxisY;
					Vector3 axisZ = p.mAxisZ;

					if (!_localTransform)
					{
						axisX = Vector3.right;
						axisY = Vector3.up;
						axisZ = Vector3.forward;
					}

					TranslateHandleUpdate(p.mPosition, axisX, new Color(1, 0, 0, 1), -1, ref _gizmoData);
					TranslateHandleUpdate(p.mPosition, axisY, new Color(0, 1, 0, 1), -1, ref _gizmoData);
					TranslateHandleUpdate(p.mPosition, axisZ, new Color(0, 0, 1, 1), -1, ref _gizmoData);
				}
			}
			else if (_tool == ETool.VERTEX)
			{
				if (p != null)
				{
					Vector3 axisX = p.mAxisX;
					Vector3 axisY = p.mAxisY;
					Vector3 axisZ = p.mAxisZ;

					TranslateHandleUpdate(p.c1, axisX, new Color(1, 0, 0, 1), 0, ref _gizmoData);
					TranslateHandleUpdate(p.c1, axisZ, new Color(0, 0, 1, 1), 0, ref _gizmoData);

					TranslateHandleUpdate(p.c2, axisX, new Color(1, 0, 0, 1), 1, ref _gizmoData);
					TranslateHandleUpdate(p.c2, axisZ, new Color(0, 0, 1, 1), 1, ref _gizmoData);

					TranslateHandleUpdate(p.c3, axisX, new Color(1, 0, 0, 1), 2, ref _gizmoData);
					TranslateHandleUpdate(p.c3, axisZ, new Color(0, 0, 1, 1), 2, ref _gizmoData);

					TranslateHandleUpdate(p.c4, axisX, new Color(1, 0, 0, 1), 3, ref _gizmoData);
					TranslateHandleUpdate(p.c4, axisZ, new Color(0, 0, 1, 1), 3, ref _gizmoData);
				}
			}

			PrimHandleUpdate(_vectorModel, ref _gizmoData, _selectedPrimID);
		}
	}

	public void PrimHandleDraw(ref SGizmoData Gizmo)
	{
		if (_selectedPrimID != -1)
		{
			CModelPlane p = _vectorModel.mPlanes[_selectedPrimID];

			Vector3 o = p.mAxisY * 0.001f;

			CDebug.DrawLine(p.c1 - o, p.c2 - o, Color.green, false);
			CDebug.DrawLine(p.c2 - o, p.c3 - o, Color.green, false);
			CDebug.DrawLine(p.c3 - o, p.c4 - o, Color.green, false);
			CDebug.DrawLine(p.c4 - o, p.c1 - o, Color.green, false);
		}

		if (Gizmo.mGizmoHover == EGizmoHandleID.PRIMTIVE && Gizmo.mHoverID != _selectedPrimID)
		{
			CModelPlane p = _vectorModel.mPlanes[Gizmo.mHoverID];
			CDebug.DrawLine(p.c1, p.c2, Color.yellow, false);
			CDebug.DrawLine(p.c2, p.c3, Color.yellow, false);
			CDebug.DrawLine(p.c3, p.c4, Color.yellow, false);
			CDebug.DrawLine(p.c4, p.c1, Color.yellow, false);
		}
	}

	public void EdgePaintHandleMouseDown(ref SGizmoData Gizmo)
	{
		if (Gizmo.mGizmoHover != EGizmoHandleID.EDGE)
			return;

		if (!Gizmo.mActedOnMouseDown)
		{
			_OnClickPrimitive(-1, true);
			Gizmo.mActedOnMouseDown = true;
			CBrushAsset b = null;

			if (_edgeBrush != "(None)")
				b = CGame.AssetManager.GetAsset<CBrushAsset>(_edgeBrush);

			_vectorModel.mPlanes[Gizmo.mHoverID].mEdge[Gizmo.mCornerID].mBrush[(int)_viewDirection] = b;

			_ModifyAsset();
		}
	}

	public void EdgePaintHandleUpdate(CVectorModel Model, ref SGizmoData Gizmo)
	{
		if (Gizmo.mGizmoHover != EGizmoHandleID.NONE)
			return;

		float minT = float.MaxValue;
		Vector3 planeHitPoint = Vector3.zero;
		int hitPlaneID = -1;
		bool planeWasHit = false;

		for (int i = 0; i < Model.mPlanes.Count; ++i)
		{
			CModelPlane p = Model.mPlanes[i];
			Vector3 hit;
			float t;
			if (p.IntersectRay(Gizmo.mStartMouseRay, out hit, out t))
			{
				planeWasHit = true;
				if (t < minT)
				{
					minT = t;
					planeHitPoint = hit;
					hitPlaneID = i;
				}
			}
		}

		if (planeWasHit)
		{
			CModelPlane p = Model.mPlanes[hitPlaneID];
			
			// Find closest impact point on plane
			Vector3 projPoint;
			float d1 = CIntersections.PointVsLine(planeHitPoint, p.c1, p.c2, out projPoint);
			float d2 = CIntersections.PointVsLine(planeHitPoint, p.c2, p.c3, out projPoint);
			float d3 = CIntersections.PointVsLine(planeHitPoint, p.c3, p.c4, out projPoint);
			float d4 = CIntersections.PointVsLine(planeHitPoint, p.c4, p.c1, out projPoint);

			//CDebug.DrawLine(planeHitPoint, planeHitPoint + new Vector3(0, 0.1f, 0), Color.yellow, false);
			//CDebug.DrawLine(projPoint, projPoint + new Vector3(0, 0.1f, 0), Color.magenta, false);

			Gizmo.mGizmoHover = EGizmoHandleID.EDGE;
			Gizmo.mHoverID = hitPlaneID;

			if (d4 < d2 && d4 < d3 && d4 < d1)
			{
				Gizmo.mCornerID = 3;
				CDebug.DrawLine(p.c4, p.c1, Color.blue, false);
			}
			else if (d2 < d1 && d2 < d3 && d2 < d4)
			{
				Gizmo.mCornerID = 1;
				CDebug.DrawLine(p.c2, p.c3, Color.blue, false);
			}
			else if (d3 < d2 && d3 < d1 && d3 < d4)
			{
				Gizmo.mCornerID = 2;
				CDebug.DrawLine(p.c3, p.c4, Color.blue, false);
			}
			else
			{
				Gizmo.mCornerID = 0;
				CDebug.DrawLine(p.c1, p.c2, Color.blue, false);
			}
		}
	}

	public void PrimHandleUpdate(CVectorModel Model, ref SGizmoData Gizmo, int SelectedPrimID)
	{
		if (Gizmo.mGizmoHover != EGizmoHandleID.NONE)
			return;

		float minT = float.MaxValue;
		Vector3 planeHitPoint = Vector3.zero;
		int hitPlaneID = -1;
		bool planeWasHit = false;

		for (int i = 0; i < Model.mPlanes.Count; ++i)
		{
			CModelPlane p = Model.mPlanes[i];
			Vector3 hit;
			float t;
			if (p.IntersectRay(Gizmo.mStartMouseRay, out hit, out t))
			{
				planeWasHit = true;
				if (t < minT)
				{
					minT = t;
					planeHitPoint = hit;
					hitPlaneID = i;
				}
			}
		}

		if (planeWasHit)
		{
			Gizmo.mGizmoHover = EGizmoHandleID.PRIMTIVE;
			Gizmo.mHoverID = hitPlaneID;
		}
	}

	public void PrimHandleMouseDown(ref SGizmoData Gizmo)
	{	
		if (!Gizmo.mActedOnMouseDown)
		{
			Gizmo.mActedOnMouseDown = true;

			if (Gizmo.mGizmoHover == EGizmoHandleID.PRIMTIVE)
				_OnClickPrimitive(Gizmo.mHoverID, true);
			else if (Gizmo.mGizmoHover == EGizmoHandleID.NONE)
				_OnClickPrimitive(-1, true);
		}
	}

	public void TranslateHandleMouseDown(ref SGizmoData Gizmo)
	{
		if (Gizmo.mGizmoHover != EGizmoHandleID.TRANSLATE)
			return;

		CModelPlane p = _vectorModel.mPlanes[Gizmo.mHoverID];
		float t;
		Ray R = Gizmo.mCurrentMouseRay;

		if (!Gizmo.mActedOnMouseDown)
		{
			Gizmo.mActedOnMouseDown = true;
			
			GetClosestPoints(Gizmo.mOrigin, Gizmo.mAxis, R.origin, R.direction, out t);
			Gizmo.mStartPos = (Gizmo.mAxis * t);
		}

		GetClosestPoints(Gizmo.mOrigin, Gizmo.mAxis, R.origin, R.direction, out t);
		Vector3 newRayPos = Gizmo.mAxis * t;
		Vector3 deltaPos = newRayPos - Gizmo.mStartPos;
		Gizmo.mStartPos = newRayPos;
		//Vector3 newGizmoPos = p.mPosition + Gizmo.mAxis * t;
		//CDebug.DrawLine(newGizmoPos, newGizmoPos + Vector3.up, Color.magenta);
		//Vector3 newPos = newGizmoPos - Gizmo.mStartPos;

		/*
		float scale = _snapSpacingTranslate;
		float half = scale * 0.5f;

		if (newPos.x >= 0.0f) newPos.x += half; else newPos.x -= half;
		if (newPos.y >= 0.0f) newPos.y += half; else newPos.y -= half;
		if (newPos.z >= 0.0f) newPos.z += half; else newPos.z -= half;

		newPos.x = (int)((newPos.x) / scale) * scale;
		newPos.y = (int)((newPos.y) / scale) * scale;
		newPos.z = (int)((newPos.z) / scale) * scale;
		*/

		if (Gizmo.mCornerID == -1)
		{
			p.mPosition += deltaPos;
		}
		else
		{
			Vector3 localPos;
			localPos.y = 0.0f;
			localPos.x = Vector3.Dot(p.mAxisX, deltaPos);
			localPos.z = Vector3.Dot(p.mAxisZ, deltaPos);
			p.mCorner[Gizmo.mCornerID].mPosition += localPos;
		}

		_ModifyAsset();
		mUI.ModifyVector3Editor(_primPosField, p.mPosition);
	}

	public void TranslateHandleUpdate(Vector3 Origin, Vector3 Axis, Color DefaultColor, int CornerID, ref SGizmoData Gizmo)
	{	
		float scale = 1.0f;
		bool hitHandle = false;
		Ray R = Gizmo.mStartMouseRay;

		if (Gizmo.mGizmoHover == EGizmoHandleID.NONE)
		{
			Bounds bounds = new Bounds(new Vector3(0.5f * scale, 0.0f, 0.0f), new Vector3(1.0f * scale, 0.2f * scale, 0.2f * scale));

			Vector3 up = Vector3.up;

			if (Axis == up)
				up = Vector3.forward;

			Vector3 axisX = Axis;
			Vector3 axisZ = Vector3.Cross(up, axisX).normalized;
			Vector3 axisY = Vector3.Cross(axisX, axisZ);

			//Vector3 o = new Vector3(0, 0, 0);
			//CDebug.DrawLine(o, o + axisX, Color.red);
			//CDebug.DrawLine(o, o + axisY, Color.green);
			//CDebug.DrawLine(o, o + axisZ, Color.blue);

			Vector3 rDir = R.direction;
			rDir.x = Vector3.Dot(R.direction, axisX);
			rDir.y = Vector3.Dot(R.direction, axisY);
			rDir.z = Vector3.Dot(R.direction, axisZ);
			R.direction = rDir;

			R.origin -= Origin;
			Vector4 rO = R.origin;
			rO.x = Vector3.Dot(R.origin, axisX);
			rO.y = Vector3.Dot(R.origin, axisY);
			rO.z = Vector3.Dot(R.origin, axisZ);
			R.origin = rO;

			if (bounds.IntersectRay(R))
			{
				Gizmo.mGizmoHover = EGizmoHandleID.TRANSLATE;
				Gizmo.mHoverID = _selectedPrimID;
				Gizmo.mAxis = Axis;
				Gizmo.mCornerID = CornerID;
				Gizmo.mOrigin = Origin;
				hitHandle = true;
			}
		}

		if (hitHandle)
			CDebug.DrawLine(Origin, Origin + Axis * scale, Color.yellow, false);
		else
			CDebug.DrawLine(Origin, Origin + Axis * scale, DefaultColor, false);
	}

	// TODO: Should be moved to math utlity.
	// Returns point on Line A.
	public bool GetClosestPoints(Vector3 AO, Vector3 AD, Vector3 BO, Vector3 BD, out float AT)
	{
		AT = 0.0f;

		Vector3 w = AO - BO;
		float a = Vector3.Dot(AD, AD);
		float b = Vector3.Dot(AD, BD);
		float c = Vector3.Dot(BD, BD);
		float d = Vector3.Dot(AD, w);
		float e = Vector3.Dot(BD, w);

		float denom = a * c - b * b;

		if (denom == 0.0f)
			return false;

		AT = (b * e - c * d) / denom;
		
		return true;
	}

	public override string GetTabName()
	{
		return "Model (" + _asset.mName + ")";
	}

	private void _OnClickPrimitive(int ID, bool UpdateTreeView)
	{
		CUtility.DestroyChildren(_primProps);

		_selectedPrimID = ID;

		if (UpdateTreeView)
			_RebuildComponentTree();

		if (ID < 0)
			return;

		CModelPlane prim = _vectorModel.mPlanes[ID];
		GameObject content;
		GameObject editor;
		mUI.CreateFoldOut(_primProps, "Details", out content);
		mUI.CreateFieldElement(content, "Type", out editor); mUI.CreateTextElement(editor, "Plane");

		mUI.CreateFieldElement(content, "Name", out editor);
		mUI.CreateStringEditor(editor, prim.mName, (string Name) => { prim.mName = Name; _ModifyAsset(); _RebuildComponentTree(); });
		
		mUI.CreateFoldOut(_primProps, "Surface", out content);
		mUI.CreateFieldElement(content, "Position", out editor);
		_primPosField = mUI.CreateVector3Editor(editor, prim.mPosition, (Vector3 vec) => { prim.mPosition = vec; _ModifyAsset(); } );

		mUI.CreateFieldElement(content, "Rotation", out editor);
		mUI.CreateVector3Editor(editor, prim.mRotation, (Vector3 vec) => { prim.mRotation = vec; _ModifyAsset(); });
		mUI.CreateFieldElement(content, "Fill Brush", out editor); mUI.CreateComboBox(editor, GetBrushName(prim.mFillBrush), _GetBrushComboData, (string Name) => { _SetFillBrush(Name); });

		mUI.CreateFoldOut(_primProps, "Point Offsets", out content);
		mUI.CreateFieldElement(content, "1", out editor);
		mUI.CreateVector2Editor(editor, new Vector2(prim.mCorner[0].mPosition.x, prim.mCorner[0].mPosition.z), (Vector2 vec) => { prim.mCorner[0].mPosition = new Vector3(vec.x, 0.0f, vec.y); _ModifyAsset(); });

		mUI.CreateFieldElement(content, "2", out editor);
		mUI.CreateVector2Editor(editor, new Vector2(prim.mCorner[1].mPosition.x, prim.mCorner[1].mPosition.z), (Vector2 vec) => { prim.mCorner[1].mPosition = new Vector3(vec.x, 0.0f, vec.y); _ModifyAsset(); });

		mUI.CreateFieldElement(content, "3", out editor);
		mUI.CreateVector2Editor(editor, new Vector2(prim.mCorner[2].mPosition.x, prim.mCorner[2].mPosition.z), (Vector2 vec) => { prim.mCorner[2].mPosition = new Vector3(vec.x, 0.0f, vec.y); _ModifyAsset(); });

		mUI.CreateFieldElement(content, "4", out editor);
		mUI.CreateVector2Editor(editor, new Vector2(prim.mCorner[3].mPosition.x, prim.mCorner[3].mPosition.z), (Vector2 vec) => { prim.mCorner[3].mPosition = new Vector3(vec.x, 0.0f, vec.y); _ModifyAsset(); });

		mUI.CreateFoldOut(_primProps, "Edge Brushes for " + _viewDirection.ToString(), out content);
		mUI.CreateFieldElement(content, "1", out editor); mUI.CreateComboBox(editor, GetBrushName(prim.mEdge[0].mBrush[(int)_viewDirection]), _GetBrushComboData, (string Name) => { _SetEdgeBrush(Name, 0); });
		mUI.CreateFieldElement(content, "2", out editor); mUI.CreateComboBox(editor, GetBrushName(prim.mEdge[1].mBrush[(int)_viewDirection]), _GetBrushComboData, (string Name) => { _SetEdgeBrush(Name, 1); });
		mUI.CreateFieldElement(content, "3", out editor); mUI.CreateComboBox(editor, GetBrushName(prim.mEdge[2].mBrush[(int)_viewDirection]), _GetBrushComboData, (string Name) => { _SetEdgeBrush(Name, 2); });
		mUI.CreateFieldElement(content, "4", out editor); mUI.CreateComboBox(editor, GetBrushName(prim.mEdge[3].mBrush[(int)_viewDirection]), _GetBrushComboData, (string Name) => { _SetEdgeBrush(Name, 3); });
	}

	private void _SetFillBrush(string BrushName)
	{
		if (_selectedPrimID == -1)
			return;

		CModelPlane p = _vectorModel.mPlanes[_selectedPrimID];
		CBrushAsset b = null;

		if (BrushName != "(None)")
			b = CGame.AssetManager.GetAsset<CBrushAsset>(BrushName);

		p.mFillBrush = b;
		_ModifyAsset();
	}

	private void _SetEdgeBrush(string BrushName, int EdgeIndex)
	{
		if (_selectedPrimID == -1)
			return;

		CModelPlane p = _vectorModel.mPlanes[_selectedPrimID];
		CBrushAsset b = null;

		if (BrushName != "(None)")
			b = CGame.AssetManager.GetAsset<CBrushAsset>(BrushName);

		p.mEdge[EdgeIndex].mBrush[(int)_viewDirection] = b;

		_ModifyAsset();
	}

	private string GetBrushName(CBrushAsset Brush)
	{
		if (Brush != null)
			return Brush.mName;

		return "None";
	}

	private void _OnGlobalLocalClicked()
	{
		_localTransform = !_localTransform;
		mUI.SetToolbarButtonHighlight(_tbbLocal, _localTransform ? CToolkitUI.EButtonHighlight.SELECTED : CToolkitUI.EButtonHighlight.NOTHING);
	}

	private void _OnClickAddComponent()
	{
		CModelPlane p = new CModelPlane();
		p.mName = "New Plane";
		p.mFillBrush = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_SURFACE_BRUSH);
		p.mPosition = new Vector3(0, 0, 0);
		p.mRotation = new Vector3(0, 0, 0);
		p.mCorner[0].mPosition = new Vector3(-1, 0, 1);
		p.mCorner[1].mPosition = new Vector3(1, 0, 1);
		p.mCorner[2].mPosition = new Vector3(1, 0, -1);
		p.mCorner[3].mPosition = new Vector3(-1, 0, -1);

		CBrushAsset brush = CGame.AssetManager.GetAsset<CBrushAsset>(CVectorModel.DEFAULT_EDGE_BRUSH);

		for (int i = 0; i < 4; ++i)
			for (int j = 0; j < 4; ++j)
				p.mEdge[j].mBrush[i] = brush;
		
		_vectorModel.mPlanes.Add(p);
		_ModifyAsset();
		_OnClickPrimitive(_vectorModel.mPlanes.Count - 1, true);
	}

	private void _OnClickDuplicateComponent()
	{
		if (_selectedPrimID == -1)
			return;

		_vectorModel.mPlanes.Add(_vectorModel.mPlanes[_selectedPrimID].Clone());
		_ModifyAsset();
		_OnClickPrimitive(_vectorModel.mPlanes.Count - 1, true);
	}

	private void _OnClickRemoveComponent()
	{
		if (_selectedPrimID == -1)
			return;

		_vectorModel.mPlanes.RemoveAt(_selectedPrimID);
		_ModifyAsset();
		_OnClickPrimitive(-1, true);
	}

	private void _SetTool(ETool Tool)
	{
		if (Tool == ETool.TRANSLATE)
		{
			mUI.SetToolbarButtonHighlight(_tbbToolMove, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbToolVertex, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbToolEdgePaint, CToolkitUI.EButtonHighlight.NOTHING);
		}
		else if (Tool == ETool.VERTEX)
		{
			mUI.SetToolbarButtonHighlight(_tbbToolMove, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbToolVertex, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.SetToolbarButtonHighlight(_tbbToolEdgePaint, CToolkitUI.EButtonHighlight.NOTHING);
		}
		else if (Tool == ETool.EDGE)
		{
			mUI.SetToolbarButtonHighlight(_tbbToolMove, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbToolVertex, CToolkitUI.EButtonHighlight.NOTHING);
			mUI.SetToolbarButtonHighlight(_tbbToolEdgePaint, CToolkitUI.EButtonHighlight.SELECTED);
		}

		_tool = Tool;
	}

	private void _OnShowScale()
	{
		_scaleMan.SetActive(!_scaleMan.activeSelf);
		mUI.SetToolbarButtonHighlight(_tbbShowScale, _scaleMan.activeSelf ? CToolkitUI.EButtonHighlight.SELECTED : CToolkitUI.EButtonHighlight.NOTHING);
	}
}
