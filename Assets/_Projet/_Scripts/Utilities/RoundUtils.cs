using UnityEngine;

public static class RoundUtils
{
    public static TeamSideType WinningTeamRound(ref RoundComponent roundComponent)
    {
        return roundComponent.corporationLossStreak == 0 ? TeamSideType.Corpo : TeamSideType.Natif;
    }
}
