using UnityEngine;
using UnityEngine.Rendering;

public class Pipeline : RenderPipeline
{
	RenderManager m_RenderManager;
	CameraRenderer m_CameraRenderer;
	public Pipeline(RenderManager renderManager, CameraRenderer cameraRenderer)
	{
		m_RenderManager = renderManager;
		m_CameraRenderer = cameraRenderer;
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		foreach (Camera camera in cameras)
		{
			m_CameraRenderer.Render(context, camera, m_RenderManager);
		}
	}
}
