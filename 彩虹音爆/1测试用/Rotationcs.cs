using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotationcs : MonoBehaviour
{
    public Transform center;
    public bool rotation = false;
    public float upspeed = 0;
    public float rotate = 0;
    private Vector3 yuan;
    void Start()
    {
        yuan = transform.position;
    }

    void Update()
    {
        if (rotation)
        {
            transform.RotateAround(center.position, center.up, rotate * Time.deltaTime);
            transform.Translate(new Vector3(0, Time.deltaTime * upspeed, 0));
        }
        else
        {
            transform.position = yuan;
        }
    }
}
