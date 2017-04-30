using System;
using System.Collections.Generic;
using UnityEngine;

public class CCompanyView
{

}

public class CPlayerView
{
	public int mIndex;
	public Color mColor;
	public int mMoney;
	public int mDebt;
	public Vector2 mSpawnPos;

	// Cinematic Block
	public bool mCanInteractWithWorld;
	public bool mCanInteractWithUI;
	public bool mCanControlCamera;
	public bool mShowUI;

	public Vector4 mCamPosition;
	public float mCamSpeed;

	public int mMusicTrack;

	public string[] mAvailableItems;
}

/// <summary>
/// Holds all state information of the User's view of the simulation world.
/// </summary>
public class CUserWorldView
{
	private CGameSession _gameSession;
	private CWorld _world;
	private CUserSession _userSession;
	private int _viewPlayerIndex;

	// State Data
	public int mGameTick;
	public float mPaydayTimerNormalised;
	public List<CTransientEvent> mTransientEvents;
	public List<CStateView> mStateViews;
	public CPlayerView[] mPlayerViews;

	// Temporary Flow Field Tests
	public int[,] mFlowFieldIntegration;
	//private List<flowNode> _flowFloodQueue = new List<flowNode>();
	public flowNode[] mFlowQueue = new flowNode[100000];
	public int mFlowQueueStart = 0;
	public int mFlowQueueEnd = 0;

	public class CFlowTester
	{
		public Vector2 mPosition;
		public Vector2 mTargetVelocity;
		public Vector2 mVelocity;
		public Vector2 mBeingPushedForce;
	}

	public CFlowTester[] mFlowTesters;

	public CNavMesh mNavMesh;

	public CUserWorldView()
	{

	}

	/// <summary>
	/// Change the view to the perspective of a specific player.
	/// </summary>
	public void SetViewPlayerIndex(int Index)
	{
		// Insta destroy all the things!
	}

	public struct flowNode
	{
		public int mX;
		public int mY;
		public int mValue;

		public flowNode(int X, int Y, int V)
		{
			mX = X;
			mY = Y;
			mValue = V;
		}
	}

	public void AddFlowGoal(Rect GoalRect)
	{
		int minX = (int)GoalRect.xMin;
		int minY = (int)GoalRect.yMin;
		int maxX = (int)GoalRect.xMax;
		int maxY = (int)GoalRect.yMax;

		for (int iX = minX; iX < maxX; ++iX)
		{
			for (int iY = minY; iY < maxY; ++iY)
			{
				if (!_world.mMap.mGlobalCollisionTiles[iX, iY].mSolid)
				{
					PushFlowQueue(iX, iY, 1);
				}
			}
		}
	}

	public void PushFlowQueue(int X, int Y, int Value)
	{
		mFlowQueue[mFlowQueueEnd].mX = X;
		mFlowQueue[mFlowQueueEnd].mY = Y;
		mFlowQueue[mFlowQueueEnd].mValue = Value;
		++mFlowQueueEnd;
	}

	public flowNode PopFlowQueue()
	{	
		++mFlowQueueStart;
		return mFlowQueue[mFlowQueueStart - 1];
	}

	public void GenerateFlowTesters()
	{
		int flowTesters = 200;
		mFlowTesters = new CFlowTester[flowTesters];
		for (int i = 0; i < flowTesters; ++i)
		{
			CFlowTester tester = new CFlowTester();
			tester.mPosition = new Vector2(UnityEngine.Random.value * 30 + 5, UnityEngine.Random.value * 30 + 5);
			mFlowTesters[i] = tester;
		}
	}

	public void GenerateFlowField(Rect GoalRect)
	{
		int size = _world.mMap.mWidth;
		mFlowFieldIntegration = new int[size, size];

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		mFlowQueueStart = 0;
		mFlowQueueEnd = 0;

		// Stack based floodfill, not ideal.
		// Start from goal and paint outwards.

		//PushFlowQueue(20, 20, 1);

		AddFlowGoal(GoalRect);
		//AddFlowGoal(new Rect(30, 40, 4, 4));

		int processedNodes = 0;

		while (mFlowQueueStart != mFlowQueueEnd)
		{
			Flow(PopFlowQueue());
			++processedNodes;
		}

		/*
		for (int iX = 0; iX < size; ++iX)
		{
			for (int iY = 0; iY < size; ++iY)
			{
				if (mFlowFieldIntegration[iX, iY] == 0)
					mFlowFieldIntegration[iX, iY] = 1000;
			}
		}
		*/

		sw.Stop();
		Debug.Log("Flow Field Time: " + sw.Elapsed.TotalMilliseconds + "ms Nodes: " + processedNodes);
	}

