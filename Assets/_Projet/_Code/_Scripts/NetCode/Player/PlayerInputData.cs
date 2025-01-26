using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInputData :IInputComponentData
{
    [GhostField] public float2 move;
    [GhostField] public float2 look;
    [GhostField] public InputEvent jump;
    //public InputAction walk;
}
