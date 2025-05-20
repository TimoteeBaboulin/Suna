using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class HarvesterVfxLink : MonoBehaviour
{
    [SerializeField] private VisualEffect[] _vfx;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Transform _jTrans;

    private float3 _pos;
    private bool _playing = false;

    public void Play(float3 pos)
    {
        foreach (var vfx in _vfx)
        {
            vfx.Play();
            vfx.SetBool("IsAlive", true);
        }

        foreach (var particle in _particles)
        {
            particle.Play();
        }


        _playing = true;
        _jTrans.rotation = Quaternion.identity;
        _jTrans.position = pos;
        _pos = pos;
    }

    private void Update()
    {
        if (_playing)
        {
            _jTrans.position = _pos;
            _jTrans.rotation = Quaternion.identity;
        }
    }

    public void Stop()
    {
        foreach (var vfx in _vfx)
        {
            vfx.Stop();
            vfx.SetBool("IsAlive", false);
        }

        foreach (var particle in _particles)
        {
            particle.Stop();
        }

        _playing = false;
    }
}
