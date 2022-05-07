using UnityEngine;

//TODO: too many tall cell particles heavily solw down the speed of p2g/g2p, so we choose to
//delete the only tall cell particle after least squares method (we need choose a new least squares method)
public class ParticleInPostProcessingTools
{
    public ParticleInPostProcessingTools(Vector3 vMin, float vCellLength)
    {
        m_ParticleInCellToolsCS = Resources.Load<ComputeShader>(Common.ParticleInCellToolsCSPath);
        m_ComputeAnisotropyMatrixKernelIndex = m_ParticleInCellToolsCS.FindKernel("computeAnisotropyMatrix");
        
    }

    private void computeAnisotropyMatrix()
    {
       
    }

    private ComputeShader m_ParticleInCellToolsCS;
    private int m_ComputeAnisotropyMatrixKernelIndex;

}
