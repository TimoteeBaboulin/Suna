using Unity.Entities;

public struct RoundComponent : IComponentData
{
    public float timer;
    public RoundPhase currentPhase;

    public int currentRound;

    public int nativeScore;
    public int corporationScore;

    public int nativeLossStreak;
    public int corporationLossStreak;

    public int defaultCredits;
    public int victoryCredits;
    public int lossCredits;
    public int lossStreakBonus;
    public int maxStreakCount;
}

public struct RoundCollectorPlantedComponent : IComponentData
{

}
