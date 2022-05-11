using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public Utils()
    {
        UtilsCS = Resources.Load<ComputeShader>("Shaders/Utils");
        copyFloat4Texture2DToAnother = UtilsCS.FindKernel("copyFloat4Texture2DToAnother");
        copyFloat4Texture3DToAnother = UtilsCS.FindKernel("copyFloat4Texture3DToAnother");
        clearIntTexture2D = UtilsCS.FindKernel("clearIntTexture2D");
        clearIntTexture3D = UtilsCS.FindKernel("clearIntTexture3D");
        clearFloat3Texture2D = UtilsCS.FindKernel("clearFloat3Texture2D");
        clearFloatTexture3D = UtilsCS.FindKernel("clearFloatTexture3D");
        clearFloatTexture2D = UtilsCS.FindKernel("clearFloatTexture2D");
    }

    public void CopyFloat4Texture2DToAnother(Texture2D vSource, RenderTexture vDestination)
    {
        if (vSource.width != vDestination.width || vSource.height != vDestination.height)
        {
            Debug.LogError("Unmatched size!");
        }

        UtilsCS.SetTexture(copyFloat4Texture2DToAnother, "Source", vSource);
        UtilsCS.SetTexture(copyFloat4Texture2DToAnother, "Destination", vDestination);
        UtilsCS.Dispatch(copyFloat4Texture2DToAnother, Mathf.CeilToInt((float)vSource.width / Common.ThreadCount2D), Mathf.CeilToInt((float)vSource.height / Common.ThreadCount2D), 1);
    }
    
    public void CopyFloat4Texture3DToAnother(Texture3D vSource, RenderTexture vDestination)
    {
        if (vSource.width != vDestination.width || vSource.height != vDestination.height)
        {
            Debug.LogError("Unmatched size!");
        }

        UtilsCS.SetTexture(copyFloat4Texture3DToAnother, "Source3D_R", vSource);
        UtilsCS.SetTexture(copyFloat4Texture3DToAnother, "Destination3D_RW", vDestination);
        UtilsCS.Dispatch(copyFloat4Texture3DToAnother, Mathf.CeilToInt((float)vSource.width / Common.ThreadCount3D), Mathf.CeilToInt((float)vSource.height / Common.ThreadCount3D), Mathf.CeilToInt((float)vSource.depth / Common.ThreadCount3D));
    }

    public void ClearIntTexture3D(RenderTexture vClearTarget)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
            Debug.LogError("need 3d texture but given a non 3d texture!");

        UtilsCS.SetTexture(clearIntTexture3D, "ClearTarget3D_RW", vClearTarget);
        UtilsCS.Dispatch(clearIntTexture3D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount3D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount3D), Mathf.CeilToInt((float)vClearTarget.volumeDepth / Common.ThreadCount3D));
    }

    public void ClearIntTexture2D(RenderTexture vClearTarget, int vClearValue = 0)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
            Debug.LogError("need 2d texture but given a non 2d texture!");

        UtilsCS.SetInt("ClearIntTexture3DValue", vClearValue);
        UtilsCS.SetTexture(clearIntTexture2D, "ClearTarget2D_RW", vClearTarget);
        UtilsCS.Dispatch(clearIntTexture2D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount2D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount2D), 1);
    }

    public void ClearFloat3Texture2D(RenderTexture vClearTarget, Vector3 vClearValue)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
            Debug.LogError("need 2d texture but given a non 2d texture!");

        UtilsCS.SetFloats("ClearFloat3Value", vClearValue.x, vClearValue.y, vClearValue.z);

        UtilsCS.SetTexture(clearFloat3Texture2D, "ClearFloat3Target3D_RW", vClearTarget);
        UtilsCS.Dispatch(clearFloat3Texture2D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount2D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount2D), 1);
    }

    public void ClearFloatTexture3D(RenderTexture vClearTarget, float vClearValue = 0.0f)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
            Debug.LogError("need 3d texture but given a non 3d texture!");

        UtilsCS.SetFloat("clearFloatTexture3DValue", vClearValue);

        UtilsCS.SetTexture(clearFloatTexture3D, "clearFloatTexture3D_RW", vClearTarget);
        UtilsCS.Dispatch(clearFloatTexture3D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount3D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount3D), Mathf.CeilToInt((float)vClearTarget.volumeDepth / Common.ThreadCount3D));
    }

    public void ClearFloatTexture2D(RenderTexture vClearTarget, float vClearValue = 0.0f)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
            Debug.LogError("need 2d texture but given a non 2d texture!");

        UtilsCS.SetFloat("clearFloatTexture2DValue", vClearValue);

        UtilsCS.SetTexture(clearFloatTexture2D, "ClearFloatTarget2D_RW", vClearTarget);
        UtilsCS.Dispatch(clearFloatTexture2D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount2D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount2D), 1);
    }

    private ComputeShader UtilsCS;
    private int copyFloat4Texture2DToAnother;
    private int copyFloat4Texture3DToAnother;
    private int clearIntTexture2D;
    private int clearIntTexture3D;
    private int clearFloat3Texture2D;
    private int clearFloatTexture3D;
    private int clearFloatTexture2D;
}
