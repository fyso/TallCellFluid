using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public class GPUScanBlelloch
    {
        public GPUScanBlelloch()
        {
            GPUScanCS = Resources.Load<ComputeShader>(Common.GPUBlellochScanCSPath);
            ScanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            ScanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            ScanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");

            ScanCache1 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            ScanCache2 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
        }

        ~GPUScanBlelloch()
        {
            ScanCache1.Release();
            ScanCache2.Release();
        }

        public void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Input", vCountBuffer);
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Output", voOffsetBuffer);
            int GroupCount = (int)Mathf.Ceil((float)vCountBuffer.count / Common.ThreadCount1D);
            GPUScanCS.Dispatch(ScanInBucketKernel, GroupCount, 1, 1);

            GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
            if (GroupCount > 0)
            {
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", ScanCache1);
                GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", ScanCache1);
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", ScanCache2);
                    GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", ScanCache1);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", ScanCache2);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", ScanCache1);
                    GPUScanCS.Dispatch(ScanAddBucketResultKernel, GroupCount * Common.ThreadCount1D, 1, 1);
                }

                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", ScanCache1);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(ScanAddBucketResultKernel, (int)Mathf.Ceil(((float)vCountBuffer.count / Common.ThreadCount1D)), 1, 1);
            }
        }

        private ComputeShader GPUScanCS;
        private int ScanInBucketKernel;
        private int ScanBucketResultKernel;
        private int ScanAddBucketResultKernel;

        private ComputeBuffer ScanCache1;
        private ComputeBuffer ScanCache2;
    }
}
