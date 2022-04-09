using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUScan
    {
        public GPUScan()
        {
            GPUScanCS = Resources.Load<ComputeShader>(Common.GPUScanCSPath);
            ScanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            ScanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            ScanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");
        }

        public void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer, ComputeBuffer vScanCache1, ComputeBuffer vScanCache2)
        {
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Input", vCountBuffer);
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Output", voOffsetBuffer);
            int GroupCount = (int)Mathf.Ceil((float)vCountBuffer.count / Common.ThreadCount1D);
            GPUScanCS.Dispatch(ScanInBucketKernel, GroupCount, 1, 1);

            GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
            if (GroupCount > 0)
            {
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", vScanCache1);
                GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", vScanCache1);
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", vScanCache2);
                    GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", vScanCache1);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", vScanCache2);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", vScanCache1);
                    GPUScanCS.Dispatch(ScanAddBucketResultKernel, GroupCount * Common.ThreadCount1D, 1, 1);
                }

                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", vScanCache1);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(ScanAddBucketResultKernel, (int)Mathf.Ceil(((float)vCountBuffer.count / Common.ThreadCount1D)), 1, 1);
            }
        }

        private ComputeShader GPUScanCS;
        private int ScanInBucketKernel;
        private int ScanBucketResultKernel;
        private int ScanAddBucketResultKernel;
    }
}
