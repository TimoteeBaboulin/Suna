using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

using RaycastHit = Unity.Physics.RaycastHit;

public class WeaponUtilities 
{
    public static RaycastHit GetClosestHit(NativeList<RaycastHit> hits, in Entity shooter, in float3 startPosition)
    {
        //Raycast récupére les hit dans le mauvais ordre, il faut les triers en fonction de la distance
        RaycastHit closestHit = hits[0];
        float closestDist = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.Entity == shooter) continue;

            float currentDist = math.distancesq(startPosition, hit.Position);

            if (currentDist < closestDist)
            {
                closestHit = hit;
                closestDist = currentDist;
            }
        }

        return closestHit;
    }
}
