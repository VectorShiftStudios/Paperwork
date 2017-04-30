using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CItemView : CStateView, ISelectable
{
	private GameObject _Gob;
	private GameObject _briedcaseGOB;
	private GameObject _paperInGob;
	private GameObject _paperOutGob;
	private AudioSource _audioSource;

	private float _doorAngle;
	private float _doorTarget;
	public bool mLocked;

	private Color _targetSurfaceColor;
	private Color _currentSurfaceColor;

	private GameObject _textGob;
	private TextMesh _textMesh;

	// Spawn Point
	private GameObject _punchOut;
	private Mesh _punchOutMesh;

	// State Duplication
	public CItemAsset mAsset;
	public int mItemID;
	public int mProxyID;
	public int mOwnerID;
	public bool mBlueprint;
	public Vector2 mPosition;
	public int mRotation;
	public Bounds mBounds;
	public int mUserID;
	public int mUserOwner;
	public float mMaxDurability;
	public float mDurability;
	public int mValue;

	public Color mSurfaceColor;
	public float mDoorAngle;

	// Desk
	public int mAssignedUnitID;
	public int mPaperStackUpdateTick;
	public int mLastPaperStackUpdateTick = -1;
	public int mPaperStackCount;
	public int mPaperStackOutCount;
	public int mMaxCompletedPapers;
	
	public void CopyInitialState(CItemProxy ItemProxy)
	{
		mAsset = ItemProxy.mAsset;
		mProxyID = ItemProxy.mID;
		mItemID = ItemProxy.mItemID;
		mBlueprint = ItemProxy.mBlueprint;
		//_targetSurfaceColor = ItemProxy.mSurfaceColor;
		_currentSurfaceColor = ItemProxy.mSurfaceColor;
	}

	public void CopyState(CItemProxy ItemProxy)
	{
		mOwnerID = ItemProxy.mOwnerID;
		mSurfaceColor = ItemProxy.mSurfaceColor;
		mDoorAngle = ItemProxy.mDoorPosition;
		mLocked = ItemProxy.mLocked;
		mPosition = ItemProxy.mPosition;
		mRotation = ItemProxy.mRotation;
		mBounds = ItemProxy.mBounds;
		mUserID = ItemProxy.mUserID;
		mUserOwner = ItemProxy.mUserOwner;
		mDurability = ItemProxy.mDurability;
		mMaxDurability = ItemProxy.mMaxDurability;
		mValue = ItemProxy.mValue;
		mAssignedUnitID = ItemProxy.mAssignedUnitID;
		mPaperStackUpdateTick = ItemProxy.mPaperStackUpdateTick;
		mPaperStackCount = ItemProxy.mAssignedPaperStacks.Count;
		mPaperStackOutCount = ItemProxy.mCompletedPaperStacks;
		mMaxCompletedPapers = ItemProxy.mMaxCompletedPapers;
	}

	protected override void _New(CUserSession UserSession)
	{
		_Gob = mAsset.CreateVisuals((EViewDirection)mRotation);
		_Gob.name = mAsset.mName;
		_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);

		if (mBlueprint)
		{
			CUtility.SetLayerRecursively(_Gob.transform, 9);
			_currentSurfaceColor = Color.white;
		}
		else
		{
			if (mAsset.mItemType == EItemType.DESK)
			{
				_briedcaseGOB = GameObject.CreatePrimitive(PrimitiveType.Cube);
				_briedcaseGOB.GetComponent<MeshRenderer>().material = CGame.PrimaryResources.FlatMat;
				_briedcaseGOB.transform.SetParent(_Gob.transform);
				_briedcaseGOB.transform.localScale = new Vector3(0.15f, 0.3f, 0.45f);
				_briedcaseGOB.transform.localPosition = new Vector3(0.626f, 0.69f, 1.473f);
				_briedcaseGOB.transform.rotation = Quaternion.Euler(0.0f, -10.0f, 270.0f);

				_briedcaseGOB.SetActive(false);
			}
			else if (mAsset.mItemType == EItemType.SAFE)
			{
				/*
				_textGob = new GameObject("sceneText");
				_textGob.transform.SetParent(_Gob.transform);

				_textMesh = _textGob.AddComponent<TextMesh>();
				_textMesh.text = mValue.ToString();
				_textMesh.characterSize = 0.09f;
				_textMesh.fontSize = 32;
				_textMesh.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
				_textMesh.anchor = TextAnchor.MiddleCenter;
				//text.font = CGame.ToolkitUI.SceneTextFont;

				_textGob.transform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);
				_textGob.transform.rotation = Quaternion.Euler(90, 0, 0);
				*/
			}
			else if (mAsset.mItemType == EItemType.START)
			{
				if (mOwnerID == UserSession.mPlayerIndex)
				{
					_punchOut = CPunchOut.Create(out _punchOutMesh);
					_punchOut.transform.SetParent(_Gob.transform);
				}
			}
			else if (mAsset.mItemType == EItemType.DOOR)
			{
				_briedcaseGOB = (GameObject)GameObject.Instantiate(CGame.WorldResources.PadlockPrefab, new Vector3(0.5f, 2.25f, 1.0f), Quaternion.identity);
				_briedcaseGOB.transform.SetParent(_Gob.transform);
			}

			_audioSource = _Gob.AddComponent<AudioSource>();
			_audioSource.outputAudioMixerGroup = CGame.UIResources.SoundsMixer;
			_audioSource.spatialBlend = 1.0f;
		}
	}

	public static void SetItemSurfaceColour(EItemType ItemType, GameObject VisualObject, Color SurfaceColor)
	{
		VisualObject.GetComponent<Item>().SetSurfaceColor(SurfaceColor);

		/*
		if (ItemType == EItemType.SAFE || ItemType == EItemType.DECO || ItemType == EItemType.REST || ItemType == EItemType.FOOD || ItemType == EItemType.DOOR)
		{
			VisualObject.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_FloorColor", SurfaceColor);
		}
		else if (ItemType == EItemType.DESK)
		{
			VisualObject.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_FloorColor", SurfaceColor);
			VisualObject.transform.GetChild(1).GetComponent<MeshRenderer>().material.SetColor("_FloorColor", SurfaceColor);
		}
		else if (ItemType == EItemType.START)
		{
			VisualObject.GetComponent<Item>().SetSurfaceColor(SurfaceColor);
		}
		*/
	}

	protected override void _Update(CUserSession UserSession)
	{
		_targetSurfaceColor = mSurfaceColor;
		float colorLerp = 2.0f * Time.deltaTime;
		colorLerp = Mathf.Clamp(colorLerp, 0.0f, 1.0f);
		_currentSurfaceColor = Color.Lerp(_currentSurfaceColor, _targetSurfaceColor, colorLerp);

		SetItemSurfaceColour(mAsset.mItemType, _Gob, _currentSurfaceColor);

		Vector2 pivot = CItem.PivotRelativeToTile[mRotation];
		_Gob.transform.position = new Vector3(mPosition.x, 0.0f, mPosition.y) + new Vector3(pivot.x, 0.0f, pivot.y);
		_Gob.transform.rotation = CItem.RotationTable[mRotation];

		if (mBlueprint)
			return;

		if (mAsset.mItemType == EItemType.DOOR)
		{
			if (mDoorAngle > _doorAngle)
			{
				if (_doorAngle == 0.0f)
				{
					_audioSource.clip = CGame.PrimaryResources.AudioClips[7];
					_audioSource.Play();
					//_audioSource.PlayOneShot(CGame.PrimaryResources.AudioClips[7]);
					//AudioSource.PlayClipAtPoint(CGame.PrimaryResources.AudioClips[7], mPosition.ToWorldVec3(), 1.0f);
				}

				_doorAngle += CItemDoor.DOORSPEED * Time.deltaTime;
			}
			else if (mDoorAngle < _doorAngle)
			{
				if (_doorAngle == 1.0f)
				{
					_audioSource.clip = CGame.PrimaryResources.AudioClips[8];
					_audioSource.Play();
					//_audioSource.PlayOneShot(CGame.PrimaryResources.AudioClips[8]);
					//AudioSource.PlayClipAtPoint(CGame.PrimaryResources.AudioClips[8], mPosition.ToWorldVec3(), 1.0f);
				}

				_doorAngle -= CItemDoor.DOORSPEED * Time.deltaTime;
			}

			_doorAngle = Mathf.Clamp(_doorAngle, 0.0f, 1.0f);

			GameObject door = _Gob.transform.GetChild(0).gameObject;

			door.transform.rotation = Quaternion.Euler(0.0f, _doorAngle * 90.0f, 0.0f) * CItem.RotationTable[mRotation];

			if (mLocked)
			{
				_briedcaseGOB.SetActive(true);
				_briedcaseGOB.transform.Rotate(Vector3.up, 180.0f * Time.deltaTime);
			}
			else
			{
				_briedcaseGOB.SetActive(false);
			}
		}
		else if (mAsset.mItemType == EItemType.DESK)
		{
			if (mAssignedUnitID != -1)
			{
				/*
				Color playerColor = _worldView.mPlayerViews[_playerIndex].mColor;
				_briedcaseGOB.GetComponent<MeshRenderer>().material.SetColor("_Color", playerColor);
				_briedcaseGOB.SetActive(true);
				*/
			}
			else
			{
				//_briedcaseGOB.SetActive(false);
			}

			if (mLastPaperStackUpdateTick != mPaperStackUpdateTick)
			{
				RefreshContractVisuals();
				mLastPaperStackUpdateTick = mPaperStackUpdateTick;
			}
		}
		else if (mAsset.mItemType == EItemType.START)
		{
			if (_doorTarget != mDoorAngle)
			{
				if (mDoorAngle == 1.0f)
					//_audioSource.PlayOneShot(CGame.PrimaryResources.AudioClips[9]);
					//AudioSource.PlayClipAtPoint(CGame.PrimaryResources.AudioClips[9], mPosition.ToWorldVec3(), 1.0f);
					_audioSource.PlayOneShot(CGame.PrimaryResources.AudioClips[9]);
				else
					_audioSource.PlayOneShot(CGame.PrimaryResources.AudioClips[10]);
					//AudioSource.PlayClipAtPoint(CGame.PrimaryResources.AudioClips[10], mPosition.ToWorldVec3(), 1.0f);

				_doorTarget = mDoorAngle;
			}

			if (_doorAngle != mDoorAngle)
			{
				_doorAngle = Mathf.Lerp(_doorAngle, mDoorAngle, 2.0f * Time.deltaTime);
				_Gob.transform.GetChild(1).localScale = new Vector3(1.0f - _doorAngle, 1.0f, 1.0f);
				_Gob.transform.GetChild(2).localScale = new Vector3(1.0f - _doorAngle, 1.0f, 1.0f);
			}

			if (mOwnerID == UserSession.mPlayerIndex)
			{
				CPunchOut.UpdateMesh(_punchOutMesh, _worldView, _Gob.transform.position.ToWorldVec2(), 8.0f);
			}
		}
		else if (mAsset.mItemType == EItemType.SAFE)
		{
			//_textMesh.text = mValue.ToString();
			//_textGob.transform.position = mBounds.center + new Vector3(0.0f, 1.5f, 0.0f);
			//_textGob.transform.rotation = Quaternion.Euler(0, Time.time * 50.0f, 0);
		}

		//if (_itemView.mTaggedUsable && (tp == CEntity.EType.T_ITEM_DESK || tp == CEntity.EType.T_ITEM_FOOD || tp == CEntity.EType.T_ITEM_REST || tp == CEntity.EType.T_ITEM_SAFE))
		//CDebug.DrawXRect(_itemView.mBounds.center, 0.5f, 0.5f, Color.blue);

		//if (_itemView.mOwnerID != -1 && tp == CEntity.EType.T_ITEM_SAFE)
		//CDebug.DrawXRect(_itemView.mBounds.center, 0.2f, 0.2f, _itemView.mWorld.mPlayers[_itemView.mOwnerID].mColor);

		//CDebug.DrawZRect(_itemView.mBounds.center, 0.1f, 0.01f * _itemView.mDurability, Color.red);


		/*
		// UI Space Snippet.
		Vector3 screenPos = Camera.main.WorldToScreenPoint(_Gob.transform.position + new Vector3(0.0f, 1.6f, 0.0f));
		Vector2 bubbleUIPos = CGame.UIManager.ConvertScreenSpaceToUISpace(new Vector2(screenPos.x, screenPos.y));
		((RectTransform)_iconStackGob.transform).anchoredPosition = bubbleUIPos;
		*/
	}

	/// <summary>
	/// Display the in & out contract stack queues.
	/// </summary>
	public void RefreshContractVisuals()
	{
		// TODO: Cleanup punchout mesh?

		if (_paperInGob != null)
		{
			GameObject.Destroy(_paperInGob);
			_paperInGob = null;
		}

		CModelAsset paperModel = CGame.AssetManager.GetAsset<CModelAsset>("default_contract_paper");

		if (mPaperStackCount > 0)
		{
			GameObject.Destroy(_paperInGob);
			_paperInGob = new GameObject();
			_paperInGob.transform.parent = _Gob.transform;
			_paperInGob.transform.localPosition = mAsset.mPaperInPosition;
			
			for (int i = 0; i < mPaperStackCount; ++i)
			{
				GameObject paperGob = paperModel.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
				paperGob.transform.SetParent(_paperInGob.transform);
				paperGob.transform.localPosition = new Vector3(0, i * 0.15f, 0);
				paperGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", CGame.GameUIStyle.ThemeColorA);
			}
		}

		if (mPaperStackOutCount > 0)
		{
			GameObject.Destroy(_paperOutGob);
			_paperOutGob = new GameObject();
			_paperOutGob.transform.parent = _Gob.transform;
			_paperOutGob.transform.localPosition = mAsset.mPaperOutPosition;

			for (int i = 0; i < mPaperStackOutCount; ++i)
			{
				GameObject paperGob = paperModel.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
				paperGob.transform.SetParent(_paperOutGob.transform);
				paperGob.transform.localPosition = new Vector3(0, i * 0.15f, 0);
				paperGob.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", CGame.GameUIStyle.ThemeColorA);
			}
		}

		/*
		if (mOutPapers > 0)
		{
			GameObject.Destroy(_gobOut);
			_gobOut = new GameObject();
			_gobOut.transform.parent = _Gob.transform;
			_gobOut.transform.localPosition = mDefinition.mPapersOutPos;
			_gobOut.transform.localRotation = Quaternion.identity;
			_CreateContractPapers(_gobOut.transform, mOutPapers);
		}
		else
		{
			if (_gobOut != null)
			{
				GameObject.Destroy(_gobOut);
				_gobOut = null;
			}
		}
		*/
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		if (_Gob != null)
			GameObject.Destroy(_Gob);
	}

	public override Transform GetTransform()
	{
		if (_Gob != null)
			return _Gob.transform;

		return null;
	}

	ESelectionType ISelectable.GetType()
	{
		return ESelectionType.ITEM;
	}

	string ISelectable.GetInfo()
	{
		string result = mAsset.mFriendlyName;

		if (mBlueprint)
			result += " (Blueprint)";
		
		return result;
	}

	void ISelectable.PrintInfo()
	{

	}

	int ISelectable.GetID()
	{
		return mItemID;
		//return mProxyID;
	}

	bool ISelectable.IsStillActive()
	{
		return (mState != EState.DESTROYED);
	}

	Vector3 ISelectable.GetScreenPos()
	{
		return Vector3.zero;
	}

	Vector3 ISelectable.GetVisualPos()
	{
		return _Gob.transform.position;
	}

	CStateView ISelectable.GetStateView()
	{
		return this;
	}

	void ISelectable.Select()
	{
	}

	void ISelectable.Deselect()
	{
	}

	bool ISelectable.Intersect(Ray R, ref float D)
	{
		//CDebug.DrawBounds(mBounds, Color.white);
		if (mBounds.IntersectRay(R))
		{
			float t;
			if (IntersectRay(R, out t))
			{
				if (t < D)
				{
					D = t;
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Intersect a ray with this object.
	/// </summary>
	public bool IntersectRay(Ray R, out float T)
	{
		T = float.MaxValue;
		float t;
		bool hit = false;
		EItemType ItemType = mAsset.mItemType;

		if (ItemType == EItemType.SAFE || ItemType == EItemType.DECO || ItemType == EItemType.REST || ItemType == EItemType.DESK || ItemType == EItemType.FOOD || ItemType == EItemType.DOOR)
		{
			if (mAsset.mPMAsset.mVectorModel.IntersectRay(R, _Gob.transform.GetChild(0).transform.worldToLocalMatrix, out t))
			{
				if (t < T)
				{	
					hit = true;
					T = t;
				}
			}
		}

		if (ItemType == EItemType.DESK)
		{
			if (mAsset.mSMAsset.mVectorModel.IntersectRay(R, _Gob.transform.GetChild(1).transform.worldToLocalMatrix, out t))
			{
				if (t < T)
				{
					hit = true;
					T = t;
				}
			}
		}

		return hit;
	}

	void ISelectable.GetRenderers(List<Renderer> Renderers)
	{
		EItemType ItemType = mAsset.mItemType;

		if (ItemType == EItemType.SAFE || ItemType == EItemType.DECO || ItemType == EItemType.REST || ItemType == EItemType.FOOD || ItemType == EItemType.DOOR)
		{
			Renderers.Add(_Gob.transform.GetChild(0).GetComponent<MeshRenderer>());
		}
		else if (ItemType == EItemType.DESK)
		{
			Renderers.Add(_Gob.transform.GetChild(0).GetComponent<MeshRenderer>());
			Renderers.Add(_Gob.transform.GetChild(1).GetComponent<MeshRenderer>());
			//Renderers.Add(_Gob.transform.GetChild(2).GetComponent<MeshRenderer>());
			//Renderers.Add(_Gob.transform.GetChild(3).GetComponent<MeshRenderer>());
		}
	}

	void ISelectable.Hover()
	{
	}

	void ISelectable.HoverOut()
	{
	}
}
