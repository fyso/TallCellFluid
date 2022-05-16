using UnityEngine;

public enum CullMode
{
    None = 0,
    CullWithLayer = 1,
    CullWithAdaptive = 2,
    FreezeWithLayer = 3,
    FreezeWithAdaptive = 4,
}
[CreateAssetMenu(menuName = "Rendering/Cull Particles Setting Asset")]
public class CullParticleSetting : ScriptableObject
{

    [Range(0, 10)]
    public int m_DrawLayer = 1;
    [Range(0, 100)]
    public int m_MaxVisibleCount = 1;
    [Range(32, 256)]
    public int m_PerspectiveGridDimX = 128;
    [Range(32, 256)]
    public int m_PerspectiveGridDimY = 128;

    public CullMode m_CullMode = CullMode.None;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_DrawLayer", m_DrawLayer);
        Shader.SetGlobalInt("_MaxVisibleCount", m_MaxVisibleCount);
        Shader.SetGlobalInt("_PerspectiveGridDimX", m_PerspectiveGridDimX);
        Shader.SetGlobalInt("_PerspectiveGridDimY", m_PerspectiveGridDimY);
    }
}