	public void Flow(flowNode Node)
	{
		int X = Node.mX;
		int Y = Node.mY;
		int Value = Node.mValue;

		if (mFlowFieldIntegration[X, Y] != 0)
			return;

		mFlowFieldIntegration[X, Y] = Value;
		
		if (Y < 99)
		{
			if ((!_world.mMap.mGlobalCollisionTiles[X, Y + 1].mSolid) &&
				(!_world.mMap.mGlobalCollisionTiles[X, Y + 1].mWallXSolid))
			{
				PushFlowQueue(X, Y + 1, Value + 1);
				//_flowFloodQueue.Add(new flowNode(X, Y + 1, Value + 1));
			}
		}

		if (Y > 0)
		{
			if ((!_world.mMap.mGlobalCollisionTiles[X, Y - 1].mSolid) &&
				(!_world.mMap.mGlobalCollisionTiles[X, Y].mWallXSolid))
			{
				PushFlowQueue(X, Y - 1, Value + 1);
				//_flowFloodQueue.Add(new flowNode(X, Y - 1, Value + 1));
			}
		}

		if (X < 99)
		{
			if ((!_world.mMap.mGlobalCollisionTiles[X + 1, Y].mSolid) &&
				(!_world.mMap.mGlobalCollisionTiles[X + 1, Y].mWallZSolid))
			{
				PushFlowQueue(X + 1, Y, Value + 1);
				//_flowFloodQueue.Add(new flowNode(X + 1, Y, Value + 1));
			}
		}

		if (X > 0)
		{
			if ((!_world.mMap.mGlobalCollisionTiles[X - 1, Y].mSolid) &&
				(!_world.mMap.mGlobalCollisionTiles[X, Y].mWallZSolid))
			{
				PushFlowQueue(X - 1, Y, Value + 1);
				//_flowFloodQueue.Add(new flowNode(X - 1, Y, Value + 1));
			}
		}
	}

	public void DrawFlowField()
	{
		int size = _world.mMap.mWidth;

		for (int iX = 0; iX < size; ++iX)
		{
			for (int iY = 0; iY < size; ++iY)
			{
				//if (mFlowFieldIntegration[iX, iY] > 0)
				{
					float t = mFlowFieldIntegration[iX, iY] / 40.0f;
					Color c = Color.Lerp(Color.green, Color.red, t);

					if (mFlowFieldIntegration[iX, iY] == 1)
						c = Color.blue;
					else if (mFlowFieldIntegration[iX, iY] == 0)
						c = Color.black;
					else if (mFlowFieldIntegration[iX, iY] == 1000)
						c = Color.cyan;

					c.a = 0.5f;
					
					CDebug.DrawYRectQuad(new Vector3(iX * 0.5f + 0.25f, 0.0f, iY * 0.5f + 0.25f), 0.5f, 0.5f, c, false);
				}
			}
		}

		for (int i = 0; i < mFlowTesters.Length; ++i)
		{
			CFlowTester tester = mFlowTesters[i];
			CDebug.DrawCircle(Vector3.up, tester.mPosition.ToWorldVec3(), 0.25f, Color.white, false);

			// Get neighbours and check least flow resistance.
			int tpX = (int)(tester.mPosition.x * 2);
			int tpY = (int)(tester.mPosition.y * 2);

			tpX = Mathf.Clamp(tpX, 1, 98);
			tpY = Mathf.Clamp(tpY, 1, 98);

			int res = mFlowFieldIntegration[tpX, tpY];

			int upD = res;
			int downD = res;
			int leftD = res;
			int rightD = res;

			int upR = mFlowFieldIntegration[tpX - 0, tpY + 1];
			int downR = mFlowFieldIntegration[tpX - 0, tpY - 1];
			int leftR = mFlowFieldIntegration[tpX - 1, tpY - 0];
			int rightR = mFlowFieldIntegration[tpX + 1, tpY - 0];

			if (upR != 0 && !_world.mMap.mGlobalCollisionTiles[tpX, tpY + 1].mWallXSolid) upD = upR;
			if (downR != 0 && !_world.mMap.mGlobalCollisionTiles[tpX, tpY].mWallXSolid) downD = downR;
			if (leftR != 0 && !_world.mMap.mGlobalCollisionTiles[tpX, tpY].mWallZSolid) leftD = leftR;
			if (rightR != 0 && !_world.mMap.mGlobalCollisionTiles[tpX + 1, tpY].mWallZSolid) rightD = rightR;

			tester.mTargetVelocity = new Vector2(leftD - rightD, downD - upD).normalized;
			tester.mVelocity = Vector3.Lerp(tester.mVelocity, tester.mTargetVelocity, Time.deltaTime * 4.0f).normalized;

			Vector2 dir = tester.mVelocity;
			Vector2 dest = tester.mPosition + dir * Time.deltaTime * 1.0f;
			dest += tester.mBeingPushedForce * Time.deltaTime * 1.0f;
			CDebug.DrawLine(tester.mPosition.ToWorldVec3(), (tester.mPosition + dir).ToWorldVec3(), Color.black, false);
			tester.mPosition = _world.mMap.Move(tester.mPosition, dest, 0.25f);
			//tester.mPosition = dest;

			//tester.mPosition += tester.mBeingPushedForce * Time.deltaTime * 10.0f;
			tester.mBeingPushedForce *= Time.deltaTime * 0.01f;
		}
		
		float pushDistance = 0.4f;
		float pushDistSqr = pushDistance * pushDistance;

		// Units Vs Units
		for (int i = 0; i < mFlowTesters.Length; ++i)
		{
			for (int j = i + 1; j < mFlowTesters.Length; ++j)
			{
				Vector2 dir = (mFlowTesters[i].mPosition - mFlowTesters[j].mPosition);
				float sqrMag = dir.sqrMagnitude;

				if (sqrMag <= pushDistSqr)
				{
					if (sqrMag == 0.0f)
					{
						dir.x = UnityEngine.Random.value;
						dir.y = UnityEngine.Random.value;
					}

					dir.Normalize();
					//dir *= 0.1f;
					dir *= 0.5f;

					mFlowTesters[i].mBeingPushedForce += dir;
					mFlowTesters[j].mBeingPushedForce -= dir;
				}
			}
		}
	}

