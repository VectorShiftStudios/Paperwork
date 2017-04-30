using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;

public class CLevelEditor : CAssetToolkitWindow
{
	public enum ETool
	{
		NONE,
		TILE,
		WALL,
		ENTITY,
		REGION
	}

	public enum EWallTool
	{
		NONE,
		WALL,
		DOOR
	}

	public static Color[] mPlayerColors = {
		new Color(100.0f / 255.0f, 150.0f / 255.0f, 150.0f / 255.0f),
		new Color(200.0f / 255.0f, 50.0f / 255.0f, 100.0f / 255.0f),
		new Color(150.0f / 255.0f, 180.0f / 255.0f, 100.0f / 255.0f),
		new Color(0.0f / 255.0f, 100.0f / 255.0f, 150.0f / 255.0f),
		new Color(0.5f, 0.5f, 0.5f),
	};

	public static string[] mPlayerIDs = {
		"Player 0",
		"Player 1",
		"Player 2",
		"Player 3",
		"Player 4"
	};

	private CLevelAsset _asset;
	private GameObject _sceneGraph;
	private GameObject _viewport;
	private SCameraState _camState;
	
	private GameObject _tbbSave;
	private GameObject _tbbShowGrid;

	private GameObject _toolPanelContent;
	private GameObject _propsPanelContent;

	private GameObject _tbbWallNormal;
	private GameObject _tbbWallDoor;

	private GameObject _undoButton;
	private GameObject _redoButton;

	private Text _viewHelpText;

	private bool _showGrid = true;

	private CMap _map;

	private ETool _tool;
	private Color _toolColor;
	private int _floorHoverX;
	private int _floorHoverY;

	private int _wallHoverTileX;
	private int _wallHoverTileY;
	private int _wallHoverDirection;

	private bool _wallLock = false;

	private EWallTool _wallTool;

	private bool _viewportPrevMouseLeftClick;
	private bool _viewportMouseLeftDown;
	private bool _viewportMouseRightDown;
	private Vector3 _mouseFloorHitStart;
	private Vector2 _mouseViewPosStart;
	private bool _mouseDragon;
	private Vector3 _metaDragStartPos;

	private bool _assetStartModify;

	private GameObject _tileColourEditor;

	private CEntityPlacer _entityPlacer;
	private string _defaultOwner = mPlayerIDs[0];
	private int _hoverObject = -1;
	private int _selectedObject = -1;

	private List<MemoryStream> _undoStack = new List<MemoryStream>();
	private int _undoIndex = 0;

	private CLevelMetaObject[] _playerMetas;

	public CLevelEditor(string AssetName)
	{
		mAssetName = AssetName;
		_asset = CGame.AssetManager.GetAsset<CLevelAsset>(AssetName);
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

		mUI.CreateToolbarSeparator(toolbar);
		_tbbShowGrid = mUI.CreateToolbarButton(toolbar, "Show Grid", mUI.BrushImage, () =>
		{
			_showGrid = !_showGrid;
			mUI.SetToolbarButtonHighlight(_tbbShowGrid, _showGrid ? CToolkitUI.EButtonHighlight.SELECTED : CToolkitUI.EButtonHighlight.NOTHING);
		});
		mUI.SetToolbarButtonHighlight(_tbbShowGrid, CToolkitUI.EButtonHighlight.SELECTED);

		mUI.CreateToolbarSeparator(toolbar);
		_undoButton = mUI.CreateToolbarButton(toolbar, "Undo", mUI.UndoImage, _Undo);
		_redoButton = mUI.CreateToolbarButton(toolbar, "Redo", mUI.RedoImage, _Redo);

		mUI.CreateToolbarSeparator(toolbar);
		mUI.CreateToolbarButton(toolbar, "Entity", mUI.SaveImage, () => _SetTool(ETool.ENTITY));
		mUI.CreateToolbarButton(toolbar, "Tile", mUI.SaveImage, () => _SetTool(ETool.TILE));
		mUI.CreateToolbarButton(toolbar, "Wall", mUI.SaveImage, () => _SetTool(ETool.WALL));
		
		mUI.CreateToolbarSeparator(toolbar);
		mUI.CreateToolbarButton(toolbar, "Properties", mUI.SheetImage, () => { _DeselectObjects(); _EndPlaceEntity(); });

		mUI.CreateToolbarSeparator(toolbar);
		mUI.CreateToolbarButton(toolbar, "Play", mUI.LevelImage, _PlayLevel);

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
		
		// Split 2
		GameObject w2pContent;
		GameObject w2p = mUI.CreateWindowPanel(w2, out w2pContent, "Level View");
		mUI.AddLayout(w2p, -1, -1, 1.0f, 1.0f);

		_viewport = mUI.CreateElement(w2pContent, "levelView");
		_viewport.AddComponent<CameraView>();
		_viewport.AddComponent<RawImage>().texture = mToolkit.mPrimaryRT;
		_viewport.GetComponent<CameraView>().mOnMouseDown = _OnViewportMouseDown;
		_viewport.GetComponent<CameraView>().mOnMouseUp = _OnViewportMouseUp;
		mUI.AddLayout(_viewport, -1, -1, 1.0f, 1.0f);

		mUI.AddVerticalLayout(_viewport);

		_viewHelpText = mUI.CreateTextElement(w2pContent, "Viewport").GetComponent<Text>();
		//_viewHelpText.color = Color.black;

		// Split 3
		GameObject w3p = mUI.CreateWindowPanel(w3, out _propsPanelContent, "Properties");
		mUI.AddLayout(w3p, -1, -1, 1.0f, 1.0f);

		GameObject scrollContent;
		GameObject scrollV1 = mUI.CreateScrollView(_propsPanelContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);
		scrollContent.GetComponent<VerticalLayoutGroup>().spacing = 4;

		_propsPanelContent = scrollContent;

		_camState.SetViewGame(EViewDirection.VD_FRONT);

		_PreparePlayers();

		_PushUndoState();
		_DeselectObjects();
		_SetTool(ETool.ENTITY);
		_UpdateUndoState(false);
		_sceneGraph.SetActive(false);
	}

