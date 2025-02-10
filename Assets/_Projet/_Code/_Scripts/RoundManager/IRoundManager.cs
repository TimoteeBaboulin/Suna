using System;
using Unity.Entities;
using UnityEngine;

public interface IRoundManager
{
    public static Action<int, int> OnRoundStart;
    public static Action OnCollectorPlanted;

    protected static float _currentTime;
    public static float CurrentTime => _currentTime;
}

public partial struct ScoreChangedComponent : IComponentData
{

}

public partial struct CollectorPlantedComponent : IComponentData
{

}