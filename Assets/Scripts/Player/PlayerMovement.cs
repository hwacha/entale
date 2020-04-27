using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    public CharacterController controller;

    public float speed = 12f;

    void Update() {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = Vector3.Normalize(transform.right * x + transform.forward * z);
        controller.Move(move * speed * Time.deltaTime);
    }
}
