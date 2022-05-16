using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Reconstruction Manager Asset")]
public class SettingManager : ScriptableObject
{
    public Simulator2ReconstructionData m_Simulator2ReconstructionData;
    public CullParticleSetting m_CullParticleSetting;
    public ReconstructSetting m_ReconstructSetting;
    public RenderingSetting m_RenderingSetting;
}