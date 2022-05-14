using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Reconstruction Manager Asset")]
public class SettingManager : ScriptableObject
{
    public SimulatorData m_SimulatorData;
    public CullParticleSetting m_CullParticleSetting;
    public ReconstructSetting m_ReconstructSetting;
}