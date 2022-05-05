using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Reconstruction Manager Asset")]
public class RenderManager : ScriptableObject
{
    public ParticleData m_ParticleData;
    public FilterSetting m_FilterSetting;
}