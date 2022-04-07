using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallCellGridTest : MonoBehaviour
{
    public Texture m_Terrian;
    public Vector3 m_Min;
    public Vector2Int m_ResolutionXZ;
    public int m_RegularCellCount;
    public float m_Celllength;
    public float m_SeaLevel;
    public int m_MaxParticleCount;
    public float m_TimeStep;

    public bool ShowDebugInfo = false;

    private TallCellGrid m_TallCellGrid;

    void Start()
    {
        m_TallCellGrid = new TallCellGrid(m_Terrian, m_ResolutionXZ, m_RegularCellCount, m_Min, m_Celllength, m_SeaLevel, m_MaxParticleCount);
    }

    // Update is called once per frame
    void Update()
    {
        m_TallCellGrid.Step(m_TimeStep);
    }

    private void OnDrawGizmos()
    {
        if(m_TallCellGrid != null && ShowDebugInfo)
        {
            TallCellGridLayer FineLevel = m_TallCellGrid.TallCellGridLayers[0];
            Texture2D TerrianHeight = new Texture2D(FineLevel.ResolutionXZ.x, FineLevel.ResolutionXZ.y, TextureFormat.RFloat, false);
            Texture2D TallCellHeight = new Texture2D(FineLevel.ResolutionXZ.x, FineLevel.ResolutionXZ.y, TextureFormat.RFloat, false);

            RenderTexture Temp = RenderTexture.active;
            RenderTexture.active = FineLevel.TerrrianHeight;
            TerrianHeight.ReadPixels(new Rect(0, 0, FineLevel.TerrrianHeight.width, FineLevel.TerrrianHeight.height), 0, 0);
            RenderTexture.active = FineLevel.TallCellHeight;
            TallCellHeight.ReadPixels(new Rect(0, 0, FineLevel.TallCellHeight.width, FineLevel.TallCellHeight.height), 0, 0);
            RenderTexture.active = Temp;

            for (int i = 0; i < FineLevel.ResolutionXZ.x; i++)
            {
                for (int j = 0; j < FineLevel.ResolutionXZ.y; j++)
                {
                    Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
                    float CurrTerrianHeight = TerrianHeight.GetPixel(i, j).r;
                    Vector3 TerrianCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTerrianHeight * 0.5f, j * m_Celllength);
                    Gizmos.DrawWireCube(TerrianCellCenter, new Vector3(m_Celllength, CurrTerrianHeight, m_Celllength));

                    Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
                    float CurrTallCellHeight = TallCellHeight.GetPixel(i, j).r;
                    Vector3 TallCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTallCellHeight * 0.5f + CurrTerrianHeight, j * m_Celllength);
                    Gizmos.DrawWireCube(TallCellCenter, new Vector3(m_Celllength, CurrTallCellHeight, m_Celllength));

                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f);
                    Vector3 RegularCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTallCellHeight + CurrTerrianHeight + 0.5f * m_Celllength, j * m_Celllength);
                    for (int k = 0; k < m_RegularCellCount; k++)
                    {
                        Gizmos.DrawWireCube(RegularCellCenter, new Vector3(m_Celllength, m_Celllength, m_Celllength));
                        RegularCellCenter.y += m_Celllength;
                    }
                }
            }
        }
    }
}
