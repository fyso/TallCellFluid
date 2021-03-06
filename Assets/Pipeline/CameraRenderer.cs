using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

public class PerspectiveGridData
{
    float m_CameraFov;
    float m_CameraNear;
    float m_CameraFar;
    float m_GridLeft;
    float m_GridRight;
    float m_GridDown;
    float m_GridUp;
    float m_GridNear;
    float m_GridFar;

    Vector3 m_GridMin;
    Vector3 m_GridMax;
    DepthSplitMode m_DepthSplitMode;
    int m_PerspectiveGridDimX;
    int m_PerspectiveGridDimY;
    Matrix4x4 m_ViewMatrixForGrid;

    public ComputeBuffer m_VisibleGridBuffer;
    public ComputeBuffer m_ParticleCountOfGridBuffer;
    public int m_GridCount;

    public PerspectiveGridData(Camera camera, Vector3 gridMin, Vector3 gridMax, int perspectiveGridDimX, int perspectiveGridDimY, DepthSplitMode depthSplitMode)
    {
        m_GridMin = gridMin;
        m_GridMax = gridMax;
        m_CameraFov = camera.fieldOfView;
        m_CameraNear = camera.nearClipPlane;
        m_CameraFar = camera.farClipPlane;
        m_DepthSplitMode = depthSplitMode;
        m_PerspectiveGridDimX = perspectiveGridDimX;
        m_PerspectiveGridDimY = perspectiveGridDimY;
        m_ViewMatrixForGrid = camera.worldToCameraMatrix;
        CalculateGridDim(camera.aspect);

        Shader.SetGlobalFloat("_LeftPlane", m_GridLeft);
        Shader.SetGlobalFloat("_PerspectiveGridLengthX", m_GridRight - m_GridLeft);
        Shader.SetGlobalFloat("_DownPlane", m_GridDown);
        Shader.SetGlobalFloat("_PerspectiveGridLengthY", m_GridUp - m_GridDown);
        Shader.SetGlobalFloat("_NearPlane", m_GridNear);
        Shader.SetGlobalFloat("_FarPlane", m_GridFar);
        Shader.SetGlobalMatrix("_ViewMatrixForGrid", m_ViewMatrixForGrid);
        Shader.SetGlobalBuffer("_VisibleGridBuffer", m_VisibleGridBuffer);
        Shader.SetGlobalBuffer("_ParticleCountOfGrid", m_ParticleCountOfGridBuffer);
    }

    public void UpdatePerspectiveGridData(Camera camera, Vector3 gridMin, Vector3 gridMax, int perspectiveGridDimX, int perspectiveGridDimY, DepthSplitMode depthSplitMode, bool isFreeze)
    {
        if (!isFreeze)
        {
            if (m_GridMin != gridMin ||
                m_GridMax != gridMax ||
                m_CameraFov != camera.fieldOfView ||
                m_CameraFar != camera.farClipPlane ||
                m_CameraNear != camera.nearClipPlane ||
                m_PerspectiveGridDimX != perspectiveGridDimX ||
                m_PerspectiveGridDimY != perspectiveGridDimY ||
                m_DepthSplitMode != depthSplitMode ||
                m_ViewMatrixForGrid != camera.worldToCameraMatrix)
            {
                m_GridMin = gridMin;
                m_GridMax = gridMax;
                m_CameraFov = camera.fieldOfView;
                m_CameraNear = camera.nearClipPlane;
                m_CameraFar = camera.farClipPlane;
                m_DepthSplitMode = depthSplitMode;
                m_PerspectiveGridDimX = perspectiveGridDimX;
                m_PerspectiveGridDimY = perspectiveGridDimY;
                m_ViewMatrixForGrid = camera.worldToCameraMatrix;
                CalculateGridDim(camera.aspect);
            }
        }
        else
        {
            if (m_DepthSplitMode != depthSplitMode)
            {
                m_DepthSplitMode = depthSplitMode;
                CalculateGridDim(camera.aspect);
            }
        }

        Shader.SetGlobalFloat("_LeftPlane", m_GridLeft);
        Shader.SetGlobalFloat("_PerspectiveGridLengthX", m_GridRight - m_GridLeft);
        Shader.SetGlobalFloat("_DownPlane", m_GridDown);
        Shader.SetGlobalFloat("_PerspectiveGridLengthY", m_GridUp - m_GridDown);
        Shader.SetGlobalFloat("_NearPlane", m_GridNear);
        Shader.SetGlobalFloat("_FarPlane", m_GridFar);
        Shader.SetGlobalMatrix("_ViewMatrixForGrid", m_ViewMatrixForGrid);
        Shader.SetGlobalBuffer("_VisibleGridBuffer", m_VisibleGridBuffer);
        Shader.SetGlobalBuffer("_ParticleCountOfGrid", m_ParticleCountOfGridBuffer);
    }

