using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "HandleData", menuName = "Scriptable Objects/HandleData")]
public class HandleData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public StuffType type;
    public TeamSideType side;

    [Header("Health")]

    [Tooltip("percent"), Range(0, 1)]
    public float reloadSpeedReduce;

    [Tooltip("percent"), Range(0, 1)]
    public float horizontalRecoil;

    [Tooltip("percent"), Range(0, 1)]
    public float verticalRecoil;

    [Tooltip("percent"), Range(0, 1)]
    public float ergonomicReduce;

    [Tooltip("scalar")]
    public float sprayCoefReduce;
}
