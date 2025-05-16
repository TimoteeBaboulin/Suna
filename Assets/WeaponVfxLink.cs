using Unity.VisualScripting;
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
        VisualEffect muzzle = Instantiate(_vfx, _vfx.transform);
        DecalTimer timer = muzzle.AddComponent<DecalTimer>();
        timer._timer = 0.1f;

        muzzle.Play();
        //_vfx.Play();
        Debug.Log("Fire");
    }
}
