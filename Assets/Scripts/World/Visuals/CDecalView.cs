using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CDecalView : CStateView
{
	public static GameObject CreateDecal(SDecalInfo Info)
	{
		Material sharedMat = GameObject.Instantiate(CGame.WorldResources.DecalMat);
		GameObject Gob = null;

		if (Info.mType == CDecal.EDecalType.TEXT)
		{
			Gob = new GameObject("decal");

			TextMesh textMesh = Gob.AddComponent<TextMesh>();
			Gob.GetComponent<MeshRenderer>().material = sharedMat;
			sharedMat.SetTexture("_MainTex", CGame.WorldResources.DecalFontA.material.GetTexture("_MainTex"));
			sharedMat.SetFloat("_Add", 1.0f);
			textMesh.font = CGame.WorldResources.DecalFontA;
			textMesh.text = Info.mText;
			textMesh.characterSize = 0.2f;
			textMesh.fontSize = 54;
			textMesh.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			textMesh.anchor = TextAnchor.MiddleCenter;
		}
		else if (Info.mType == CDecal.EDecalType.IMAGE)
		{
			Gob = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Gob.name = "decal";
			Gob.GetComponent<MeshRenderer>().material = sharedMat;
			sharedMat.SetTexture("_MainTex", CGame.PrimaryResources.GetDecalImage(Info.mVisualId));
		}

		Gob.transform.localPosition = Info.mPosition;
		Gob.transform.rotation = Info.mRotation;
		Gob.transform.localScale = new Vector3(Info.mSize.x, Info.mSize.y, 1.0f);
		sharedMat.SetColor("_Color", Info.mColor);

		Gob.AddComponent<Item>().Init(sharedMat);

		return Gob;
	}

	// State
	public int mID;
	public SDecalInfo mInfo;

	private GameObject _Gob;
	private TextMesh _textMesh;
	private Material _mat;

	public void CopyInitialState(CDecal Decal)
	{
		mID = Decal.mID;
		mInfo = Decal.mInfo;
	}

	public void CopyState(CDecal Decal)
	{
		mInfo = Decal.mInfo;
	}

	protected override void _New(CUserSession UserSession)
	{
		Material mat = null;

		if (mInfo.mVis == CDecal.EDecalVis.ALWAYS) mat = CGame.WorldResources.DecalMat;
		else if (mInfo.mVis == CDecal.EDecalVis.FOW) mat = CGame.WorldResources.DecalFOWMat;
		else if (mInfo.mVis == CDecal.EDecalVis.LOS) mat = CGame.WorldResources.DecalLOSMat;

		if (mInfo.mType == CDecal.EDecalType.TEXT)
		{
			_Gob = new GameObject("decal");
			_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);

			_textMesh = _Gob.AddComponent<TextMesh>();			
			_Gob.GetComponent<MeshRenderer>().material = mat;
			_Gob.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", CGame.WorldResources.DecalFontA.material.GetTexture("_MainTex"));
			_Gob.GetComponent<MeshRenderer>().material.SetFloat("_Add", 1.0f);
			_textMesh.font = CGame.WorldResources.DecalFontA;
			_textMesh.text = mInfo.mText;
			_textMesh.characterSize = 0.2f;
			_textMesh.fontSize = 54;
			_textMesh.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			_textMesh.anchor = TextAnchor.MiddleCenter;
		}
		else if (mInfo.mType == CDecal.EDecalType.IMAGE)
		{
			_Gob = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_Gob.name = "decal";
			_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);
			_Gob.GetComponent<MeshRenderer>().material = mat;
			_Gob.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", CGame.PrimaryResources.GetDecalImage(mInfo.mVisualId));
		}

		_mat = _Gob.GetComponent<MeshRenderer>().material;
	}

	protected override void _Update(CUserSession UserSession)
	{
		_Gob.transform.position = mInfo.mPosition;
		_Gob.transform.localRotation = mInfo.mRotation;
		_Gob.transform.localScale = new Vector3(mInfo.mSize.x, mInfo.mSize.y, 1.0f);
		_mat.SetColor("_Color", mInfo.mColor);
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		if (_Gob != null)
			GameObject.Destroy(_Gob);
	}
}
