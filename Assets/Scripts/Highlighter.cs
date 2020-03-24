using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Highlighter : MonoBehaviour {
    public float rayDistance = 3f;
    private bool isHighlighting = false;

    void Start() {

    }

    void Update() {
        // Space bar toggles hightlighting mode.
        if (Input.GetButtonDown("Jump")) {
            isHighlighting = !isHighlighting;
        }

        if (isHighlighting) {
            // Only collide with objects in layer/tagged 8 ("Highlightable")
            int layerMask = 1 << 8;

            RaycastHit hit;

            // Draw debug raycast
            Debug.DrawRay(
                transform.position,
                transform.TransformDirection(Vector3.forward) * rayDistance,
                Color.yellow);

            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, rayDistance, layerMask)) {
                // Draw debug hit raycast
                Color c = hit.transform.gameObject.tag == "Highlightable" ? Color.blue : Color.red;
                Debug.DrawRay(
                    transform.position,
                    transform.TransformDirection(Vector3.forward) * hit.distance,
                    c
                );

                // Get hit game object
                Renderer gameObjectRenderer = hit.transform.gameObject.GetComponent<Renderer>();
                if (gameObjectRenderer != null) {
                    gameObjectRenderer.material.SetInt("_IsHighlighted", 1);
                    gameObjectRenderer.material.SetColor("_HighlightColor", Color.red);
                } else {
                    Debug.Log("Object with highlightable layer doesn't have renderer");
                }
            }
        }
    }
}
