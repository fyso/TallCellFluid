using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Filter Setting Asset")]
public class ReconstructSetting : ScriptableObject
{
    [Range(0, 3)]
    public float m_ParticlesRadius = 0.25f;
    [Range(0, 5)]
    public float m_FilterRadiusWS = 0.5f;
    [Range(0, 10)]
    public float m_ClampRatio = 1.0f;
    [Range(0, 20)]
    public float m_ThresholdRatio = 10.5f;
    [Range(0, 1)]
    public float m_Sigma = 0.5f;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalFloat("_ParticlesRadius", m_ParticlesRadius);
        Shader.SetGlobalFloat("_FilterRadiusWS", m_FilterRadiusWS);
        Shader.SetGlobalFloat("_ClampRatio", m_ClampRatio);
        Shader.SetGlobalFloat("_ThresholdRatio", m_ThresholdRatio);
        Shader.SetGlobalFloat("_Sigma", m_Sigma);
    }
}