using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class PerspectiveGridData
{
    float m_CameraFov;
    float m_CameraNear;
    float m_CameraFar;
    Vector3 m_GridMin;
    Vector3 m_GridMax;
    int m_PerspectiveGridDimX;
    int m_PerspectiveGridDimY;

    Matrix4x4 m_ViewMatrixForGrid;
    public ComputeBuffer m_VisibleGridBuffer;
    public ComputeBuffer m_ParticleCountOfGridBuffer;
    public int m_GridCount;

    public PerspectiveGridData(Camera camera, Vector3 gridMin, Vector3 gridMax, int perspectiveGridDimX, int perspectiveGridDimY)
    {
        m_ViewMatrixForGrid = camera.worldToCameraMatrix;
        m_CameraFov = camera.fieldOfView;
        m_CameraNear = camera.nearClipPlane;
        m_CameraFar = camera.farClipPlane;
        m_GridMin = gridMin;
        m_GridMax = gridMax;
        m_PerspectiveGridDimX = perspectiveGridDimX;
        m_PerspectiveGridDimY = perspectiveGridDimY;
        CalculateGridDim();

        Shader.SetGlobalMatrix("_ViewMatrixForGrid", m_ViewMatrixForGrid);
        Shader.SetGlobalBuffer("_VisibleGridBuffer", m_VisibleGridBuffer);
        Shader.SetGlobalBuffer("_ParticleCountOfGrid", m_ParticleCountOfGridBuffer);
    }

    public void UpdatePerspectiveGridData(Camera camera, Vector3 gridMin, Vector3 gridMax, int perspectiveGridDimX, int perspectiveGridDimY, bool isFreeze)
    {
        if(!isFreeze)
        {
            m_ViewMatrixForGrid = camera.worldToCameraMatrix;
        }

        if(
            camera.fieldOfView != m_CameraFov ||
            camera.nearClipPlane != m_CameraNear ||
            camera.farClipPlane != m_CameraFar ||
            gridMin != m_GridMin ||
            gridMax != m_GridMax ||
            perspectiveGridDimX != m_PerspectiveGridDimX ||
            perspectiveGridDimY != m_PerspectiveGridDimY
        )
        {
            m_CameraFov = camera.fieldOfView;
            m_CameraNear = camera.nearClipPlane;
            m_CameraFar = camera.farClipPlane;
            m_GridMin = gridMin;
            m_GridMax = gridMax;
            m_PerspectiveGridDimX = perspectiveGridDimX;
            m_PerspectiveGridDimY = perspectiveGridDimY;
            CalculateGridDim();
        }

        Shader.SetGlobalMatrix("_ViewMatrixForGrid", m_ViewMatrixForGrid);
        Shader.SetGlobalBuffer("_VisibleGridBuffer", m_VisibleGridBuffer);
        Shader.SetGlobalBuffer("_ParticleCountOfGrid", m_ParticleCountOfGridBuffer);
    }

    void CalculateGridDim()
    {
        Vector4 p0 = new Vector4(m_GridMin.x, m_GridMin.y, m_GridMin.z, 1.0f);
        Vector4 p6 = new Vector4(m_GridMax.x, m_GridMax.y, m_GridMax.z, 1.0f);
        Vector4 p1 = new Vector4(p6.x, p0.y, p0.z, 1.0f);
        Vector4 p2 = new Vector4(p6.x, p6.y, p0.z, 1.0f);
        Vector4 p3 = new Vector4(p0.x, p6.y, p0.z, 1.0f);
        Vector4 p4 = new Vector4(p0.x, p0.y, p6.z, 1.0f);
        Vector4 p5 = new Vector4(p6.x, p0.y, p6.z, 1.0f);
        Vector4 p7 = new Vector4(p0.x, p6.y, p6.z, 1.0f);

        p0 = m_ViewMatrixForGrid * p0;
        p1 = m_ViewMatrixForGrid * p1;
        p2 = m_ViewMatrixForGrid * p2;
        p3 = m_ViewMatrixForGrid * p3;
        p4 = m_ViewMatrixForGrid * p4;
        p5 = m_ViewMatrixForGrid * p5;
        p6 = m_ViewMatrixForGrid * p6;
        p7 = m_ViewMatrixForGrid * p7;

        float near = -1000;
        near = Math.Max(p0.z, near);
        near = Math.Max(p1.z, near);
        near = Math.Max(p2.z, near);
        near = Math.Max(p3.z, near);
        near = Math.Max(p4.z, near);
        near = Math.Max(p5.z, near);
        near = Math.Max(p6.z, near);
        near = Math.Max(p7.z, near);
        near = Math.Max(m_CameraNear, -near);

        float far = 0;
        far = Math.Min(p0.z, far);
        far = Math.Min(p1.z, far);
        far = Math.Min(p2.z, far);
        far = Math.Min(p3.z, far);
        far = Math.Min(p4.z, far);
        far = Math.Min(p5.z, far);
        far = Math.Min(p6.z, far);
        far = Math.Min(p7.z, far);
        far = Math.Min(m_CameraFar, -far);

        float logFarAndNear = Mathf.Log(far / near);
        float fieldOfView = m_CameraFov * Mathf.Deg2Rad * 0.5f; //TODO:Orthographic
        float sampleRadioInv = m_PerspectiveGridDimY / (2.0f * Mathf.Tan(fieldOfView));
        int perspectiveGridDimZ = Mathf.CeilToInt(logFarAndNear * sampleRadioInv);
        m_GridCount = m_PerspectiveGridDimX * m_PerspectiveGridDimY * perspectiveGridDimZ;

        Shader.SetGlobalFloat("_NearPlane", near);
        Shader.SetGlobalFloat("_SampleRadioInv", sampleRadioInv);
        Shader.SetGlobalInt("_PerspectiveGridDimZ", perspectiveGridDimZ);

        if (m_ParticleCountOfGridBuffer != null) m_ParticleCountOfGridBuffer.Dispose();
        if (m_VisibleGridBuffer != null) m_VisibleGridBuffer.Dispose();
        m_ParticleCountOfGridBuffer = new ComputeBuffer(m_GridCount, sizeof(uint));
        m_VisibleGridBuffer = new ComputeBuffer(m_GridCount, sizeof(uint));
    }
}

