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

    private ComputeShader UtilsCS;
    private int copyFloat4Texture2DToAnother;
    private int copyFloat4Texture3DToAnother;
}
