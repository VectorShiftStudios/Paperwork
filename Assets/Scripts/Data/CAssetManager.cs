using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public enum EAssetType
{
	AT_UNKNOWN = 0,
	AT_BRUSH = 1,
	AT_MODEL = 2,
	AT_ITEM = 3,
	AT_LEVEL = 4,
	AT_COMPANY = 5,
	AT_SHEET = 6
}

/// <summary>
/// Indicates that an asset exists, but possibly not loaded to memory.
/// </summary>
public class CAssetDeclaration
{
	public string mName;
	public string mFileName;
	public string mModifiedDate;
	public EAssetType mType;
	public int mVersion;		 
	public bool mLoaded;
	public CAsset mAsset;

	public void LoadAsset()
	{
		CAsset asset = null;
		if (mType == EAssetType.AT_BRUSH) asset = new CBrushAsset();
		else if (mType == EAssetType.AT_MODEL) asset = new CModelAsset();
		else if (mType == EAssetType.AT_LEVEL) asset = new CLevelAsset();
		else if (mType == EAssetType.AT_ITEM) asset = new CItemAsset();

		if (asset != null)
		{
			asset.mName = mName;
			asset.mFileName = mFileName;
			asset.mType = mType;
			asset.Load();
			mAsset = asset;
		}
		else
		{
			Debug.LogError("Could not load asset: " + mName);
		}
	}
}

/// <summary>
/// Base class for all assets.
/// </summary>
public class CAsset
{
	public string mName;
	public string mFileName;
	public EAssetType mType;
	public int mVersion;

	public virtual void Serialize(BinaryWriter W)
	{
		W.Write((int)mType);
	}

	public virtual void Deserialize(BinaryReader R)
	{
		R.ReadInt32();
	}

	public void Load()
	{
		Debug.Log("Load Asset: " + mName);

		FileStream file = File.OpenRead(mFileName);
		BinaryReader r = new BinaryReader(file);

		Deserialize(r);

		file.Close();
	}

	public void Save()
	{
		FileStream file = File.OpenWrite(mFileName);
		BinaryWriter w = new BinaryWriter(file);

		Serialize(w);

		file.Close();
	}
}

public class CBrushAsset : CAsset
{
	public const int VERSION = 2;

	public Color mRawColor;
	public float mWeight;
	public float mFloorMix;

	public Color32 mColor;

	public CBrushAsset()
	{
		mType = EAssetType.AT_BRUSH;

		mRawColor = new Color(1, 1, 1, 1);
		mWeight = 1.0f;
		mFloorMix = 0.0f;

		CompileColor();
	}

	public override void Serialize(BinaryWriter W)
	{
		base.Serialize(W);

		W.Write(VERSION);

		W.Write(mRawColor.r);
		W.Write(mRawColor.g);
		W.Write(mRawColor.b);
		W.Write(mRawColor.a);
		W.Write(mWeight);
		W.Write(mFloorMix);
	}

	public override void Deserialize(BinaryReader R)
	{
		base.Deserialize(R);
		int version = R.ReadInt32();

		if (version == 2)
		{
			mRawColor.r = R.ReadSingle();
			mRawColor.g = R.ReadSingle();
			mRawColor.b = R.ReadSingle();
			mRawColor.a = R.ReadSingle();
			mWeight = R.ReadSingle();
			mFloorMix = R.ReadSingle();
		}
		
		CompileColor();
	}

	public void CompileColor()
	{
		mColor = new Color32((byte)(mRawColor.r * 255), (byte)(mRawColor.g * 255), (byte)(mRawColor.b * 255), (byte)((1.0f - mFloorMix) * 255));
	}
}

public class CModelAsset : CAsset
{
	public const int VERSION = 3;

	public CVectorModel mVectorModel;

	public CModelAsset()
	{
		mType = EAssetType.AT_MODEL;
		mVectorModel = new CVectorModel();
		mVectorModel.RebuildEverything();
	}

	public override void Serialize(BinaryWriter W)
	{
		base.Serialize(W);
		W.Write(VERSION);
		mVectorModel.Serialize(W);
	}

	public override void Deserialize(BinaryReader R)
	{
		base.Deserialize(R);
		int version = R.ReadInt32();
		mVectorModel.Deserialize(R, version);

		if (version == 3)
		{
			
		}

		mVectorModel.RebuildEverything();
	}
}

