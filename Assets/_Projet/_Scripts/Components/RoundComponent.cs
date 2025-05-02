using Unity.Entities;
using Unity.NetCode;

public struct RoundComponent : IComponentData
{
    public float timer;
    public RoundPhase currentPhase;

    public int currentRound;
    public int maxRounds;

    public TeamSideType winners;

    public int nativeScore;
    public int corporationScore;

    public int nativeLossStreak;
    public int corporationLossStreak;

    public int defaultCredits;
    public int victoryCredits;
    public int lossCredits;
    public int lossStreakBonus;
    public int maxStreakCount;

    public bool gameWon;
    public bool roundSystemActive;
}

public struct PlayerAliveCounts : IComponentData
{
    public int nativePlayersAlive;
    public int corpoPlayersAlive;
}

public struct RoundCollectorPlantedComponent : IComponentData
{

}

public struct GameOverRpcCommand : IRpcCommand
{
    public TeamSideType winners;
}

public struct DeactivateSpawnBarriersCommand : IRpcCommand
{
    public bool value;
}
