using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUDPP
{
    public class GPUDPPTest : MonoBehaviour
    {
        public int ElementCount = 1000000;
        private ComputeBuffer m_Target;
        private ComputeBuffer m_Result1;
        private ComputeBuffer m_Result2;
        private ComputeBuffer m_ScanCache1;
        private ComputeBuffer m_ScanCache2;
        private GPUScanBlelloch m_GPUScanBlelloch;
        private GPUScanHillisWarp m_GPUScanHillisWarp;
        void Start()
        {
            m_Target = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result1 = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result2 = new ComputeBuffer(ElementCount, sizeof(uint));
            m_ScanCache1 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            m_ScanCache2 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
            m_GPUScanBlelloch = new GPUScanBlelloch();
            m_GPUScanHillisWarp = new GPUScanHillisWarp();

            int[] CPU = new int[ElementCount];
            System.Random Rand = new System.Random();
            for (int i = 0; i < ElementCount; i++)
            {
                CPU[i] = 1;
            }
            m_Target.SetData(CPU);
        }

        void Update()
        {
            Profiler.BeginSample("B Scan");
            m_GPUScanBlelloch.Scan(m_Target, m_Result1);
            Profiler.EndSample();

            Profiler.BeginSample("H Scan");
            m_GPUScanHillisWarp.Scan(m_Target, m_Result2);
            Profiler.EndSample();

            if (!Input.GetKey(KeyCode.Space))
            {
                int[] CPU = new int[ElementCount];
                m_Result2.GetData(CPU);
                for(int i = 0; i < ElementCount; i++)
                {
                    if(CPU[i] != i)
                    {
                        Debug.LogError(" Error result!");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            m_Target.Release();
            m_Result1.Release();
            m_Result2.Release();
            m_ScanCache1.Release();
            m_ScanCache2.Release();
        }
    }
}