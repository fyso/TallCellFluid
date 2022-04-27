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
        clearUIntTexture2D = UtilsCS.FindKernel("clearUIntTexture2D");
        clearIntTexture3D = UtilsCS.FindKernel("clearIntTexture3D");
        updateArgment = UtilsCS.FindKernel("updateArgment");
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

    public void ClearUIntTexture2D(RenderTexture vClearTarget)
    {
        if (vClearTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
            Debug.LogError("need 2d texture but given a non 2d texture!");

        UtilsCS.SetTexture(clearUIntTexture2D, "ClearTarget2D_RW", vClearTarget);
        UtilsCS.Dispatch(clearUIntTexture2D, Mathf.CeilToInt((float)vClearTarget.width / Common.ThreadCount2D), Mathf.CeilToInt((float)vClearTarget.height / Common.ThreadCount2D), 1);
    }

    public void UpdateArgment(ComputeBuffer vTallCellArgument, ComputeBuffer vParticleArgument, int vOnlyTallCellParticleXGridCountArgumentOffset, int vScatterOnlyTallCellParticleArgmentOffset)
    {
        UtilsCS.SetInt("ScatterOnlyTallCellParticleArgmentOffset", vScatterOnlyTallCellParticleArgmentOffset);
        UtilsCS.SetInt("OnlyTallCellParticleXGridCountArgumentOffset", vOnlyTallCellParticleXGridCountArgumentOffset);
        UtilsCS.SetBuffer(updateArgment, "ParticleIndirectArgment_R", vParticleArgument);
        UtilsCS.SetBuffer(updateArgment, "TallCellIndirectArgment_RW", vTallCellArgument);
        UtilsCS.Dispatch(updateArgment, 1, 1, 1);
    }

    private ComputeShader UtilsCS;
    private int copyFloat4Texture2DToAnother;
    private int copyFloat4Texture3DToAnother;
    private int clearUIntTexture2D;
    private int clearIntTexture3D;
    private int updateArgment;
}
