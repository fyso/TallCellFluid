using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUBufferClear
    {
        public GPUBufferClear()
        {
            GPUBufferClearCS = Resources.Load<ComputeShader>(Common.GPUBufferClearCSPath);
            ClearUIntBufferWithZeroKernel = GPUBufferClearCS.FindKernel("clearUIntBufferWithZero");
        }

        public void ClearUIntBufferWithZero(ComputeBuffer voTarget)
        {
            GPUBufferClearCS.SetInt("BufferSize", voTarget.count);
            GPUBufferClearCS.SetBuffer(ClearUIntBufferWithZeroKernel, "TargetUIntBuffer_RW", voTarget);
            GPUBufferClearCS.Dispatch(ClearUIntBufferWithZeroKernel, (int)Mathf.Ceil(((float)voTarget.count / Common.ThreadCount1D)), 1, 1);
        }

        private ComputeShader GPUBufferClearCS;
        private int ClearUIntBufferWithZeroKernel;
    }
}
