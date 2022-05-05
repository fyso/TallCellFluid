using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Pipeline Asset")]
public class PipelineAsset : RenderPipelineAsset
{
	public RenderManager m_RenderManager;

	protected override RenderPipeline CreatePipeline()
	{
		return new Pipeline(m_RenderManager, new CameraRenderer());
	}
}