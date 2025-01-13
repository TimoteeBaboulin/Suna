using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInputData :IInputComponentData
{
    public float2 move;
    public InputEvent jump;
}
