using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CAssetEditor : CAssetToolkitWindow
{
	private GameObject _propertiesContent;

	private CTUITreeViewItem _tviBrushes;
	private CTUITreeViewItem _tviModels;
	private CTUITreeViewItem _tviItems;
	private CTUITreeViewItem _tviLevels;
	private CTUITreeViewItem _tviSheets;

	public override void Init(CAssetToolkit Toolkit)
	{
		base.Init(Toolkit);

		mPrimaryContent = mUI.CreateElement(Toolkit.mPrimaryContent, "horzLayout");
		mPrimaryContent.SetActive(false);
		mUI.AddLayout(mPrimaryContent, -1, -1, 1.0f, 1.0f);
		mUI.AddVerticalLayout(mPrimaryContent);
		mPrimaryContent.GetComponent<VerticalLayoutGroup>().spacing = 4;

		GameObject toolbar = mUI.CreateElement(mPrimaryContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, 0.0f);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		mUI.CreateToolbarButton(toolbar, "Sheet", mUI.SheetImage);
		mUI.CreateToolbarButton(toolbar, "Level", mUI.LevelImage, () => _OnClickNewModel(EAssetType.AT_LEVEL));
		mUI.CreateToolbarButton(toolbar, "Brush", mUI.BrushImage, () => _OnClickNewModel(EAssetType.AT_BRUSH));
		mUI.CreateToolbarButton(toolbar, "Model", mUI.ModelImage, () => _OnClickNewModel(EAssetType.AT_MODEL));
		mUI.CreateToolbarButton(toolbar, "Item", mUI.ItemImage, () => _OnClickNewModel(EAssetType.AT_ITEM));

		mUI.CreateToolbarSeparator(toolbar);
		mUI.CreateToolbarButton(toolbar, "Character", mUI.BrushImage, mToolkit.EditCharacter);
		
		GameObject hSplit = mUI.CreateElement(mPrimaryContent, "horzLayout");
		mUI.AddLayout(hSplit, -1, -1, 1.0f, 1.0f);
		mUI.AddHorizontalLayout(hSplit);
		hSplit.GetComponent<HorizontalLayoutGroup>().spacing = 4;

		GameObject w1 = mUI.CreateElement(hSplit, "split1");
		mUI.AddLayout(w1, -1, -1, 1.0f, 1.0f);
		mUI.AddVerticalLayout(w1);

		GameObject w2 = mUI.CreateElement(hSplit, "split2");		
		mUI.AddLayout(w2, 300, -1, 0.0f, 1.0f);
		mUI.AddVerticalLayout(w2);

		// Split 1
		GameObject w1pContent;
		GameObject w1p = mUI.CreateWindowPanel(w1, out w1pContent, "Tools");
		mUI.AddLayout(w1p, -1, -1, 1.0f, 1.0f);

		//mUI.CreateTextElement(w1pContent, "Create New Asset", "text", CToolkitUI.ETextStyle.TS_HEADING);

		/*toolbar = mUI.CreateElement(w1pContent, "toolbarView");
		mUI.AddLayout(toolbar, -1, 48, 1.0f, -1);
		mUI.AddImage(toolbar, mUI.WindowPanelBackground);
		mUI.AddHorizontalLayout(toolbar);
		toolbar.GetComponent<HorizontalLayoutGroup>().spacing = 4;
		*/
				
		//mUI.CreateToolbarButton(toolbar, "Company", mUI.CompanyImage);
		//mUI.CreateToolbarButton(toolbar, "Sheet", mUI.SheetImage);

		mUI.CreateTextElement(w1pContent, "Asset Directory", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject scrollContent;
		GameObject scrollV1 = mUI.CreateScrollView(w1pContent, out scrollContent, true);
		mUI.AddLayout(scrollV1, -1, -1, 1.0f, 1.0f);

		CTUITreeView treeView;
		GameObject treeViewGob = mUI.CreateTreeView(scrollContent, out treeView);
		mUI.AddLayout(treeViewGob, -1, -1, 1.0f, 1.0f);

		_tviBrushes = treeView.mRootItem.AddItem("Brushes");
		_tviModels = treeView.mRootItem.AddItem("Models");
		_tviItems = treeView.mRootItem.AddItem("Items");
		_tviLevels = treeView.mRootItem.AddItem("Levels");
		_tviSheets = treeView.mRootItem.AddItem("Sheets");

		//item.AddItem(entry.Value.mName);		
		//item = treeView.mRootItem.AddItem("Models");
		//item.AddItem(entry.Value.mName, () => OnClickAsset(0, guid), () => mToolkit.EditAsset(guid));

		foreach (KeyValuePair<string, CAssetDeclaration> entry in CGame.AssetManager.mAssetDeclarations)
		{
			CAssetDeclaration decl = entry.Value;
			CTUITreeViewItem item = null;

			if (decl.mType == EAssetType.AT_BRUSH) item = _tviBrushes;
			else if (decl.mType == EAssetType.AT_MODEL) item = _tviModels;
			else if (decl.mType == EAssetType.AT_LEVEL) item = _tviLevels;
			else if (decl.mType == EAssetType.AT_ITEM) item = _tviItems;

			if (item != null)
				item.AddItem(decl.mName, () => OnClickAsset(decl.mName), () => mToolkit.EditAsset(decl.mName));
		}

		treeView.Rebuild();

		// Split 2		
		GameObject w2p = mUI.CreateWindowPanel(w2, out _propertiesContent, "Properties");
		mUI.AddLayout(w2p, -1, -1, 1.0f, 1.0f);

		mUI.CreateTextElement(_propertiesContent, "(No Asset Selected)", "text", CToolkitUI.ETextStyle.TS_HEADING);
	}

	public void Destroy()
	{
	}

	public void Update()
	{
	}

	public override string GetTabName()
	{
		return "Asset Manager";
	}

	private void OnClickAsset(string AssetName)
	{
		CUtility.DestroyChildren(_propertiesContent);

		CAssetDeclaration decl = CGame.AssetManager.GetDeclaration(AssetName);

		mUI.CreateTextElement(_propertiesContent, "Selected " + decl.mName, "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject foldContent;
		mUI.CreateFoldOut(_propertiesContent, "Asset Details", out foldContent);

		GameObject fieldEditor;
		mUI.CreateFieldElement(foldContent, "Name", out fieldEditor); mUI.CreateTextElement(fieldEditor, decl.mName);
		mUI.CreateFieldElement(foldContent, "File Path", out fieldEditor); mUI.CreateTextElement(fieldEditor, decl.mFileName);
		mUI.CreateFieldElement(foldContent, "Type", out fieldEditor); mUI.CreateTextElement(fieldEditor, decl.mType.ToString());
		//mUI.CreateFieldElement(foldContent, "Modified Date", out fieldEditor); mUI.CreateTextElement(fieldEditor, decl.mModifiedDate);

		GameObject btn = mUI.CreateButton(_propertiesContent, "Edit", () => { mToolkit.EditAsset(AssetName); });
		mUI.AddLayout(btn, -1, 20, 1.0f, -1);

		//btn = mUI.CreateButton(_propertiesContent, "Delete");
		mUI.AddLayout(btn, -1, 20, 1.0f, -1);
	}

	private void _OnClickNewModel(EAssetType Type)
	{
		// Open (modal?) window for new asset with naming scheme control		
		GameObject content;
		GameObject window = mUI.CreateWindowPanel(mUI.Canvas, out content, "Create New Asset");
		RectTransform rect = window.GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(300, 200);
		rect.anchoredPosition = new Vector3(Screen.width / 2 - 150, -(Screen.height / 2 - 100));
		window.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);
		mUI.AddImage(window, mUI.WindowBackground);

		string[] assetNames = { "Unknown", "Brush", "Model", "Item", "Level", "Company", "Sheet" };

		mUI.CreateTextElement(content, "Create New " + assetNames[(int)Type], "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject editor;
		mUI.CreateFieldElement(content, "Name", out editor);
		InputField nameField = mUI.CreateInputElement(editor, 0).GetComponent<InputField>();
		nameField.text = CGame.AssetManager.GetUniqueAssetName();

		mUI.CreateTextElement(content, "(Only lower case letters, numbers and underscores, must be unique across all assets)");

		GameObject spacer = mUI.CreateElement(content);
		mUI.AddLayout(spacer, -1, -1, 1.0f, 1.0f);

		GameObject hsplit = mUI.CreateElement(content);
		mUI.AddLayout(hsplit, -1, 20, 1.0f, -1);
		mUI.AddHorizontalLayout(hsplit, null, 4);
		
		GameObject btn = mUI.CreateButton(hsplit, "Create", () => _OnClickCreateNewAssetWindow(window, Type, nameField.text));
		mUI.AddLayout(btn, -1, 20, 1.0f, -1);

		btn = mUI.CreateButton(hsplit, "Cancel", () => _OnClickCancelNewAssetWindow(window));
		mUI.AddLayout(btn, -1, 20, 1.0f, -1);
	}

	private void _OnClickCancelNewAssetWindow(GameObject Window)
	{
		GameObject.Destroy(Window);
	}

	private void _OnClickCreateNewAssetWindow(GameObject Window, EAssetType Type, string AssetName)
	{
		string errorStr;
		if (CGame.AssetManager.IsAssetNameValid(AssetName, out errorStr))
		{
			GameObject.Destroy(Window);

			CTUITreeViewItem treeItem = null;
			CAsset asset = null;

			if (Type == EAssetType.AT_MODEL)
			{
				treeItem = _tviModels;
				asset = new CModelAsset();				
			}
			else if (Type == EAssetType.AT_BRUSH)
			{
				treeItem = _tviBrushes;
				asset = new CBrushAsset();
			}
			else if (Type == EAssetType.AT_LEVEL)
			{
				treeItem = _tviLevels;
				asset = new CLevelAsset();
			}
			else if (Type == EAssetType.AT_ITEM)
			{
				treeItem = _tviItems;
				asset = new CItemAsset();
			}

			asset.mName = AssetName;
			asset.mFileName = CGame.DataDirectory + asset.mName + "." + CAssetManager.ASSET_FILE_EXTENSION;
			Debug.Log("New Asset Path: " + asset.mFileName);
			asset.Save();

			CAssetDeclaration decl = CGame.AssetManager.CreateAssetDeclaration(asset);
			treeItem.AddItem(decl.mName, () => OnClickAsset(decl.mName), () => mToolkit.EditAsset(decl.mName));

			treeItem.RebuildEntireTree();
			mToolkit.EditAsset(AssetName);
		}
		else
		{
			Debug.Log("Asset creation failed: " + errorStr);
		}
	}
}
