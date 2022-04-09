using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUScanHillisWarp
    {
        public GPUScanHillisWarp()
        {
            GPUScanHillisWarpCS = Resources.Load<ComputeShader>(Common.GPUHillisWarpScanCSPath);
            scanInBlock = GPUScanHillisWarpCS.FindKernel("scanInBlock");
            sumAcrossPow2BlockSize = GPUScanHillisWarpCS.FindKernel("sumAcrossPow2BlockSize");
            sumAcrossBlockSize = GPUScanHillisWarpCS.FindKernel("sumAcrossBlockSize");
            ScanCache1 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            ScanCache2 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            ScanCache3 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
            ScanCache4 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
        }

        ~GPUScanHillisWarp()
        {
            ScanCache1.Release();
            ScanCache2.Release();
            ScanCache3.Release();
            ScanCache4.Release();
        }

        public void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            if(vCountBuffer.count != voOffsetBuffer.count)
                Debug.LogError("Input of Hillis Warp level scan do not have the same size!");

            if (vCountBuffer.count > Mathf.Pow(Common.ThreadCount1D, 2.0f) && vCountBuffer.count <= Mathf.Pow(Common.ThreadCount1D, 3.0f))
                ScanPow3BlockSize(vCountBuffer, voOffsetBuffer);
            else if (vCountBuffer.count > Common.ThreadCount1D && vCountBuffer.count <= Mathf.Pow(Common.ThreadCount1D, 2.0f))
                ScanPow2BlockSize(vCountBuffer, voOffsetBuffer);
            else if (vCountBuffer.count <= Common.ThreadCount1D)
                ScanBlockSize(vCountBuffer, voOffsetBuffer);
            else
                Debug.LogError("Out of max element count of Hillis Warp level scan!");
        }

        private void ScanPow3BlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vCountBuffer.count / Common.ThreadCount1D), 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", ScanCache1);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", ScanCache2);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Common.ThreadCount1D, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", ScanCache3);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, 1, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "ScanCachePow2BlockSize", ScanCache2);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "ScanCacheBlockSize", ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "TargetResult", voOffsetBuffer);
            GPUScanHillisWarpCS.Dispatch(sumAcrossPow2BlockSize, Mathf.CeilToInt((float)vCountBuffer.count / Common.ThreadCount1D), 1, 1);
        }

        private void ScanPow2BlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vCountBuffer.count / Common.ThreadCount1D), 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", ScanCache3);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, 1, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(sumAcrossBlockSize, "ScanCacheBlockSize", ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossBlockSize, "TargetResult", voOffsetBuffer);
            GPUScanHillisWarpCS.Dispatch(sumAcrossBlockSize, Mathf.CeilToInt((float)vCountBuffer.count / Common.ThreadCount1D), 1, 1);
        }

        private void ScanBlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vCountBuffer.count / Common.ThreadCount1D), 1, 1);
        }

        private ComputeShader GPUScanHillisWarpCS;
        private int scanInBlock;
        private int sumAcrossPow2BlockSize;
        private int sumAcrossBlockSize;
        private ComputeBuffer ScanCache1;
        private ComputeBuffer ScanCache2;
        private ComputeBuffer ScanCache3;
        private ComputeBuffer ScanCache4;
    }
}
