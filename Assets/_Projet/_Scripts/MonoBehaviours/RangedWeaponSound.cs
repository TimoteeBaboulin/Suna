using Unity.Entities;
using UnityEngine;

#if! UNITY_SERVER
public class RangedWeaponSound : MonoBehaviour
{
    [SerializeField] AK.Wwise.Event shoot;
    [SerializeField] AK.Wwise.Event reload;

    public void Shoot()
    {
        shoot.Post(gameObject);
    }

    public void Reload()
    {
        reload.Post(gameObject);
    }
}
#endif
