using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;

public class CEditorTools
{
	[MenuItem("Paperwork/Reimport Unit Animations")]
	public static void ReimportUnitAnimations()
	{
		// Confirm dialog
		//AssetDatabase.ImportAsset("Assets/Animations/Unit/running_no_item.fbx");
	}

	[MenuItem("Paperwork/Rebuild Unit Anim Controller")]
	public static void RebuildUnitAnimController()
	{
		// Warn about controller overwrite.
		// Gather all clips from folder.
		// Gather all anims from anim decl and build those with clips.

		string clipsAssetPath = "Assets/Animations/Unit";

		string[] assetGUIDs = AssetDatabase.FindAssets("", new string[] { clipsAssetPath });

		Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

		foreach (string str in assetGUIDs)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(str);

			if (assetPath.EndsWith(".fbx"))
			{
				UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

				foreach (UnityEngine.Object obj in allAssets)
				{
					if (obj.GetType() == typeof(AnimationClip))
					{
						AnimationClip clip = obj as AnimationClip;
						clips[clip.name] = clip;
					}
				}
			}
		}

		AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/unitAnimController.controller");
		AnimatorStateMachine rootState = controller.layers[0].stateMachine;
		
		for (int i = 0; i < Animation.FBXEntries.Length; ++i)
		{
			AnimationClip clip = null;

			if (clips.TryGetValue(Animation.FBXEntries[i].mName, out clip))
			{
				Debug.Log("Match for: " + clip.name);

				AnimatorState state = rootState.AddState(clip.name);
				state.motion = clip;
				state.speed = Animation.FBXEntries[i].mSpeed;
			}
		}

		string defaultState = Animation.GetDefaultAnimation();

		for (int i = 0; i < rootState.states.Length; ++i)
		{
			if (rootState.states[i].state.name == defaultState)
			{
				rootState.defaultState = rootState.states[i].state;
				break;
			}
		}

		//AnimatorStateMachine rootState = controller.layers[0].stateMachine;
		//AnimatorState state = rootState.AddState("run");
		//state.add = newClips[0];
		//controller
		//AnimationClip a = new AnimationClip();
		//controller.AddMotion(a);

		//AnimationClip[] clip = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/new_char_rig.fbx/", typeof(AnimationClip)) as AnimationClip[];

		//controller.AddMotion(clip);
	}
}

public class CAnimPostprocessor : AssetPostprocessor
{
	public void OnPreprocessModel()
	{
		Debug.Log("Processing: " + assetPath);

		if (!assetPath.Contains("Assets/Animations/Unit/"))
			return;

		int tagIndex = assetPath.LastIndexOf('/');
		string declName = assetPath.Substring(tagIndex + 1);
		declName = declName.Substring(0, declName.Length - 4);

		Debug.Log("Anim Decl: " + declName);

		string avatarAssetPath = "Assets/Models/unit_default_2.fbx";

		// Resources
		ModelImporter importer = assetImporter as ModelImporter;
		Avatar defaultAvatar = null;
		Animation.CAnimationEntry animDecl = Animation.GetAnimEntry(declName);
		
		importer.importMaterials = false;

		UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(avatarAssetPath);
		foreach (UnityEngine.Object obj in objs)
		{
			if (obj.GetType() == typeof(Avatar))
			{
				defaultAvatar = obj as Avatar;
				break;
			}
		}

		if (animDecl == null)
		{
			Debug.LogError("Can't find animation declaration for '" + declName + "'");
			return;
		}

		if (defaultAvatar == null)
		{
			Debug.LogError("Can't find the default avatar from '" + avatarAssetPath + "'");
			return;
		}

		// Apply settings to imported model
		importer.animationType = ModelImporterAnimationType.Generic;
		importer.sourceAvatar = defaultAvatar;

		importer.importAnimation = true;
		importer.resampleCurves = true;
		importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
		importer.animationRotationError = 0.5f;
		importer.animationPositionError = 0.5f;
		importer.animationScaleError = 0.5f;
		
		// TODO: Get a list of anim clips that could occur in this file. Not just one clip per FBX.

		ModelImporterClipAnimation[] newClips = new ModelImporterClipAnimation[1];
		newClips[0] = new ModelImporterClipAnimation();
		newClips[0].name = declName;
		newClips[0].firstFrame = animDecl.mStartTime * animDecl.mFPS;
		newClips[0].lastFrame = animDecl.mDuration * animDecl.mFPS;
		newClips[0].lockRootRotation = true;
		newClips[0].lockRootHeightY = true;
		newClips[0].lockRootPositionXZ = true;
		newClips[0].loop = animDecl.mLoopable;
		newClips[0].loopPose = animDecl.mLoopable;
		newClips[0].loopTime = animDecl.mLoopable;

		importer.clipAnimations = newClips;
	}
}