public enum EItemType
{
	NONE = 0,
	START = 1,
	DECO = 2,
	DESK = 3,
	SAFE = 4,
	REST = 5,
	FOOD = 6,
	DOOR = 7,
	CAMERA = 8
}

public class CItemType
{
}

public class CItemAsset : CAsset
{
	public class CModelDecl
	{
		public CModelAsset mAsset;
		public Vector3 mPosition;
		public Vector3 mRotation;
	}

	public class CUsageSlot
	{
		public Vector3 mEntryPosition;
		public Vector3 mEntryRotation;
		public Vector3 mEntrySize;
		public Vector3 mUsePosition;
		public Vector3 mUseRotation;
	}
	
	public struct STile
	{
		public bool mSolid;
		// block projectile height?
		// block vission?
		// entry point for usage slot?
	}

	public const int VERSION = 11;

	public EItemType mItemType;
	
	public string mFriendlyName;
	public string mFlavourText;
	public int mCost;
	public int mDurability;
	public float mValue;
	public float mStress;
	public float mAreaOfEffect;
	public int mSheetsInMax;
	public int mSheetsOutMax;

	public int mWidth;
	public int mLength;

	public STile[,] mTiles;
	
	public List<CUsageSlot> mUsageSlots;

	// TODO: Models REALLY should be attach points or something, list of models etc.
	// None of this primary, secondary nonsense.

	public CModelAsset mPMAsset;
	public Vector3 mPMPosition;
	public Vector3 mPMRotation;

	public CModelAsset mSMAsset;
	public Vector3 mSMPosition;
	public Vector3 mSMRotation;

	public Vector3 mPaperInPosition;
	public Vector3 mPaperInRotation;

	public Vector3 mPaperOutPosition;
	public Vector3 mPaperOutRotation;

	public Vector3 mIconCameraPostion; // X, Z, Zoom
	public EViewDirection mIconViewDirection;

	// Thumbnail rendered at runtime
	public Texture mIconTexture;
	public Rect mIconRect;

	public CItemAsset()
	{
		mType = EAssetType.AT_ITEM;
		mItemType = EItemType.DECO;
		mFriendlyName = "New Item";
		mFlavourText = "No Description";
		mWidth = 1;
		mLength = 1;
		mUsageSlots = new List<CUsageSlot>();
		mTiles = new STile[mWidth * 2, mLength * 2];

		/*
		for (int iX = 0; iX < mWidth * 2; ++iX)
			for (int iY = 0; iY < mLength * 2; ++iY)
				mTiles[iX, iY] = new STile();
		*/
	}

	public override void Serialize(BinaryWriter W)
	{
		base.Serialize(W);
		W.Write(VERSION);

		W.Write((int)mItemType);
		W.Write(mFriendlyName);
		W.Write(mFlavourText);
		W.Write(mWidth);
		W.Write(mLength);

		for (int iX = 0; iX < mWidth * 2; ++iX)
			for (int iY = 0; iY < mLength * 2; ++iY)
			{
				W.Write(mTiles[iX, iY].mSolid);
			}

		W.Write(mDurability);
		W.Write(mCost);
		W.Write(mValue);
		W.Write(mStress);
		W.Write(mAreaOfEffect);
		W.Write(mSheetsInMax);
		W.Write(mSheetsOutMax);

		if (mPMAsset == null)
			W.Write("");
		else
			W.Write(mPMAsset.mName);

		CUtility.WriteVec3(W, mPMPosition);
		CUtility.WriteVec3(W, mPMRotation);

		if (mSMAsset == null)
			W.Write("");
		else
			W.Write(mSMAsset.mName);

		CUtility.WriteVec3(W, mSMPosition);
		CUtility.WriteVec3(W, mSMRotation);

		W.Write(mUsageSlots.Count);
		for (int i = 0; i < mUsageSlots.Count; ++i)
		{
			CUsageSlot s = mUsageSlots[i];
			CUtility.WriteVec3(W, s.mEntryPosition);
			CUtility.WriteVec3(W, s.mEntryRotation);
			CUtility.WriteVec3(W, s.mEntrySize);
			CUtility.WriteVec3(W, s.mUsePosition);
			CUtility.WriteVec3(W, s.mUseRotation);
		}

		// Desk
		CUtility.WriteVec3(W, mPaperInPosition);
		CUtility.WriteVec3(W, mPaperInRotation);
		CUtility.WriteVec3(W, mPaperOutPosition);
		CUtility.WriteVec3(W, mPaperOutRotation);

		//Icon
		CUtility.WriteVec3(W, mIconCameraPostion);
		W.Write((int)mIconViewDirection);
	}

