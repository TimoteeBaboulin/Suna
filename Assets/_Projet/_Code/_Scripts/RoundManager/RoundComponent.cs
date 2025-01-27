using Unity.Entities;

public struct RoundComponent : IComponentData
{
    public float timer;
    public RoundPhase currentPhase;

    public int currentRound;
}

public struct RoundCollectorPlantedComponent : IComponentData
{

}
