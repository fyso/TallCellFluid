using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Rendering Setting Asset")]
public class RenderingSetting : ScriptableObject
{
    public Cubemap m_Skybox;

    public Color m_Diffuse = new Color(0.0f, 0.1f, 0.7f, 1.0f);
    public Color m_GrazingDiffuse = new Color(0.2f, 0.4f, 0.6f, 1.0f);

    [Range(1.0f, 2.0f)]
    public float m_WaterIOF = 1.33f;

    public bool m_ShowSkyBox = true;
    public bool m_ShowDiffuse = true;
    public bool m_ShowSpecular = true;
    public bool m_ShowReflecion = true;
    public bool m_ShowRefraction = true;

    [Range(0, 2)]
    public float m_DiffuseStrength = 1;
    [Range(0, 2)]
    public float m_SpecularSpottedStrength = 1;
    [Range(0, 2)]
    public float m_SpecularStrength = 1;
    [Range(0, 2)]
    public float m_ReflecionStrength = 1;
    [Range(0, 2)]
    public float m_RefractionStrength = 1;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_ShowSkyBox", m_ShowSkyBox ? 1 : 0);
        Shader.SetGlobalInt("_ShowDiffuse", m_ShowDiffuse ? 1 : 0);
        Shader.SetGlobalInt("_ShowSpecular", m_ShowSpecular ? 1 : 0);
        Shader.SetGlobalInt("_ShowReflecion", m_ShowReflecion ? 1 : 0);
        Shader.SetGlobalInt("_ShowRefraction", m_ShowRefraction ? 1 : 0);

        Shader.SetGlobalVector("_Diffuse", m_Diffuse);
        Shader.SetGlobalVector("_GrazingDiffuse", m_GrazingDiffuse);
        Shader.SetGlobalFloat("_WaterIOF", m_WaterIOF);
        Shader.SetGlobalFloat("_DiffuseStrength", m_DiffuseStrength);
        Shader.SetGlobalFloat("_SpecularSpottedStrength", m_SpecularSpottedStrength);
        Shader.SetGlobalFloat("_SpecularStrength", m_SpecularStrength);
        Shader.SetGlobalFloat("_ReflecionStrength", m_ReflecionStrength);
        Shader.SetGlobalFloat("_RefractionStrength", m_RefractionStrength);
    }
}