	public override void Deserialize(BinaryReader R)
	{
		base.Deserialize(R);
		int version = R.ReadInt32();
		mVersion = version;

		if (version == 11)
		{
			mItemType = (EItemType)R.ReadInt32();
			mFriendlyName = R.ReadString();
			mFlavourText = R.ReadString();
			mWidth = R.ReadInt32();
			mLength = R.ReadInt32();

			mTiles = new STile[mWidth * 2, mLength * 2];
			for (int iX = 0; iX < mWidth * 2; ++iX)
				for (int iY = 0; iY < mLength * 2; ++iY)
				{
					mTiles[iX, iY].mSolid = R.ReadBoolean();
				}

			mDurability = R.ReadInt32();
			mCost = R.ReadInt32();
			mValue = R.ReadSingle();
			mStress = R.ReadSingle();
			mAreaOfEffect = R.ReadSingle();
			mSheetsInMax = R.ReadInt32();
			mSheetsOutMax = R.ReadInt32();

			string modelName = R.ReadString();
			if (modelName != "")
				mPMAsset = CGame.AssetManager.GetAsset<CModelAsset>(modelName);

			mPMPosition = CUtility.ReadVec3(R);
			mPMRotation = CUtility.ReadVec3(R);

			modelName = R.ReadString();
			if (modelName != "")
				mSMAsset = CGame.AssetManager.GetAsset<CModelAsset>(modelName);

			mSMPosition = CUtility.ReadVec3(R);
			mSMRotation = CUtility.ReadVec3(R);

			int usageSlotCount = R.ReadInt32();
			mUsageSlots.Clear();
			for (int i = 0; i < usageSlotCount; ++i)
			{
				CUsageSlot s = new CUsageSlot();
				s.mEntryPosition = CUtility.ReadVec3(R);
				s.mEntryRotation = CUtility.ReadVec3(R);
				s.mEntrySize = CUtility.ReadVec3(R);
				s.mUsePosition = CUtility.ReadVec3(R);
				s.mUseRotation = CUtility.ReadVec3(R);
				mUsageSlots.Add(s);
			}

			// Desk
			mPaperInPosition = CUtility.ReadVec3(R);
			mPaperInRotation = CUtility.ReadVec3(R);
			mPaperOutPosition = CUtility.ReadVec3(R);
			mPaperOutRotation = CUtility.ReadVec3(R);

			// Icon
			mIconCameraPostion = CUtility.ReadVec3(R);
			mIconViewDirection = (EViewDirection)R.ReadInt32();
		}
		else
		{
			Debug.LogError("Can't load item asset '" + mName + "' with version " + version);
		}
	}

	private void _CreatePrimaryModel(GameObject Parent, Material SharedMat = null)
	{
		if (mPMAsset != null)
		{
			// TODO: Compress all these identical creations.
			//GameObject modelGob = mPMAsset.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT, SharedMat);
			GameObject modelGob = mPMAsset.mVectorModel.CreateGameObject(SharedMat);
			modelGob.transform.SetParent(Parent.transform);
			modelGob.transform.localPosition = mPMPosition;
			modelGob.transform.localRotation = Quaternion.Euler(mPMRotation);
		}
	}

	private void _CreateSecondaryModel(GameObject Parent, Material SharedMat = null)
	{
		if (mSMAsset != null)
		{
			GameObject modelGob = mSMAsset.mVectorModel.CreateGameObject(SharedMat);
			modelGob.transform.SetParent(Parent.transform);
			modelGob.transform.localPosition = mSMPosition;
			modelGob.transform.localRotation = Quaternion.Euler(mSMRotation);
		}
	}

	private void _CreatePaperModel(GameObject Parent, Vector3 Position, Vector3 Rotation)
	{
		CModelAsset paperModel = CGame.AssetManager.GetAsset<CModelAsset>("default_contract_paper");

		GameObject paper = paperModel.mVectorModel.CreateGameObject();
		paper.transform.SetParent(Parent.transform);
		paper.transform.localPosition = Position;
		paper.transform.localRotation = Quaternion.Euler(Rotation);
	}

