using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer : MonoBehaviour
{
    ScriptableRenderContext m_Context;
    Camera m_Camera;
    RenderManager m_RenderManager;
    CullingResults m_CullingResults;
    CommandBuffer m_CommandBuffer;

    RenderTexture m_SceneColorRT;
    RenderTexture m_SceneDepthRT;
    RenderTexture m_FluidDepthRT;
    RenderTexture m_SmoothFluidDepthRT;
    RenderTexture m_FluidNormalRT;

    Material m_DrawFluidParticlesMaterial = Resources.Load<Material>("Materials/DrawFluidParticles");
    Material m_FilterMaterial = Resources.Load<Material>("Materials/Filter");
    Material m_GenerateNoramalMaterial = Resources.Load<Material>("Materials/GenerateNoramal");

    public void Render(ScriptableRenderContext context, Camera camera, RenderManager renderManager)
    {
        m_Context = context;
        m_Camera = camera;
        m_RenderManager = renderManager;
        m_CommandBuffer = new CommandBuffer();
        if (!Cull()) return;

        UpdateLightData();
        UpdateCameraData();
        RenderScene();
        if(Application.isPlaying)
        {
            DrawParticles();
            SmoothFluidDepth();
            GenerateFluidNoramal();
        }
        Show(m_SceneColorRT);

        DrawUnsupportedShaders();
        DrawGizmos();

        m_Context.Submit();
        Clear();
    }

    bool Cull()
    {
        if (m_Camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            m_CullingResults = m_Context.Cull(ref p);
            return true;
        }
        return false;
    }

    public void UpdateLightData()
    {
        foreach (var light in m_CullingResults.visibleLights)
        {
            if (light.lightType == LightType.Directional)
            {
                m_CommandBuffer.SetGlobalVector("_WorldSpaceLightDir0", -light.localToWorldMatrix.GetColumn(2));
                m_CommandBuffer.SetGlobalColor("_LightColor0", light.finalColor);

                break;
            }
        }
    }

    public void UpdateCameraData()
    {
        m_CommandBuffer.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);

        var projMatrix = GL.GetGPUProjectionMatrix(m_Camera.projectionMatrix, false);
        var viewProjMatrix = projMatrix * m_Camera.worldToCameraMatrix;
        var invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
        var invUnityCameraProjMatrix = Matrix4x4.Inverse(m_Camera.projectionMatrix);

        m_CommandBuffer.SetGlobalMatrix("unity_MatrixIV", m_Camera.cameraToWorldMatrix);
        m_CommandBuffer.SetGlobalMatrix("unity_MatrixIP", invUnityCameraProjMatrix);
        m_CommandBuffer.SetGlobalMatrix("unity_MatrixIVP", m_Camera.cameraToWorldMatrix * Matrix4x4.Inverse(m_Camera.projectionMatrix));
        m_CommandBuffer.SetGlobalMatrix("glstate_matrix_inv_projection", Matrix4x4.Inverse(projMatrix));
        m_CommandBuffer.SetGlobalMatrix("glstate_matrix_view_projection", viewProjMatrix);
        m_CommandBuffer.SetGlobalMatrix("glstate_matrix_inv_view_projection", invViewProjMatrix);
        m_CommandBuffer.SetGlobalFloat("_CameraFarDistance", m_Camera.farClipPlane);
        m_CommandBuffer.SetGlobalFloat("_CameraFieldOfView", m_Camera.fieldOfView / 180 * 3.14f);
        m_CommandBuffer.SetGlobalVector("_WorldSpaceCameraPos", m_Camera.transform.position);
        m_CommandBuffer.SetGlobalVector("_ScreenParams", new Vector4(m_Camera.pixelWidth, m_Camera.pixelHeight, 1 + 1 / m_Camera.pixelWidth, 1 + 1 / m_Camera.pixelHeight));
    }

    void RenderScene()
    {
        m_SceneColorRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGB32);
        m_SceneDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "RenderScene";
        m_CommandBuffer.SetRenderTarget(m_SceneColorRT, m_SceneDepthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        _ExecuteCommandBuffer();

        var drawingSettings = new DrawingSettings(new ShaderTagId("Diffuse"), new SortingSettings(m_Camera));
        var filteringSettings = FilteringSettings.defaultValue;
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }

    void DrawParticles()
    {
        m_FluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "DrawParticles";
        m_CommandBuffer.SetRenderTarget(m_FluidDepthRT, m_FluidDepthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalBuffer("_ParticlePositionBuffer", m_RenderManager.m_ParticleData.PositionBuffer);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", m_SceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", m_RenderManager.m_FilterSetting.m_ParticlesRadius);
        m_CommandBuffer.DrawProceduralIndirect(
            Matrix4x4.identity,
            m_DrawFluidParticlesMaterial, 0,
            MeshTopology.Triangles, m_RenderManager.m_ParticleData.ArgumentBuffer, 12);

        _ExecuteCommandBuffer();
    }

    void SmoothFluidDepth()
    {
        m_SmoothFluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "SmoothFluidDepth";
        m_RenderManager.m_FilterSetting.UpdateShaderProperty();
        m_CommandBuffer.SetRenderTarget(m_SmoothFluidDepthRT, m_SmoothFluidDepthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_FluidDepthRT);
        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 0, MeshTopology.Triangles, 3);

        _ExecuteCommandBuffer();
    }

    void GenerateFluidNoramal()
    {
        m_FluidNormalRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf);

        m_CommandBuffer.name = "GenerateFluidNoramal";
        m_CommandBuffer.SetRenderTarget(m_FluidNormalRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_SmoothFluidDepthRT", m_SmoothFluidDepthRT);

        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_GenerateNoramalMaterial, 0, MeshTopology.Triangles, 3);

        _ExecuteCommandBuffer();
    }

    void Show(RenderTexture outputRT)
    {
        m_CommandBuffer.Blit(outputRT, m_Camera.targetTexture, Vector2.one, Vector2.zero);
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
    }

    void Clear()
    {
        RenderTexture.ReleaseTemporary(m_SceneColorRT);
        RenderTexture.ReleaseTemporary(m_SceneDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidDepthRT);
        RenderTexture.ReleaseTemporary(m_SmoothFluidDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidNormalRT);
    }

    void _ExecuteCommandBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}