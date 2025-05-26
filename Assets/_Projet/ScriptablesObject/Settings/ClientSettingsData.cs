using UnityEngine;

[CreateAssetMenu(fileName = "ClientSettingsData", menuName = "Scriptable Objects/ClientSettingsData")]
public class ClientSettingsData : ScriptableObject
{
    [Header("Controls")]
    public float Sensivity;
    public float Volume;
    public string Pseudo;
}
