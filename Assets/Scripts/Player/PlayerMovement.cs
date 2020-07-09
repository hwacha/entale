using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    public CharacterController controller;

    public float jumpSpeed = 20;
    public float speed = 8;
    public float g = -50;

    private float dy = 0;

    void Update() {
        float dx = Input.GetAxis("Horizontal");
        float dz = Input.GetAxis("Vertical");

        if (controller.isGrounded) {
            if (Input.GetButton("Jump")) {
                dy = jumpSpeed;
            } else {
                dy = 0;
            }
        } else {
            dy += g * Time.deltaTime;
        }

        Vector3 move = Vector3.Normalize(transform.right * dx + transform.forward * dz) * speed;
        move.y = dy;

        controller.Move(move * Time.deltaTime);
    }
}
