using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Simulator To Reconstruction Data")]
public class Simulator2ReconstructionData : ScriptableObject
{
    public ComputeBuffer ArgumentBuffer;
    public ComputeBuffer NarrowPositionBuffer;
    public ComputeBuffer AnisotropyBuffer;
    public Vector3 MinPos;
    public Vector3 MaxPos;
}