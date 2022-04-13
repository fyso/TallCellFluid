using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUDPP
{
    public class GPUDPPTest : MonoBehaviour
    {
        [Range(32, 1000000)]
        public int ElementCount = 500000;
        public int BucketCount = 3;
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
            m_GPUMultiSplitPlan = new GPUMultiSplitPlan(ElementCount, BucketCount, 32);

            Key = new int[ElementCount];
            System.Random Rand = new System.Random();
            for (int i = 0; i < ElementCount; i++)
            {
                //Key[i] = Rand.Next() % 10;
                Key[i] = i % BucketCount;
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
            //Profiler.BeginSample("B Scan");
            //m_GPUScanBlelloch.Scan(m_Key, m_Result1);
            //Profiler.EndSample();

            Profiler.BeginSample("H Scan");
            m_GPUScanHillis.Scan(m_Key, m_Result2, m_GPUScanHillisPlan, ElementCount);
            Profiler.EndSample();

            Profiler.BeginSample("MultiSplit");
            m_GPUMultiSplit.MultiSplit(ref m_Key, ref m_Value, m_GPUMultiSplitPlan, m_GPUScanHillis, m_GPUScanHillisPlan);
            Profiler.EndSample();

            uint[] KeyDebug = new uint[m_Key.count];
            m_Key.GetData(KeyDebug);

            uint[] Histogram = new uint[m_GPUMultiSplitPlan.WarpLevelHistogram.count];
            m_GPUMultiSplitPlan.WarpLevelHistogram.GetData(Histogram);

            uint[] Offset = new uint[m_GPUMultiSplitPlan.HistogramOffset.count];
            m_GPUMultiSplitPlan.HistogramOffset.GetData(Offset);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                HScanTestCase();
                MultiSplitTestCase();
            }

            m_Key.SetData(Key);
            m_Value.SetData(Value);
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

        private void MultiSplitTestCase()
        {
            bool NoError = true;

            uint[] Result = new uint[ElementCount];
            m_Key.GetData(Result);

            uint[] EachBucketCount = new uint[BucketCount];
            for (int i = 0; i < ElementCount; i++)
            {
                EachBucketCount[Key[i]]++;
            }

            uint[] EachBucketOffset = new uint[BucketCount + 1];
            for (int i = 0; i < BucketCount; i++)
            {
                if (i != 0)
                {
                    EachBucketOffset[i] = EachBucketOffset[i - 1] + EachBucketCount[i - 1];
                }
                else
                {
                    EachBucketOffset[i] = 0;
                }
            }
            EachBucketOffset[BucketCount] = EachBucketOffset[BucketCount - 1] + EachBucketCount[BucketCount - 1];

            uint CurrentBucket = 0;
            for(uint i = 0; i < ElementCount; i++)
            {
                if(Result[i] != CurrentBucket)
                {
                    NoError = false;
                }

                if(i == EachBucketOffset[CurrentBucket + 1] - 1)
                {
                    CurrentBucket++;
                }
            }

            if (!NoError)
                Debug.LogError("Multi Split Error!");
            else
                Debug.Log("Multi Split Pass!");
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