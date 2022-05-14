using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Particle Data Asset")]
public class SimulatorData : ScriptableObject
{
    public ComputeBuffer ArgumentBuffer;
    public ComputeBuffer NarrowPositionBuffer;
    public ComputeBuffer AnisotropyBuffer;
    public Vector3 MinPos;
    public Vector3 MaxPos;
}