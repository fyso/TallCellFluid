using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUScanHillisPlan
    {
        public ComputeBuffer ScanCache1;
        public ComputeBuffer ScanCache2;
        public ComputeBuffer ScanCache3;
        public ComputeBuffer ScanCache4;

        public GPUScanHillisPlan()
        {
            ScanCache1 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            ScanCache2 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            ScanCache3 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
            ScanCache4 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
        }

        ~GPUScanHillisPlan()
        {
            ScanCache1.Release();
            ScanCache2.Release();
            ScanCache3.Release();
            ScanCache4.Release();
        }
    }

    public class GPUScanHillis
    {
        public GPUScanHillis()
        {
            GPUScanHillisWarpCS = Resources.Load<ComputeShader>(Common.GPUHillisWarpScanCSPath);
            scanInBlock = GPUScanHillisWarpCS.FindKernel("scanInBlock");
            sumAcrossPow2BlockSize = GPUScanHillisWarpCS.FindKernel("sumAcrossPow2BlockSize");
            sumAcrossBlockSize = GPUScanHillisWarpCS.FindKernel("sumAcrossBlockSize");
        }

        public void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, GPUScanHillisPlan vCache)
        {
            if(vCountBuffer.count != voOffsetBuffer.count)
                Debug.LogError("Input of Hillis Warp level scan do not have the same size!");

            int ElementCount = vCountBuffer.count;

            GPUScanHillisWarpCS.SetInt("ElementCount", ElementCount);

            if (ElementCount > Mathf.Pow(Common.ThreadCount1D, 2.0f) && ElementCount <= Mathf.Pow(Common.ThreadCount1D, 3.0f))
                ScanPow3BlockSize(vCountBuffer, voOffsetBuffer, vCache, ElementCount);
            else if (ElementCount > Common.ThreadCount1D && ElementCount <= Mathf.Pow(Common.ThreadCount1D, 2.0f))
                ScanPow2BlockSize(vCountBuffer, voOffsetBuffer, vCache, ElementCount);
            else if (ElementCount <= Common.ThreadCount1D)
                ScanBlockSize(vCountBuffer, voOffsetBuffer, vCache, ElementCount);
            else
                Debug.LogError("Out of max element count of Hillis Warp level scan!");
        }

        private void ScanPow3BlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, GPUScanHillisPlan vCache, int vElementCount)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vElementCount / Common.ThreadCount1D), 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCache.ScanCache1);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", vCache.ScanCache2);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Common.ThreadCount1D, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCache.ScanCache3);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", vCache.ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, 1, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "ScanCachePow2BlockSize", vCache.ScanCache2);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "ScanCacheBlockSize", vCache.ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossPow2BlockSize, "TargetResult", voOffsetBuffer);
            GPUScanHillisWarpCS.Dispatch(sumAcrossPow2BlockSize, Mathf.CeilToInt((float)vElementCount / Common.ThreadCount1D), 1, 1);
        }

        private void ScanPow2BlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, GPUScanHillisPlan vCache, int vElementCount)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vElementCount / Common.ThreadCount1D), 1, 1);

            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCache.ScanCache3);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", vCache.ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache1);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, 1, 1, 1);

            GPUScanHillisWarpCS.SetBuffer(sumAcrossBlockSize, "ScanCacheBlockSize", vCache.ScanCache4);
            GPUScanHillisWarpCS.SetBuffer(sumAcrossBlockSize, "TargetResult", voOffsetBuffer);
            GPUScanHillisWarpCS.Dispatch(sumAcrossBlockSize, Mathf.CeilToInt((float)vElementCount / Common.ThreadCount1D), 1, 1);
        }

        private void ScanBlockSize(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, GPUScanHillisPlan vCache, int vElementCount)
        {
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "Target", vCountBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockResult", voOffsetBuffer);
            GPUScanHillisWarpCS.SetBuffer(scanInBlock, "BlockSum", vCache.ScanCache3);
            GPUScanHillisWarpCS.Dispatch(scanInBlock, Mathf.CeilToInt((float)vElementCount / Common.ThreadCount1D), 1, 1);
        }

        private ComputeShader GPUScanHillisWarpCS;
        private int scanInBlock;
        private int sumAcrossPow2BlockSize;
        private int sumAcrossBlockSize;
    }
}
