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
    [Header("Connection Settings")]
    public bool isClientLocal = false;
    [Tooltip("IP to reach/to connect on")]
    public string IP = "51.210.222.138"; 
    public ushort Port = ClientServerBootstrap.AutoConnectPort;
}

public class ConnectionSettingsBaker : Baker<ConnectionSettings>
{
    public override void Bake(ConnectionSettings authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new ConnectionInfo {
            IP = authoring.IP, 
            Port = authoring.Port,
            IsClientLocal = authoring.isClientLocal
        });
    }
}
