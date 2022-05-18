using UnityEngine;

public enum CullMode
{
    None = 0,
    CullWithLayer = 1,
    CullWithAdaptive = 2,
    FreezeWithLayer = 3,
    FreezeWithAdaptive = 4,
}

public enum DepthSplitMode
{
    Uniform = 0,
    Log = 1,
    Cube = 2,
}

[CreateAssetMenu(menuName = "Rendering/Cull Particles Setting Asset")]
public class CullParticleSetting : ScriptableObject
{

    [Range(0, 10)]
    public int m_DrawLayer = 1;
    [Range(0, 20)]
    public int m_MaxVisibleCount = 1;
    [Range(0, 1)]
    public float m_MidFactor = 0.5f;
    [Range(32, 256)]
    public int m_PerspectiveGridDimX = 128;
    [Range(32, 256)]
    public int m_PerspectiveGridDimY = 128;

    public DepthSplitMode m_DepthSplitMode = DepthSplitMode.Uniform;
    public CullMode m_CullMode = CullMode.None;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_DrawLayer", m_DrawLayer);
        Shader.SetGlobalInt("_MaxVisibleCount", m_MaxVisibleCount);
        Shader.SetGlobalFloat("_MidFactor", m_MidFactor);
        Shader.SetGlobalInt("_PerspectiveGridDimX", m_PerspectiveGridDimX);
        Shader.SetGlobalInt("_PerspectiveGridDimY", m_PerspectiveGridDimY);
    }
}