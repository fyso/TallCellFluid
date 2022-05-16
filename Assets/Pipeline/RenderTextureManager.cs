using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

class RenderTextureManager
{
    Dictionary<string, RenderTexture> m_CameraRT = new Dictionary<string, RenderTexture>();
    CommandBuffer m_CommandBuffer;
    protected RayTracingShader m_ClearRTShader = Resources.Load<RayTracingShader>("Shaders/ClearRT");
    public bool m_EnableRandomWrite = false;
    public bool m_UseMipMap = false;
    public bool m_AutoGenerateMips = false;
    public GraphicsFormat m_GraphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;

    public RenderTextureManager(string passName)
    {
        m_CommandBuffer = new CommandBuffer
        {
            name = "Clear" + passName
        };
    }

    public RenderTexture GetOrCreateRT(ScriptableRenderContext context, string name, int width, int height, bool clear, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture outputRT;
        if (m_CameraRT.ContainsKey(name))
        {
            outputRT = m_CameraRT[name];
            if (outputRT == null)
            {
                m_CameraRT.Remove(name);
            }
            else
            {
                if (outputRT.width != width || outputRT.height != height)
                {
                    outputRT.Release();
                    outputRT = createRT(width, height);
                    m_CameraRT[name] = outputRT;
                }
                else if (clear)
                {
                    ClearRT(context, outputRT);
                }
                return outputRT;
            }
        }

        outputRT = createRT(width, height);
        m_CameraRT.Add(name, outputRT);
        return outputRT;
    }

    RenderTexture createRT(int width, int height, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 0, format)
        {
            autoGenerateMips = m_AutoGenerateMips
        };
        renderTexture.enableRandomWrite = m_EnableRandomWrite;
        renderTexture.useMipMap = m_UseMipMap;
        renderTexture.Create();

        return renderTexture;
    }

    void ClearRT(ScriptableRenderContext context, RenderTexture renderTexture)
    {
        Profiler.BeginSample(m_CommandBuffer.name);

        if (m_EnableRandomWrite)
        {
            m_CommandBuffer.SetRayTracingTextureParam(m_ClearRTShader, Shader.PropertyToID("_Output"), renderTexture);
            m_CommandBuffer.DispatchRays(m_ClearRTShader, "ClearRT", (uint)renderTexture.width, (uint)renderTexture.height, 1);
        }
        else
        {
            m_CommandBuffer.SetRenderTarget(renderTexture);
            m_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        }

        Profiler.EndSample();

        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }

    ~RenderTextureManager()
    {
        foreach (var rt in m_CameraRT)
        {
            rt.Value.Release();
        }
    }
}

