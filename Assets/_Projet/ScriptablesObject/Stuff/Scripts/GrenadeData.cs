using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeData", menuName = "Stuff Data/Grenade")]
public class GrenadeData : ScriptableObject
{
    public GameObject viewPrefab;
    public Texture2D UIImage;
    public StuffInventoryLocation location;
    public StuffType type;
    public TeamSideType side;
    public string entityName;
    public int price;
    public uint killGain = 300;
    public Vector3 _stuffLocalOffsetView; //temp
}
