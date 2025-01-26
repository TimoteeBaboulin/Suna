using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted,OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct PlayerInput :IInputComponentData
{
    [GhostField] public float2 move;
    [GhostField] public float2 look;
    [GhostField] public InputEvent jump;
    [GhostField] public InputEvent walkStarted;
    [GhostField] public InputEvent walkCanceled;
}