	public GameObject CreateVisuals(EViewDirection ViewDirection)
	{
		GameObject gob = new GameObject();

		// TODO: Take the overall view direction, and work out the individual model view directions.

		Material sharedMat = (Material)GameObject.Instantiate(CGame.WorldResources.VectorMat);

		if (mItemType == EItemType.DESK)
		{
			_CreatePrimaryModel(gob, sharedMat);
			_CreateSecondaryModel(gob, sharedMat);

			/*
			CModelAsset paperModel = CGame.AssetManager.GetAsset<CModelAsset>("default_contract_paper");

			GameObject paper = paperModel.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
			paper.transform.SetParent(gob.transform);
			paper.transform.localPosition = mPaperInPosition;
			paper.transform.localRotation = Quaternion.Euler(mPaperInRotation);

			paper = paperModel.mVectorModel.CreateGameObject(EViewDirection.VD_FRONT);
			paper.transform.SetParent(gob.transform);
			paper.transform.localPosition = mPaperOutPosition;
			paper.transform.localRotation = Quaternion.Euler(mPaperOutRotation);
			*/
		}
		else if (mItemType == EItemType.DECO)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}
		else if (mItemType == EItemType.SAFE)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}
		else if (mItemType == EItemType.FOOD)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}
		else if (mItemType == EItemType.REST)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}
		else if (mItemType == EItemType.START)
		{
			_CreatePrimaryModel(gob, sharedMat);

			if (mSMAsset != null)
			{
				GameObject doorGob = mSMAsset.mVectorModel.CreateGameObject(sharedMat);

				doorGob.transform.SetParent(gob.transform);
				doorGob.transform.localPosition = new Vector3(0, 0, 1.001f);
				doorGob.transform.localRotation = Quaternion.Euler(0, 0, 0);

				doorGob = mSMAsset.mVectorModel.CreateGameObject(sharedMat);
				doorGob.transform.SetParent(gob.transform);
				doorGob.transform.localPosition = new Vector3(2, 0, 1.001f);
				doorGob.transform.localRotation = Quaternion.Euler(0, 180, 0);
			}
			else
			{
				Debug.LogError("Start Item Asset needs a door model!");
			}
		}
		else if (mItemType == EItemType.DOOR)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}
		else if (mItemType == EItemType.CAMERA)
		{
			_CreatePrimaryModel(gob, sharedMat);
		}

		gob.AddComponent<Item>().Init(sharedMat);

		return gob;
	}

	// TODO: Consider this.
	public GameObject CreateSimulationItem()
	{
		// Create entity directly?
		// Factory depending on type.
		// Might be in another class responsible for spawning things.
		// Deserialize props from level template or save game.
		return null;
	}
}

public class CLevelMetaObject
{
	public enum EType
	{
		ITEM,
		UNIT,
		VOLUME,
		PICKUP,
		DECAL,
		PLAYER,
	}
	
	public GameObject mGOB = null;
	public int mID = 0;
	public EType mType = EType.ITEM;
	public int mSubtype = 0;
	public int mOwner = 0;
	public string mIdentifier = "";
	public int mData = 0;
	public Vector3 mPositionA = Vector3.zero;
	public Vector3 mPositionB = Vector3.zero;
	public int mRotation = 0;
	public Vector3 mOrientation = Vector3.zero;
	public int mExtraIntData = 0;
	public float mExtraFloatData = 0.0f;
	public bool mExtraBoolData = false;
	public Color mColor = new Color(1, 1, 1, 1);

	public CLevelMetaObject() { }

	public CLevelMetaObject Clone(CLevelMetaObject MetaObject, int Id)
	{
		CLevelMetaObject meta = (CLevelMetaObject)MetaObject.MemberwiseClone();
		meta.mGOB = null;
		meta.mID = Id;

		return meta;
	}

	public CLevelMetaObject(CLevelMetaObject MetaObject, int Id)
	{
		
		mGOB = null;
		mID = Id;
		mType = MetaObject.mType;
		mSubtype = MetaObject.mSubtype;
		mOwner = MetaObject.mOwner;
		mIdentifier = MetaObject.mIdentifier;
		mData = MetaObject.mData;
		mPositionA = MetaObject.mPositionA;
		mPositionB = MetaObject.mPositionB;
		mRotation = MetaObject.mRotation;
		mOrientation = MetaObject.mOrientation;
		mExtraIntData = MetaObject.mExtraIntData;
		mExtraFloatData = MetaObject.mExtraFloatData;
		mExtraBoolData = MetaObject.mExtraBoolData;
		mColor = MetaObject.mColor;
	}
}

