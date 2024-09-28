using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public Camera Camera;
    public float lmd = 1;
    private float Cx;
    private float Cy;
    private CharacterController Player;
    public float speed;
    public float gaodu;
    private float v;

    private void Start()
    {
        Player = GetComponent<CharacterController>();
    }

    void Update()
    {
        Cx += Input.GetAxis("Mouse X");
        Cy -= Input.GetAxis("Mouse Y");
        transform.localRotation = Quaternion.Euler(Cy * lmd, Cx * lmd, 0);

        bool dimian = Player.isGrounded;
        if (dimian)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                v = Mathf.Sqrt(-(gaodu * -9.8f));
            }
            else
            {
                v = 0;
            }
        }
        v += -9.8f * Time.deltaTime;
        Player.Move(new Vector3(Input.GetAxis("Horizontal") * speed, v, Input.GetAxis("Vertical") * speed) * Time.deltaTime);
    }
}
