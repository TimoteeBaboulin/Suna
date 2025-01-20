using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(4)]
public struct PhaseTimesBuffer : IBufferElementData
{
    public static implicit operator float(PhaseTimesBuffer e) { return e.Value; }
    public static implicit operator PhaseTimesBuffer(int e) { return new PhaseTimesBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public float Value;
}