public class CLevelAsset : CAsset
{
	public const int VERSION = 5;

	public Color mBackgroundColor;

	public Color32[,] mTileColors;
	public int[,] mWallX;
	public int[,] mWallZ;

	public List<CLevelMetaObject> mObjects;
	
	public CLevelAsset()
	{
		mType = EAssetType.AT_LEVEL;

		mBackgroundColor = new Color(151.0f / 255.0f, 145.0f / 255.0f, 136.0f / 255.0f, 1.0f);

		mTileColors = new Color32[100, 100];
		mWallX = new int[100, 100];
		mWallZ = new int[100, 100];

		for (int iX = 0; iX < 100; ++iX)
			for (int iY = 0; iY < 100; ++iY)
			{
				mTileColors[iX, iY] = new Color32(128, 128, 128, 255);
				mWallX[iX, iY] = 0;
				mWallZ[iX, iY] = 0;
			}

		mObjects = new List<CLevelMetaObject>();
	}

	public override void Serialize(BinaryWriter W)
	{
		base.Serialize(W);
		W.Write(VERSION);

		W.Write(mBackgroundColor.r);
		W.Write(mBackgroundColor.g);
		W.Write(mBackgroundColor.b);

		for (int iX = 0; iX < 100; ++iX)
		{
			for (int iY = 0; iY < 100; ++iY)
			{
				W.Write(mTileColors[iX, iY].r);
				W.Write(mTileColors[iX, iY].g);
				W.Write(mTileColors[iX, iY].b);

				W.Write(mWallX[iX, iY]);
				W.Write(mWallZ[iX, iY]);
			}
		}

		W.Write(mObjects.Count);
		for (int i = 0; i < mObjects.Count; ++i)
		{
			CLevelMetaObject meta = mObjects[i];

			W.Write(meta.mID);
			W.Write((int)meta.mType);
			W.Write(meta.mSubtype);
			W.Write(meta.mOwner);
			W.Write(meta.mIdentifier);
			W.Write(meta.mData);
			CUtility.WriteVec3(W, meta.mPositionA);
			CUtility.WriteVec3(W, meta.mPositionB);
			W.Write(meta.mRotation);
			CUtility.WriteVec3(W, meta.mOrientation);
			W.Write(meta.mExtraIntData);
			W.Write(meta.mExtraFloatData);
			W.Write(meta.mExtraBoolData);
			CUtility.WriteColor(W, meta.mColor);
		}
	}

	public override void Deserialize(BinaryReader R)
	{
		base.Deserialize(R);
		int version = R.ReadInt32();

		if (version == 4 || version == 5)
		{
			mBackgroundColor.r = R.ReadSingle();
			mBackgroundColor.g = R.ReadSingle();
			mBackgroundColor.b = R.ReadSingle();

			for (int iX = 0; iX < 100; ++iX)
			{
				for (int iY = 0; iY < 100; ++iY)
				{
					Color32 tileCol;
					tileCol.r = R.ReadByte();
					tileCol.g = R.ReadByte();
					tileCol.b = R.ReadByte();
					tileCol.a = 255;

					mTileColors[iX, iY] = tileCol;

					mWallX[iX, iY] = R.ReadInt32();
					mWallZ[iX, iY] = R.ReadInt32();
				}
			}
		}

		if (version == 5)
		{
			mObjects.Clear();
			int metaCount = R.ReadInt32();
			for (int i = 0; i < metaCount; ++i)
			{
				CLevelMetaObject meta = new CLevelMetaObject();
				mObjects.Add(meta);

				meta.mID = R.ReadInt32();
				meta.mType = (CLevelMetaObject.EType)R.ReadInt32();
				meta.mSubtype = R.ReadInt32();
				meta.mOwner = R.ReadInt32();
				meta.mIdentifier = R.ReadString();
				meta.mData = R.ReadInt32();
				meta.mPositionA = CUtility.ReadVec3(R);
				meta.mPositionB = CUtility.ReadVec3(R);
				meta.mRotation = R.ReadInt32();
				meta.mOrientation = CUtility.ReadVec3(R);
				meta.mExtraIntData = R.ReadInt32();
				meta.mExtraFloatData = R.ReadSingle();
				meta.mExtraBoolData = R.ReadBoolean();
				meta.mColor = CUtility.ReadColor(R);
			}
		}
	}

