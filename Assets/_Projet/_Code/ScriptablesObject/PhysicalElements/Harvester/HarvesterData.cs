using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Harvester Data", menuName = "Stuff Data/Harvester Data")]
public class HarvesterData : ScriptableObject
{
    [Header("Stuff infos")]
    public GameObject viewPrefab;
    public Image UIImage;
    public StuffInventoryLocation location;
    public StuffType type;
    public TeamSideType side;
    public string entityName;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
    public uint killGain;
    public Vector3 _stuffLocalOffsetView; //temp

    [Header("Static Data")]
    public float defuseRange;
    public float pickupDistance;
}