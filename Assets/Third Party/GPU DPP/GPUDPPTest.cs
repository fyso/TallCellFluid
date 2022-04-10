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
        private GPUScanBlelloch m_GPUScanBlelloch;
        private GPUScanHillis m_GPUScanHillisWarp;
        private int[] CPU;
        void Start()
        {
            m_Target = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result1 = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result2 = new ComputeBuffer(ElementCount, sizeof(uint));
            m_GPUScanBlelloch = new GPUScanBlelloch();
            m_GPUScanHillisWarp = new GPUScanHillis();

            CPU = new int[ElementCount];
            System.Random Rand = new System.Random();
            for (int i = 0; i < ElementCount; i++)
            {
                CPU[i] = Rand.Next() % 10;
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

            if (Input.GetKeyDown(KeyCode.Space))
            {
                int[] Result = new int[ElementCount];
                m_Result2.GetData(Result);
                for(int i = 0; i < ElementCount; i++)
                {
                    int Sum = 0;
                    if(i != 0)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            Sum += CPU[j];
                        }
                    }
                    if (Result[i] != Sum)
                    {
                        Debug.LogError("H Scan Error!");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            m_Target.Release();
            m_Result1.Release();
            m_Result2.Release();
        }
    }
}