	private void _PreparePlayers(bool ClearExisting = false)
	{
		_playerMetas = new CLevelMetaObject[CWorld.MAX_PLAYERS];
		
		for (int i = 0; i < _asset.mObjects.Count; ++i)
		{
			CLevelMetaObject meta = _asset.mObjects[i];

			if (meta.mType == CLevelMetaObject.EType.PLAYER && meta.mID >= 0 && meta.mID < CWorld.MAX_PLAYERS)
			{
				if (ClearExisting)
					_asset.mObjects.RemoveAt(i--);
				else
					_playerMetas[meta.mID] = meta;
			}
		}

		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
		{
			if (_playerMetas[i] == null)
			{
				CLevelMetaObject meta = new CLevelMetaObject();
				meta.mType = CLevelMetaObject.EType.PLAYER;
				meta.mID = i;
				meta.mIdentifier = "Player " + i;
				meta.mData = (1 << i) | (1 << 4);
				meta.mExtraIntData = 0;
				meta.mColor = mPlayerColors[i];

				// Primary Player
				if (i == 0)
				{
					meta.mExtraIntData = CPlayer.FLAG_PLAYABLE | CPlayer.FLAG_COMPETING;
				}

				// Neutral Player
				if (i == 4)
				{
					for (int j = 0; j < CWorld.MAX_PLAYERS; ++j)
						meta.mData |= (1 << j);
				}

				_asset.mObjects.Add(meta);
				_playerMetas[i] = meta;
			}
		}
	}

