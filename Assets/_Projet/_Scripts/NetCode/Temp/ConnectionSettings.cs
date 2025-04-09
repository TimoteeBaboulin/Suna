using GameNetwork.Utils;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct ConnectionInfo : IComponentData
{
    public FixedString64Bytes IP;
    public ushort Port;
    public bool IsClientLocal;
}
public class ConnectionSettings : MonoBehaviour
{
    public enum SceneIDToLoad
    {
        MultiplayerTest = 1,
        GameplayTestScene = 2,
    }

    [Serializable]
    public enum PortToUse
    {
        Production = 7979,
        Debug = 59692,
        Adrien = 53959,
        Aurelien = 52406,
        Leonnel = 53970,
        Theo = 54064,
        Thomas = 59557,
        Timotee = 59573,
    }


    [Header("Connection Settings")]
    public bool isClientLocal = false;
    [Tooltip("IP to reach/to connect on")]
    public string IP = "51.210.222.138"; 
    public PortToUse Port = PortToUse.Production;
    public SceneIDToLoad sceneToLoad = SceneIDToLoad.MultiplayerTest;
}

public class ConnectionSettingsBaker : Baker<ConnectionSettings>
{
    public override void Bake(ConnectionSettings authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new ConnectionInfo {
            IP = authoring.IP, 
            Port = (ushort)authoring.Port,
            IsClientLocal = authoring.isClientLocal
        });
    }
}

[CreateAssetMenu(fileName = "ConnectionConfig", menuName = "Network/Connection Config")]
public class ConnectionConfig : ScriptableObject
{
    public ConnectionSettings.PortToUse portToUse = ConnectionSettings.PortToUse.Production;
    public string IP = "51.210.222.138";
    public bool isClientLocal = false;
}
