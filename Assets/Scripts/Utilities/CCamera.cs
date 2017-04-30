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

	void Start()
	{
		_camera = GetComponent<Camera>();

		_cmdBuffer = new CommandBuffer();
		_camera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeImageEffects, _cmdBuffer);
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

	public void OnPostRender()
	{
		CDebug.Draw();
	}
}