	/// <summary>
	/// Copy initial state.
	/// </summary>
	public void Init(CGameSession Session, CWorld World, CUserSession PlayMode, int ViewPlayerIndex)
	{
		_gameSession = Session;
		_world = World;
		_userSession = PlayMode;
		_viewPlayerIndex = ViewPlayerIndex;
		mStateViews = new List<CStateView>();

		mPlayerViews = new CPlayerView[_world.mPlayers.Length];

		for (int i = 0; i < mPlayerViews.Length; ++i)
			mPlayerViews[i] = new CPlayerView();

		GenerateFlowTesters();
		GenerateFlowField(new Rect(15, 21, 4, 4));
		mNavMesh = new CNavMesh(World.mMap.mStaticVisSegments);
	}

	/// <summary>
	/// Terminate.
	/// </summary>
	public void Destroy()
	{
		// TODO: Determine what else needs to be destroyed here.

	}
	
	/// <summary>
	/// Gets the user's view of the local collision.
	/// </summary>
	public CCollisionTile[,] GetCollisionView()
	{
		// NOTE: This reads directly from the simulation so be careful!
		// Don't ever write to this!
		// Could change as you read from it.

		return _world.mMap.mLocalCollisionTiles[_viewPlayerIndex];
	}

	/// <summary>
	/// Get the user's view of the map tiles.
	/// </summary>
	public CTile[,] GetTileView()
	{
		// NOTE: This reads directly from the simulation so be careful!
		// Don't ever write to this!
		// Could change as you read from it.

		return _world.mMap.mTiles;
	}

	/// <summary>
	/// Get the visibility segements.
	/// </summary>
	public List<CVisibilityBlockingSegment> GetVisSegments()
	{
		return _world.mMap.mStaticVisSegments;
	}

