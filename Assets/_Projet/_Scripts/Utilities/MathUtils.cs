using NUnit.Framework.Constraints;
using Unity.Mathematics;
using UnityEngine;

public class MathUtils
{
    public static System.Object Swizzle(string mask, System.Object f2)
    {
        switch(mask.Length)
        {
            case 1:
                return Mask(mask[0], f2);
            case 2:
                return new float2(Mask(mask[0], f2), Mask(mask[1], f2));
            case 3:
                return new float3(Mask(mask[0], f2), Mask(mask[1], f2), Mask(mask[2], f2));
            case 4:
                return new float4(Mask(mask[0], f2), Mask(mask[1], f2), Mask(mask[2], f2), Mask(mask[3], f2));
            default:
                throw new System.Exception("Mask should be 1, 2, 3 or 4 characters long");
        }
    }

    public static float Mask(char mask, System.Object f)
    {
        switch (mask)
        {
            case 'x':
            case 'r':
            case 's':
            case '1':
                return f is float2 ? ((float2)f).x : (f is float3) ? ((float3)f).x : ((float4)f).x;
            case 'y':
            case 'g':
            case 't':
            case '2':
                return f is float2 ? ((float2)f).y : (f is float3) ? ((float3)f).y : ((float4)f).y;
            case 'z':
            case 'b':
            case 'p':
            case '3':
                return (f is float3) ? ((float3)f).z : ((float4)f).z;
            case 'w':
            case 'a':
            case 'q':
            case '4':
                return ((float4)f).w;
            default:
                throw new System.Exception("Mask should be x, y, z or w");
        }
    }
}
