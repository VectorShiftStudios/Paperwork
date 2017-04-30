using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CItemEditor : CAssetToolkitWindow
{
	public static string[] ITEM_TYPE_NAMES = { "None", "Start", "Deco", "Desk", "Safe", "Rest", "Food", "Door", "Camera" };

	private CItemAsset _asset;
	private GameObject _sceneGraph;
	private GameObject _helpers;
	private GameObject _itemRoot;
	private GameObject _tileIcons;
	private GameObject _viewport;
	private SCameraState _camState;
	
	private GameObject _tbbSave;
	private GameObject _tbbShowGrid;

	private GameObject _toolPanelContent;
	private GameObject _componentContent;
	private GameObject _propViewContent;

	private GameObject[] _tbbView = new GameObject[4];

	private bool _showGrid = true;

	private int _hoverTileX;
	private int _hoverTileY;

	private int _selectedTileX = -1;
	private int _selectedTileY = -1;

	private bool _viewportMouseDown;
	private bool _prevMouseDown;

	private EViewDirection _viewDirection;

	// Cell types:
	// Rather have flags or just more complex cell structure?
	// free - no effect
	// solid - full block solid for interaction
	// low - low block for interaction
	// select - can be selected but is not solid to units
	// entryN - represents a tile that leads to a usage node on the item

	// Item interaction is per polygon, so solid/low etc only for combat projectiles?
	// Collision objects for projectiles, particles?

	// Usage slot:
	// Determines if a unit is using the item.
	// Describes where a unit should be visually.
	// Turn into general attach node?

	public CItemEditor(string AssetName)
	{
		mAssetName = AssetName;
		_asset = CGame.AssetManager.GetAsset<CItemAsset>(AssetName);
	}

	public override void Init(CAssetToolkit Toolkit)
	{
		base.Init(Toolkit);

		// NOTE: Must assign new icon ASAP before UI elemnts are created that reference it.
		// TODO: Is this needed??
		//CGame.IconBuilder.RebuildItemIcons();

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
		_tbbShowGrid = mUI.CreateToolbarButton(toolbar, "Show Helpers", mUI.BrushImage, () =>
		{
			_showGrid = !_showGrid;
			mUI.SetToolbarButtonHighlight(_tbbShowGrid, _showGrid ? CToolkitUI.EButtonHighlight.SELECTED : CToolkitUI.EButtonHighlight.NOTHING);
		});
		mUI.SetToolbarButtonHighlight(_tbbShowGrid, CToolkitUI.EButtonHighlight.SELECTED);

		mUI.CreateToolbarSeparator(toolbar);
		mUI.CreateToolbarButton(toolbar, "Properties", mUI.SheetImage, () =>
		{
			_SelectTile(-1, -1);
			_RebuildItemPropertiesView();
		});

		//mUI.CreateToolbarSeparator(toolbar);
		//mUI.CreateToolbarButton(toolbar, "Tiles", mUI.SaveImage);
		//mUI.CreateToolbarButton(toolbar, "Move", mUI.SaveImage);
		//mUI.SetToolbarButtonHighlight(_tbbShowGrid, CToolkitUI.EButtonHighlight.SELECTED);

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
		GameObject w1p = mUI.CreateWindowPanel(w1, out _toolPanelContent, "Components");
		mUI.AddLayout(w1p, -1, -1, 1.0f, 1.0f);

		mUI.CreateTextElement(_toolPanelContent, "View Direction", "text", CToolkitUI.ETextStyle.TS_HEADING);

		toolbar = mUI.CreateElement(_toolPanelContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		_tbbView[0] = mUI.CreateToolbarButton(toolbar, "Front", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_FRONT));
		_tbbView[1] = mUI.CreateToolbarButton(toolbar, "Right", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_RIGHT));
		_tbbView[2] = mUI.CreateToolbarButton(toolbar, "Back", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_BACK));
		_tbbView[3] = mUI.CreateToolbarButton(toolbar, "Left", mUI.LevelImage, () => _OnClickEdgeSetView(EViewDirection.VD_LEFT));
		mUI.SetToolbarButtonHighlight(_tbbView[0], CToolkitUI.EButtonHighlight.SELECTED);

		_componentContent = mUI.CreateElement(_toolPanelContent);
		mUI.AddVerticalLayout(_componentContent);
		
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

		// Split 3
		GameObject w3pContent;
		GameObject w3p = mUI.CreateWindowPanel(w3, out w3pContent, "Properties");
		mUI.AddLayout(w3p, -1, -1, 1.0f, 1.0f);
		
		GameObject scrollV2 = mUI.CreateScrollView(w3pContent, out _propViewContent, true);
		mUI.AddLayout(scrollV2, -1, -1, 1.0f, 1.0f);
		_propViewContent.GetComponent<VerticalLayoutGroup>().spacing = 4;

		// World Scene
		_sceneGraph = new GameObject("itemEditorRoot");
		_sceneGraph.SetActive(false);

		_helpers = new GameObject("itemEditorHelpers");
		_helpers.transform.SetParent(_sceneGraph.transform);

		_itemRoot = new GameObject("itemRoot");
		_itemRoot.transform.SetParent(_sceneGraph.transform);

		_tileIcons = new GameObject("itemTileIcons");
		_tileIcons.transform.SetParent(_helpers.transform);

		_camState.mBackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1.0f);
		_camState.SetViewGame(EViewDirection.VD_FRONT);

		_RebuildComponentView();
		_RebuildItemPropertiesView();

		GameObject sceneText = _CreateSceneText(_helpers, "Width");
		sceneText.transform.position = new Vector3(0.5f, 0.0f, 0.0f);
		sceneText.transform.rotation = Quaternion.Euler(90, 0, 0);

		sceneText = _CreateSceneText(_helpers, "Length");
		sceneText.transform.position = new Vector3(0.0f, 0.0f, 0.5f);
		sceneText.transform.rotation = Quaternion.Euler(90, 90, 0);
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
		_RefreshGOB();
	}

	public override void Hide()
	{
		_camState = CGame.CameraManager.GetCamState();
		_sceneGraph.SetActive(false);
	}

	private void _RefreshGOB()
	{
		// Call modify here?
		// After any changes, current item state.
		// Created directly from asset, then additions added depending on type.
		// Take view direction into account.

		CUtility.DestroyChildren(_itemRoot);
		GameObject gob = _asset.CreateVisuals(EViewDirection.VD_FRONT);
		gob.transform.SetParent(_itemRoot.transform);

		// Create usage slot visuals
		// Create specific visuals for items that use extended models?

		if (_asset.mItemType == EItemType.DESK || _asset.mItemType == EItemType.REST)
		{
			for (int i = 0; i < _asset.mUsageSlots.Count; ++i)
			{
				GameObject unit = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7] as GameObject);
				unit.transform.SetParent(_itemRoot.transform);
				unit.transform.localPosition = _asset.mUsageSlots[i].mUsePosition;
				unit.transform.rotation = Quaternion.Euler(_asset.mUsageSlots[i].mUseRotation);
				Animator _animator = unit.transform.GetChild(0).GetComponent<Animator>();
				_animator.Play("work_working_action_2");
			}
		}

		_RebuildTileIcons();
	}

	private void _DrawGrid(int CellCount)
	{
		float cellSize = 0.5f;
		float cellCount = (float)CellCount;
		float totalSize = cellSize * cellCount;
		float halfSize = totalSize * 0.5f;

		Color color = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		Color tileColor = new Color(0.27f, 0.27f, 0.27f, 1.0f);
		Color axisLineColor = new Color(0.34f, 0.34f, 0.34f, 1.0f);

		for (int i = 0; i < cellCount + 1; ++i)
		{
			Color c = color;

			if (i == cellCount / 2)
				c = axisLineColor;
			else if (i % 2 == 0)
				c = tileColor;

			CDebug.DrawLine(new Vector3(i * cellSize - halfSize, 0.0f, -halfSize), new Vector3(i * cellSize - halfSize, 0.0f, halfSize), c);
			CDebug.DrawLine(new Vector3(-halfSize, 0.0f, i * cellSize - halfSize), new Vector3(halfSize, 0.0f, i * cellSize - halfSize), c);
		}
	}

	private void _ModifyAsset()
	{
		_RefreshGOB();
		CGame.IconBuilder.RebuildItemIcons(true);
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.WARNING);
	}

	private void _Save()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.NOTHING);
		_asset.Save();
	}

	public override void Destroy()
	{
	}

	private void _OnViewportMouseDown(CameraView.EMouseButton Button)
	{
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

	private void _RebuildTileIcons()
	{
		CUtility.DestroyChildren(_tileIcons);

		for (int iX = 0; iX < _asset.mWidth * 2; ++iX)
			for (int iY = 0; iY < _asset.mLength * 2; ++iY)
			{
				if (_asset.mTiles[iX, iY].mSolid)
				{
					GameObject gob = GameObject.CreatePrimitive(PrimitiveType.Quad);
					//GameObject gob = new GameObject();
					gob.transform.SetParent(_tileIcons.transform);
					gob.transform.localPosition = new Vector3(iX * 0.5f + 0.25f, 0.0f, iY * 0.5f + 0.25f);
					gob.transform.rotation = Quaternion.Euler(90, 0, 0);
					gob.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
					gob.GetComponent<MeshRenderer>().material = CGame.WorldResources.TileIconSolid;
				}
			}


		for (int i = 0; i < _asset.mUsageSlots.Count; ++i)
		{
			CItemAsset.CUsageSlot s = _asset.mUsageSlots[i];

			// Entry point tile hits
			Vector3 size = s.mEntrySize;
			Vector3 halfExtents = size * 0.5f;
			Vector3 min = s.mEntryPosition - halfExtents;
			Vector3 max = s.mEntryPosition + halfExtents;

			// Search for tiles within bounds
			int sX = (int)(min.x * 2.0f) - 1;
			int sY = (int)(min.z * 2.0f) - 1;
			int eX = (int)(max.x * 2.0f);
			int eY = (int)(max.z * 2.0f);

			for (int iX = sX; iX <= eX; ++iX)
				for (int iY = sY; iY <= eY; ++iY)
				{
					Vector3 origin = new Vector3(iX * 0.5f + 0.25f, -0.01f, iY * 0.5f + 0.25f);

					if (origin.x >= min.x && origin.x <= max.x && origin.z >= min.z && origin.z <= max.z)
					{
						CDebug.DrawYSquare(origin, 0.45f, Color.blue, true);
						GameObject gob = GameObject.CreatePrimitive(PrimitiveType.Quad);
						//GameObject gob = new GameObject();
						gob.transform.SetParent(_tileIcons.transform);
						gob.transform.localPosition = origin;
						gob.transform.rotation = Quaternion.Euler(90, 0, 0);
						gob.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
						gob.GetComponent<MeshRenderer>().material = CGame.WorldResources.TileIconTrigger;
					}
				}
		}
	}

	public override void Update(CInputState InputState)
	{
		if (_showGrid)
		{
			_DrawGrid(20);

			Vector3 c1 = new Vector3(0.0f, 0.01f, 0.0f);
			Vector3 c2 = new Vector3(0.0f, 0.01f, _asset.mLength);
			Vector3 c3 = new Vector3(_asset.mWidth, 0.01f, _asset.mLength);
			Vector3 c4 = new Vector3(_asset.mWidth, 0.01f, 0.0f);

			CDebug.DrawLine(c1, c2, Color.white);
			CDebug.DrawLine(c2, c3, Color.white);
			CDebug.DrawLine(c3, c4, Color.white);
			CDebug.DrawLine(c4, c1, Color.white);

			for (int i = 0; i < _asset.mUsageSlots.Count; ++i)
			{
				CItemAsset.CUsageSlot s = _asset.mUsageSlots[i];
				CDebug.DrawYSquare(s.mEntryPosition, 0.2f, Color.blue, false);

				// Entry point tile hits
				Vector3 size = s.mEntrySize;
				CDebug.DrawYRect(s.mEntryPosition, size.x, size.z, Color.cyan, true);

				/*
				Vector3 halfExtents = size * 0.5f;
				Vector3 min = s.mEntryPosition - halfExtents;
				Vector3 max = s.mEntryPosition + halfExtents;

				// Search for tiles within bounds
				int sX = (int)(min.x * 2.0f) - 1;
				int sY = (int)(min.z * 2.0f) - 1;
				int eX = (int)(max.x * 2.0f);
				int eY = (int)(max.z * 2.0f);

				for (int iX = sX; iX <= eX; ++iX)
					for (int iY = sY; iY <= eY; ++iY)
					{
						Vector3 origin = new Vector3(iX * 0.5f + 0.25f, 0.0f, iY * 0.5f + 0.25f);

						if (origin.x >= min.x && origin.x <= max.x && origin.z >= min.z && origin.z <= max.z)
							CDebug.DrawYSquare(origin, 0.45f, Color.blue, true);
					}
				*/

				CDebug.DrawLine(s.mEntryPosition, s.mUsePosition, Color.cyan, false);
				CDebug.DrawYSquare(s.mUsePosition, 0.2f, Color.green, false);
			}
		}

		_helpers.SetActive(_showGrid);

		if (_selectedTileX != -1 && _selectedTileY != -1)
			CDebug.DrawYSquare(new Vector3(_selectedTileX * 0.5f + 0.25f, 0.0f, _selectedTileY * 0.5f + 0.25f), 0.5f, Color.green, false);

		Ray mouseRay = _GetViewportMouseRay();

		Vector3 floorHitPoint;
		if (_IntersectFloor(mouseRay, out floorHitPoint))
		{
			//CDebug.DrawLine(floorHitPoint, floorHitPoint + Vector3.up, Color.white);

			_hoverTileX = -1;
			_hoverTileY = -1;

			if (floorHitPoint.x >= 0.0f && floorHitPoint.z >= 0.0f)
			{
				int tileX = (int)(floorHitPoint.x * 2.0f);
				int tileY = (int)(floorHitPoint.z * 2.0f);

				if ((tileX >= 0 && tileX < _asset.mWidth * 2) &&
					(tileY >= 0 && tileY < _asset.mLength * 2))
				{
					_hoverTileX = tileX;
					_hoverTileY = tileY;

					CDebug.DrawYSquare(new Vector3(tileX * 0.5f + 0.25f, 0.0f, tileY * 0.5f + 0.25f), 0.5f, Color.yellow, false);
				}
			}
		}

		if (_viewportMouseDown && !_prevMouseDown)
		{
			_prevMouseDown = _viewportMouseDown;

			_SelectTile(_hoverTileX, _hoverTileY);

			if (_hoverTileX == -1 || _hoverTileY == -1)
				_RebuildItemPropertiesView();
		}
		else if (!_viewportMouseDown && _prevMouseDown)
		{
			_prevMouseDown = _viewportMouseDown;
		}
	}

	public override string GetTabName()
	{
		return "Item (" + _asset.mName + ")";
	}

	private void _SelectTile(int X, int Y)
	{
		CUtility.DestroyChildren(_propViewContent);

		_selectedTileX = X;
		_selectedTileY = Y;

		if (X == -1 || Y == -1)
			return;
		
		mUI.CreateTextElement(_propViewContent, "Tile: " + X + ", " + Y, "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject content;
		GameObject editor;
		mUI.CreateFoldOut(_propViewContent, "Tile Properties", out content);

		int solidVal = _asset.mTiles[X, Y].mSolid ? 1 : 0;

		mUI.CreateFieldElement(content, "Solid", out editor);
		mUI.CreateIntEditor(editor, solidVal, (int Value) =>
		{
			_asset.mTiles[X, Y].mSolid = Value == 0 ? false : true;
			_ModifyAsset();
		});
	}

	/*
	private void _SetTool(ETool Tool)
	{
		_tool = Tool;

		CUtility.DestroyChildren(_toolPanelContent);

		if (Tool == ETool.TILE)
		{
			mUI.CreateTextElement(_toolPanelContent, "Tile Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject editor;
			mUI.CreateFieldElement(_toolPanelContent, "Tint Colour", out editor);
			mUI.CreateColorEditor(editor, _toolColor, (Color color) => { _toolColor = color; });
		}
		else if (Tool == ETool.WALL)
		{
			mUI.CreateTextElement(_toolPanelContent, "Wall Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);

			GameObject toolbar = mUI.CreateElement(_toolPanelContent, "toolbarView");
			mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
			mUI.AddImage(toolbar, mUI.WindowPanelBackground);
			mUI.AddHorizontalLayout(toolbar);
			toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

			GameObject _tbbWallTypeNormal = mUI.CreateToolbarButton(toolbar, "Normal", mUI.LevelImage);
			mUI.SetToolbarButtonHighlight(_tbbWallTypeNormal, CToolkitUI.EButtonHighlight.SELECTED);
			mUI.CreateToolbarButton(toolbar, "Door", mUI.LevelImage);
			mUI.CreateToolbarButton(toolbar, "None", mUI.LevelImage);
		}
		else if (Tool == ETool.ENTITY)
		{
			mUI.CreateTextElement(_toolPanelContent, "Entity Tool", "text", CToolkitUI.ETextStyle.TS_HEADING);
			
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
			itemItems.AddItem("Start");
			itemItems.AddItem("Decorations");
			itemItems.AddItem("Desk");
			itemItems.AddItem("Food");
			itemItems.AddItem("Rest");
			itemItems.AddItem("Other");

			item.AddItem("Units");
			item.AddItem("Pickups");
			item.AddItem("Triggers");

			treeView.Rebuild();
		}
	}
	*/

	private List<string> _GetModelsComboData()
	{
		return CGame.AssetManager.GetAllAssetNames(EAssetType.AT_MODEL);
	}

	private List<string> _GetTypeComboData()
	{
		List<string> data = new List<string>();

		for (int i = 1; i < ITEM_TYPE_NAMES.Length; ++i)
			data.Add(ITEM_TYPE_NAMES[i]);

		return data;
	}

	private string _GetItemTypeString(EItemType Type)
	{
		return ITEM_TYPE_NAMES[(int)Type];
	}

	private void _ChangeType(string Type)
	{
		EItemType type = EItemType.NONE;

		for (int i = 0; i < ITEM_TYPE_NAMES.Length; ++i)
		{
			if (ITEM_TYPE_NAMES[i] == Type)
			{
				type = (EItemType)i;
				break;
			}
		}

		_asset.mItemType = type;

		if (type == EItemType.DESK)
		{
			_asset.mUsageSlots.Clear();
			_asset.mUsageSlots.Add(new CItemAsset.CUsageSlot());
		}
		else
		{
			_asset.mUsageSlots.Clear();
		}

		// Update model stuff?
		// Desk uses 2 model slots
		// 0 = Desk
		// 1 = Chair
		// 2 = PaperIn
		// 3 = PaperOut

		_ModifyAsset();
		_RebuildComponentView();
		_RebuildItemPropertiesView();
	}

	private string _GetPrimaryModelName(CModelAsset Model)
	{
		if (Model != null)
		{
			return Model.mName;
		}

		return "(None)";
	}

	private void _ChangeModel(ref CModelAsset Model, string Name)
	{
		Model = CGame.AssetManager.GetAsset<CModelAsset>(Name);
		_ModifyAsset();
	}

	private void _AdjustBaseSize(int Width, int Length)
	{
		CItemAsset.STile[,] newTiles = new CItemAsset.STile[Width * 2, Length * 2];

		int widthOverlap = Mathf.Min(Width * 2, _asset.mWidth * 2);
		int lengthOverlap = Mathf.Min(Length * 2, _asset.mLength * 2);

		for (int iX = 0; iX < widthOverlap; ++iX)
			for (int iY = 0; iY < lengthOverlap; ++iY)
			{
				newTiles[iX, iY] = _asset.mTiles[iX, iY];
			}

		_asset.mTiles = newTiles;
		_asset.mWidth = Width;
		_asset.mLength = Length;
	}

	private void _RebuildComponentView()
	{
		CUtility.DestroyChildren(_componentContent);

		/*
		mUI.CreateTextElement(_componentContent, "Item Components", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject toolbar = mUI.CreateElement(_componentContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		mUI.CreateToolbarButton(toolbar, "Add Model", mUI.LevelImage, _AddModel);
		mUI.CreateToolbarButton(toolbar, "Add Slot", mUI.LevelImage);
		mUI.CreateToolbarButton(toolbar, "Duplicate", mUI.LevelImage);
		mUI.CreateToolbarButton(toolbar, "Remove", mUI.LevelImage);

		GameObject scrollContent;
		GameObject scrollV1 = mUI.CreateScrollView(_componentContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);

		CTUITreeView treeView;
		GameObject treeViewGob = mUI.CreateTreeView(scrollContent, out treeView);
		mUI.AddLayout(treeViewGob, -1, -1, 1.0f, 1.0f);

		CTUITreeViewItem tvCat = treeView.mRootItem.AddItem("Models");
		tvCat.mExpanded = true;
		for (int i = 0; i < _asset.mModels.Count; ++i)
		{
			int index = i;
			tvCat.AddItem("Model " + i.ToString(), () => { _SelectModel(index); });
		}

		tvCat = treeView.mRootItem.AddItem("Slots");
		tvCat.mExpanded = true;
		for (int i = 0; i < _asset.mUsageSlots.Count; ++i)
		{
			CTUITreeViewItem tvSlot = tvCat.AddItem("Slot " + i.ToString());
			tvSlot.mExpanded = true;

			for (int j = 0; j < _asset.mUsageSlots[i].mEntrySlots.Count; ++j)
			{
				tvSlot.AddItem("Entry " + j);
			}
		}

		tvCat = treeView.mRootItem.AddItem("Item Specific");

		if (_asset.mItemType == EItemType.DESK)
		{
			tvCat.AddItem("Chair");
		}

		tvCat.RebuildEntireTree();
		*/
	}

	private void _RebuildItemPropertiesView()
	{
		CUtility.DestroyChildren(_propViewContent);

		mUI.CreateTextElement(_propViewContent, "Item Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject content;
		GameObject editor;
		mUI.CreateFoldOut(_propViewContent, "Generic Item Properties", out content);

		mUI.CreateFieldElement(content, "Type", out editor);
		mUI.CreateComboBox(editor, _GetItemTypeString(_asset.mItemType), _GetTypeComboData, (string Name) => { _ChangeType(Name); });

		mUI.CreateFieldElement(content, "Name", out editor);
		mUI.CreateStringEditor(editor, _asset.mFriendlyName, (string Text) => { _asset.mFriendlyName = Text; _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Flavour Text", out editor);
		mUI.CreateStringEditor(editor, _asset.mFlavourText, (string Text) => { _asset.mFlavourText = Text; _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Width", out editor);
		mUI.CreateIntEditor(editor, _asset.mWidth, (int Value) => { _AdjustBaseSize(Value, _asset.mLength); _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Length", out editor);
		mUI.CreateIntEditor(editor, _asset.mLength, (int Value) => { _AdjustBaseSize(_asset.mWidth, Value); _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Durability", out editor);
		mUI.CreateIntEditor(editor, _asset.mDurability, (int Value) => { _asset.mDurability = Value; _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Cost", out editor);
		mUI.CreateIntEditor(editor, _asset.mCost, (int Value) => { _asset.mCost = Value; _ModifyAsset(); });

		// Icon
		{
			mUI.CreateFoldOut(_propViewContent, "Icon", out content, false);

			//mUI.CreateFieldElement(content, "Model", out editor);
			//mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "X, Y, Zoom", out editor);
			mUI.CreateVector3Editor(editor, _asset.mIconCameraPostion, (Vector3 vec) => { _asset.mIconCameraPostion = vec; _ModifyAsset(); });

			//mUI.CreateFieldElement(content, "Direction", out editor);
			//mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });

			//mUI.CreateFieldElement(content, "Model", out editor);

			GameObject icon = mUI.CreateElement(content, "icon");
			mUI.AddImage(icon, new Color(0.2f, 0.2f, 0.2f, 1.0f));
			mUI.AddLayout(icon, 54, 54, -1, -1);

			GameObject iconImg = mUI.CreateElement(icon, "img");
			mUI.SetRectFillParent(iconImg, 0);
			RawImage ii = iconImg.AddComponent<RawImage>();
			ii.texture = _asset.mIconTexture;
			ii.uvRect = _asset.mIconRect;
		}

		// Item Specific
		if (_asset.mItemType == EItemType.DECO)
		{
			mUI.CreateFoldOut(_propViewContent, "Deco", out content, false);

			mUI.CreateFieldElement(content, "Stress (AOE)", out editor);
			mUI.CreateFloatEditor(editor, _asset.mStress, (float Value) => { _asset.mStress = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Efficiency (AOE)", out editor);
			mUI.CreateFloatEditor(editor, _asset.mValue, (float Value) => { _asset.mValue = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "AOE Radius", out editor);
			mUI.CreateFloatEditor(editor, _asset.mAreaOfEffect, (float Value) => { _asset.mAreaOfEffect = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.DESK)
		{
			mUI.CreateFoldOut(_propViewContent, "Desk", out content, false);

			mUI.CreateFieldElement(content, "Stress", out editor);
			mUI.CreateFloatEditor(editor, _asset.mStress, (float Value) => { _asset.mStress = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Efficiency", out editor);
			mUI.CreateFloatEditor(editor, _asset.mValue, (float Value) => { _asset.mValue = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Stacks In Max", out editor);
			mUI.CreateIntEditor(editor, _asset.mSheetsInMax, (int Value) => { _asset.mSheetsInMax = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Stacks Out Max", out editor);
			mUI.CreateIntEditor(editor, _asset.mSheetsOutMax, (int Value) => { _asset.mSheetsOutMax = Value; _ModifyAsset(); });

			mUI.CreateFoldOut(_propViewContent, "Desk Visuals", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });

			mUI.CreateTextElement(content, "Paper In");

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPaperInPosition, (Vector3 vec) => { _asset.mPaperInPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPaperInRotation, (Vector3 vec) => { _asset.mPaperInRotation = vec; _ModifyAsset(); });

			mUI.CreateTextElement(content, "Paper Out");

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPaperOutPosition, (Vector3 vec) => { _asset.mPaperOutPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPaperOutRotation, (Vector3 vec) => { _asset.mPaperOutRotation = vec; _ModifyAsset(); });

			mUI.CreateFoldOut(_propViewContent, "Chair", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mSMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mSMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mSMPosition, (Vector3 vec) => { _asset.mSMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mSMRotation, (Vector3 vec) => { _asset.mSMRotation = vec; _ModifyAsset(); });

			mUI.CreateFoldOut(_propViewContent, "Usage Slot", out content, false);

			mUI.CreateTextElement(content, "Entry Point");

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mUsageSlots[0].mEntryPosition, (Vector3 vec) => { _asset.mUsageSlots[0].mEntryPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mUsageSlots[0].mEntryRotation, (Vector3 vec) => { _asset.mUsageSlots[0].mEntryRotation = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Size", out editor);
			mUI.CreateVector3Editor(editor, _asset.mUsageSlots[0].mEntrySize, (Vector3 vec) => { _asset.mUsageSlots[0].mEntrySize = vec; _ModifyAsset(); });

			mUI.CreateTextElement(content, "Usage Point");

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mUsageSlots[0].mUsePosition, (Vector3 vec) => { _asset.mUsageSlots[0].mUsePosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mUsageSlots[0].mUseRotation, (Vector3 vec) => { _asset.mUsageSlots[0].mUseRotation = vec; _ModifyAsset(); });

		}
		else if (_asset.mItemType == EItemType.SAFE)
		{
			mUI.CreateFoldOut(_propViewContent, "Safe", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.REST)
		{
			mUI.CreateFoldOut(_propViewContent, "Rest", out content, false);

			mUI.CreateFieldElement(content, "Comfort", out editor);
			mUI.CreateFloatEditor(editor, _asset.mValue, (float Value) => { _asset.mValue = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.FOOD)
		{
			mUI.CreateFoldOut(_propViewContent, "Food", out content, false);

			mUI.CreateFieldElement(content, "Food Quality", out editor);
			mUI.CreateFloatEditor(editor, _asset.mValue, (float Value) => { _asset.mValue = Value; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.DOOR)
		{
			mUI.CreateFoldOut(_propViewContent, "Door", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.START)
		{
			mUI.CreateFoldOut(_propViewContent, "Start", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });

			mUI.CreateFoldOut(_propViewContent, "Door", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mSMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mSMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mSMPosition, (Vector3 vec) => { _asset.mSMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mSMRotation, (Vector3 vec) => { _asset.mSMRotation = vec; _ModifyAsset(); });
		}
		else if (_asset.mItemType == EItemType.CAMERA)
		{
			mUI.CreateFoldOut(_propViewContent, "Camera", out content, false);

			mUI.CreateFieldElement(content, "Model", out editor);
			mUI.CreateComboBox(editor, _GetPrimaryModelName(_asset.mPMAsset), _GetModelsComboData, (string Name) => { _ChangeModel(ref _asset.mPMAsset, Name); });

			mUI.CreateFieldElement(content, "Position", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMPosition, (Vector3 vec) => { _asset.mPMPosition = vec; _ModifyAsset(); });

			mUI.CreateFieldElement(content, "Rotation", out editor);
			mUI.CreateVector3Editor(editor, _asset.mPMRotation, (Vector3 vec) => { _asset.mPMRotation = vec; _ModifyAsset(); });
		}
	}

	private GameObject _CreateSceneText(GameObject Parent, string Text)
	{
		GameObject sceneText = new GameObject("sceneText");
		sceneText.transform.SetParent(Parent.transform);

		TextMesh text = sceneText.AddComponent<TextMesh>();
		text.text = Text;
		text.characterSize = 0.1f;
		text.fontSize = 32;
		text.font = CGame.ToolkitUI.SceneTextFont;
		text.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);
		text.anchor = TextAnchor.UpperCenter;

		return sceneText;
	}

	private void _OnClickEdgeSetView(EViewDirection ViewDirection)
	{
		_viewDirection = ViewDirection;

		for (int i = 0; i < 4; ++i)
		{
			if (i == (int)ViewDirection)
				mUI.SetToolbarButtonHighlight(_tbbView[i], CToolkitUI.EButtonHighlight.SELECTED);
			else
				mUI.SetToolbarButtonHighlight(_tbbView[i], CToolkitUI.EButtonHighlight.NOTHING);
		}

		/*
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
		*/
	}
}
