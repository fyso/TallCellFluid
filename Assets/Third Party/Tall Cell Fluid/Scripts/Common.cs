using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Common
{
    public static uint ThreadCount1D = 512;
    public static uint ThreadCount2D = 16;
    public static uint ThreadCount3D = 2;
    public static string DownsampleToolsCSPath = "Shaders/DownSampleTools";
    public static string RemeshToolsCSPath = "Shaders/RemeshTools";
    public static string ParticleInCellToolsCSPath = "Shaders/ParticleInCellTools";

    public static Texture2D CopyRenderTextureToCPU(RenderTexture vTarget)
    {
        Texture2D Result = new Texture2D(vTarget.width, vTarget.height, TextureFormat.RFloat, false);

        RenderTexture Temp = RenderTexture.active;
        RenderTexture.active = vTarget;
        Result.ReadPixels(new Rect(0, 0, vTarget.width, vTarget.height), 0, 0);
        RenderTexture.active = Temp;

        return Result;
    }
}