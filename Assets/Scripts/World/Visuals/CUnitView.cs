using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CUnitView : CStateView, ISelectable
{
	// State Duplication
	public int mID;
	public int mOwner;
	public Vector2 mPosition;
	public Quaternion mRotation;
	public bool mIntern;
	public bool mPathing;
	public bool mCarryingPickup;
	public bool mWalking;
	public bool mAttacking;
	public bool mEating;
	public bool mDead;
	public int mAssignedDeskID;
	public string mName;
	public string mActionAnim;
	public float mAnimSpeed;
	public float mWorkIdleTimer;
	public float mWorkIdle;
	public CUnitActions.EType mAction;
	public CUnitActions.EPhase mActionPhase;

	// Core Stats
	public SUnitBasicStats mStats;

	// Dynamic Stats
	public float mStamina;
	public float mStress;
	public float mHunger;
	public float mSpeed;
	public int mCollectedSalary;
	public bool mFrustrated;
	public int mCarriedPapers;
	public string mSpeech;
	public float mPromotionCounter;
	public CUnit.EThoughtState mThoughtState;

	// Visuals
	public Bounds mBounds;
	private GameObject _Gob;
	private Animator _Animator;
	private AudioSource _AudioSource;
	private Quaternion _prevRotation;
	private GameObject _carryGOB;
	private GameObject _briefcaseGOB;
	private GameObject _punchOut;
	private Mesh _punchOutMesh;
	private int _texState;
	private SDynamicsJoint[] _bagJoints;
	private SDynamicsJoint[] _joints;
	private GameObject _tieGob;
	private Mesh _tieMesh;
	public CUnit.EThoughtState mLastThoughtState;
	public float mThoughtTime;
	public float mThoughtAlpha;

	private struct SDynamicsJoint
	{
		public Vector3 mPosition;
		public Vector3 mPreviousPosition;
		public Vector3 mVelocity;
		public Vector3 mAcceleration;
		public bool mFixed;			 

		public SDynamicsJoint(Vector3 Position, bool Fixed)
		{
			mPosition = Position;
			mPreviousPosition = Position;
			mVelocity = Vector3.zero;
			mAcceleration = Vector3.zero;
			mFixed = Fixed;
		}

		public void Update(float Time, float Damping, float Gravity)
		{	
			if (mFixed)
			{
				mAcceleration = Vector3.zero;
				mVelocity = Vector3.zero;
				mPreviousPosition = mPosition;
			}
			else
			{
				mAcceleration = new Vector3(0.0f, -Gravity, 0);
				mVelocity = mPosition - mPreviousPosition;
				mPreviousPosition = mPosition;
				mPosition = mPosition + mVelocity * Damping + mAcceleration * (Time * Time);
			}
		}
	};

	// Sound Channel 1
	public int SoundType;
	public int SoundID;
	public int SoundLastPlayedID;
	public float SoundVolume;

	private float _footStepAudioTimer;
	private float _bubbleTime;

	// UI Components
	public CEmployeeEntry mUIEmployeeEntry;
	public GameObject mSpeechBubble;
	public Text mSpeechBubbleText;
	public float mSpeechTimer;

	private GameObject _indicatorGob;

	/*
	private GameObject _UIThought;
	private Image _UIThoughtIcon;
	private Text _UIThoughtText;
	private string _thoughtText;
	private GameObject _iconStackGob;
	private float _iconStackBaselineTarget;
	*/

	public void CopyInitialState(CUnit Unit)
	{
		mID = Unit.mID;
		mOwner = Unit.mOwner;
		mIntern = Unit.mIntern;
		mName = Unit.mName;
		mActionAnim = Unit.mActionAnim;
		mAnimSpeed = Unit.mAnimSpeed;
		mPosition = Unit.mPosition;
		mRotation = Unit.mRotation;

		mLastThoughtState = CUnit.EThoughtState.UNKNOWN;
		mThoughtTime = 0.0f;
		// TODO: if owner changes then we need to inform the user session we are no longer an employee
	}

	public void CopyState(CUnit Unit)
	{
		mDead = Unit.mDead;

		mPosition = Unit.mPosition;
		mRotation = Unit.mRotation;
		mPathing = Unit.mPathing;
		mCarryingPickup = (Unit.mCarryingPickup != null);
		mWalking = mPathing || Unit.mAnimWalk;
		mAssignedDeskID = Unit.mAssignedDeskID;
		mAttacking = Unit.mAttacking;
		mActionAnim = Unit.mActionAnim;
		mAnimSpeed = Unit.mAnimSpeed;
		mAction = Unit.mActionID;
		mActionPhase = Unit.mActionPhase;
		mWorkIdleTimer = Unit.mWorkIdleTimer;
		mWorkIdle = Unit.mWorkTotalIdleTime;

		// Core Stats
		mStats = Unit.mStats;
		
		// Dynamic Stats
		mStamina = Unit.mStamina;
		mStress = Unit.mStress;
		mHunger = Unit.mHunger;
		mSpeed = Unit.mSpeed;
		mCollectedSalary = Unit.mCollectedSalary;
		mFrustrated = Unit.mFrustrated;
		mCarriedPapers = (int)Unit.mPapersCarried;
		mPromotionCounter = Unit.mPromotionCounter;
		mThoughtState = Unit.mThoughtState;

		mSpeech = Unit.mSpeech;
	}

	protected override void _New(CUserSession UserSession)
	{
		/*
		_iconStackGob = new GameObject("iconStack", typeof(RectTransform));
		_iconStackGob.transform.SetParent(CGame.UIResources.UnitUIPanel.transform);
		_iconStackGob.transform.localScale = Vector3.one;
		_iconStackGob.GetComponent<RectTransform>().anchorMin = new Vector2(0.0f, 0.0f);
		_iconStackGob.GetComponent<RectTransform>().anchorMax = new Vector2(0.0f, 0.0f);
		_iconStackGob.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.0f);

		_UIThought = GameObject.Instantiate(CGame.UIResources.UnitThought) as GameObject;
		_UIThought.transform.SetParent(_iconStackGob.transform);
		_UIThought.transform.localScale = Vector3.one;
		_UIThought.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.0f);
		_UIThoughtIcon = _UIThought.transform.GetChild(0).GetComponent<Image>();
		_UIThoughtText = _UIThought.transform.GetChild(1).GetComponent<Text>();
		_UIThoughtIcon.enabled = true;
		_UIThought.SetActive(false);
		*/

		_texState = 0;
		Color playerColor = _worldView.mPlayerViews[mOwner].mColor;

		if (mIntern)
		{
			//_Gob = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[6]) as GameObject;
			_Gob = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7]) as GameObject;

			// Tie Colour
			_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetColor("_PlayerColor", playerColor);
			_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetTexture("_MainTex", CGame.PrimaryResources.UnitTexTieSling);
			
			_briefcaseGOB = new GameObject();
			Transform hand = _Gob.transform.FindChild("unit_test/Hips/");
			_briefcaseGOB.transform.SetParent(hand);
			_briefcaseGOB.transform.localPosition = new Vector3(0.02f, 0.02f, -0.065f);
			_briefcaseGOB.transform.localScale = Vector3.one;

			GameObject briefcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
			briefcase.transform.SetParent(_briefcaseGOB.transform);
			briefcase.GetComponent<MeshRenderer>().material = CGame.PrimaryResources.FlatMat;
			briefcase.GetComponent<MeshRenderer>().material.SetColor("_Color", playerColor);
			briefcase.transform.localScale = new Vector3(0.05f * 0.7f, 0.1f * 0.7f, 0.15f * 0.7f);
			briefcase.transform.localPosition = new Vector3(0.0f, -0.035f, 0.0f);
			briefcase.transform.localRotation = Quaternion.Euler(0, 0, 0);

			_bagJoints = new SDynamicsJoint[2];
			_bagJoints[0] = new SDynamicsJoint(hand.position, true);
			_bagJoints[1] = new SDynamicsJoint(hand.position + Vector3.down * 0.1f, false);
		}
		else
		{
			_Gob = GameObject.Instantiate(CGame.PrimaryResources.Prefabs[7]) as GameObject;

			// Tie Colour
			_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetColor("_PlayerColor", playerColor);
			_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetTexture("_MainTex", CGame.PrimaryResources.UnitTexTie);

			_briefcaseGOB = new GameObject();
			Transform hand = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest/R_Clavicle1/R_Up_Arm 1/R_Elbow_Top 1");
			_briefcaseGOB.transform.SetParent(hand);
			_briefcaseGOB.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
			_briefcaseGOB.transform.localScale = Vector3.one;

			GameObject briefcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
			briefcase.GetComponent<MeshRenderer>().material = CGame.PrimaryResources.FlatMat;
			briefcase.GetComponent<MeshRenderer>().material.SetColor("_Color", playerColor);
			briefcase.transform.SetParent(_briefcaseGOB.transform);
			briefcase.transform.localScale = new Vector3(0.05f, 0.1f, 0.15f);
			briefcase.transform.localPosition = new Vector3(0.0f, -0.05f, 0.0f);

			_bagJoints = new SDynamicsJoint[2];
			_bagJoints[0] = new SDynamicsJoint(hand.position, true);
			_bagJoints[1] = new SDynamicsJoint(hand.position + Vector3.down * 0.1f, false);
		}

		_Gob.name = "Unit " + mID;
		_Gob.transform.SetParent(UserSession.mPrimaryScene.transform);

		_AudioSource = _Gob.GetComponent<AudioSource>();
		_Animator = _Gob.GetComponentInChildren<Animator>();
		//_Material = _Gob.GetComponentInChildren<SkinnedMeshRenderer>().material;

		_prevRotation = mRotation;
		
		// Punch Out
		if (mOwner == UserSession.mPlayerIndex)
		{
			_punchOut = CPunchOut.Create(out _punchOutMesh);
			_punchOut.transform.SetParent(_Gob.transform);
			_punchOut.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
		}

		if (UserSession.mPlayerIndex == mOwner)
		{
			UserSession.OnEmployeeAdded(this);
		}

		// Dynamics
		Transform neck = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest/Neck");
		Vector3 forward = _Gob.transform.TransformDirection(Vector3.forward);
		
		_joints = new SDynamicsJoint[4];
		_joints[0] = new SDynamicsJoint(neck.position + forward * 0.1f, true);
		_joints[1] = new SDynamicsJoint(_joints[0].mPosition + Vector3.down * 0.13f, false);
		_joints[2] = new SDynamicsJoint(_joints[1].mPosition + Vector3.down * 0.13f, false);
		_joints[3] = new SDynamicsJoint(_joints[2].mPosition + Vector3.down * 0.13f, false);

		_tieGob = new GameObject("tie");
		_tieGob.transform.SetParent(UserSession.mPrimaryScene.transform);
		_tieGob.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
		MeshRenderer meshRenderer = _tieGob.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = _tieGob.AddComponent<MeshFilter>();		

		_tieMesh = new Mesh();
		_tieMesh.MarkDynamic();
		meshFilter.mesh = _tieMesh;
		meshRenderer.material = CGame.PrimaryResources.TieMat;
		meshRenderer.material.SetColor("_Color", playerColor);
	}

	protected override void _Update(CUserSession UserSession)
	{
		CToolkitUI ui = CGame.ToolkitUI;

		// TODO: Do this after we have moved the unit -__-
		Vector3 pivot = _Gob.transform.position;
		mBounds = new Bounds(pivot + new Vector3(0.0f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f));

		if (_carryGOB != null)
		{
			if (!mCarryingPickup)
			{
				GameObject.Destroy(_carryGOB);
			}
			else
			{
				if (mWalking)
					_carryGOB.transform.localPosition = new Vector3(0.0f, 1.05f + Mathf.Sin(Time.time * 20.0f) * 0.05f, 0.3f);
			}
		}
		else
		{
			if (mCarryingPickup)
			{
				_carryGOB = CGame.AssetManager.GetAsset<CModelAsset>("pickup").mVectorModel.CreateGameObject();
				_carryGOB.GetComponent<MeshRenderer>().material.SetColor("_FloorColor", new Color(150.0f / 255.0f, 150.0f / 255.0f, 150.0f / 255.0f, 1.0f));

				_carryGOB.transform.SetParent(_Gob.transform);
				_carryGOB.transform.localPosition = new Vector3(0.0f, 1.0f, 0.3f);
				_carryGOB.transform.localRotation = Quaternion.Euler(-10, 0, 0);
			}
		}
		
		// Icon Stack
		/*
		{
			Vector3 screenPos = Camera.main.WorldToScreenPoint(_Gob.transform.position + new Vector3(0.0f, 1.6f, 0.0f));
			Vector2 bubbleUIPos = CGame.UIManager.ConvertScreenSpaceToUISpace(new Vector2(screenPos.x, screenPos.y));
			((RectTransform)_iconStackGob.transform).anchoredPosition = bubbleUIPos;
		}
		if (_bubbleTime > 0.0f)
		{
			_UIThought.SetActive(true);
			_UIThoughtText.text = _thoughtText;

			float y = ((RectTransform)_UIThought.transform).anchoredPosition.y;
			// TODO: Safegaurd lerp.
			y = Mathf.Lerp(y, _iconStackBaselineTarget, 10.0f * Time.deltaTime);
			((RectTransform)_UIThought.transform).anchoredPosition = new Vector2(0.0f, y);
			_bubbleTime -= Time.deltaTime;
		}
		else
		{
			_UIThought.SetActive(false);
		}

		if (_iconSelectorGob != null)
		{
			_iconSelectorLerp -= Time.deltaTime * 7.0f;

			if (_iconSelectorLerp < 0.0f)
				_iconSelectorLerp = 0.0f;

			((RectTransform)_iconSelectorGob.transform).anchoredPosition = new Vector2(0.0f, Mathf.Sin(Time.time * 10.0f) * 8.0f - 4.0f + 600.0f * CGame.UIResources.UnitIconDropCurve.Evaluate(1.0f - _iconSelectorLerp));
		}
		*/
			
		/*
		if (!mDead)
		{
			_HealthBar.SetActive(true);
			Vector3 screenPos = Camera.main.WorldToScreenPoint(GetVisualPos() + new Vector3(0.0f, 2.0f, 0.0f));
			((RectTransform)_HealthBar.transform).anchoredPosition = new Vector2((int)screenPos.x, (int)screenPos.y);
			_HealthBarFill.transform.localScale = new Vector3(mStamina / mMaxStamina, 1.0f, 1.0f);
		}
		else
		{
			_HealthBar.SetActive(false);
		}
		*/

		/*
		if (mPathing)
		{
			if (mSpeed >= 4.0f)
				_footStepAudioTimer += 3.0f * Time.deltaTime;
			else if (mSpeed >= 2.0f)
				_footStepAudioTimer += 2.2f * Time.deltaTime;
			else if (mSpeed > 0.0f)
				_footStepAudioTimer += 1.6f * Time.deltaTime;

			if (_footStepAudioTimer > 1.0f)
			{
				_footStepAudioTimer = 0.0f;
				PlaySound(0, 6.0f);
			}

			// Walking

			//_Animator.SetFloat("Speed", mSpeed);
		}
		else
		{
			_footStepAudioTimer = 0.0f;
			//_Animator.SetFloat("Speed", 0.0f);
		}
		*/

		//_Animator.SetBool("Sitting", mSitting);
		//_Animator.SetBool("Dead", mDead);

		/*
		if (mFrustrated || mEating)
			_Animator.SetLayerWeight(1, 1.0f);
		else
			_Animator.SetLayerWeight(1, 0.0f);

		_Animator.SetBool("Eating", mEating);

		if (mFrustrated != _oldFrustrated)
		{
			_oldFrustrated = mFrustrated;

			if (mFrustrated)
				_Animator.SetTrigger("FrustrateTrigger");
		}
			* */

		//CDebug.DrawZRect(_unit.mBounds.center, 0.1f, 0.01f * _unit.mStamina, Color.red);

		// Interpolate to simulated position
		Vector3 targetPos = new Vector3(mPosition.x, 0.0f, mPosition.y);
		Vector3 currentPos = _Gob.transform.position;
		Vector3 dir = targetPos - currentPos;
		float distance = dir.magnitude;

		// Teleport to position if we are too far away
		if (distance >= 3.0f)
		{
			currentPos = targetPos;

			// Reset dynamics on teleport
			Transform neck = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest/Neck");
			Vector3 forward = _Gob.transform.TransformDirection(Vector3.forward);

			_joints[0] = new SDynamicsJoint(neck.position + forward * 0.1f, true);
			_joints[1] = new SDynamicsJoint(_joints[0].mPosition + Vector3.down * 0.13f, false);
			_joints[2] = new SDynamicsJoint(_joints[1].mPosition + Vector3.down * 0.13f, false);
			_joints[3] = new SDynamicsJoint(_joints[2].mPosition + Vector3.down * 0.13f, false);
		}
		else
		{
			currentPos = Vector3.Lerp(currentPos, targetPos, Mathf.Clamp(Time.deltaTime * 10.0f, 0.0f, 1.0f));
		}

		_prevRotation = Quaternion.Slerp(_prevRotation, mRotation, 5.0f * Time.deltaTime);
		//_prevRotation = Quaternion.RotateTowards(_prevRotation, mRotation, 300.0f * Time.deltaTime);
		_Gob.transform.position = currentPos;
		_Gob.transform.rotation = _prevRotation;

		_Animator.speed = 1.0f;

		if (_Animator.isInitialized)
		{
			if (mDead)
			{
				_PlayAnimation("dying_1", -1);
			}
			else if (mActionAnim != "")
			{
				_Animator.speed = mAnimSpeed;
				_PlayAnimation(mActionAnim, -1, false);
			}
			else if (mWalking)
			{
				if (mCarryingPickup)
				{
					_PlayAnimation("walking_with_item", -1);
				}
				else
				{
					if (mSpeed > 2.0f)
					{
						_PlayAnimation("running_no_item", -1);
						_Animator.SetFloat("walkSpeed", mSpeed * 0.6f);
					}
					else
					{
						if (mStamina < 50.0f)
						{
							_Animator.SetFloat("walkSpeed", mSpeed * 1.0f);
							_PlayAnimation("walking_exhausted", -1);
						}
						else
						{
							_Animator.SetFloat("walkSpeed", mSpeed * 0.5f + 0.5f);
							_PlayAnimation("walking_no_item", -1);
						}
					}
				}
			}
			else if (mAttacking)
			{
				_PlayAnimation("combat_bashing", -1);
			}
			else
			{
				float r = UnityEngine.Random.value;

				if (r > 0.8f)
					_PlayAnimation("idle_standing_action_1", 1.0f);
				else if (r > 0.6f)
					_PlayAnimation("idle_standing_action_2", 1.0f);
				else
					_PlayAnimation("idle_standing_general", 5.0f);
			}
		}

		// Sound
		if (SoundID > SoundLastPlayedID)
		{
			_AudioSource.pitch = CGame.UniversalRandom.GetNextFloat() * 0.5f + 0.75f;
			_AudioSource.PlayOneShot(CGame.Resources.GetAudioClip(SoundType), SoundVolume);
			SoundLastPlayedID = SoundID;
		}

		if (mAssignedDeskID == -1)
		{
			if (_texState != 0)
			{
				_texState = 0;
				_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetTexture("_MainTex", CGame.PrimaryResources.UnitTexTie);
			}
		}
		else
		{
			if (_texState == 0)
			{
				_texState = 1;
				//_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetTexture("_MainTex", CGame.PrimaryResources.UnitTexTieArmband);
			}
		}

		// TODO: Hide briefcase when sitting/using?
		/*
		if (_briefcaseGOB != null)
		{
			if (mAssignedDeskID == -1)
			{
				_briefcaseGOB.SetActive(true);
			}
			else
			{
				_briefcaseGOB.SetActive(false);
			}
		}
		*/

		if (mOwner == UserSession.mPlayerIndex)
		{
			if (_indicatorGob == null)
			{
				_indicatorGob = ui.CreateElement(CGame.UIManager.overlayLayer, "indicator");
				ui.SetAnchors(_indicatorGob, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.0f));
				ui.SetTransform(_indicatorGob, 50, -200, 56, 65);
				Image indImg = _indicatorGob.AddComponent<Image>();
				indImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
				indImg.sprite = ui.IndicatorBkg;

				GameObject indIcon = ui.CreateElement(_indicatorGob, "icon");
				ui.SetAnchors(indIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
				ui.SetTransform(indIcon, 0, 3, 56, 56);
				indImg = indIcon.AddComponent<Image>();
				indImg.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

				_indicatorGob.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
				//_indicatorGob.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
			}

			mThoughtTime += Time.deltaTime;

			if (mLastThoughtState != mThoughtState)
			{
				mThoughtTime = 0.0f;
				Image indImg = _indicatorGob.transform.GetChild(0).GetComponent<Image>();
				mLastThoughtState = mThoughtState;

				indImg.sprite = ui.IndicatorNoDesk;
			}

			if (mThoughtTime < 2.0f)
			{
				mThoughtAlpha += Time.deltaTime * 5.0f;
			}
			else
			{
				mThoughtAlpha -= Time.deltaTime * 5.0f;
			}

			mThoughtAlpha = Mathf.Clamp01(mThoughtAlpha);

			if (mThoughtAlpha > 0.0f)
			{
				_indicatorGob.SetActive(true);
				Vector3 screenPos = Camera.main.WorldToScreenPoint(_Gob.transform.position + new Vector3(0.0f, 1.8f, 0.0f));
				((RectTransform)_indicatorGob.transform).anchoredPosition = new Vector2((int)screenPos.x, (int)screenPos.y);
				_indicatorGob.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f * mThoughtAlpha);
				_indicatorGob.transform.GetChild(0).GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f * mThoughtAlpha);
			}
			else
			{
				_indicatorGob.SetActive(false);
			}
		}

		if (mSpeech != "")
		{
			if (mSpeechBubble == null)
			{
				mSpeechBubble = ui.CreateElement(CGame.UIManager.overlayLayer, "speechBubble");
				ui.SetAnchors(mSpeechBubble, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.0f));
				ui.SetTransform(mSpeechBubble, 50, -200, 100, 20);
				ui.AddImage(mSpeechBubble, new Color(1.0f, 1.0f, 1.0f, 1.0f));
				ui.AddHorizontalLayout(mSpeechBubble, new RectOffset(8, 8, 8, 8));
				ContentSizeFitter fitter = mSpeechBubble.AddComponent<ContentSizeFitter>();
				fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

				GameObject text = ui.CreateElement(mSpeechBubble, "text");
				mSpeechBubbleText = text.AddComponent<Text>();
				mSpeechBubbleText.font = CGame.GameUIStyle.FontB;
				mSpeechBubbleText.color = CGame.GameUIStyle.ThemeColorC;
				mSpeechBubbleText.text = "";

				mSpeechTimer = 0;
			}

			Vector3 screenPos = Camera.main.WorldToScreenPoint(_Gob.transform.position + new Vector3(0.0f, 2.0f, 0.0f));
			((RectTransform)mSpeechBubble.transform).anchoredPosition = new Vector2((int)screenPos.x, (int)screenPos.y);

			while (mSpeechTimer >= 0.03f)
			{
				mSpeechTimer -= 0.03f;

				if (mSpeechBubbleText.text != mSpeech)
				{
					if (mSpeech.Length < mSpeechBubbleText.text.Length)
					{
						mSpeechBubbleText.text = mSpeech[0].ToString();
					}
					else
					{
						bool match = true;
						for (int i = 0; i < mSpeechBubbleText.text.Length; ++i)
						{
							if (mSpeechBubbleText.text[i] != mSpeech[i])
							{
								mSpeechBubbleText.text = mSpeech[0].ToString();
								match = false;
								break;
							}
						}

						if (match)
						{
							mSpeechBubbleText.text += mSpeech[mSpeechBubbleText.text.Length];
						}
					}
				}
			}

			mSpeechTimer += Time.deltaTime;
		}
		else
		{
			GameObject.Destroy(mSpeechBubble);
			mSpeechBubble = null;
			mSpeechBubbleText = null;
		}

		if (mOwner == UserSession.mPlayerIndex)
		{
			Vector3 pos = _Gob.transform.position;
			CPunchOut.UpdateMesh(_punchOutMesh, _worldView, pos.ToWorldVec2(), 8.0f);
			_punchOut.transform.position = new Vector3(pos.x, 0.0f, pos.z);
			_punchOut.transform.rotation = Quaternion.identity;

			if (mUIEmployeeEntry != null)
			{
				//mUIEmployeeEntry.mStaminaBar.localScale = new Vector3(mStamina / mStats.mMaxStamina, 1, 1);
			}
		}

		/*
		if (mID == 6)
		{
			CGame.CameraManager.SetTargetPosition(mPosition.ToWorldVec3());
		}
		*/

		_Gob.transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material.SetFloat("_Stam", mStamina / 100.0f);
	}

	private string _playingState = "";
	private float _cancelTimeout = 0.0f;

	private void _PlayAnimation(string Name, float CancelTimeout = 0.0f, bool Restart = false)
	{
		if ((CancelTimeout != -1) && (_cancelTimeout > Time.realtimeSinceStartup))
			return;
		
		_cancelTimeout = Time.realtimeSinceStartup + CancelTimeout;

		/*
		AnimatorStateInfo state = _Animator.GetCurrentAnimatorStateInfo(0);

		if (!state.loop && state.normalizedTime >= 1.0f)
		{
			_Animator.Play(state.shortNameHash, 0, state.normalizedTime - 1.0f);
		}
		*/

		if (Restart || _playingState != Name)
		{
			_playingState = Name;
			_Animator.CrossFadeInFixedTime(Name, 0.2f);
		}
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		if (UserSession.mPlayerIndex == mOwner)
		{
			UserSession.OnEmployeeRemoved(this);
		}
		//Debug.Log("Destroy VC");

		if (_Gob != null)
			GameObject.Destroy(_Gob);

		// TODO: Cleanup punchout mesh??

		//if (_UIThought != null)
			//GameObject.Destroy(_UIThought);

		if (_carryGOB != null)
			GameObject.Destroy(_carryGOB);

		if (mSpeechBubble != null)
			GameObject.Destroy(mSpeechBubble);

		if (_indicatorGob != null)
			GameObject.Destroy(_indicatorGob);

		if (_tieGob != null)
		{
			GameObject.Destroy(_tieGob);
			GameObject.Destroy(_tieMesh);
			// TODO: Kill tie material copy.
		}
	}

	public void SetInfoBubble(string Icon)
	{
		//_thoughtText = Icon;
		//_bubbleTime = 2.0f;
	}

	/// <summary>
	/// Play audio on channel 1.
	/// </summary>
	public void PlaySound(int Id, float Volume)
	{
		SoundVolume = Volume;
		SoundType = Id;
		++SoundID;
	}

	private GameObject _iconSelectorGob;
	private float _iconSelectorLerp;

	public override void Select()
	{
		/*
		_iconStackBaselineTarget = 40.0f;
		_iconSelectorLerp = 1.0f;
		_iconSelectorGob = GameObject.Instantiate(CGame.UIResources.UnitIconSelector) as GameObject;
		_iconSelectorGob.transform.SetParent(_iconStackGob.transform);
		_iconSelectorGob.transform.localScale = Vector3.one;
		_iconSelectorGob.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.0f);
		*/

		/*
		if (_unit.mWorld.GetCurrentPlayerID() == -1)
			_iconSelectorGob.GetComponent<Image>().color = Color.black;
		else
			_iconSelectorGob.GetComponent<Image>().color = _unit.mWorld.mPlayers[_unit.mWorld.GetCurrentPlayerID()].mColor;
		*/
	}

	public override void Deselect()
	{
		/*
		_iconStackBaselineTarget = 0.0f;

		if (_iconSelectorGob != null)
			GameObject.Destroy(_iconSelectorGob);
		*/
	}

	ESelectionType ISelectable.GetType()
	{
		return ESelectionType.UNIT;
	}

	string ISelectable.GetInfo()
	{
		return mName;
	}

	void ISelectable.PrintInfo()
	{
		
	}

	int ISelectable.GetID()
	{
		return mID;
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
		return mBounds.IntersectRay(R);
	}

	void ISelectable.GetRenderers(List<Renderer> Renderers)
	{
		Renderers.Add(_Gob.transform.GetChild(0).GetChild(0).GetComponent<Renderer>());

		Renderers.Add(_tieGob.GetComponent<Renderer>());

		if (_carryGOB != null)
			Renderers.Add(_carryGOB.GetComponent<MeshRenderer>());

		if (_briefcaseGOB != null && mAssignedDeskID == -1)
			Renderers.Add(_briefcaseGOB.transform.GetChild(0).GetComponent<MeshRenderer>());
	}

	public override void DrawDebugPrims()
	{
		Transform neck = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest/Neck");
		Transform chest = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest");

		Vector3 forward = _Gob.transform.TransformDirection(Vector3.forward);
		Vector3 right = _Gob.transform.TransformDirection(Vector3.right);
		_joints[0].mPosition = neck.position + forward * 0.1f;

		int solveIters = 15;
		float restDistance = 0.13f;
		float sphereRadius = 0.2f;

		for (int i = 0; i < _joints.Length; ++i)
		{
			Vector3 diff = _joints[i].mPosition - chest.transform.position;
			float distSq = diff.sqrMagnitude;

			if (distSq < sphereRadius * sphereRadius)
				_joints[i].mPosition = chest.transform.position + diff.normalized * sphereRadius;
			
			_joints[i].Update(Time.deltaTime, 0.9985f, 50.0f);
		}

		for (int i = 0; i < solveIters; ++i)
		{
			for (int c = 0; c < 3; ++c)
			{
				// For each constraint
				// Solve

				int ji0 = c + 0;
				int ji1 = c + 1;

				Vector3 diff = _joints[ji0].mPosition - _joints[ji1].mPosition;
				float distance = diff.magnitude;
				float scalar = (restDistance - distance) / distance;
				Vector3 translation = diff * 0.5f * scalar;

				if (!_joints[ji0].mFixed)
					_joints[ji0].mPosition += translation;

				if (!_joints[ji1].mFixed)
					_joints[ji1].mPosition -= translation;
			}
		}

		// Bag Joints
		if (!mIntern)
		{
			Transform hand = _Gob.transform.FindChild("unit_test/Hips/Spine/Chest/R_Clavicle1/R_Up_Arm 1/R_Elbow_Top 1");
			_bagJoints[0].mPosition = hand.position;
		}
		else
		{
			Transform hand = _Gob.transform.FindChild("unit_test/Hips");
			_bagJoints[0].mPosition = hand.TransformPoint(new Vector3(0.02f, 0.02f, -0.065f));
		}

		_bagJoints[1].Update(Time.deltaTime, 0.9999f, 50.0f);

		float bagRestDistance = 0.5f;

		for (int i = 0; i < solveIters; ++i)
		{
			//Vector3 diff = _bagJoints[0].mPosition - _bagJoints[1].mPosition;
			Vector3 diff = (_bagJoints[0].mPosition - right * 0.2f) - _bagJoints[1].mPosition;
			float distance = diff.magnitude;
			float scalar = (bagRestDistance - distance) / distance;
			Vector3 translation = diff * 0.5f * scalar;

			_bagJoints[1].mPosition -= translation;
		}

		Vector3 bagDir = (_bagJoints[0].mPosition - _bagJoints[1].mPosition).normalized;
		Vector3 bagRight = Vector3.Cross(right, bagDir);

		if (!mIntern)
			_briefcaseGOB.transform.rotation = Quaternion.LookRotation(bagRight, bagDir);
		else
			_briefcaseGOB.transform.rotation = Quaternion.LookRotation(bagRight, bagDir);
		
		//CDebug.DrawLine(_bagJoints[0].mPosition, _bagJoints[1].mPosition, Color.red);

		/*
		for (int i = 0; i < _joints.Length - 1; ++i)
		{
			CDebug.DrawYRectQuad(_joints[i].mPosition, 0.05f, 0.05f, Color.green);
			CDebug.DrawLine(_joints[i + 0].mPosition, _joints[i + 1].mPosition, Color.red);
		}
		*/

		float tiehw = 0.05f;
		// TODO: Can keep these arrays for next tie update.
		// Don't need to re-update index buffer or UVs.
		Vector3[] verts = new Vector3[8];
		Vector2[] uvs = new Vector2[8];
		int[] tris = new int[18];		
		
		verts[0] = tiehw * right + _joints[0].mPosition;		
		verts[1] = tiehw * -right + _joints[0].mPosition;
		uvs[0] = new Vector2(0, 1);
		uvs[1] = new Vector2(1, 1);

		verts[2] = tiehw * right + _joints[1].mPosition;
		verts[3] = -tiehw * right + _joints[1].mPosition;
		uvs[2] = new Vector2(0, 0.66f);
		uvs[3] = new Vector2(1, 0.66f);

		verts[4] = tiehw * right + _joints[2].mPosition;
		verts[5] = -tiehw * right + _joints[2].mPosition;
		uvs[4] = new Vector2(0, 0.33f);
		uvs[5] = new Vector2(1, 0.33f);

		verts[6] = tiehw * right + _joints[3].mPosition;
		verts[7] = tiehw * -right + _joints[3].mPosition;
		uvs[6] = new Vector2(0, 0);
		uvs[7] = new Vector2(1, 0);

		tris[0] = 0;
		tris[1] = 1;
		tris[2] = 3;
		tris[3] = 0;
		tris[4] = 3;
		tris[5] = 2;

		tris[6] = 2;
		tris[7] = 3;
		tris[8] = 5;
		tris[9] = 2;
		tris[10] = 5;
		tris[11] = 4;

		tris[12] = 4;
		tris[13] = 5;
		tris[14] = 7;
		tris[15] = 4;
		tris[16] = 7;
		tris[17] = 6;

		_tieMesh.Clear(true);
		_tieMesh.vertices = verts;
		_tieMesh.triangles = tris;
		_tieMesh.uv = uvs;
	}

	void ISelectable.Hover()
	{
		mThoughtTime = 0.0f;
	}

	void ISelectable.HoverOut()
	{
	}
}
