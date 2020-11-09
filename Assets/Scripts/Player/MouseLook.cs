using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    public float mouseSensitivity = 100f;
    public Transform playerBody;
    bool disabled = true;

    float xRotation = 0f;

    // Start is called before the first frame update
    void Start() {
        enable();
    }

    // Update is called once per frame
    void Update() {
        if (disabled) {
            return;
        }
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void disable() {
        disabled = true;
        Cursor.lockState = CursorLockMode.None;
    }


    public void enable() {
        disabled = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
