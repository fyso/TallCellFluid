using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUMultiSplitPlan
    {
        public ComputeBuffer WarpLevelHistogram;
        public ComputeBuffer HistogramOffset;
        public ComputeBuffer BackKey;
        public ComputeBuffer BackValue;
        public ComputeBuffer DEBUG;
        public int ElementCount;
        public int BucketCount;
        public int LaneCount;
        public int KeyTypeSize;
        public int WarpCount;

        public GPUMultiSplitPlan(int vElementCount, int vBucketCount, int vLaneCount, int vKeyTypeSize = sizeof(uint), int vValueTypeSize = sizeof(uint))
        {
            ElementCount = vElementCount;
            BucketCount = vBucketCount;
            LaneCount = vLaneCount;
            KeyTypeSize = vKeyTypeSize;

            WarpCount = Mathf.CeilToInt((float)vElementCount / vLaneCount);
            WarpLevelHistogram = new ComputeBuffer(WarpCount * vBucketCount, sizeof(uint));
            HistogramOffset = new ComputeBuffer(WarpCount * vBucketCount, sizeof(uint));
            BackKey = new ComputeBuffer(vElementCount, vKeyTypeSize);
            BackValue = new ComputeBuffer(vElementCount, vValueTypeSize);
            DEBUG = new ComputeBuffer(vElementCount, sizeof(uint) * 3);
        }

        ~GPUMultiSplitPlan()
        {
            WarpLevelHistogram.Release();
            HistogramOffset.Release();
            BackKey.Release();
            BackValue.Release();
            DEBUG.Release();
        }
    }

    public class GPUMultiSplit
    {
        public GPUMultiSplit()
        {
            m_GPUMultiSplitCS = Resources.Load<ComputeShader>(Common.GPUMultiSplitCSPath);
            preScan = m_GPUMultiSplitCS.FindKernel("preScan");
            postScan = m_GPUMultiSplitCS.FindKernel("postScan");
        }

        public void MultiSplit(ref ComputeBuffer voKey, ref ComputeBuffer voValue, GPUMultiSplitPlan vPlan, GPUScanHillis vScan, GPUScanHillisPlan vScanCache)
        {
            if (voKey.count != voValue.count)
                Debug.LogError("Input of MultiSplit do not have the same size!");
            if (vPlan.ElementCount != voKey.count || vPlan.KeyTypeSize != voKey.stride)
                Debug.LogError("Unmatching MultiSplit Plane!");

            m_GPUMultiSplitCS.SetInt("ElementCount", vPlan.ElementCount);
            m_GPUMultiSplitCS.SetInt("BucketCount", vPlan.BucketCount);

            m_GPUMultiSplitCS.SetBuffer(preScan, "Key_R", voKey);
            m_GPUMultiSplitCS.SetBuffer(preScan, "WarpLevelHistogram_RW", vPlan.WarpLevelHistogram);
            m_GPUMultiSplitCS.Dispatch(preScan, Mathf.CeilToInt((float)vPlan.ElementCount / Common.ThreadCount1D), 1, 1);
            
            vScan.Scan(vPlan.WarpLevelHistogram, vPlan.HistogramOffset, vScanCache, vPlan.WarpCount * vPlan.BucketCount);

            m_GPUMultiSplitCS.SetBuffer(postScan, "Key_R", voKey);
            m_GPUMultiSplitCS.SetBuffer(postScan, "Value_R", voValue);
            m_GPUMultiSplitCS.SetBuffer(postScan, "WarpLevelHistogramOffset_R", vPlan.HistogramOffset);
            m_GPUMultiSplitCS.SetBuffer(postScan, "KeyBack_RW", vPlan.BackKey);
            m_GPUMultiSplitCS.SetBuffer(postScan, "ValueBack_RW", vPlan.BackValue);
            m_GPUMultiSplitCS.SetBuffer(postScan, "DEBUG", vPlan.DEBUG);
            m_GPUMultiSplitCS.Dispatch(postScan, Mathf.CeilToInt((float)vPlan.ElementCount / Common.ThreadCount1D), 1, 1);

            ComputeBuffer Temp = voValue;
            voValue = vPlan.BackValue;
            vPlan.BackValue = Temp;

            Temp = voKey;
            voKey = vPlan.BackKey;
            vPlan.BackKey = Temp;
        }

        private ComputeShader m_GPUMultiSplitCS;
        private int preScan;
        private int postScan;

    }
}