using UnityEngine;
using UnityEngine.VFX;

public class HarvesterVfxLink : MonoBehaviour
{
    [SerializeField] private VisualEffect _vfx;

    public void Play()
    {
        _vfx.Play();
        _vfx.SetBool("IsAlive", true);
    }

    public void Stop()
    {
        _vfx.Stop();
        _vfx.SetBool("IsAlive", false);
    }
}
