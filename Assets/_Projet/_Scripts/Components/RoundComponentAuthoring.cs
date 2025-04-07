using Unity.Entities;
using UnityEngine;

public class RoundComponentAuthoring : MonoBehaviour
{
    [SerializeField] private float[] _phaseTimes;

    [SerializeField] private int _defaultCredits;
    [SerializeField] private int _victoryCredits;
    [SerializeField] private int _lossCredits;
    [SerializeField] private int _lossStreakBonus;
    [SerializeField] private int _maxStreakCount;

    public class RoundComponentBaker : Baker<RoundComponentAuthoring>
    {
        public override void Bake(RoundComponentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new RoundComponent()
            {
                defaultCredits = authoring._defaultCredits,
                victoryCredits = authoring._victoryCredits,
                lossCredits = authoring._lossCredits,
                lossStreakBonus = authoring._lossStreakBonus,
                maxStreakCount = authoring._maxStreakCount
            });

            AddComponent<PlayerCounts>(entity);
            var buffer = AddBuffer<PhaseTimesBuffer>(entity);
            for (int i = 0; i < 4; i++)
            {
                buffer.Add(new PhaseTimesBuffer() { Value = authoring._phaseTimes[i] });
            }
        }
    }
}
