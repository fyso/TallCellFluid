using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/PerspectiveGrid Setting Asset")]
public class PerspectiveGridSetting : ScriptableObject
{
    [Range(0, 10)]
    public int m_Layer = 1;
    [Range(0, 20)]
    public int m_MaxSurfaceDensity = 2;

    public bool m_OcclusionCullingDebug = false;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_Layer", m_Layer);
        Shader.SetGlobalInt("_MaxSurfaceDensity", m_MaxSurfaceDensity);
    }
}