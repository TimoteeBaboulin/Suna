using UnityEngine;
using UnityEngine.VFX;

public class WeaponVfxLink : MonoBehaviour
{
    [SerializeField] private VisualEffect _vfx;
    [SerializeField] private float _playSpeed = 1;

    private void Awake()
    {
        _vfx.playRate = _playSpeed;
    }

    public void Fire()
    {
        _vfx.Play();
        Debug.Log("Fire");
    }
}