public partial class CameraRenderer : MonoBehaviour
{
    ScriptableRenderContext m_Context;
    Camera m_Camera;
    SettingManager m_SettingManager;
    CullingResults m_CullingResults;
    CommandBuffer m_CommandBuffer;

    RenderTexture m_SceneColorRT;
    RenderTexture m_SceneDepthRT;
    RenderTexture m_FluidDepthRT;
    RenderTexture m_SmoothFluidDepthRT;
    RenderTexture m_FluidNormalRT;
    RenderTexture m_CullDebugRT;

    Material m_DrawFluidParticlesMaterial = Resources.Load<Material>("Materials/DrawFluidParticles");
    Material m_FilterMaterial = Resources.Load<Material>("Materials/Filter");
    Material m_GenerateNoramalMaterial = Resources.Load<Material>("Materials/GenerateNoramal");
    Material m_ToolsMaterial = Resources.Load<Material>("Materials/Tools");
    ComputeShader m_CullParticlesCS = Resources.Load("Shaders/CullParticles") as ComputeShader;

    Dictionary<string, PerspectiveGridData> PerspectiveGridData = new Dictionary<string, PerspectiveGridData>();

    public void Render(ScriptableRenderContext context, Camera camera, SettingManager renderManager)
    {
        m_Context = context;
        m_Camera = camera;
        m_SettingManager = renderManager;
        m_CommandBuffer = new CommandBuffer();

        if (!m_Camera.TryGetCullingParameters(out ScriptableCullingParameters p)) return;
        m_CullingResults = m_Context.Cull(ref p);

        UpdateLightData();
        UpdateCameraData();
        RenderScene();
        if(Application.isPlaying)
        {
            CullParticles();
            DrawParticles();
            SmoothFluidDepth();
            GenerateFluidNoramal();
        }
        if (m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FREEZE)
            Show(m_CullDebugRT);
        else Show(m_FluidNormalRT, m_SceneDepthRT);

        DrawUnsupportedShaders();
        DrawGizmos();

        m_Context.Submit();
        Clear();
    }

