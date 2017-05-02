using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Allows user input to move the camera.
/// </summary>
public class CCamera : MonoBehaviour
{
	public Transform TargetTransform;

	private Camera _camera;

	private CommandBuffer _cmdBuffer;
	private CommandBuffer _poseBuffer;

	void Start()
	{
		_camera = GetComponent<Camera>();

		_cmdBuffer = new CommandBuffer();
		_camera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeImageEffects, _cmdBuffer);

		_poseBuffer = new CommandBuffer();
		_camera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardOpaque, _poseBuffer);
	}

	public void SetRenderEffects(List<Renderer> Renderers, List<Renderer> TranslucentRenderers)
	{
		// TODO: Frustum culling on the renderers?

		_cmdBuffer.Clear();
		
		//if (Renderers == null || Renderers.Count == 0)
			//return;

		_cmdBuffer.GetTemporaryRT(0, Screen.width, Screen.height, 24);
		_cmdBuffer.SetRenderTarget(new RenderTargetIdentifier(0));
		_cmdBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0), 1.0f);
		
		for (int i = 0; i < TranslucentRenderers.Count; ++i)
		{
			//TranslucentRenderers[i]

			_cmdBuffer.DrawRenderer(TranslucentRenderers[i], CGame.PrimaryResources.TranslucentModelMat);
		}

		_cmdBuffer.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
		_cmdBuffer.Blit(new RenderTargetIdentifier(0), new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget), CGame.PrimaryResources.TranslucentBlitMat);
		
		_cmdBuffer.SetRenderTarget(new RenderTargetIdentifier(0));
		_cmdBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0), 1.0f);

		for (int i = 0; i < Renderers.Count; ++i)
		{
			_cmdBuffer.DrawRenderer(Renderers[i], CGame.PrimaryResources.HighlightMat);
		}

		_cmdBuffer.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
		_cmdBuffer.Blit(new RenderTargetIdentifier(0), new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget), CGame.PrimaryResources.EdgeBlitMat);

		_cmdBuffer.ReleaseTemporaryRT(0);
	}

	public void SetPoseRenderers(List<Renderer> Renderers)
	{
		_poseBuffer.Clear();
		_poseBuffer.SetRenderTarget(new RenderTargetIdentifier(CGame.UIManager.mPoseRT));
		_poseBuffer.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0), 1.0f);

		for (int i = 0; i < Renderers.Count; ++i)
		{
			Vector3 pos = Renderers[i].transform.position;
			pos.y = 0.0f;
			//_poseBuffer.SetViewMatrix(Matrix4x4.TRS(CGame.CameraManager.mMainCamera.transform.position, Quaternion.LookRotation(pos, Vector3.up), Vector3.one));
			//_poseBuffer.SetViewMatrix(CGame.CameraManager.mMainCamera.worldToCameraMatrix);
			//_poseBuffer.SetProjectionMatrix(CGame.CameraManager.mMainCamera.projectionMatrix);

			//_poseBuffer.SetProjectionMatrix();

			if (i % 3 == 0)
			{
				Transform camT = CGame.CameraManager.mMainCamera.transform;

				//*
				Matrix4x4 t0 = Matrix4x4.Translate(-pos - new Vector3(3.0f, 4.0f, 0.0f));
				Matrix4x4 r0 = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0.0f, -90, 0.0f), Vector3.one);
				Matrix4x4 r1 = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0.0f, 0.0f, -45.0f), Vector3.one);				
				Matrix4x4 viewMat = r0 * r1 * t0;
				//*/

				/*
				Matrix4x4 t0 = Matrix4x4.Translate(-pos);
				Matrix4x4 t1 = Matrix4x4.Translate(new Vector3(0.0f, 0, CGame.PrimaryResources.Z));
				Matrix4x4 r0 = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, CGame.PrimaryResources.X, 0), Vector3.one);
				Matrix4x4 r1 = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(-30, Vector3.right), Vector3.one);
				Matrix4x4 viewMat = r0 * t1;
				*/

				//Matrix4x4 viewMat = Matrix4x4.TRS(-pos - new Vector3(-6.0f, 0.0f, -6.0f), Quaternion.identity, Vector3.one);
				//viewMat *= Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.AngleAxis(-49.0f, Vector3.left), Vector3.one);
				//viewMat *= Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.AngleAxis(225.0f, Vector3.up), Vector3.one);

				_poseBuffer.SetViewProjectionMatrices(viewMat, Matrix4x4.Perspective(50.0f, 1.0f, 0.1f, 500.0f));
				_poseBuffer.SetViewport(new Rect(0, 0, 50, 50));
			}
			//Matrix4x4 viewMat = CGame.CameraManager.mMainCamera.worldToCameraMatrix;
			
			
			_poseBuffer.DrawRenderer(Renderers[i], Renderers[i].sharedMaterial);
		}
	}

	public void OnPostRender()
	{
		CDebug.Draw();
	}
}
