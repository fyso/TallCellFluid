using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUDPP
{
    public static class Common
    {
        public static int ThreadCount1D = 512;
        public static int ThreadCount2D = 32;
        public static int ThreadCount3D = 8;
        public static string GPUScanCSPath = "GPUScan";
        public static string GPUBufferClearCSPath = "GPUBufferClear";
    }
}