	/// <summary>
	/// Update entire view and alert session of changes.
	/// </summary>
	public bool Update(Vector2 MouseFloorPos)
	{
		// This is the only time that we lock the world on the primary thread (Except for rare debugging cases in CGameSession).
		// All state information is copied from the world from the perspective of a player (_viewPlayerIndex).
		// As little work as possible should be done during the state copy. We want to unlock the world ASAP.
		
		CGame.PrimaryThreadProfiler.Push(CProfiler.EID.I_RENDER_TICK);
		lock (_world)
		{
			// TODO: Check for a console var to remain on crash.
			if (_world.mCrashed)
			{
				CGame.PrimaryThreadProfiler.Pop();
				return false;
			}

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			CGame.PrimaryThreadProfiler.Push(CProfiler.EID.I_RENDER_TICK);

			// Copy door segments for visibility
			_world.mMap.TrimStaticVisSegments();
			for (int i = 0; i < _world.mItems.Count; ++i)
			{
				CItem item = _world.mItems[i];
				if (item.mType == CEntity.EType.ITEM_DOOR && !item.mBluerprint && !(item as CItemDoor).mOpen)
				{
					_world.mMap.mStaticVisSegments.Add(new CVisibilityBlockingSegment(item.ItemToWorldSpacePosition(new Vector2(0, 1)), item.ItemToWorldSpacePosition(new Vector2(1, 1))));
				}
			}

			// Copy all the state!!!!!
			mTransientEvents = _world.SwapAndClearTransientEvents();
			mPaydayTimerNormalised = 1.0f - _world.GetPaydayTimeNormalize();
			mGameTick = _world.mGameTick;

			// Company Data
			// ...

			// Player Data
			for (int i = 0; i < mPlayerViews.Length; ++i)
			{
				CPlayer player = _world.mPlayers[i];
				CPlayerView view = mPlayerViews[i];

				view.mIndex = player.mID;
				view.mColor = player.mColor;
				view.mMoney = player.mMoney;
				view.mDebt = player.mDebt;
				view.mSpawnPos = player.GetSpawnPos();
				view.mCanInteractWithWorld = player.mCanInteractWithWorld;
				view.mCanInteractWithUI = player.mCanInteractWithUI;
				view.mCanControlCamera = player.mCanControlCamera;
				view.mMusicTrack = player.mMusicTrack;
				view.mShowUI = player.mShowUI;
				view.mCamPosition = player.mCamPosition;
				view.mCamSpeed = player.mCamSpeed;
				view.mAvailableItems = player.mAvailableItems.ToArray();
			}

			// TODO: Should just iterate all entities and ask them if they are valid for state copy.

			// Unit Data
			// TODO: Surely the sim knows what units are visible to the player already?
			for (int i = 0; i < _world.mUnits.Count; ++i)
			{
				if (_world.mUnits[i].IsVisibleToPlayer(_viewPlayerIndex))
				{
					if (_world.mUnits[i].mStateView == null)
					{
						CUnitView view = new CUnitView();
						view.mState = CStateView.EState.NEW;
						mStateViews.Add(view);
						view.CopyInitialState(_world.mUnits[i]);
						view.Init(_viewPlayerIndex, this);
						_world.mUnits[i].mStateView = view;
					}
					else
					{
						_world.mUnits[i].mStateView.mState = CStateView.EState.UPDATING;
					}

					_world.mUnits[i].mStateView.CopyState(_world.mUnits[i]);
				}
				else
				{
					_world.mUnits[i].mStateView = null;
				}
			}

			// Pickups
			for (int i = 0; i < _world.mPickups.Count; ++i)
			{
				if (_world.mPickups[i].IsVisibleToPlayer(_viewPlayerIndex))
				{
					if (_world.mPickups[i].mStateView == null)
					{
						CPickupView view = new CPickupView();
						view.mState = CStateView.EState.NEW;
						mStateViews.Add(view);
						view.CopyInitialState(_world.mPickups[i]);
						view.Init(_viewPlayerIndex, this);
						_world.mPickups[i].mStateView = view;
					}
					else
					{
						_world.mPickups[i].mStateView.mState = CStateView.EState.UPDATING;
					}

					_world.mPickups[i].mStateView.CopyState(_world.mPickups[i]);
				}
				else
				{
					_world.mPickups[i].mStateView = null;
				}
			}

			// Missiles
			for (int i = 0; i < _world.mMissiles.Count; ++i)
			{
				if (_world.mMissiles[i].IsVisibleToPlayer(_viewPlayerIndex))
				{
					if (_world.mMissiles[i].mStateView == null)
					{
						CMissileView view = new CMissileView();
						view.mState = CStateView.EState.NEW;
						mStateViews.Add(view);
						view.CopyInitialState(_world.mMissiles[i]);
						view.Init(_viewPlayerIndex, this);
						_world.mMissiles[i].mStateView = view;
					}
					else
					{
						_world.mMissiles[i].mStateView.mState = CStateView.EState.UPDATING;
					}

					_world.mMissiles[i].mStateView.CopyState(_world.mMissiles[i]);
				}
				else
				{
					_world.mMissiles[i].mStateView = null;
				}
			}

			// Item View Data
			for (int i = 0; i < _world.mItemProxies[_viewPlayerIndex].Count; ++i)
			{
				CItemProxy proxy = _world.mItemProxies[_viewPlayerIndex][i];

				if (proxy.mVisuals == null)
				{
					// new
					CItemView view = new CItemView();
					view.mState = CStateView.EState.NEW;
					mStateViews.Add(view);
					view.CopyInitialState(proxy);
					view.Init(_viewPlayerIndex, this);
					proxy.mVisuals = view;
				}
				else
				{
					proxy.mVisuals.mState = CStateView.EState.UPDATING;
				}

				proxy.mVisuals.CopyState(proxy);
			}

			// Contract Data
			for (int i = 0; i < _world.mContracts.Count; ++i)
			{
				CContract contract = _world.mContracts[i];

				// TODO: All contracts always part of user view state?
				//if (contract.mOwner == -1 || contract.mOwner == _viewPlayerIndex)
				if (true)
				{
					if (contract.mStateView == null)
					{
						// new
						CContractView view = new CContractView();
						view.mState = CStateView.EState.NEW;
						mStateViews.Add(view);
						view.CopyInitialState(contract);
						view.Init(_viewPlayerIndex, this);
						contract.mStateView = view;
					}
					else
					{
						contract.mStateView.mState = CStateView.EState.UPDATING;
					}

					contract.mStateView.CopyState(contract);
				}
				else
				{
					contract.mStateView = null;
				}
			}

			// Resume View Data
			for (int i = 0; i < _world.mResumes.Count; ++i)
			{
				CResume resume = _world.mResumes[i];

				if (resume.mOwner != -1)
				{
					if (resume.mStateView == null)
					{
						// new
						CResumeView view = new CResumeView();
						view.mState = CStateView.EState.NEW;
						mStateViews.Add(view);
						view.CopyInitialState(resume);
						view.Init(_viewPlayerIndex, this);
						resume.mStateView = view;
					}
					else
					{
						resume.mStateView.mState = CStateView.EState.UPDATING;
					}

					resume.mStateView.CopyState(resume);
				}
				else
				{
					resume.mStateView = null;
				}
			}

			// Decal View Data
			for (int i = 0; i < _world.mDecals.Count; ++i)
			{
				CDecal decal = _world.mDecals[i];

				// TODO: Allow decals to be visible to certain players only.

				if (decal.mStateView == null)
				{
					// new
					CDecalView view = new CDecalView();
					view.mState = CStateView.EState.NEW;
					mStateViews.Add(view);
					view.CopyInitialState(decal);
					view.Init(_viewPlayerIndex, this);
					decal.mStateView = view;
				}
				else
				{
					decal.mStateView.mState = CStateView.EState.UPDATING;
				}

				decal.mStateView.CopyState(decal);
			}

			sw.Stop();
			//Debug.Log("Copy " + sw.Elapsed.TotalMilliseconds + "ms");

			CGame.PrimaryThreadProfiler.Pop();
		}
		CGame.PrimaryThreadProfiler.Pop();

		// NOTE: Now that the state has been copied from the simulation and we have released the thread lock, we can
		// process the new state and make appropriate changes to the Unity scene.

		CGame.UIManager.PlayMusic(mPlayerViews[_viewPlayerIndex].mMusicTrack);
		CGame.UIManager.mShowUI = mPlayerViews[_viewPlayerIndex].mShowUI;
		_userSession.mUserInteraction = mPlayerViews[_viewPlayerIndex].mCanInteractWithWorld;
		CGame.CameraManager.SetInteractable(mPlayerViews[_viewPlayerIndex].mCanControlCamera);
		CGame.CameraManager.SetLerpSpeed(mPlayerViews[_viewPlayerIndex].mCamSpeed);

		// TODO: Different bool to control tracked camera?
		if (!mPlayerViews[_viewPlayerIndex].mCanControlCamera)
		{
			// TODO: Immediate of on different cam step.
			CGame.CameraManager.SetTargetPosition(mPlayerViews[_viewPlayerIndex].mCamPosition, false, mPlayerViews[_viewPlayerIndex].mCamPosition.w);
		}
		
		for (int i = 0; i < mTransientEvents.Count; ++i)
		{
			CTransientEvent tev = mTransientEvents[i];
			
			// Ignore events that occured over 1 second ago.
			if (mGameTick > tev.mGameTick + 20)
				continue;
			
			if ((tev.mViewerFlags & (1 << _viewPlayerIndex)) == 0)
				continue;

			if (tev.mType == CTransientEvent.EType.SOUND)
			{
				//AudioSource.PlayClipAtPoint(CGame.PrimaryResources.AudioClips[tev.mData], tev.mPosition, 1.0f);
				AudioClip clip = CGame.PrimaryResources.AudioClips[tev.mData];
				GameObject source = new GameObject("AudioPosOneShot");
				source.transform.SetParent(_userSession.mPrimaryScene.transform);
				source.transform.position = tev.mPosition;
				AudioSource audio = source.AddComponent<AudioSource>();
				audio.clip = clip;
				audio.outputAudioMixerGroup = CGame.UIResources.SoundsMixer;
				audio.spatialBlend = 1.0f;
				audio.Play();
				GameObject.Destroy(source, clip.length);
			}
			else if (tev.mType == CTransientEvent.EType.UI_SOUND)
			{
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[tev.mData]);
			}
			else if (tev.mType == CTransientEvent.EType.EFFECT)
			{
				GameObject.Instantiate(CGame.PrimaryResources.Particles, tev.mPosition, Quaternion.identity);
				//CGame.CameraManager.Shake();
			}
			else if (tev.mType == CTransientEvent.EType.PAYDAY)
			{
				CGame.UIManager.PlaySound(CGame.PrimaryResources.AudioClips[17]);
			}
			else if (tev.mType == CTransientEvent.EType.NOTIFY)
			{
				Debug.Log("Got notify: " + tev.mMessage);
				_userSession.CreateNotifyStackIcon(2, null, CGame.AssetManager.GetAsset<CItemAsset>(tev.mMessage));
			}
		}

