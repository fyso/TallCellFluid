using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Reconstruction Manager Asset")]
public class RenderManager : ScriptableObject
{
    public Bounding m_Bounding;
    public ParticleData m_ParticleData;
    public FilterSetting m_FilterSetting;
    public PerspectiveGridSetting m_PerspectiveGridSetting;
}