    #region Scene
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
        m_CommandBuffer.ClearRenderTarget(true, true, Color.gray);
        ExecuteCommandBuffer();

        var drawingSettings = new DrawingSettings(new ShaderTagId("Diffuse"), new SortingSettings(m_Camera));
        var filteringSettings = FilteringSettings.defaultValue;
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }
    #endregion

    #region Cull
    void CullParticles()
    {
        if (PerspectiveGridData.ContainsKey(m_Camera.name))
        {
            PerspectiveGridData[m_Camera.name].UpdatePerspectiveGridData(
                m_Camera,
                m_SettingManager.m_SimulatorData.MinPos,
                m_SettingManager.m_SimulatorData.MaxPos,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimX,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimY,
                m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FREEZE);
        }
        else
        {
            PerspectiveGridData perspectiveGridData = new PerspectiveGridData(
                m_Camera,
                m_SettingManager.m_SimulatorData.MinPos,
                m_SettingManager.m_SimulatorData.MaxPos,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimX,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimY);
            PerspectiveGridData.Add(m_Camera.name, perspectiveGridData);
        }
        m_SettingManager.m_CullParticleSetting.UpdateShaderProperty();

        int clearParticleCountOfGridKernel = m_CullParticlesCS.FindKernel("clearParticleCountOfGrid");
        int addUpParticleCountOfGridKernel = m_CullParticlesCS.FindKernel("addUpParticleCountOfGrid");
        int clearVisibleGridKernel = m_CullParticlesCS.FindKernel("clearVisibleGrid");
        int searchVisibleGridKernel = m_CullParticlesCS.FindKernel("searchVisibleGrid");

        m_CommandBuffer.name = "PerspectiveGrid ReSampling";

        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, clearParticleCountOfGridKernel, Mathf.CeilToInt((float)PerspectiveGridData[m_Camera.name].m_GridCount / 256), 1, 1);
        m_CommandBuffer.SetComputeBufferParam(m_CullParticlesCS, addUpParticleCountOfGridKernel, "_ParticlePositionBuffer", m_SettingManager.m_SimulatorData.NarrowPositionBuffer);
        m_CommandBuffer.SetComputeBufferParam(m_CullParticlesCS, addUpParticleCountOfGridKernel, "_ParticleIndirectArgment", m_SettingManager.m_SimulatorData.ArgumentBuffer);
        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, addUpParticleCountOfGridKernel, m_SettingManager.m_SimulatorData.ArgumentBuffer, 0);

        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, clearVisibleGridKernel, Mathf.CeilToInt((float)PerspectiveGridData[m_Camera.name].m_GridCount / 256), 1, 1);

        switch (m_SettingManager.m_CullParticleSetting.m_CullMode)
        {
            case CullMode.CullWithLayer:
                m_CommandBuffer.EnableShaderKeyword("_CULLWITYLAYER");
                m_CommandBuffer.DisableShaderKeyword("_CULLWITHADAPTIVE");
                break;
            case CullMode.CullWithAdaptive:
                m_CommandBuffer.DisableShaderKeyword("_CULLWITYLAYER");
                m_CommandBuffer.EnableShaderKeyword("_CULLWITHADAPTIVE");
                break;
        }
        m_CommandBuffer.DispatchCompute(
            m_CullParticlesCS, searchVisibleGridKernel,
            Mathf.CeilToInt((float)m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimX / 8),
            Mathf.CeilToInt((float)m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimY / 8), 1);
        ExecuteCommandBuffer();
    }
    #endregion

    #region Reconstruct
    void DrawParticles()
    {
        m_FluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "DrawParticles";

        switch(m_SettingManager.m_CullParticleSetting.m_CullMode)
        {
            case CullMode.None:
                m_CommandBuffer.DisableShaderKeyword("_CULL");
                m_CommandBuffer.DisableShaderKeyword("_FREEZE");
                m_CommandBuffer.SetRenderTarget(m_FluidDepthRT, m_FluidDepthRT);
                break;
            case CullMode.CullWithLayer:
            case CullMode.CullWithAdaptive:
                m_CommandBuffer.EnableShaderKeyword("_CULL");
                m_CommandBuffer.DisableShaderKeyword("_FREEZE");
                m_CommandBuffer.SetRenderTarget(m_FluidDepthRT, m_FluidDepthRT);
                break;
            case CullMode.FREEZE:
                m_CommandBuffer.DisableShaderKeyword("_CULL");
                m_CommandBuffer.EnableShaderKeyword("_FREEZE");
                m_CullDebugRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                m_CommandBuffer.SetRenderTarget(m_CullDebugRT, m_FluidDepthRT);
                break;
        }

        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalBuffer("_ParticlePositionBuffer", m_SettingManager.m_SimulatorData.NarrowPositionBuffer);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", m_SceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", m_SettingManager.m_ReconstructSetting.m_ParticlesRadius);
        if (m_SettingManager.m_SimulatorData.AnisotropyBuffer != null)
        {
            m_CommandBuffer.SetGlobalBuffer("_AnisotropyBuffer", m_SettingManager.m_SimulatorData.AnisotropyBuffer);
            m_CommandBuffer.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_DrawFluidParticlesMaterial, 1,
                MeshTopology.Triangles, m_SettingManager.m_SimulatorData.ArgumentBuffer, 12);
        }
        else
        {
            m_CommandBuffer.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_DrawFluidParticlesMaterial, 0,
                MeshTopology.Triangles, m_SettingManager.m_SimulatorData.ArgumentBuffer, 12);
        }
        ExecuteCommandBuffer();
    }

    void SmoothFluidDepth()
    {
        m_SmoothFluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "SmoothFluidDepth";
        m_SettingManager.m_ReconstructSetting.UpdateShaderProperty();
        m_CommandBuffer.SetRenderTarget(m_SmoothFluidDepthRT, m_SmoothFluidDepthRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_FluidDepthRT);
        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 0, MeshTopology.Triangles, 3);

        ExecuteCommandBuffer();
    }

    void GenerateFluidNoramal()
    {
        m_FluidNormalRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf);

        m_CommandBuffer.name = "GenerateFluidNoramal";
        m_CommandBuffer.SetRenderTarget(m_FluidNormalRT);
        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalTexture("_SmoothFluidDepthRT", m_SmoothFluidDepthRT);

        m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_GenerateNoramalMaterial, 0, MeshTopology.Triangles, 3);

        ExecuteCommandBuffer();
    }
    #endregion

    void Show(RenderTexture outputRT, RenderTexture depthRT = null)
    {
        m_CommandBuffer.name = "Show";
        m_CommandBuffer.Blit(outputRT, m_Camera.targetTexture, Vector2.one, Vector2.zero);

        if (depthRT)
        {
            m_CommandBuffer.SetGlobalTexture("_SrcDepth", depthRT);
            m_CommandBuffer.SetRenderTarget(0, m_Camera.targetTexture);
            m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_ToolsMaterial, 0, MeshTopology.Triangles, 3);
        }
        ExecuteCommandBuffer();
    }

    void Clear()
    {
        RenderTexture.ReleaseTemporary(m_SceneColorRT);
        RenderTexture.ReleaseTemporary(m_SceneDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidDepthRT);
        RenderTexture.ReleaseTemporary(m_SmoothFluidDepthRT);
        RenderTexture.ReleaseTemporary(m_FluidNormalRT);

        RenderTexture.ReleaseTemporary(m_CullDebugRT);
    }

    void ExecuteCommandBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }
}
