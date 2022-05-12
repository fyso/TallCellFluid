using System;
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
    RenderTexture m_GridDebugRT;

    Material m_DrawFluidParticlesMaterial = Resources.Load<Material>("Materials/DrawFluidParticles");
    Material m_FilterMaterial = Resources.Load<Material>("Materials/Filter");
    Material m_GenerateNoramalMaterial = Resources.Load<Material>("Materials/GenerateNoramal");
    Material m_ToolsMaterial = Resources.Load<Material>("Materials/Tools");

    uint m_FrameIndex = 0;
    ComputeBuffer m_SurfaceGridBuffer;
    ComputeBuffer m_ParticleCountOfGridBuffer;
    Matrix4x4 m_ViewMatrixHistory = Matrix4x4.identity;
    ComputeShader m_PerspectiveGridReSamplingCS = Resources.Load("Shaders/PerspectiveGridReSampling") as ComputeShader;

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
            PerspectiveGridReSampling();
            DrawParticles();
            SmoothFluidDepth();
            GenerateFluidNoramal();
        }
        if (m_RenderManager.m_PerspectiveGridSetting.m_OcclusionCullingDebug)
            Show(m_GridDebugRT); 
        else Show(m_FluidNormalRT);

        DrawUnsupportedShaders();
        DrawGizmos();

        m_Context.Submit();
        Clear();
        m_FrameIndex++;
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
        if (!m_RenderManager.m_PerspectiveGridSetting.m_OcclusionCullingDebug || (m_FrameIndex == 0))
            m_ViewMatrixHistory = m_Camera.worldToCameraMatrix;

        var projMatrix = GL.GetGPUProjectionMatrix(m_Camera.projectionMatrix, false);
        var viewProjMatrix = projMatrix * m_Camera.worldToCameraMatrix;
        var invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
        var invUnityCameraProjMatrix = Matrix4x4.Inverse(m_Camera.projectionMatrix);

        m_CommandBuffer.SetGlobalMatrix("unity_MatrixVHistory", m_ViewMatrixHistory);
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
        m_CommandBuffer.ClearRenderTarget(true, true, Color.gray);
        _ExecuteCommandBuffer();

        var drawingSettings = new DrawingSettings(new ShaderTagId("Diffuse"), new SortingSettings(m_Camera));
        var filteringSettings = FilteringSettings.defaultValue;
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }

    void PerspectiveGridReSampling()
    {
        float zNear, zFar;
        int clusterDimX = 128;
        int clusterDimY = 128;
        AdaptiveFliudPlane(out zNear, out zFar);
        float fieldOfView = m_Camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float logFarAndNear = Mathf.Log(zFar / zNear);
        float sampleRadioInv = clusterDimY / (2.0f * Mathf.Tan(fieldOfView));
        //float scaleFactorZ   = Mathf.Pow((float)(0.5 * clusterDimX / sampleRadioInv), 2) + Mathf.Pow((float)0.5 * clusterDimY / sampleRadioInv, 2) + 1;
        //scaleFactorZ = Mathf.Sqrt(scaleFactorZ);
        float scaleFactorZ = 1.0f;
        int clusterDimZ = Mathf.CeilToInt(logFarAndNear * sampleRadioInv * scaleFactorZ);
        int gridCount = clusterDimX * clusterDimY * clusterDimZ;
        int surfaceGridKernel = m_PerspectiveGridReSamplingCS.FindKernel("searchSurfaceGrid");
        int clearSurfaceGridKernel = m_PerspectiveGridReSamplingCS.FindKernel("clearSurfaceGrid");
        int clearParticleCountOfGridKernel = m_PerspectiveGridReSamplingCS.FindKernel("clearParticleCountOfGrid");
        int insertParticle2PerspectiveGridKernel = m_PerspectiveGridReSamplingCS.FindKernel("insertParticle2PerspectiveGrid");
        uint surfaceGridGroupX, surfaceGridGroupY, clearParticleCountOfGridGroupX;

        m_PerspectiveGridReSamplingCS.GetKernelThreadGroupSizes(clearParticleCountOfGridKernel, out clearParticleCountOfGridGroupX, out _, out _);
        m_PerspectiveGridReSamplingCS.GetKernelThreadGroupSizes(surfaceGridKernel, out surfaceGridGroupX, out surfaceGridGroupY, out _);

        m_ParticleCountOfGridBuffer = new ComputeBuffer(gridCount, sizeof(uint));
        m_SurfaceGridBuffer = new ComputeBuffer(gridCount, sizeof(uint));

        //Update 
        m_RenderManager.m_PerspectiveGridSetting.UpdateShaderProperty();

        m_CommandBuffer.name = "PerspectiveGrid ReSampling";
        //Global value
        m_CommandBuffer.SetGlobalFloat("_NearPlane", zNear);
        m_CommandBuffer.SetGlobalFloat("_SampleRadioInv", sampleRadioInv);
        m_CommandBuffer.SetGlobalInt("_PerspectiveGridDimX", clusterDimX);
        m_CommandBuffer.SetGlobalInt("_PerspectiveGridDimY", clusterDimY);
        m_CommandBuffer.SetGlobalInt("_PerspectiveGridDimZ", clusterDimZ);

        //Mapping Perspective Grid
        m_CommandBuffer.SetGlobalBuffer("SurfaceGrid_RW", m_SurfaceGridBuffer);
        m_CommandBuffer.SetGlobalBuffer("ParticleCountOfGrid", m_ParticleCountOfGridBuffer);

        m_CommandBuffer.DispatchCompute(m_PerspectiveGridReSamplingCS, clearParticleCountOfGridKernel, Mathf.CeilToInt((float)gridCount / clearParticleCountOfGridGroupX), 1, 1);

        m_CommandBuffer.SetComputeBufferParam(m_PerspectiveGridReSamplingCS, insertParticle2PerspectiveGridKernel, "_ParticlePositionBuffer", m_RenderManager.m_ParticleData.PositionBuffer);
        m_CommandBuffer.SetComputeBufferParam(m_PerspectiveGridReSamplingCS, insertParticle2PerspectiveGridKernel, "ParticleIndirectArgment_R", m_RenderManager.m_ParticleData.ArgumentBuffer);
        m_CommandBuffer.DispatchCompute(m_PerspectiveGridReSamplingCS, insertParticle2PerspectiveGridKernel, m_RenderManager.m_ParticleData.ArgumentBuffer, 0);

        //Search Surface
        m_CommandBuffer.DispatchCompute(m_PerspectiveGridReSamplingCS, clearSurfaceGridKernel, Mathf.CeilToInt((float)gridCount / clearParticleCountOfGridGroupX), 1, 1);

        m_CommandBuffer.DispatchCompute(m_PerspectiveGridReSamplingCS, surfaceGridKernel, Mathf.CeilToInt((float)clusterDimX / surfaceGridGroupX), Mathf.CeilToInt((float)clusterDimY / surfaceGridGroupY), 1);
        _ExecuteCommandBuffer();
    }

    void DrawParticles()
    {
        m_FluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "DrawParticles";

        if (m_RenderManager.m_PerspectiveGridSetting.m_OcclusionCullingDebug)
        {
            m_GridDebugRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
            m_CommandBuffer.EnableShaderKeyword("_OCCLUSIONCULLDEBUG");
            m_CommandBuffer.SetRenderTarget(m_GridDebugRT, m_FluidDepthRT);
        }
        else
        {
            m_CommandBuffer.DisableShaderKeyword("_OCCLUSIONCULLDEBUG");
            m_CommandBuffer.SetRenderTarget(m_FluidDepthRT, m_FluidDepthRT);
        }

        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalBuffer("_AnisotropyBuffer", m_RenderManager.m_ParticleData.AnisotropyBuffer);
        m_CommandBuffer.SetGlobalBuffer("_ParticlePositionBuffer", m_RenderManager.m_ParticleData.PositionBuffer);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", m_SceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", m_RenderManager.m_FilterSetting.m_ParticlesRadius);
        m_CommandBuffer.DrawProceduralIndirect(
            Matrix4x4.identity,
            m_DrawFluidParticlesMaterial, 1,
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

    void AdaptiveFliudPlane(out float vNear, out float vFar)
    {
        Vector3 minPos = m_RenderManager.m_Bounding.MinPos;
        Vector3 maxPos = m_RenderManager.m_Bounding.MaxPos;
        Vector4 p0 = new Vector4(minPos.x, minPos.y, minPos.z, 1.0f);
        Vector4 p6 = new Vector4(maxPos.x, maxPos.y, maxPos.z, 1.0f);
        Vector4 p1 = new Vector4(p6.x, p0.y, p0.z, 1.0f);
        Vector4 p2 = new Vector4(p6.x, p6.y, p0.z, 1.0f);
        Vector4 p3 = new Vector4(p0.x, p6.y, p0.z, 1.0f);
        Vector4 p4 = new Vector4(p0.x, p0.y, p6.z, 1.0f);
        Vector4 p5 = new Vector4(p6.x, p0.y, p6.z, 1.0f);
        Vector4 p7 = new Vector4(p0.x, p6.y, p6.z, 1.0f);

        p0 = m_ViewMatrixHistory * p0;
        p1 = m_ViewMatrixHistory * p1;
        p2 = m_ViewMatrixHistory * p2;
        p3 = m_ViewMatrixHistory * p3;
        p4 = m_ViewMatrixHistory * p4;
        p5 = m_ViewMatrixHistory * p5;
        p6 = m_ViewMatrixHistory * p6;
        p7 = m_ViewMatrixHistory * p7;

        vNear = -1000;
        vNear = Math.Max(p0.z, vNear);
        vNear = Math.Max(p1.z, vNear);
        vNear = Math.Max(p2.z, vNear);
        vNear = Math.Max(p3.z, vNear);
        vNear = Math.Max(p4.z, vNear);
        vNear = Math.Max(p5.z, vNear);
        vNear = Math.Max(p6.z, vNear);
        vNear = Math.Max(p7.z, vNear);
        vNear = Math.Max(m_Camera.nearClipPlane, -vNear);

        vFar = 0;
        vFar = Math.Min(p0.z, vFar);
        vFar = Math.Min(p1.z, vFar);
        vFar = Math.Min(p2.z, vFar);
        vFar = Math.Min(p3.z, vFar);
        vFar = Math.Min(p4.z, vFar);
        vFar = Math.Min(p5.z, vFar);
        vFar = Math.Min(p6.z, vFar);
        vFar = Math.Min(p7.z, vFar);
        vFar = Math.Min(m_Camera.farClipPlane, -vFar);
    }
    void Show(RenderTexture outputRT)
    {
        m_CommandBuffer.name = "Show";
        m_CommandBuffer.Blit(outputRT, m_Camera.targetTexture, Vector2.one, Vector2.zero);

        if (depthRT)
        {
            m_CommandBuffer.SetGlobalTexture("_SrcDepth", depthRT);
            m_CommandBuffer.SetRenderTarget(0, m_Camera.targetTexture);
            m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_ToolsMaterial, 0, MeshTopology.Triangles, 3);
        }
        _ExecuteCommandBuffer();
    }

    void Clear()
    {
        RenderTexture.ReleaseTemporary(m_SceneColorRT);
        RenderTexture.ReleaseTemporary(m_SceneDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidDepthRT);
        RenderTexture.ReleaseTemporary(m_SmoothFluidDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidNormalRT);

        if (m_GridDebugRT != null) RenderTexture.ReleaseTemporary(m_GridDebugRT);
        if (m_SurfaceGridBuffer != null) m_SurfaceGridBuffer.Dispose();
        if (m_ParticleCountOfGridBuffer != null) m_ParticleCountOfGridBuffer.Dispose();
    }

    void _ExecuteCommandBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}
