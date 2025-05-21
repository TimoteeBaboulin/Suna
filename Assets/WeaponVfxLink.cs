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
        muzzle.transform.localPosition = Vector3.zero;
        DecalTimer timer = muzzle.AddComponent<DecalTimer>();
        timer._timer = 1.0f;

        muzzle.Play();
    }
}
