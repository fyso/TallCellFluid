using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUDPP
{
    public class GPUMultiSplitPlan
    {
        public ComputeBuffer WarpLevelHistogram { get { return m_WarpLevelHistogram; } }
        public ComputeBuffer WarpLevelHistogramOffset { get { return m_WarpLevelHistogramOffset; } }
        public ComputeBuffer NewIndex { get { return m_NewIndex; } }
        public ComputeBuffer BackKey { get { return m_BackKey; } }
        public ComputeBuffer BackValue { get { return m_BackValue; } }
        public ComputeBuffer DEBUG { get { return m_DEBUG; } }
        public GPUScanHillis Scan { get { return m_Scan; } }
        public GPUScanHillisPlan ScanCache { get { return m_ScanCache; } }
        public int KeyTypeSize { get { return m_KeyTypeSize; } }

        public GPUMultiSplitPlan(int vMaxElementCount, int vMaxBucketCount, int vMinLaneCount, int vKeyTypeSize = sizeof(uint), int vValueTypeSize = sizeof(uint))
        {
            m_KeyTypeSize = vKeyTypeSize;

            int MaxWarpCount = Mathf.CeilToInt((float)vMaxElementCount / vMinLaneCount);
            m_WarpLevelHistogram = new ComputeBuffer(MaxWarpCount * vMaxBucketCount, sizeof(uint));
            m_WarpLevelHistogramOffset = new ComputeBuffer(MaxWarpCount * vMaxBucketCount, sizeof(uint));
            m_NewIndex = new ComputeBuffer(vMaxElementCount, vKeyTypeSize);
            m_BackKey = new ComputeBuffer(vMaxElementCount, vKeyTypeSize);
            m_BackValue = new ComputeBuffer(vMaxElementCount, vValueTypeSize);

            m_Scan = new GPUScanHillis();
            m_ScanCache = new GPUScanHillisPlan();
            m_DEBUG = new ComputeBuffer(vMaxElementCount * 2, sizeof(uint) * 3);
        }

        ~GPUMultiSplitPlan()
        {
            m_WarpLevelHistogram.Release();
            m_WarpLevelHistogramOffset.Release();
            m_NewIndex.Release();
            m_BackKey.Release();
            m_BackValue.Release();
            m_DEBUG.Release();
        }

        public void SwapBackAndFront(ref ComputeBuffer vioFrontKey, ref ComputeBuffer vioFrontValue)
        {
            ComputeBuffer Temp = vioFrontValue;
            vioFrontValue = m_BackValue;
            m_BackValue = Temp;

            Temp = vioFrontKey;
            vioFrontKey = m_BackKey;
            m_BackKey = Temp;
        }

        private ComputeBuffer m_WarpLevelHistogram;
        private ComputeBuffer m_WarpLevelHistogramOffset;
        private ComputeBuffer m_NewIndex;
        private ComputeBuffer m_BackKey;
        private ComputeBuffer m_BackValue;
        private ComputeBuffer m_DEBUG;
        private GPUScanHillis m_Scan;
        private GPUScanHillisPlan m_ScanCache;
        private int m_KeyTypeSize;
    }

    public class GPUMultiSplit
    {
        public GPUMultiSplit()
        {
            m_GPUBufferClear = new GPUBufferClear();

            m_GPUMultiSplitCS = Resources.Load<ComputeShader>(Common.GPUMultiSplitCSPath);
            preScan = m_GPUMultiSplitCS.FindKernel("preScan");
            postScan = m_GPUMultiSplitCS.FindKernel("postScan");
            rearrangeKeyValue = m_GPUMultiSplitCS.FindKernel("rearrangeKeyValue");
            updateSplitPoint32 = m_GPUMultiSplitCS.FindKernel("updateSplitPoint32");
        }

        public void ComputeNewIndex(ComputeBuffer vKey, GPUMultiSplitPlan vPlan, int vBucketCount, ComputeBuffer vArgument, int vElementCountOffset, int vGroupCountOffset, int vSplitPointOffset)
        {
            m_GPUBufferClear.ClraeUIntBufferWithZero(vPlan.WarpLevelHistogram);
            m_GPUBufferClear.ClraeUIntBufferWithZero(vPlan.WarpLevelHistogramOffset);

            m_GPUMultiSplitCS.SetInt("BucketCount", vBucketCount);
            m_GPUMultiSplitCS.SetInt("ElementCountOffset", vElementCountOffset);
            m_GPUMultiSplitCS.SetInt("GroupCountOffset", vGroupCountOffset);
            m_GPUMultiSplitCS.SetInt("SplitPointOffset", vSplitPointOffset);

            Profiler.BeginSample("preScan");
            m_GPUMultiSplitCS.SetBuffer(preScan, "Argument_R", vArgument);
            m_GPUMultiSplitCS.SetBuffer(preScan, "Key_R", vKey);
            m_GPUMultiSplitCS.SetBuffer(preScan, "WarpLevelHistogram_RW", vPlan.WarpLevelHistogram);
            m_GPUMultiSplitCS.DispatchIndirect(preScan, vArgument, 0);
            Profiler.EndSample();

            Profiler.BeginSample("Scan");
            vPlan.Scan.Scan(vPlan.WarpLevelHistogram, vPlan.WarpLevelHistogramOffset, vPlan.ScanCache);
            Profiler.EndSample();

            Profiler.BeginSample("postScan");
            m_GPUMultiSplitCS.SetBuffer(postScan, "Argument_R", vArgument);
            m_GPUMultiSplitCS.SetBuffer(postScan, "Key_R", vKey);
            m_GPUMultiSplitCS.SetBuffer(postScan, "WarpLevelHistogramOffset_R", vPlan.WarpLevelHistogramOffset);
            m_GPUMultiSplitCS.SetBuffer(postScan, "NewIndex_RW", vPlan.NewIndex);
            m_GPUMultiSplitCS.DispatchIndirect(postScan, vArgument, 0);
            Profiler.EndSample();

            Profiler.BeginSample("updateSplitPoint32");
            m_GPUMultiSplitCS.SetBuffer(updateSplitPoint32, "Argument_RW", vArgument);
            m_GPUMultiSplitCS.SetBuffer(updateSplitPoint32, "WarpLevelHistogramOffset_R", vPlan.WarpLevelHistogramOffset);
            m_GPUMultiSplitCS.Dispatch(updateSplitPoint32, 1, 1, 1);
            Profiler.EndSample();
        }

        public void DefaultRearrangeKeyValue(ref ComputeBuffer vioKey, ref ComputeBuffer vioValue, GPUMultiSplitPlan vPlan, ComputeBuffer vArgument, int vElementCountOffset)
        {
            if (vioKey.count != vioValue.count)
                Debug.LogError("Input of MultiSplit do not have the same size!");

            m_GPUMultiSplitCS.SetInt("ElementCountOffset", vElementCountOffset);

            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "Argument_R", vArgument);
            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "NewIndex_R", vPlan.NewIndex);
            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "OldKey_R", vioKey);
            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "OldValue_R", vioValue);
            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "NewKey_RW", vPlan.BackKey);
            m_GPUMultiSplitCS.SetBuffer(rearrangeKeyValue, "NewValue_RW", vPlan.BackValue);
            m_GPUMultiSplitCS.DispatchIndirect(rearrangeKeyValue, vArgument, 0);

            vPlan.SwapBackAndFront(ref vioKey, ref vioValue);
        }

        private ComputeShader m_GPUMultiSplitCS;
        private GPUBufferClear m_GPUBufferClear;
        private int preScan;
        private int postScan;
        private int rearrangeKeyValue;
        private int updateSplitPoint32;
    }
}