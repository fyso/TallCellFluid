using UnityEngine;

public enum CullMode
{
    None = 0,
    Cull = 1,
    FREEZE = 2,
}
[CreateAssetMenu(menuName = "Rendering/PerspectiveGrid Setting Asset")]
public class CullParticleSetting : ScriptableObject
{
    [Range(0, 10)]
    public int m_DrawLayer = 1;
    [Range(32, 256)]
    public int m_PerspectiveGridDimX = 128;
    [Range(32, 256)]
    public int m_PerspectiveGridDimY = 128;

    public CullMode m_CullMode = CullMode.None;

    public void UpdateShaderProperty()
    {
        Shader.SetGlobalInt("_DrawLayer", m_DrawLayer);
        Shader.SetGlobalInt("_PerspectiveGridDimX", m_PerspectiveGridDimX);
        Shader.SetGlobalInt("_PerspectiveGridDimY", m_PerspectiveGridDimY);
    }
}