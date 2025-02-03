using System;
using UnityEngine;

public interface IRoundManager
{
    public static Action<int, int> OnRoundStart;
    public static Action OnCollectorPlanted;

    protected static float _currentTime;
    public static float CurrentTime => _currentTime;
}