    void CalculateGridDim(float vAspect)
    {
        float fieldView = m_CameraFov * Mathf.Deg2Rad * 0.5f;
        float tanFieldView = Mathf.Tan(fieldView);

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
        //X??
        float result;
        result = p0.z > p1.z ? p0.z : p1.z;
        result = p2.z > result ? p2.z : result;
        result = p3.z > result ? p3.z : result;
        result = p4.z > result ? p4.z : result;
        result = p5.z > result ? p5.z : result;
        result = p6.z > result ? p6.z : result;
        result = p7.z > result ? p7.z : result;
        m_GridNear = m_CameraNear > -result ? m_CameraNear : -result;

        result = p0.z < p1.z ? p0.z : p1.z;
        result = p2.z < result ? p2.z : result;
        result = p3.z < result ? p3.z : result;
        result = p4.z < result ? p4.z : result;
        result = p5.z < result ? p5.z : result;
        result = p6.z < result ? p6.z : result;
        result = p7.z < result ? p7.z : result;
        m_GridFar = m_CameraFar < -result ? m_CameraFar : -result;

        result = p0.y < p1.y ? p0.y : p1.y;
        result = p2.y < result ? p2.y : result;
        result = p3.y < result ? p3.y : result;
        result = p4.y < result ? p4.y : result;
        result = p5.y < result ? p5.y : result;
        result = p6.y < result ? p6.y : result;
        result = p7.y < result ? p7.y : result;
        float maxHalfHeight = m_GridFar * tanFieldView;
        m_GridDown = -maxHalfHeight < result ? result : -maxHalfHeight;

        result = p0.y > p1.y ? p0.y : p1.y;
        result = p2.y > result ? p2.y : result;
        result = p3.y > result ? p3.y : result;
        result = p4.y > result ? p4.y : result;
        result = p5.y > result ? p5.y : result;
        result = p6.y > result ? p6.y : result;
        result = p7.y > result ? p7.y : result;
        m_GridUp = maxHalfHeight > result ? result : maxHalfHeight;

        float maxHalfWidth = maxHalfHeight * vAspect;
        result = p0.x < p1.x ? p0.x : p1.x;
        result = p2.x < result ? p2.x : result;
        result = p3.x < result ? p3.x : result;
        result = p4.x < result ? p4.x : result;
        result = p5.x < result ? p5.x : result;
        result = p6.x < result ? p6.x : result;
        result = p7.x < result ? p7.x : result;
        m_GridLeft = -maxHalfWidth < result ? result : -maxHalfWidth;

        result = p0.x > p1.x ? p0.x : p1.x;
        result = p2.x > result ? p2.x : result;
        result = p3.x > result ? p3.x : result;
        result = p4.x > result ? p4.x : result;
        result = p5.x > result ? p5.x : result;
        result = p6.x > result ? p6.x : result;
        result = p7.x > result ? p7.x : result;
        m_GridRight = maxHalfWidth > result ? result : maxHalfWidth;

        float sampleRadioInv = 0f;
        int perspectiveGridDimZ = 0;
        switch (m_DepthSplitMode)
        {
            case DepthSplitMode.Uniform:
                sampleRadioInv = m_GridFar - m_GridNear;
                perspectiveGridDimZ = 128; //TODO
                break;
            case DepthSplitMode.Log:
                float log = Mathf.Log(m_GridFar / m_GridNear);
                sampleRadioInv = m_PerspectiveGridDimY / (2.0f * tanFieldView);
                perspectiveGridDimZ = Mathf.CeilToInt(log * sampleRadioInv);
                break;
            case DepthSplitMode.Cube:
                float logFarAndNear = Mathf.Log(m_GridFar / m_GridNear);
                float sD = 2.0f * tanFieldView / m_PerspectiveGridDimY;
                sampleRadioInv = 1.0f / Mathf.Log(1.0f + sD);
                perspectiveGridDimZ = Mathf.CeilToInt(logFarAndNear * sampleRadioInv);
                break;
        }

        m_GridCount = m_PerspectiveGridDimX * m_PerspectiveGridDimY * perspectiveGridDimZ;
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

    #region Resource
    RenderTextureManager m_OutputRTManager = new RenderTextureManager("Output")
    {
        m_EnableRandomWrite = true
    };
    RenderTexture m_OutputRT;
    RenderTexture m_SceneDepthRT;
    RenderTexture m_FluidDepthRT;
    RenderTexture m_SmoothFluidDepthRT;
    RenderTexture m_FluidNormalRT;
    RenderTexture m_CullDebugRT;
    Dictionary<string, PerspectiveGridData> PerspectiveGridData = new Dictionary<string, PerspectiveGridData>();
    #endregion

    #region Shader
    Material m_DrawFluidParticlesMaterial = Resources.Load<Material>("Materials/DrawFluidParticles");
    Material m_FilterMaterial = Resources.Load<Material>("Materials/Filter");
    Material m_GenerateNoramalMaterial = Resources.Load<Material>("Materials/GenerateNoramal");
    Material m_ToolsMaterial = Resources.Load<Material>("Materials/Tools");
    ComputeShader m_CullParticlesCS = Resources.Load("Shaders/CullParticles") as ComputeShader;

    RayTracingAccelerationStructure m_AccelerationStructure;
    RayTracingShader m_ShadeFluidShader = Resources.Load<RayTracingShader>("Shaders/ShadeFluid");

    Material m_FoamMaterial = Resources.Load<Material>("Materials/DrawFoam");
    #endregion

    public CameraRenderer()
    {
        RayTracingAccelerationStructure.RASSettings setting = new RayTracingAccelerationStructure.RASSettings(
            RayTracingAccelerationStructure.ManagementMode.Automatic,
            RayTracingAccelerationStructure.RayTracingModeMask.Everything,
            -1 ^ (1 << 7));
        m_AccelerationStructure = new RayTracingAccelerationStructure(setting);
    }

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
            RenderFluid();
            RenderOutsideFoam();
        }
        if (m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FreezeWithLayer || 
            m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FreezeWithAdaptive)
            Show(m_CullDebugRT);
        else Show(m_OutputRT);

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
        m_OutputRT = m_OutputRTManager.GetOrCreateRT(m_Context, m_Camera.name, m_Camera.pixelWidth, m_Camera.pixelHeight, true);
        m_SceneDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "RenderScene";
        m_CommandBuffer.SetRenderTarget(m_OutputRT, m_SceneDepthRT);
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
        if (m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.None) return;
        if (PerspectiveGridData.ContainsKey(m_Camera.name))
        {
            PerspectiveGridData[m_Camera.name].UpdatePerspectiveGridData(
                m_Camera,
                m_SettingManager.m_Simulator2ReconstructionData.MinPos,
                m_SettingManager.m_Simulator2ReconstructionData.MaxPos,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimX,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimY,
                m_SettingManager.m_CullParticleSetting.m_DepthSplitMode,
                m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FreezeWithLayer || 
                m_SettingManager.m_CullParticleSetting.m_CullMode == CullMode.FreezeWithAdaptive);
        }
        else
        {
            PerspectiveGridData perspectiveGridData = new PerspectiveGridData(
                m_Camera,
                m_SettingManager.m_Simulator2ReconstructionData.MinPos,
                m_SettingManager.m_Simulator2ReconstructionData.MaxPos,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimX,
                m_SettingManager.m_CullParticleSetting.m_PerspectiveGridDimY,
                m_SettingManager.m_CullParticleSetting.m_DepthSplitMode);
            PerspectiveGridData.Add(m_Camera.name, perspectiveGridData);
        }
        m_SettingManager.m_CullParticleSetting.UpdateShaderProperty();

