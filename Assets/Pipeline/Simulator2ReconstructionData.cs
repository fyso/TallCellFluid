using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Simulator To Reconstruction Data")]
public class Simulator2ReconstructionData : ScriptableObject
{
    public ComputeBuffer ParticleArgumentBuffer;
    public ComputeBuffer PositionBuffer;
    public ComputeBuffer AnisotropyBuffer;

    public ComputeBuffer FoamArgumentBuffer;
    public ComputeBuffer FoamPositionBuffer;
    public ComputeBuffer FoamVelocityBuffer;
    public ComputeBuffer FoamLifeTimeBuffer;

    public Vector3 MinPos;
    public Vector3 MaxPos;
}