	public void CreateMap(CMap Map)
	{
		Map.Init(100, mBackgroundColor);

		for (int iX = 0; iX < 100; ++iX)
			for (int iY = 0; iY < 100; ++iY)
			{
				Map.mTiles[iX, iY].mTint = mTileColors[iX, iY];
				Map.mTiles[iX, iY].mWallX.mType = mWallX[iX, iY];
				Map.mTiles[iX, iY].mWallZ.mType = mWallZ[iX, iY];
			}

		Map.GenerateStaticWallCollisions();
		Map.GenerateVisibilitySegments();
	}
}

public class CTierStat
{
}

public class CTierStatI : CTierStat
{
	public int mBase;
	public int mIncrease;
}

public class CTierStatF : CTierStat
{
	public float mBase;
	public float mIncrease;

	public void Set(CJSONValue Data)
	{
		mBase = Data.GetFloat("base");
		mIncrease = Data.GetFloat("inc");
	}

	public float Get(int Level)
	{
		return mBase + mIncrease * (Level - 1);
	}
}

/// <summary>
/// Describes stats for a tier.
/// </summary>
public class CTierInfo
{
	public string mTitle;
	public int mTierIndex;
	public int mMaxLevel;

	public CTierStatF mSalary = new CTierStatF();
	public CTierStatF mIntelligence = new CTierStatF();
	public CTierStatF mMaxStamina = new CTierStatF();
	public CTierStatF mMaxSpeed = new CTierStatF();
	public CTierStatF mHungerRate = new CTierStatF();
	public CTierStatF mMaxHunger = new CTierStatF();
	public CTierStatF mPaperCapacity = new CTierStatF();
	public CTierStatF mAttackDamage = new CTierStatF();
	public CTierStatF mDefense = new CTierStatF();
	public CTierStatF mIdleTime = new CTierStatF();
	public CTierStatF mWorkStaminaRate = new CTierStatF();
	public CTierStatF mRequiredXP = new CTierStatF();
	public CTierStatF mPromotionDemand = new CTierStatF();

	public CTierInfo()
	{
	}
}

// TODO: Needs to become a proper asset
public class CUnitRules
{
	public List<CTierInfo> mTiers;

	public CTierInfo GetTier(int TierIndex)
	{
		if (mTiers.Count == 0)
			return null;

		if (TierIndex < 0)
			TierIndex = 0;

		if (TierIndex >= mTiers.Count)
			TierIndex = mTiers.Count - 1;

		return mTiers[TierIndex];
	}

	public CUnitRules()
	{
		mTiers = new List<CTierInfo>();
		CJSONParser tiers = new CJSONParser();
		CJSONValue tiersArray = tiers.Parse(CGame.DataDirectory + "rules.txt");

		if (tiersArray == null)
		{
			Debug.LogError("Can't load rules");
			return;
		}

		for (int i = 0; i < tiersArray.GetCount(); ++i)
		{
			CJSONValue t = tiersArray[i];

			CTierInfo tier = new CTierInfo();
			tier.mTitle = t.GetString("title", "unknown");
			tier.mTierIndex = i;
			tier.mMaxLevel = t.GetInt("max_level");
			tier.mSalary.Set(t["salary"]);
			tier.mIntelligence.Set(t["intelligence"]);
			tier.mMaxStamina.Set(t["max_stamina"]);
			tier.mMaxSpeed.Set(t["max_speed"]);
			tier.mHungerRate.Set(t["hunger_rate"]);
			tier.mMaxHunger.Set(t["max_hunger"]);
			tier.mPaperCapacity.Set(t["paper_capacity"]);
			tier.mAttackDamage.Set(t["attack_damage"]);
			tier.mDefense.Set(t["defense"]);
			tier.mIdleTime.Set(t["idle_time"]);
			tier.mWorkStaminaRate.Set(t["work_stamina_rate"]);
			tier.mRequiredXP.Set(t["required_xp"]);
			tier.mPromotionDemand.Set(t["promotion_demand"]);
			mTiers.Add(tier);
		}
	}
}

