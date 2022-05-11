using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Bounding Data Asset")]
public class Bounding : ScriptableObject
{
    public Vector3 MinPos;
    public Vector3 MaxPos;
}