		for (int i = 0; i < mStateViews.Count; ++i)
		{
			if (!mStateViews[i].Update(_userSession))
			{
				mStateViews.RemoveAt(i);
				--i;
			}
			else
			{
				mStateViews[i].mState = CStateView.EState.WAITING;
				mStateViews[i].DrawDebugPrims();
			}
		}

		/*
		for (int i = 0; i < _stateViews.Count; ++i)
		{
			_stateViews[i].DrawDebugPrims();
		}
		*/

		if (CGame.VarShowFlowField.mValue)
		{
			DrawFlowField();
		}

		if (CGame.VarShowNavMesh.mValue)
		{
			mNavMesh.DebugDraw();
		}

		if (CGame.VarShowNavRect.mValue > 0)
		{
			_world.mMap.mNavMeshes[CGame.VarShowNavRect.mValue - 1].DebugDraw();
		}

		return true;
	}

	public ISelectable GetSelectable(Ray R)
	{
		// TODO: Flags to determine what type of selectable?
		float d = float.MaxValue;
		ISelectable result = null;

		for (int i = 0; i < mStateViews.Count; ++i)
		{
			ISelectable selectable = mStateViews[i] as ISelectable;

			if (selectable != null)
			{
				if (selectable.Intersect(R, ref d))
					result = selectable;
			}
		}					 

		return result;
	}

	public ISelectable GetSelectableDesk(Ray R)
	{
		float d = float.MaxValue;
		ISelectable result = null;

		for (int i = 0; i < mStateViews.Count; ++i)
		{
			CItemView itemView = mStateViews[i] as CItemView;

			if (itemView == null || itemView.mAsset.mItemType != EItemType.DESK || itemView.mBlueprint)
				continue;

			ISelectable selectable = itemView as ISelectable;

			if (selectable != null)
			{
				if (selectable.Intersect(R, ref d))
					result = selectable;
			}
		}

		return result;
	}

	/// <summary>
	/// Get an item view that is pointing to a proxy item with ID.
	/// </summary>
	public CItemView GetItemView(int ItemID)
	{
		for (int i = 0; i < mStateViews.Count; ++i)
		{
			CItemView view = mStateViews[i] as CItemView;

			if (view != null)
			{
				if (view.mProxyID == ItemID)
				{
					return view;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Get a contract view by ID.
	/// </summary>
	public CContractView GetContractView(int ContractViewID)
	{
		for (int i = 0; i < mStateViews.Count; ++i)
		{
			CContractView view = mStateViews[i] as CContractView;

			if (view != null)
			{
				if (view.mID == ContractViewID)
				{
					return view;
				}
			}
		}

		return null;
	}
}