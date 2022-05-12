using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Particle Data Asset")]
public class ParticleData : ScriptableObject
{
    public ComputeBuffer ArgumentBuffer;
    public ComputeBuffer PositionBuffer;
    public ComputeBuffer AnisotropyBuffer;
}