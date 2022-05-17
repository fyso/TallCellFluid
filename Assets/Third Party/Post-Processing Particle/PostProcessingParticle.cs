using UnityEngine;

public class PostProcessingParticle
{
    public PostProcessingParticle(int vMaxParticleCount, Vector3 vMin, float vCellLength, ComputeBuffer vArgument, ComputeBuffer vHashCount, ComputeBuffer vHashOffset)
    {
        m_ParticlePostProcessingToolsCS = Resources.Load<ComputeShader>("Shaders/ParticlePostProcessingTools");
        m_ComputeAnisotropyMatrixKernelIndex = m_ParticlePostProcessingToolsCS.FindKernel("computeAnisotropyMatrix");
        m_ArgumentBuffer = vArgument;
        m_HashCountBuffer = vHashCount;
        m_HashOffsetBuffer = vHashOffset;
        m_ParticlePostProcessingToolsCS.SetFloats("Min", vMin.x, vMin.y, vMin.z);
        m_ParticlePostProcessingToolsCS.SetFloat("CellLength", vCellLength);

        m_NarrowPositionBuffer = new ComputeBuffer(vMaxParticleCount, sizeof(float) * 3);
        m_AnisotropyBuffer = new ComputeBuffer(vMaxParticleCount, sizeof(float) * 2);
    }

    public void computeAnisotropyMatrix(ComputeBuffer vParticlePos, int vIterNum)
    {
        m_ParticlePostProcessingToolsCS.SetInt("IterNum", vIterNum);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "IndirectArgmentBuffer", m_ArgumentBuffer);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "HashCountBuffer", m_HashCountBuffer);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "HashOffsetBuffer", m_HashOffsetBuffer);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "ParticlePosBuffer", vParticlePos);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "NarrowPositionBuffer", m_NarrowPositionBuffer);
        m_ParticlePostProcessingToolsCS.SetBuffer(m_ComputeAnisotropyMatrixKernelIndex, "AnisotropyBuffer", m_AnisotropyBuffer);
        m_ParticlePostProcessingToolsCS.DispatchIndirect(m_ComputeAnisotropyMatrixKernelIndex, m_ArgumentBuffer);
    }

    ~PostProcessingParticle()
    {
        m_NarrowPositionBuffer.Release();
        m_AnisotropyBuffer.Release();
    }

    private ComputeShader m_ParticlePostProcessingToolsCS;
    private int m_ComputeAnisotropyMatrixKernelIndex;
    private ComputeBuffer m_ArgumentBuffer;
    private ComputeBuffer m_HashCountBuffer;
    private ComputeBuffer m_HashOffsetBuffer;

    public ComputeBuffer m_NarrowPositionBuffer;
    public ComputeBuffer m_AnisotropyBuffer;
}