	private void _BuildMetaObject(CLevelMetaObject Meta)
	{
		if (Meta.mType == CLevelMetaObject.EType.ITEM)
		{
			CItemAsset itemAsset = CGame.AssetManager.GetAsset<CItemAsset>(Meta.mIdentifier);

			if (itemAsset != null)
			{
				Meta.mGOB = itemAsset.CreateVisuals(EViewDirection.VD_FRONT);
				Meta.mGOB.transform.SetParent(_sceneGraph.transform);
				Vector2 pivot = CItem.PivotRelativeToTile[Meta.mRotation];
				Meta.mGOB.transform.position = new Vector3(Meta.mPositionA.x, 0.0f, Meta.mPositionA.y) + new Vector3(pivot.x, 0.0f, pivot.y);
				Meta.mGOB.transform.rotation = CItem.RotationTable[Meta.mRotation];
				CItemView.SetItemSurfaceColour(itemAsset.mItemType, Meta.mGOB, _playerMetas[Meta.mOwner].mColor);

				if (itemAsset.mItemType == EItemType.DOOR)
				{
					if (Meta.mExtraBoolData)
					{
						GameObject padlock = (GameObject)GameObject.Instantiate(CGame.WorldResources.PadlockPrefab);
						padlock.transform.SetParent(Meta.mGOB.transform);
						padlock.transform.localPosition = new Vector3(0.5f, 2.25f, 1.0f);
					}
				}
			}
		}
		else if (Meta.mType == CLevelMetaObject.EType.UNIT)
		{
			Meta.mGOB = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7] as GameObject);
			Meta.mGOB.transform.SetParent(_sceneGraph.transform);
			Meta.mGOB.transform.localPosition = new Vector3(Meta.mPositionA.x, 0.0f, Meta.mPositionA.y);
			Meta.mGOB.transform.rotation = Quaternion.AngleAxis(Meta.mRotation, Vector3.up);
			Material sharedMat = Meta.mGOB.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material;
			Meta.mGOB.AddComponent<Item>().Init(sharedMat);
			sharedMat.SetColor("_PlayerColor", _playerMetas[Meta.mOwner].mColor);
			sharedMat.SetTexture("_MainTex", CGame.PrimaryResources.UnitTexTie);
		}
		else if (Meta.mType == CLevelMetaObject.EType.DECAL)
		{
			SDecalInfo info = new SDecalInfo();
			info.mPosition = Meta.mPositionA;
			info.mSize = Meta.mPositionB;
			info.mRotation = Quaternion.Euler(Meta.mOrientation);
			info.mText = Meta.mIdentifier;
			info.mType = (CDecal.EDecalType)Meta.mSubtype;
			info.mVisualId = Meta.mData;
			info.mColor = Meta.mColor;

			Meta.mGOB = CDecalView.CreateDecal(info);
			Meta.mGOB.transform.SetParent(_sceneGraph.transform);
		}
	}

	private int _IntersectMetaObjects(Ray R)
	{
		float minD = float.MaxValue;
		int metaId = -1;

		for (int i = 0; i < _asset.mObjects.Count; ++i)
		{
			CLevelMetaObject meta = _asset.mObjects[i];

			if (meta.mType == CLevelMetaObject.EType.ITEM)
			{
				CItemAsset itemAsset = CGame.AssetManager.GetAsset<CItemAsset>(meta.mIdentifier);
				Bounds bounds = CItem.CalculateBounds(new Vector2(meta.mPositionA.x, meta.mPositionA.y), meta.mRotation, itemAsset.mWidth, itemAsset.mLength);

				float d;
				if (bounds.IntersectRay(R, out d))
				{
					if (d < minD)
					{
						minD = d;
						metaId = i;
					}
				}
			}
			else if (meta.mType == CLevelMetaObject.EType.UNIT)
			{
				Bounds bounds = new Bounds(new Vector3(meta.mPositionA.x, 0.6f, meta.mPositionA.y), new Vector3(0.5f, 1.2f, 0.5f));

				float d;
				if (bounds.IntersectRay(R, out d))
				{
					if (d < minD)
					{
						minD = d;
						metaId = i;
					}
				}
			}
			else if (meta.mType == CLevelMetaObject.EType.VOLUME)
			{
				Bounds bounds = new Bounds(meta.mPositionA, new Vector3(meta.mPositionB.x, 0.5f, meta.mPositionB.y));

				Plane pOuter = new Plane(Vector3.up, meta.mPositionA);

				float t;
				if (pOuter.Raycast(R, out t))
				{
					Vector3 hitPoint = R.origin + R.direction * t;
					Vector3 localHit = hitPoint - meta.mPositionA;
					float hitWidth = 0.3f;
					float outerWidth = meta.mPositionB.x + hitWidth;
					float outerHeight = meta.mPositionB.y + hitWidth;
					Rect outer = new Rect(-outerWidth * 0.5f, -outerHeight * 0.5f, outerWidth, outerHeight);

					if (outer.Contains(localHit.ToWorldVec2()))
					{
						float innerWidth = Mathf.Max(meta.mPositionB.x - hitWidth, 0.0f);
						float innerHeight = Mathf.Max(meta.mPositionB.y - hitWidth, 0.0f);
						Rect inner = new Rect(-innerWidth * 0.5f, -innerHeight * 0.5f, innerWidth, innerHeight);

						if (!inner.Contains(localHit.ToWorldVec2()))
						{
							float d = (R.direction * t).magnitude;
							if (d < minD)
							{
								minD = d;
								metaId = i;
							}
						}
					}
				}
			}
			else if (meta.mType == CLevelMetaObject.EType.DECAL)
			{
				Bounds bounds = _GetDecalBounds(meta);

				float d;
				if (bounds.IntersectRay(R, out d))
				{
					if (d < minD)
					{
						minD = d;
						metaId = i;
					}
				}
			}
		}

		return metaId;
	}

	private void _DrawMetaObjectHighlight(int MetaId, Color HighlightColor)
	{
		CLevelMetaObject meta = _asset.mObjects[MetaId];

		if (meta.mType == CLevelMetaObject.EType.ITEM)
		{
			CItemAsset itemAsset = CGame.AssetManager.GetAsset<CItemAsset>(meta.mIdentifier);
			Bounds bounds = CItem.CalculateBounds(new Vector2(meta.mPositionA.x, meta.mPositionA.y), meta.mRotation, itemAsset.mWidth, itemAsset.mLength);
			CDebug.DrawBounds(bounds, HighlightColor, false);
		}
		else if (meta.mType == CLevelMetaObject.EType.UNIT)
		{
			Bounds bounds = new Bounds(new Vector3(meta.mPositionA.x, 0.6f, meta.mPositionA.y), new Vector3(0.5f, 1.2f, 0.5f));
			CDebug.DrawBounds(bounds, HighlightColor, false);
		}
		else if (meta.mType == CLevelMetaObject.EType.VOLUME)
		{
			Bounds bounds = new Bounds(meta.mPositionA, new Vector3(meta.mPositionB.x, 0.3f, meta.mPositionB.y));
			CDebug.DrawBounds(bounds, HighlightColor, false);
		}
		else if (meta.mType == CLevelMetaObject.EType.DECAL)
		{
			Bounds bounds = _GetDecalBounds(meta);
			CDebug.DrawBounds(bounds, HighlightColor, false);
		}
	}

	private Bounds _GetDecalBounds(CLevelMetaObject Meta)
	{
		return Meta.mGOB.GetComponent<Renderer>().bounds;
	}

	private void _SelectMetaObject(int MetaId)
	{
		if (MetaId == -1)
		{
			if (_selectedObject != -1)
				_DeselectObjects();

			return;
		}

		CUtility.DestroyChildren(_propsPanelContent);
		_selectedObject = MetaId;
		CLevelMetaObject meta = _asset.mObjects[MetaId];

		GameObject toolbar = mUI.CreateElement(_propsPanelContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		mUI.CreateToolbarButton(toolbar, "Duplicate", mUI.LevelImage, () => { _EndPlaceEntity(); _DuplicateSelected(); });
		mUI.CreateToolbarButton(toolbar, "Delete", mUI.LevelImage, () =>
		{
			if (_selectedObject != -1)
			{
				_RemoveMetaObject(_selectedObject);
				_DeselectObjects();
			}
		});

		if (meta.mType == CLevelMetaObject.EType.ITEM)
		{
			CItemAsset asset = CGame.AssetManager.GetAsset<CItemAsset>(meta.mIdentifier);
			
			mUI.CreateTextElement(_propsPanelContent, "Item Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);
			
			GameObject editor;
			mUI.CreateFieldElement(_propsPanelContent, "Entity ID", out editor);
			mUI.CreateTextElement(editor, meta.mID.ToString());

			mUI.CreateFieldElement(_propsPanelContent, "Owner", out editor);
			mUI.CreateComboBox(editor, _GetOwnerId(meta.mOwner), _GetOwnerComboData, (string Name) => { meta.mOwner = _GetOwnerId(Name); _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Position", out editor);
			mUI.CreateVector2Editor(editor, meta.mPositionA, (pos) => { meta.mPositionA = pos; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Rotation", out editor);
			mUI.CreateIntEditor(editor, meta.mRotation, (rot) => { meta.mRotation = rot; _ModifyMetaObject(meta); });

			if (asset.mItemType == EItemType.SAFE)
			{
				mUI.CreateFieldElement(_propsPanelContent, "Paper Value", out editor);
				mUI.CreateIntEditor(editor, meta.mExtraIntData, (value) => { meta.mExtraIntData = value; _ModifyMetaObject(meta); });
			}
			else if (asset.mItemType == EItemType.DOOR)
			{
				mUI.CreateFieldElement(_propsPanelContent, "Locked", out editor);
				mUI.CreateBoolEditor(editor, meta.mExtraBoolData, (value) => { meta.mExtraBoolData = value; _ModifyMetaObject(meta); });
			}
		}
		else if (meta.mType == CLevelMetaObject.EType.UNIT)
		{
			mUI.CreateTextElement(_propsPanelContent, "Unit Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject editor;
			mUI.CreateFieldElement(_propsPanelContent, "Entity ID", out editor);
			mUI.CreateTextElement(editor, meta.mID.ToString());

			mUI.CreateFieldElement(_propsPanelContent, "Owner", out editor);
			mUI.CreateComboBox(editor, _GetOwnerId(meta.mOwner), _GetOwnerComboData, (string Name) => { meta.mOwner = _GetOwnerId(Name); _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Position", out editor);
			mUI.CreateVector2Editor(editor, meta.mPositionA, (pos) => { meta.mPositionA = pos; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Rotation", out editor);
			mUI.CreateIntEditor(editor, meta.mRotation, (rot) => { meta.mRotation = rot; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Tier", out editor);
			mUI.CreateComboBox(editor, _GetUnitTierType(meta.mSubtype), _GetUnitTierComboData, (value) => { meta.mSubtype = _GetUnitTierType(value); _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Level", out editor);
			mUI.CreateIntEditor(editor, meta.mExtraIntData, (value) => { meta.mExtraIntData = value; _ModifyMetaObject(meta); });
		}
		else if (meta.mType == CLevelMetaObject.EType.VOLUME)
		{
			mUI.CreateTextElement(_propsPanelContent, "Volume Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject editor;
			mUI.CreateFieldElement(_propsPanelContent, "Entity ID", out editor);
			mUI.CreateTextElement(editor, meta.mID.ToString());

			mUI.CreateFieldElement(_propsPanelContent, "Position", out editor);
			mUI.CreateVector3Editor(editor, meta.mPositionA, (pos) => { meta.mPositionA = pos; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Size", out editor);
			mUI.CreateVector2Editor(editor, meta.mPositionB, (size) => { meta.mPositionB = size; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Reveal FOW", out editor);
			mUI.CreateBoolEditor(editor, meta.mExtraBoolData, (value) => { meta.mExtraBoolData = value; _ModifyMetaObject(meta); });
		}
		else if (meta.mType == CLevelMetaObject.EType.DECAL)
		{
			mUI.CreateTextElement(_propsPanelContent, "Decal Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject editor;
			mUI.CreateFieldElement(_propsPanelContent, "Entity ID", out editor);
			mUI.CreateTextElement(editor, meta.mID.ToString());

			mUI.CreateFieldElement(_propsPanelContent, "Position", out editor);
			mUI.CreateVector3Editor(editor, meta.mPositionA, (pos) => { meta.mPositionA = pos; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, meta.mOrientation, (rot) => { meta.mOrientation = rot; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Size", out editor);
			mUI.CreateVector2Editor(editor, meta.mPositionB, (size) => { meta.mPositionB = size; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Type", out editor);
			mUI.CreateComboBox(editor, _GetDecalType(meta.mSubtype), _GetDecalTypeComboData, (string Name) => { meta.mSubtype = _GetDecalType(Name); _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Visibility", out editor);
			mUI.CreateComboBox(editor, _GetDecalVisType(meta.mExtraIntData), _GetDecalVisComboData, (string Name) => { meta.mExtraIntData = _GetDecalVisType(Name); _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Visual Index (Font/Image)", out editor);
			mUI.CreateIntEditor(editor, meta.mData, (value) => { meta.mData = value; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Text", out editor);
			mUI.CreateStringEditor(editor, meta.mIdentifier, (value) => { meta.mIdentifier = value; _ModifyMetaObject(meta); });

			mUI.CreateFieldElement(_propsPanelContent, "Colour", out editor);
			mUI.CreateColorEditor(editor, meta.mColor, (value) => {	meta.mColor = value; _ModifyMetaObject(meta); });
		}
	}

	private void _AddMetaObject(CLevelMetaObject Meta)
	{
		_asset.mObjects.Add(Meta);
		_ModifyMetaObject(Meta);
	}

	private void _RemoveMetaObject(int MetaId)
	{
		CLevelMetaObject meta = _asset.mObjects[MetaId];
		_asset.mObjects.Remove(meta);

		if (meta.mGOB)
			GameObject.Destroy(meta.mGOB);

		_ModifyAsset();
	}

	private void _ModifyMetaObject(CLevelMetaObject Meta, bool DeferAssetMod = false)
	{
		if (Meta.mGOB)
			GameObject.Destroy(Meta.mGOB);

		_BuildMetaObject(Meta);

		if (!DeferAssetMod)
			_ModifyAsset();
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
	}

	public override void Hide()
	{
		_camState = CGame.CameraManager.GetCamState();
		_sceneGraph.SetActive(false);
	}

	// TODO: This should really be a debug draw utility function.
	public void DrawGrid(int CellCount)
	{
		Color color = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		Color axisLineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);

		float y = 0.01f;

		for (int i = 0; i < CellCount + 1; ++i)
			CDebug.DrawLine(new Vector3(i, y, 0.0f), new Vector3(i, y, CellCount), color);

		for (int i = 0; i < CellCount + 1; ++i)
			CDebug.DrawLine(new Vector3(0.0f, y, i), new Vector3(CellCount, y, i), color);

		//CDebug.DrawLine(new Vector3(0.0f, 0.0f, -halfWidth - 0.5f), new Vector3(0.0f, 0.0f, halfWidth - 0.5f), axisLineColor);
		//CDebug.DrawLine(new Vector3(-halfWidth - 0.5f, 0.0f, 0.0f), new Vector3(halfWidth - 0.5f, 0.0f, 0.0f), axisLineColor);
	}

	private void _ModifyAsset()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.WARNING);
		_PushUndoState();
	}

	private void _PushUndoState()
	{
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		MemoryStream m = new MemoryStream();
		BinaryWriter w = new BinaryWriter(m);
		_asset.Serialize(w);
		sw.Stop();
		Debug.Log("Serialize: " + m.Length + "bytes in " + sw.Elapsed.TotalMilliseconds + "ms");

		if (_undoIndex < _undoStack.Count - 1)
			_undoStack.RemoveRange(_undoIndex + 1, _undoStack.Count - (_undoIndex + 1));

		_undoIndex = _undoStack.Count;
		m.Seek(0, SeekOrigin.Begin);
		_undoStack.Add(m);
		_UpdateUndoButtons();
	}

	private void _UpdateUndoButtons()
	{
		mUI.SetToolbarButtonEnabled(_undoButton, (_undoIndex > 0));
		mUI.SetToolbarButtonEnabled(_redoButton, (_undoIndex < _undoStack.Count - 1));
	}

	private void _Undo()
	{
		if (_undoIndex > 0)
		{
			--_undoIndex;
			_UpdateUndoState();
			_UpdateUndoButtons();
		}
	}

	private void _Redo()
	{
		if (_undoIndex < _undoStack.Count - 1)
		{
			++_undoIndex;
			_UpdateUndoState();
			_UpdateUndoButtons();
		}
	}

	private void _UpdateUndoState(bool UpdateCamState = true)
	{
		int selectedMetaID = -1;

		if (_selectedObject != -1)
		{
			selectedMetaID = _asset.mObjects[_selectedObject].mID;
			_DeselectObjects();
		}

		BinaryReader r = new BinaryReader(_undoStack[_undoIndex]);
		_asset.Deserialize(r);
		_undoStack[_undoIndex].Seek(0, SeekOrigin.Begin);
		
		_camState.mBackgroundColor = _asset.mBackgroundColor;

		if (UpdateCamState)
		{
			CGame.CameraManager.SetBackgroundColor(_asset.mBackgroundColor);
		}

		GameObject.Destroy(_sceneGraph);
		_sceneGraph = new GameObject("levelEditorRoot");

		if (_map != null)
			_map.Destroy();

		_map = new CMap();
		_asset.CreateMap(_map);
		_map.RebuildMesh();
		_map.GetLevelGOB().transform.SetParent(_sceneGraph.transform);
		_map.SetFloorAlwaysVisible(true);

		_PreparePlayers();

		int highestId = 0;
		for (int i = 0; i < _asset.mObjects.Count; ++i)
		{
			if (_asset.mObjects[i].mID > highestId)
				highestId = _asset.mObjects[i].mID;

			_BuildMetaObject(_asset.mObjects[i]);
		}
		
		CEntity.SetCurrentID(++highestId);

		if (selectedMetaID != -1)
		{
			for (int i = 0; i < _asset.mObjects.Count; ++i)
			{
				if (_asset.mObjects[i].mID == selectedMetaID)
					_SelectMetaObject(i);
			}
		}
		else
		{
			_CreateLevelProperties();
		}
	}

	private void _RebuildAllMetaObjects()
	{
		for (int i = 0; i < _asset.mObjects.Count; ++i)
		{
			if (_asset.mObjects[i].mGOB != null)
			{
				GameObject.Destroy(_asset.mObjects[i].mGOB);
				_asset.mObjects[i].mGOB = null;
			}

			_BuildMetaObject(_asset.mObjects[i]);
		}
	}

	private void _Save()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.NOTHING);
		_asset.Save();
	}

	private void _PlayLevel()
	{
		CGame.Game.LaunchProcess("-dev -map " + _asset.mName);
	}

	public override void Destroy()
	{
	}

	private void _OnViewportMouseDown(CameraView.EMouseButton Button)
	{
		if (Button == CameraView.EMouseButton.LEFT)
			_viewportMouseLeftDown = true;
		else if (Button == CameraView.EMouseButton.RIGHT)
			_viewportMouseRightDown = true;
	}

	private void _OnViewportMouseUp(CameraView.EMouseButton Button)
	{
		if (Button == CameraView.EMouseButton.LEFT)
			_viewportMouseLeftDown = false;
		else if (Button == CameraView.EMouseButton.RIGHT)
			_viewportMouseRightDown = false;
	}

	private Ray _GetViewportMouseRay()
	{	
		return Camera.main.ScreenPointToRay(_GetViewportMousePos());
	}

	private Vector2 _GetViewportMousePos()
	{
		return _viewport.GetComponent<CameraView>().mLocalMousePos;
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
		bool leftClick = false;
		_hoverObject = -1;

		if (_viewportPrevMouseLeftClick != _viewportMouseLeftDown)
		{
			_viewportPrevMouseLeftClick = _viewportMouseLeftDown;
			leftClick = _viewportMouseLeftDown;
			_mouseViewPosStart = _GetViewportMousePos();
			_mouseDragon = false;
		}

		if (_showGrid)
			DrawGrid(100);

		for (int i = 0; i < _asset.mObjects.Count; ++i)
		{
			CLevelMetaObject meta = _asset.mObjects[i];

			if (meta.mType == CLevelMetaObject.EType.VOLUME)
			{
				CDebug.DrawBorderQuads(meta.mPositionA, meta.mPositionB, Color.red, false);
			}
		}

		Ray mouseRay = _GetViewportMouseRay();

		Vector3 floorHitPoint;
		if (_IntersectFloor(mouseRay, out floorHitPoint))
		{
			//CDebug.DrawLine(floorHitPoint, floorHitPoint + Vector3.up, Color.white);

			if (leftClick)
				_mouseFloorHitStart = floorHitPoint;

			int tileX = (int)floorHitPoint.x;
			int tileY = (int)floorHitPoint.z;

			_floorHoverX = Mathf.Clamp(tileX, 0, 99);
			_floorHoverY = Mathf.Clamp(tileY, 0, 99);

			if (_tool == ETool.TILE)
				CDebug.DrawYSquare(new Vector3(tileX + 0.5f, 0.0f, tileY + 0.5f), 1.0f, Color.yellow, false);

			if (_tool == ETool.WALL)
			{
				if (!_wallLock)
				{
					Vector3 c1 = new Vector3(tileX + 0, 0.0f, tileY + 0);
					Vector3 c2 = new Vector3(tileX + 1, 0.0f, tileY + 0);
					Vector3 c3 = new Vector3(tileX + 1, 0.0f, tileY + 1);
					Vector3 c4 = new Vector3(tileX + 0, 0.0f, tileY + 1);

					Vector3 projPoint;
					float d1 = CIntersections.PointVsLine(floorHitPoint, c1, c2, out projPoint);
					float d2 = CIntersections.PointVsLine(floorHitPoint, c2, c3, out projPoint);
					float d3 = CIntersections.PointVsLine(floorHitPoint, c3, c4, out projPoint);
					float d4 = CIntersections.PointVsLine(floorHitPoint, c4, c1, out projPoint);

					if (d4 < d2 && d4 < d3 && d4 < d1)
					{
						CDebug.DrawLine(c4, c1, Color.yellow, false);

						_wallHoverTileX = _floorHoverX;
						_wallHoverTileY = _floorHoverY;
						_wallHoverDirection = 2;
					}
					else if (d2 < d1 && d2 < d3 && d2 < d4)
					{
						CDebug.DrawLine(c2, c3, Color.yellow, false);
						_wallHoverTileX = _floorHoverX + 1;
						_wallHoverTileY = _floorHoverY;
						_wallHoverDirection = 2;
					}
					else if (d3 < d2 && d3 < d1 && d3 < d4)
					{
						CDebug.DrawLine(c3, c4, Color.yellow, false);
						_wallHoverTileX = _floorHoverX;
						_wallHoverTileY = _floorHoverY + 1;
						_wallHoverDirection = 1;
					}
					else
					{
						CDebug.DrawLine(c1, c2, Color.yellow, false);
						_wallHoverTileX = _floorHoverX;
						_wallHoverTileY = _floorHoverY;
						_wallHoverDirection = 1;
					}
				}
				else
				{
					if (_wallHoverDirection == 1)
					{
						_wallHoverTileX = _floorHoverX;
					}
					else if (_wallHoverDirection == 2)
					{
						_wallHoverTileY = _floorHoverY;
					}
				}
			}

			if (_tool == ETool.ENTITY)
			{
				if (_entityPlacer != null)
				{
					if (InputState.GetCommand("itemPlaceRotate").mPressed)
						_entityPlacer.Rotate();

					_entityPlacer.SetPosition(floorHitPoint);
					_entityPlacer.Update();
				}
				else
				{
					if (!_mouseDragon)
					{
						_hoverObject = _IntersectMetaObjects(mouseRay);

						if (_hoverObject != -1)
						{
							_DrawMetaObjectHighlight(_hoverObject, Color.yellow);
						}
					}
				}

				if (_selectedObject != -1)
				{
					_DrawMetaObjectHighlight(_selectedObject, Color.white);
				}
			}
		}

		_wallLock = false;

		if (_viewportMouseLeftDown)
		{
			_OnClick(true, leftClick);
		}
		else if (_viewportMouseRightDown)
		{
			_OnClick(false, false);
		}
		else
		{
			if (_assetStartModify)
			{
				_assetStartModify = false;
				_ModifyAsset();
			}

			if (InputState.GetCommand("editorUndo").mPressed)
				_Undo();

			if (InputState.GetCommand("editorRedo").mPressed)
				_Redo();

			if (InputState.GetCommand("editorDelete").mPressed)
			{
				if (_selectedObject != -1)
				{
					_RemoveMetaObject(_selectedObject);
					_DeselectObjects();
				}
			}

			if (InputState.GetCommand("editorDuplicate").mPressed)
			{
				if (_selectedObject != -1)
				{
					_EndPlaceEntity();
					_DuplicateSelected();
				}
			}

			if (InputState.GetCommand("editorSave").mPressed)
			{
				_Save();
			}
		}

		if (_tool == ETool.ENTITY)
		{
			if (_selectedObject != -1)
			{
				if (_viewportMouseLeftDown && _entityPlacer == null)
				{
					if (!_mouseDragon)
					{
						Vector2 mouseD = _GetViewportMousePos() - _mouseViewPosStart;
						if (mouseD.magnitude >= 4.0f)
						{
							_mouseDragon = true;
							_metaDragStartPos = _asset.mObjects[_selectedObject].mPositionA;
						}
					}
					else
					{
						CLevelMetaObject meta = _asset.mObjects[_selectedObject];
						Vector3 floorHitD = floorHitPoint - _mouseFloorHitStart;

						if (meta.mType == CLevelMetaObject.EType.UNIT)
						{
							meta.mPositionA = _metaDragStartPos + new Vector3(floorHitD.x, floorHitD.z, 0.0f);
						}
						else if (meta.mType == CLevelMetaObject.EType.ITEM)
						{
							meta.mPositionA = _metaDragStartPos + new Vector3((int)floorHitD.x, (int)floorHitD.z, 0.0f);
						}
						else if (meta.mType == CLevelMetaObject.EType.VOLUME)
						{
							meta.mPositionA = _metaDragStartPos + floorHitD;
						}
						else if (meta.mType == CLevelMetaObject.EType.DECAL)
						{
							meta.mPositionA = _metaDragStartPos + floorHitD;
						}

						_assetStartModify = true;
						_ModifyMetaObject(meta, true);
					}
				}
			}
		}

		Vector3 camPos = CGame.CameraManager.GetCamTargetPosition();
		float camZoom = CGame.CameraManager.GetZoom();
		_viewHelpText.text = "Camera: " + camPos.x.ToString("0.0") + ", " + camPos.y.ToString("0.0") + ", " + camPos.z.ToString("0.0") + ", " + camZoom.ToString("0.0")
			+ "     Mouse Pos: " + floorHitPoint.x.ToString("0.0") + ", " + floorHitPoint.z.ToString("0.0");
	}

	private void _OnClick(bool Left, bool Click)
	{
		if (_tool == ETool.TILE)
		{
			if (Left)
			{
				_asset.mTileColors[_floorHoverX, _floorHoverY] = _toolColor;
				_map.mTiles[_floorHoverX, _floorHoverY].mTint = _toolColor;
				_map.RebuildMesh();
				_assetStartModify = true;
			}
			else
			{
				Color32 c = _asset.mTileColors[_floorHoverX, _floorHoverY];
				_toolColor = new Color(c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, 1.0f);

				if (_tileColourEditor != null)
					mUI.SetColorEditorValue(_tileColourEditor, _toolColor);
			}
		}
		else if (_tool == ETool.WALL)
		{
			_wallLock = true;

			int value = 0;

			if (Left)
			{
				if (_wallTool == EWallTool.WALL)
					value = 10;
				else if (_wallTool == EWallTool.DOOR)
					value = 100;
			}
			
			if (_wallHoverDirection == 1)
				_asset.mWallX[_wallHoverTileX, _wallHoverTileY] = value;
			else if (_wallHoverDirection == 2)
				_asset.mWallZ[_wallHoverTileX, _wallHoverTileY] = value;

			_map.SetTileRebuild(_wallHoverDirection, value, _wallHoverTileX, _wallHoverTileY);
			_assetStartModify = true;
		}
		else if (_tool == ETool.ENTITY)
		{
			if (Click)
			{
				if (_entityPlacer != null)
				{
					if (_entityPlacer.IsPlaceable())
					{
						int ownerId = _GetOwnerId(_defaultOwner);
						
						CGame.CameraManager.Shake();
						//CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[11]);
						CLevelMetaObject metaObject = new CLevelMetaObject();
						metaObject.mID = CEntity.GetNextID();
						metaObject.mOwner = ownerId;

						if (_entityPlacer.mType == CEntityPlacer.EPlaceType.ITEM)
						{
							metaObject.mType = CLevelMetaObject.EType.ITEM;
							metaObject.mIdentifier = _entityPlacer.mAsset.mName;
							metaObject.mPositionA = new Vector3(_entityPlacer.mX, _entityPlacer.mY, 0);
							metaObject.mRotation = _entityPlacer.mRotation;
						}
						else if (_entityPlacer.mType == CEntityPlacer.EPlaceType.UNIT)
						{
							metaObject.mType = CLevelMetaObject.EType.UNIT;
							metaObject.mPositionA = new Vector3(_entityPlacer.mPosition.x, _entityPlacer.mPosition.z, 0);
							metaObject.mRotation = _entityPlacer.mRotation;
							metaObject.mSubtype = 0;
							metaObject.mExtraIntData = 0;
						}
						else if (_entityPlacer.mType == CEntityPlacer.EPlaceType.VOLUME)
						{
							metaObject.mType = CLevelMetaObject.EType.VOLUME;
							metaObject.mPositionA = _entityPlacer.mPosition;
							metaObject.mPositionB = new Vector3(1.0f, 1.0f, 0.0f);
							metaObject.mExtraBoolData = false;
						}
						else if (_entityPlacer.mType == CEntityPlacer.EPlaceType.DECAL)
						{
							metaObject.mType = CLevelMetaObject.EType.DECAL;
							metaObject.mPositionA = _entityPlacer.mPosition;
							metaObject.mPositionA.y = 0.01f;
							metaObject.mPositionB = new Vector3(1.0f, 1.0f, 0.0f);
							metaObject.mIdentifier = "Decal";
							metaObject.mSubtype = (int)CDecal.EDecalType.IMAGE;
							metaObject.mExtraIntData = 0;
							metaObject.mOrientation = new Vector3(90, 0, 0);
							metaObject.mColor = new Color(1, 1, 1, 1);
							metaObject.mData = 0;
						}

						_AddMetaObject(metaObject);
					}
				}
				else
				{	
					_SelectMetaObject(_hoverObject);
				}
			}
			else if (!Left)
			{
				_EndPlaceEntity();
			}
		}
	}

	public override string GetTabName()
	{
		return "Level (" + _asset.mName + ")";
	}

	private void _SetTool(ETool Tool)
	{
		_EndPlaceEntity();
		_tool = Tool;
		_tileColourEditor = null;
		_selectedObject = -1;
		CUtility.DestroyChildren(_toolPanelContent);

		if (Tool == ETool.TILE)
		{
			mUI.CreateTextElement(_toolPanelContent, "Tile Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);
			mUI.CreateTextElement(_toolPanelContent, "(Right click on tile to pick its colour)", "text", CToolkitUI.ETextStyle.TS_DEFAULT);

			GameObject editor;
			mUI.CreateFieldElement(_toolPanelContent, "Tint Colour", out editor);
			_tileColourEditor = mUI.CreateColorEditor(editor, _toolColor, (Color color) => { _toolColor = color; });
		}
		else if (Tool == ETool.WALL)
		{
			mUI.CreateTextElement(_toolPanelContent, "Wall Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);
			mUI.CreateTextElement(_toolPanelContent, "(Right click on wall to remove it)", "text", CToolkitUI.ETextStyle.TS_DEFAULT);

			GameObject toolbar = mUI.CreateElement(_toolPanelContent, "toolbarView");
			mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
			mUI.AddImage(toolbar, mUI.WindowPanelBackground);
			mUI.AddHorizontalLayout(toolbar);
			toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

			_tbbWallNormal = mUI.CreateToolbarButton(toolbar, "Normal", mUI.LevelImage, () =>
			{
				_wallTool = EWallTool.WALL;
				mUI.SetToolbarButtonHighlight(_tbbWallNormal, CToolkitUI.EButtonHighlight.SELECTED);
				mUI.SetToolbarButtonHighlight(_tbbWallDoor, CToolkitUI.EButtonHighlight.NOTHING);
			});

			_tbbWallDoor = mUI.CreateToolbarButton(toolbar, "Door", mUI.LevelImage, () =>
			{
				_wallTool = EWallTool.DOOR;
				mUI.SetToolbarButtonHighlight(_tbbWallNormal, CToolkitUI.EButtonHighlight.NOTHING);
				mUI.SetToolbarButtonHighlight(_tbbWallDoor, CToolkitUI.EButtonHighlight.SELECTED);
			});

			mUI.SetToolbarButtonHighlight(_tbbWallNormal, CToolkitUI.EButtonHighlight.SELECTED);

			_wallTool = EWallTool.WALL;
		}
		else if (Tool == ETool.ENTITY)
		{
			mUI.CreateTextElement(_toolPanelContent, "Entity Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject defaultOwnerCombo;
			mUI.CreateFieldElement(_toolPanelContent, "Default Owner", out defaultOwnerCombo);
			mUI.CreateComboBox(defaultOwnerCombo, _defaultOwner, _GetOwnerComboData, (string Name) => { _defaultOwner = Name; });

			GameObject scrollContent;
			GameObject scrollV1 = mUI.CreateScrollView(_toolPanelContent, out scrollContent, true);
			mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);

			CTUITreeView treeView;
			GameObject treeViewGob = mUI.CreateTreeView(scrollContent, out treeView);
			mUI.AddLayout(treeViewGob, -1, -1, 1.0f, 1.0f);

			CTUITreeViewItem item = treeView.mRootItem.AddItem("Entities");
			item.mExpanded = true;

			CTUITreeViewItem itemItems = item.AddItem("Items");
			itemItems.mExpanded = true;

			List<CItemAsset> items = CGame.AssetManager.GetAllItemAssets();

			CTUITreeViewItem itemCat = itemItems.AddItem("Start");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.START)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Decorations");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.DECO)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Workspaces");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.DESK)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Storage");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.SAFE)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Sustenance");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.FOOD)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Relaxation");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.REST)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			itemCat = itemItems.AddItem("Security");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].mItemType == EItemType.DOOR || items[i].mItemType == EItemType.CAMERA)
				{
					string assetName = items[i].mName;
					itemCat.AddItem(items[i].mFriendlyName, () => { _BeginPlaceEntity(assetName); });
				}
			}

			item.AddItem("Unit", _BeginPlaceUnit);
			//item.AddItem("Pickup", _BeginPlacePickup);
			item.AddItem("Volume", _BeginPlaceVolume);
			item.AddItem("Decal", _BeginPlaceDecal);

			treeView.Rebuild();
		}
	}

	private void _BeginPlaceEntity(string AssetName)
	{
		_EndPlaceEntity();
		_entityPlacer = new CEntityPlacer(AssetName, _sceneGraph.transform, null);
	}

	private void _BeginPlaceUnit()
	{
		_EndPlaceEntity();
		_entityPlacer = new CEntityPlacer(CEntityPlacer.EPlaceType.UNIT, _sceneGraph.transform, null);
	}

	private void _BeginPlacePickup()
	{
		_EndPlaceEntity();
		_entityPlacer = new CEntityPlacer(CEntityPlacer.EPlaceType.PICKUP, _sceneGraph.transform, null);
	}

	private void _BeginPlaceVolume()
	{
		_EndPlaceEntity();
		_entityPlacer = new CEntityPlacer(CEntityPlacer.EPlaceType.VOLUME, _sceneGraph.transform, null);
	}

	private void _BeginPlaceDecal()
	{
		_EndPlaceEntity();
		_entityPlacer = new CEntityPlacer(CEntityPlacer.EPlaceType.DECAL, _sceneGraph.transform, null);
	}

	private void _EndPlaceEntity()
	{
		if (_entityPlacer != null)
		{
			_entityPlacer.Destroy();
			_entityPlacer = null;
		}
	}

	private void _DeselectObjects()
	{
		_selectedObject = -1;
		_CreateLevelProperties();
	}

	private void _DuplicateSelected()
	{
		if (_selectedObject == -1)
			return;

		CLevelMetaObject newMeta = new CLevelMetaObject(_asset.mObjects[_selectedObject], CEntity.GetNextID());

		if (newMeta.mType == CLevelMetaObject.EType.UNIT || newMeta.mType == CLevelMetaObject.EType.ITEM)
			newMeta.mPositionA += new Vector3(1, 1, 0);
		else if (newMeta.mType == CLevelMetaObject.EType.VOLUME || newMeta.mType == CLevelMetaObject.EType.DECAL)
			newMeta.mPositionA += new Vector3(1, 0, 1);

		_AddMetaObject(newMeta);
		_SelectMetaObject(_asset.mObjects.Count - 1);
	}

	private void _CreateLevelProperties()
	{
		CUtility.DestroyChildren(_propsPanelContent);
		
		mUI.CreateTextElement(_propsPanelContent, "Level Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject content;
		mUI.CreateFoldOut(_propsPanelContent, "General", out content);

		GameObject editor;
		mUI.CreateFieldElement(content, "Background Colour", out editor);
		mUI.CreateColorEditor(editor, _asset.mBackgroundColor, (Color color) => 
		{
			_asset.mBackgroundColor = color;
			CGame.CameraManager.SetBackgroundColor(color);
			_ModifyAsset();
		});

		GameObject btn = mUI.CreateButton(_propsPanelContent, "Reset Player Info", () => { _PreparePlayers(true); _ModifyAsset(); _CreateLevelProperties(); _RebuildAllMetaObjects(); });
		mUI.AddLayout(btn, -1, 20, 1.0f, -1);

		for (int i = 0; i < CWorld.MAX_PLAYERS; ++i)
		{
			int playerId = i;
			CLevelMetaObject meta = _playerMetas[i];

			mUI.CreateFoldOut(_propsPanelContent, mPlayerIDs[i], out content, false);

			mUI.CreateFieldElement(content, "Name", out editor);
			mUI.CreateStringEditor(editor, _playerMetas[playerId].mIdentifier, (value) => { _playerMetas[playerId].mIdentifier = value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Playable", out editor);
			mUI.CreateBoolEditor(editor, (meta.mExtraIntData & CPlayer.FLAG_PLAYABLE) != 0, (value) => { meta.mExtraIntData = CUtility.SetFlag(meta.mExtraIntData, CPlayer.FLAG_PLAYABLE, value);  _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Competing", out editor);
			mUI.CreateBoolEditor(editor, (meta.mExtraIntData & CPlayer.FLAG_COMPETING) != 0, (value) => { meta.mExtraIntData = CUtility.SetFlag(meta.mExtraIntData, CPlayer.FLAG_COMPETING, value); _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Colour", out editor);
			mUI.CreateColorEditor(editor, meta.mColor, (value) => { meta.mColor = value; _ModifyAsset(); _RebuildAllMetaObjects(); });

			mUI.CreateFieldElement(content, "Allegiance", out editor);

			GameObject hl = mUI.CreateElement(editor, "horz");
			mUI.AddLayout(hl, -1, -1, 1.0f, -1);
			mUI.AddHorizontalLayout(hl);
			hl.GetComponent<HorizontalLayoutGroup>().spacing = 4.0f;

			for (int j = 0; j < CWorld.MAX_PLAYERS; ++j)
			{
				mUI.CreateTextElement(hl, j.ToString());
				int otherPlayerId = j;
				GameObject check = mUI.CreateBoolEditor(hl, CUtility.CheckBit(meta.mData, j), (value) => { meta.mData = CUtility.SetBit(meta.mData, otherPlayerId, value); _ModifyAsset(); });

				if (j == i)
					check.GetComponent<Toggle>().interactable = false;
			}
		}
	}

	private List<string> _GetOwnerComboData()
	{
		List<string> result = new List<string>();

		for (int i = 0; i < mPlayerIDs.Length; ++i)
			result.Add(mPlayerIDs[i]);

		return result;
	}

	private int _GetOwnerId(string Owner)
	{
		for (int i = 0; i < mPlayerIDs.Length; ++i)
			if (mPlayerIDs[i] == Owner)
				return i;

		return -1;
	}

	private string _GetOwnerId(int Owner)
	{
		return mPlayerIDs[Owner];
	}

	private List<string> _GetDecalTypeComboData()
	{
		List<string> result = new List<string>();
		string[] types = Enum.GetNames(typeof(CDecal.EDecalType));

		for (int i = 0; i < types.Length; ++i)
			result.Add(types[i]);

		return result;
	}

	private int _GetDecalType(string Value)
	{
		string[] types = Enum.GetNames(typeof(CDecal.EDecalType));

		for (int i = 0; i < types.Length; ++i)
			if (types[i] == Value)
				return i;

		return -1;
	}

	private string _GetDecalType(int Value)
	{
		return ((CDecal.EDecalType)Value).ToString();
	}

	private List<string> _GetDecalVisComboData()
	{
		List<string> result = new List<string>();
		string[] types = Enum.GetNames(typeof(CDecal.EDecalVis));

		for (int i = 0; i < types.Length; ++i)
			result.Add(types[i]);

		return result;
	}

	private int _GetDecalVisType(string Value)
	{
		string[] types = Enum.GetNames(typeof(CDecal.EDecalVis));

		for (int i = 0; i < types.Length; ++i)
			if (types[i] == Value)
				return i;

		return -1;
	}

	private string _GetDecalVisType(int Value)
	{
		return ((CDecal.EDecalVis)Value).ToString();
	}

	private string _GetUnitTierType(int Value)
	{
		CUnitRules rules = CGame.AssetManager.mUnitRules;

		if (Value >= rules.mTiers.Count)
			Value = 0;

		return rules.mTiers[Value].mTitle;
	}

	private int _GetUnitTierType(string Value)
	{
		CUnitRules rules = CGame.AssetManager.mUnitRules;

		for (int i = 0; i < rules.mTiers.Count; ++i)
			if (rules.mTiers[i].mTitle == Value)
				return i;

		return -1;
	}

	private List<string> _GetUnitTierComboData()
	{
		List<string> result = new List<string>();
		CUnitRules rules = CGame.AssetManager.mUnitRules;

		for (int i = 0; i < rules.mTiers.Count; ++i)
			result.Add(rules.mTiers[i].mTitle);

		return result;
	}
}
