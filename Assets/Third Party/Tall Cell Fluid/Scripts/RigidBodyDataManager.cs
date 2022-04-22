using UnityEngine;
using System.Collections.Generic;

public class RigidbodyInfo
{
    public Vector3 m_Min;
    public Vector3 m_Max;
    public Vector3 m_Pos;
    public Vector3 m_Velocity;
    public Vector3 m_AngularVelocity;
}

public class RigidBodyDataManager : MonoBehaviour
{
    private List<Texture3D> m_RigidBodySDF;
    private List<RigidbodyInfo> m_RigidbodyInfo;

    void Start()
    {
        m_RigidBodySDF = new List<Texture3D>();
        m_RigidbodyInfo = new List<RigidbodyInfo>();
    }

    public int RegisterRigidBody(Texture3D vRigidBodySDF, RigidbodyInfo vRigidbodyInfo)
    {
        m_RigidBodySDF.Add(vRigidBodySDF);
        m_RigidbodyInfo.Add(vRigidbodyInfo);
        return m_RigidbodyInfo.Count - 1;
    }

    public void UpdateRigidBodyInfo(int vRigidBodyIndex, RigidbodyInfo vRigidbodyInfo)
    {
        m_RigidbodyInfo[vRigidBodyIndex] = vRigidbodyInfo;
    }

    public void UploadRigidBodyDataToGPU(ComputeShader vComputeShader, int vKernelIndex)
    {
        vComputeShader.SetInt("RigidbodyNum", m_RigidBodySDF.Count);
        for(int i = 0; i < Mathf.Min(m_RigidBodySDF.Count, 4); i++) //TODO: No support for 3d texture arrays
        {
            vComputeShader.SetTexture(vKernelIndex, "SDF" + i, m_RigidBodySDF[i]);
            vComputeShader.SetVectorArray("RigidbodyInfo" + i, new Vector4[] {
                m_RigidbodyInfo[i].m_Min,
                m_RigidbodyInfo[i].m_Max,
                m_RigidbodyInfo[i].m_Pos,
                m_RigidbodyInfo[i].m_Velocity,
                m_RigidbodyInfo[i].m_AngularVelocity
            });
        }
    }
}