/// <summary>
/// Manages all the assets!
/// </summary>
public class CAssetManager
{
	// TODO: Make completely thread safe.
	// Both sim and primary threads will need to access asset data.
	// Asset data is only written when using the toolkit (No Sim thread)

	public const string ASSET_FILE_EXTENSION = "pwa";

	public Dictionary<string, CAssetDeclaration> mAssetDeclarations;
	public CUnitRules mUnitRules;

	public void Init()
	{
		_LoadAssetDeclarations();
		mUnitRules = new CUnitRules();
	}
	
	public void Update()
	{
	}

	private void _LoadAssetDeclarations()
	{
		mAssetDeclarations = new Dictionary<string, CAssetDeclaration>();

		string[] files = Directory.GetFiles(CGame.DataDirectory);

		for (int i = 0; i < files.Length; ++i)
		{
			if (Path.GetExtension(files[i]) == "." + ASSET_FILE_EXTENSION)
			{
				Debug.Log("Asset Decl: " + files[i]);

				string assetName = Path.GetFileNameWithoutExtension(files[i]);

				FileStream file = File.Open(files[i], FileMode.Open);
				BinaryReader reader = new BinaryReader(file);

				CAssetDeclaration decl = new CAssetDeclaration();
				decl.mFileName = files[i];
				decl.mName = assetName;
				decl.mType = (EAssetType)reader.ReadInt32();
				decl.mAsset = null;
				mAssetDeclarations[assetName] = decl;
				file.Close();
			}
		}
	}

	public CAssetDeclaration CreateAssetDeclaration(CAsset Asset)
	{
		string errorStr;
		if (!IsAssetNameValid(Asset.mName, out errorStr))
			return null;

		CAssetDeclaration decl = new CAssetDeclaration();
		decl.mName = Asset.mName;
		decl.mFileName = Asset.mFileName;
		decl.mType = Asset.mType;
		decl.mAsset = Asset;		

		mAssetDeclarations[Asset.mName] = decl;

		return decl;
	}

	public CAssetDeclaration GetDeclaration(string AssetName)
	{
		CAssetDeclaration decl = null;
		mAssetDeclarations.TryGetValue(AssetName, out decl);
		return decl;
	}

	public T GetAsset<T>(string AssetName)
		where T : CAsset
	{
		// TODO: Ensure that this is really thread safe to call.

		CAssetDeclaration decl = null;
		mAssetDeclarations.TryGetValue(AssetName, out decl);
		
		if (decl != null)
		{
			if (decl.mAsset == null)
			{
				decl.LoadAsset();
			}

			return (T)decl.mAsset;
		}

		return null;
	}

	public T GetAsset<T>(CAssetDeclaration AssetDecl)
		where T : CAsset
	{
		if (AssetDecl.mAsset == null)
		{
			AssetDecl.LoadAsset();
		}

		return (T)AssetDecl.mAsset;
	}

	public List<CItemAsset> GetAllItemAssets()
	{
		List<CItemAsset> assets = new List<CItemAsset>();

		foreach (KeyValuePair<string, CAssetDeclaration> entry in mAssetDeclarations)
		{
			if (entry.Value.mType == EAssetType.AT_ITEM)
				assets.Add(GetAsset<CItemAsset>(entry.Value));
		}

		return assets;
	}

	public List<string> GetAllAssetNames(EAssetType Type)
	{
		List<string> assets = new List<string>();

		foreach (KeyValuePair<string, CAssetDeclaration> entry in mAssetDeclarations)
		{
			if (entry.Value.mType == Type)
				assets.Add(entry.Key);
		}

		return assets;
	}

	public bool IsAssetNameValid(string AssetName, out string ErrorString)
	{
		for (int i = 0; i < AssetName.Length; ++i)
		{
			char c = AssetName[i];

			if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && (c != '_'))
			{
				ErrorString = "Invalid character in name: '" + c + "'";
				return false;
			}
		}
		
		if (GetDeclaration(AssetName) != null)
		{
			ErrorString = "An asset with the name " + AssetName + " already exists.";
			return false;
		}

		ErrorString = "no error";
		return true;
	}

	public string GetUniqueAssetName()
	{
		int i = 0;

		while (true)
		{
			string name = "asset" + i++;

			if (GetDeclaration(name) == null)
				return name;			
		}
	}
}