        int clearParticleCountOfGridKernel = m_CullParticlesCS.FindKernel("clearParticleCountOfGrid");
        int addUpParticleCountOfGridKernel = m_CullParticlesCS.FindKernel("addUpParticleCountOfGrid");
        int clearVisibleGridKernel = m_CullParticlesCS.FindKernel("clearVisibleGrid");
        int searchVisibleGridKernel = m_CullParticlesCS.FindKernel("searchVisibleGrid");

        m_CommandBuffer.name = "PerspectiveGrid ReSampling";
        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, clearParticleCountOfGridKernel, Mathf.CeilToInt((float)PerspectiveGridData[m_Camera.name].m_GridCount / 512), 1, 1);

        if (m_SettingManager.m_CullParticleSetting.m_DepthSplitMode != DepthSplitMode.Uniform)
            m_CommandBuffer.EnableShaderKeyword("_DEPTHSPLIT_NONLINEAR");
        else
            m_CommandBuffer.DisableShaderKeyword("_DEPTHSPLIT_NONLINEAR");

        m_CommandBuffer.SetComputeBufferParam(m_CullParticlesCS, addUpParticleCountOfGridKernel, "_ParticlePositionBuffer", m_SettingManager.m_Simulator2ReconstructionData.PositionBuffer);
        m_CommandBuffer.SetComputeBufferParam(m_CullParticlesCS, addUpParticleCountOfGridKernel, "_ParticleIndirectArgment", m_SettingManager.m_Simulator2ReconstructionData.ParticleArgumentBuffer);
        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, addUpParticleCountOfGridKernel, m_SettingManager.m_Simulator2ReconstructionData.ParticleArgumentBuffer, 0);
        m_CommandBuffer.DispatchCompute(m_CullParticlesCS, clearVisibleGridKernel, Mathf.CeilToInt((float)PerspectiveGridData[m_Camera.name].m_GridCount / 512), 1, 1);

        switch (m_SettingManager.m_CullParticleSetting.m_CullMode)
        {
            case CullMode.CullWithLayer:
            case CullMode.FreezeWithLayer:
                m_CommandBuffer.EnableShaderKeyword("_CULLWITHLAYER");
                m_CommandBuffer.DisableShaderKeyword("_CULLWITHADAPTIVE");
                break;
            case CullMode.CullWithAdaptive:
            case CullMode.FreezeWithAdaptive:
                m_CommandBuffer.DisableShaderKeyword("_CULLWITHLAYER");
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
            case CullMode.FreezeWithLayer:
            case CullMode.FreezeWithAdaptive:
                m_CommandBuffer.DisableShaderKeyword("_CULL");
                m_CommandBuffer.EnableShaderKeyword("_FREEZE");
                m_CullDebugRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
                m_CommandBuffer.SetRenderTarget(m_CullDebugRT, m_FluidDepthRT);
                break;
        }


        if (m_SettingManager.m_CullParticleSetting.m_DepthSplitMode != DepthSplitMode.Uniform)
            m_CommandBuffer.EnableShaderKeyword("_DEPTHSPLIT_NONLINEAR");
        else
            m_CommandBuffer.DisableShaderKeyword("_DEPTHSPLIT_NONLINEAR");

        m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        m_CommandBuffer.SetGlobalBuffer("_ParticlePositionBuffer", m_SettingManager.m_Simulator2ReconstructionData.PositionBuffer);
        m_CommandBuffer.SetGlobalTexture("_SceneDepth", m_SceneDepthRT);
        m_CommandBuffer.SetGlobalFloat("_ParticlesRadius", m_SettingManager.m_ReconstructSetting.m_ParticlesRadius);
        if (m_SettingManager.m_Simulator2ReconstructionData.AnisotropyBuffer != null)
        {
            m_CommandBuffer.SetGlobalBuffer("_AnisotropyBuffer", m_SettingManager.m_Simulator2ReconstructionData.AnisotropyBuffer);
            m_CommandBuffer.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_DrawFluidParticlesMaterial, 1,
                MeshTopology.Triangles, m_SettingManager.m_Simulator2ReconstructionData.ParticleArgumentBuffer, 12);
        }
        else
        {
            m_CommandBuffer.DrawProceduralIndirect(
                Matrix4x4.identity,
                m_DrawFluidParticlesMaterial, 0,
                MeshTopology.Triangles, m_SettingManager.m_Simulator2ReconstructionData.ParticleArgumentBuffer, 12);
        }
        ExecuteCommandBuffer();
    }

    void SmoothFluidDepth()
    {
        m_SmoothFluidDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 32, RenderTextureFormat.Depth);

        m_CommandBuffer.name = "SmoothFluidDepth";
        m_SettingManager.m_ReconstructSetting.UpdateShaderProperty();

        switch(m_SettingManager.m_ReconstructSetting.m_FilterMethod)
        {
            case FilterMethod._1D:
                m_CommandBuffer.SetRenderTarget(m_SmoothFluidDepthRT, m_SmoothFluidDepthRT);
                m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
                m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_FluidDepthRT);
                m_CommandBuffer.DisableShaderKeyword("_2D");
                m_CommandBuffer.EnableShaderKeyword("_1D_X");
                m_CommandBuffer.DisableShaderKeyword("_1D_Y");
                m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 0, MeshTopology.Triangles, 3);

                m_CommandBuffer.SetRenderTarget(m_FluidDepthRT, m_FluidDepthRT);
                m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
                m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_SmoothFluidDepthRT);
                m_CommandBuffer.DisableShaderKeyword("_2D");
                m_CommandBuffer.DisableShaderKeyword("_1D_X");
                m_CommandBuffer.EnableShaderKeyword("_1D_Y");
                m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 0, MeshTopology.Triangles, 3);

                m_CommandBuffer.SetRenderTarget(m_SmoothFluidDepthRT, m_SmoothFluidDepthRT);
                m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
                m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_FluidDepthRT);
                m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 1, MeshTopology.Triangles, 3);
                break;

            case FilterMethod._2D:
                m_CommandBuffer.SetRenderTarget(m_SmoothFluidDepthRT, m_SmoothFluidDepthRT);
                m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
                m_CommandBuffer.SetGlobalTexture("_FluidDepthRT", m_FluidDepthRT);
                m_CommandBuffer.EnableShaderKeyword("_2D");
                m_CommandBuffer.DisableShaderKeyword("_1D_X");
                m_CommandBuffer.DisableShaderKeyword("_1D_Y");
                m_CommandBuffer.DrawProcedural(Matrix4x4.identity, m_FilterMaterial, 0, MeshTopology.Triangles, 3);
            break;
        }


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

    #region Rendering
    void RenderFluid()
    {
        m_SettingManager.m_RenderingSetting.UpdateShaderProperty();
        m_AccelerationStructure.Build();

        m_CommandBuffer.name = "RenderFluid";
        m_CommandBuffer.SetRayTracingAccelerationStructure(m_ShadeFluidShader, "_AccelerationStructure", m_AccelerationStructure);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_SceneDepthRT", m_SceneDepthRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_FluidNormalRT", m_FluidNormalRT);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_Skybox", m_SettingManager.m_RenderingSetting.m_Skybox);
        m_CommandBuffer.SetRayTracingTextureParam(m_ShadeFluidShader, "_OutputRT", m_OutputRT);

        m_CommandBuffer.SetRayTracingShaderPass(m_ShadeFluidShader, "SceneHit");
        m_CommandBuffer.DispatchRays(m_ShadeFluidShader, "FluidRayGen", (uint)m_Camera.pixelWidth, (uint)m_Camera.pixelHeight, 1);

        ExecuteCommandBuffer();
    }

    void RenderOutsideFoam()
    {
        if (!m_SettingManager.m_FoamSetting.m_DrawFoam) return;

        m_CommandBuffer.name = "OutsideFoam";
        m_CommandBuffer.SetRenderTarget(m_OutputRT, m_SceneDepthRT);
        m_CommandBuffer.ClearRenderTarget(false, false, Color.clear);

        m_CommandBuffer.EnableShaderKeyword("_OUTSIDE_FOAM");
        m_CommandBuffer.SetGlobalTexture("_FluidNormalRT", m_FluidNormalRT);
        m_CommandBuffer.SetGlobalBuffer("_FoamPositionBuffer", m_SettingManager.m_Simulator2ReconstructionData.FoamPositionBuffer);
        m_CommandBuffer.SetGlobalBuffer("_FoamVelocityBuffer", m_SettingManager.m_Simulator2ReconstructionData.FoamVelocityBuffer);
        m_CommandBuffer.SetGlobalBuffer("_FoamLifeTimeBuffer", m_SettingManager.m_Simulator2ReconstructionData.FoamLifeTimeBuffer);

        m_SettingManager.m_FoamSetting.UpdateShaderProperty();

        m_CommandBuffer.DrawProceduralIndirect(
            Matrix4x4.identity,
            m_FoamMaterial, 0,
            MeshTopology.Triangles, m_SettingManager.m_Simulator2ReconstructionData.FoamArgumentBuffer, 12);

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
