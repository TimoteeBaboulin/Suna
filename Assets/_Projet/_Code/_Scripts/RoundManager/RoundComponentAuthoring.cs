using Unity.Entities;
using UnityEngine;

public class RoundComponentAuthoring : MonoBehaviour
{
    [SerializeField] private float[] _phaseTimes;

    public class RoundComponentBaker : Baker<RoundComponentAuthoring>
    {
        public override void Bake(RoundComponentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new RoundComponent());
            var buffer = AddBuffer<PhaseTimesBuffer>(entity);
            for (int i = 0; i < 4; i++)
            {
                buffer.Add(new PhaseTimesBuffer() { Value = authoring._phaseTimes[i] });
            }
        }
    }
}
