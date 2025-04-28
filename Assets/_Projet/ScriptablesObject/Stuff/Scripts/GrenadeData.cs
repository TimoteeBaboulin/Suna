using UnityEngine;

public enum GrenadeTriggerType
{
    Timer,
    Impact,
    Still,
    Bounce,
    Proximity,
}

public enum GrenadeType
{
    Frag,
    Flashbang,
}

[CreateAssetMenu(fileName = "GrenadeData", menuName = "Stuff Data/Grenade")]
public class GrenadeData : ScriptableObject
{
    public GameObject dropedEntityPrefab;
    public GameObject grenadeThrownPrefab;
    public GameObject viewPrefab;
    public Texture2D UIImage;
    public StuffSlot location;
    public StuffType type;
    public GrenadeType grenadeType;
    public TeamSideType side;
    public string entityName;
    public int price;
    public uint killGain = 300;
    public Vector3 _stuffLocalOffsetView; //temp

    public float cookingTime = 0.3f;
    public float impactRadius = 5f;
    public float damageInflicted = 80f;

    public GrenadeTriggerType triggerType = GrenadeTriggerType.Timer;
    // For the timer trigger
    public float timerTriggerDelay = 1.8f;

    // For the impact trigger
    public float maxImpactAngle = 45f;

    // For the still trigger
    public float stillTriggerDelay = .8f;

    // For the bounce trigger
    public uint bounceTriggerCount = 3;

    // For the proximity trigger
    public float proximityTriggerDistance = 5f;

}
