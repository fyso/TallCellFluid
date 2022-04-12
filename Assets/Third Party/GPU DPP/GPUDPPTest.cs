using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUDPP
{
    public class GPUDPPTest : MonoBehaviour
    {
        [Range(0, 1000000)]
        public int ElementCount = 500000;
        private ComputeBuffer m_Key;
        private ComputeBuffer m_Value;
        private ComputeBuffer m_Result1;
        private ComputeBuffer m_Result2;

        private GPUScanBlelloch m_GPUScanBlelloch;

        private GPUScanHillis m_GPUScanHillis;
        private GPUScanHillisPlan m_GPUScanHillisPlan;

        private GPUMultiSplit m_GPUMultiSplit;
        private GPUMultiSplitPlan m_GPUMultiSplitPlan;

        private int[] Key;
        private int[] Value;

        void Start()
        {
            m_Key = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Value = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result1 = new ComputeBuffer(ElementCount, sizeof(uint));
            m_Result2 = new ComputeBuffer(ElementCount, sizeof(uint));

            m_GPUScanBlelloch = new GPUScanBlelloch();

            m_GPUScanHillis = new GPUScanHillis();
            m_GPUScanHillisPlan = new GPUScanHillisPlan();

            m_GPUMultiSplit = new GPUMultiSplit();
            m_GPUMultiSplitPlan = new GPUMultiSplitPlan(ElementCount, 10, 32);

            Key = new int[ElementCount];
            System.Random Rand = new System.Random();
            for (int i = 0; i < ElementCount; i++)
            {
                //Key[i] = Rand.Next() % 10;
                Key[i] = i % 10;
            }
            m_Key.SetData(Key);

            Value = new int[ElementCount];
            for (int i = 0; i < ElementCount; i++)
            {
                Value[i] = i;
            }
            m_Value.SetData(Key);
        }

        void Update()
        {
            Profiler.BeginSample("B Scan");
            m_GPUScanBlelloch.Scan(m_Key, m_Result1);
            Profiler.EndSample();

            Profiler.BeginSample("H Scan");
            m_GPUScanHillis.Scan(m_Key, m_Result2, m_GPUScanHillisPlan, ElementCount);
            Profiler.EndSample();

            Profiler.BeginSample("MultiSplit");
            m_GPUMultiSplit.MultiSplit(ref m_Key, ref m_Value, m_GPUMultiSplitPlan, m_GPUScanHillis, m_GPUScanHillisPlan);
            Profiler.EndSample();

            uint[] Key = new uint[m_Key.count];
            m_Key.GetData(Key);

            uint[] Offset = new uint[m_GPUMultiSplitPlan.HistogramOffset.count];
            m_GPUMultiSplitPlan.HistogramOffset.GetData(Offset);

            Vector3Int[] Result = new Vector3Int[m_GPUMultiSplitPlan.DEBUG.count];
            m_GPUMultiSplitPlan.DEBUG.GetData(Result);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                HScanTestCase();
            }
        }

        private void HScanTestCase()
        {
            bool NoError = true;

            int[] Result = new int[ElementCount];
            m_Result2.GetData(Result);

            int[] CorrectResult = new int[ElementCount];
            for (int i = 0; i < ElementCount; i++)
            {
                if (i != 0)
                {
                    CorrectResult[i] = (CorrectResult[i - 1] + Key[i - 1]);
                }
                else
                {
                    CorrectResult[i] = 0;
                }

                if (Result[i] != CorrectResult[i])
                {
                    NoError = !NoError;
                }
            }
            if (!NoError)
                Debug.LogError("H Scan Error!");
            else
                Debug.Log("H Scan Pass!");
        }

        private void OnDestroy()
        {
            m_Key.Release();
            m_Value.Release();
            m_Result1.Release();
            m_Result2.Release();
        }
    }
}