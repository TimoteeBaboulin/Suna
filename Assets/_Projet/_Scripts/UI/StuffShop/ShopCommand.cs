using Unity.Collections;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public struct ShopCommand : IRpcCommand
{
    public FixedString32Bytes weaponData;
}