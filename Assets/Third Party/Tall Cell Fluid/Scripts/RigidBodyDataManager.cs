using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct RigidbodyInfo
{
    public Matrix4x4 m_WorldToObject;
    public Vector3 m_Min;
    public Vector3 m_BoundSize;
    public Vector3 m_Pos;
    public Vector3 m_Velocity;
    public Vector3 m_AngularVelocity;
}

public class RigidBodyDataManager : MonoBehaviour
{
    private List<Texture3D> m_RigidBodySDF;
    private List<RigidbodyInfo> m_RigidbodyInfo;
    private ComputeBuffer m_RigidbodyInfoComputeBuffer;

    void Start()
    {
        m_RigidBodySDF = new List<Texture3D>();
        m_RigidbodyInfo = new List<RigidbodyInfo>();
        int a = Marshal.SizeOf(typeof(RigidbodyInfo));
        m_RigidbodyInfoComputeBuffer = new ComputeBuffer(Common.MaxRigidNum, Marshal.SizeOf(typeof(RigidbodyInfo)));
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
        m_RigidbodyInfoComputeBuffer.SetData(m_RigidbodyInfo.ToArray());
        vComputeShader.SetBuffer(vKernelIndex, "RigidbodyInfos", m_RigidbodyInfoComputeBuffer);
        for(int i = 0; i < Common.MaxRigidNum; i++) //TODO: No support for 3d texture arrays
        {
            if(i < m_RigidBodySDF.Count) vComputeShader.SetTexture(vKernelIndex, "SDF" + i, m_RigidBodySDF[i]);
            else vComputeShader.SetTexture(vKernelIndex, "SDF" + i, m_RigidBodySDF[0]);
        }
    }

    private void OnDisable()
    {
        m_RigidbodyInfoComputeBuffer.Dispose();
    }
}
