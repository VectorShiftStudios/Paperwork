using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class CBrushEditor : CAssetToolkitWindow
{
	private CBrushAsset _brushAsset;

	private GameObject _propertiesContent;

	private GameObject _tbbSave;

	public CBrushEditor(string AssetName)
	{
		mAssetName = AssetName;
		_brushAsset = CGame.AssetManager.GetAsset<CBrushAsset>(AssetName);
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
		
		GameObject content;
		GameObject w1p = mUI.CreateWindowPanel(mPrimaryContent, out content, "Brush Editor");
		mUI.AddLayout(w1p, -1, -1, 1.0f, 1.0f);
		//mUI.CreateTextElement(w1pContent, "Information", "text", CToolkitUI.ETextStyle.TS_HEADING);
		//mUI.CreateTextElement(w1pContent, "Testing string");

		mUI.CreateTextElement(content, "Brush Properties", "text", CToolkitUI.ETextStyle.TS_HEADING);

		GameObject editor;
		mUI.CreateFieldElement(content, "Color", out editor);
		mUI.CreateColorEditor(editor, _brushAsset.mRawColor, (Color color) => { _brushAsset.mRawColor = color; _ModifyAsset(); });

		mUI.CreateFieldElement(content, "Line Weight", out editor);
		mUI.CreateFloatEditor(editor, _brushAsset.mWeight, (float value) => { _brushAsset.mWeight = value; _ModifyAsset();  } );

		mUI.CreateFieldElement(content, "Floor Color Mix", out editor);
		mUI.CreateFloatEditor(editor, _brushAsset.mFloorMix, (float value) => { _brushAsset.mFloorMix = value; _ModifyAsset(); });
	}

	private void _ModifyAsset()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.WARNING);
		_brushAsset.CompileColor();
	}

	private void _Save()
	{
		mUI.SetToolbarButtonHighlight(_tbbSave, CToolkitUI.EButtonHighlight.NOTHING);
		_brushAsset.Save();
	}

	public override void Destroy()
	{
	}

	public override void Update(CInputState InputState)
	{
	}

	public override string GetTabName()
	{
		return "Brush (" + _brushAsset.mName + ")";
	}
}
