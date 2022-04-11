using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUMultiSplitPlan
    {
        public ComputeBuffer WarpLevelHistogram;
        public ComputeBuffer HistogramOffset;
        public int ElementCount;
        public int BucketCount;
        public int LaneCount;
        public int KeyTypeSize;
        public int WarpCount;

        public GPUMultiSplitPlan(int vElementCount, int vBucketCount, int vLaneCount, int vKeyTypeSize = sizeof(uint))
        {
            ElementCount = vElementCount;
            BucketCount = vBucketCount;
            LaneCount = vLaneCount;
            KeyTypeSize = vKeyTypeSize;

            WarpCount = Mathf.CeilToInt((float)vElementCount / vLaneCount);
            WarpLevelHistogram = new ComputeBuffer(WarpCount * vBucketCount, vKeyTypeSize);
            HistogramOffset = new ComputeBuffer(WarpCount * vBucketCount, vKeyTypeSize);
        }

        ~GPUMultiSplitPlan()
        {
            WarpLevelHistogram.Release();
            HistogramOffset.Release();
        }
    }

    public class GPUMultiSplit
    {
        public GPUMultiSplit()
        {
            m_GPUMultiSplitCS = Resources.Load<ComputeShader>(Common.GPUMultiSplitCSPath);
            computeWarpLevelHistogram = m_GPUMultiSplitCS.FindKernel("computeWarpLevelHistogram");
        }

        public void MultiSplit(ComputeBuffer voKey, ComputeBuffer voValue, GPUMultiSplitPlan vPlan, GPUScanHillis vScan, GPUScanHillisPlan vScanCache)
        {
            if (voKey.count != voValue.count)
                Debug.LogError("Input of MultiSplit do not have the same size!");
            if (vPlan.ElementCount != voKey.count || vPlan.KeyTypeSize != voKey.stride)
                Debug.LogError("Unmatching MultiSplit Plane!");

            m_GPUMultiSplitCS.SetInt("ElementCount", vPlan.ElementCount);
            m_GPUMultiSplitCS.SetInt("BucketCount", vPlan.BucketCount);

            m_GPUMultiSplitCS.SetBuffer(computeWarpLevelHistogram, "Key_R", voKey);
            m_GPUMultiSplitCS.SetBuffer(computeWarpLevelHistogram, "WarpLevelHistogram_RW", vPlan.WarpLevelHistogram);
            m_GPUMultiSplitCS.Dispatch(computeWarpLevelHistogram, Mathf.CeilToInt((float)vPlan.ElementCount / Common.ThreadCount1D), 1, 1);
            vScan.Scan(vPlan.WarpLevelHistogram, vPlan.HistogramOffset, vScanCache, vPlan.WarpCount * vPlan.BucketCount);
        }

        private ComputeShader m_GPUMultiSplitCS;
        private int computeWarpLevelHistogram;

    }
}