using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayCam : MonoBehaviour
{
    [SerializeField]
    float _speed = 0.1f;
    [SerializeField]
    float _radius = 0.5f;
    [SerializeField]
    float _height;
    [SerializeField]
    Transform _targetTransform;

    float _accum;

    // Start is called before the first frame update
    void Start()
    {
        _accum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        _accum += Time.deltaTime * _speed;
        if(_accum > Mathf.PI)
        {
            _accum -= Mathf.PI*2;
        }
        transform.position = _targetTransform.position + new Vector3(Mathf.Cos(_accum) * _radius, _height, Mathf.Sin(_accum) * _radius);
        transform.LookAt(_targetTransform);
    }
}
