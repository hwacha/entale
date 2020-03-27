using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Highlighter : MonoBehaviour {
    public float rayDistance = 5f;
    public Image highlightModePanel;
    public Image hightlightPointPanel;
    public List<Color> highlightColors = new List<Color>();

    private int currHighlightIndex;
    private int highlightIndexCount;
    private List<GameObject>[] highlightedGameObjects;

    void Awake() {
        highlightIndexCount = highlightColors.Count;
        highlightedGameObjects = new List<GameObject>[highlightIndexCount];
        for (int i = 0; i < highlightedGameObjects.Length; i++) {
            highlightedGameObjects[i] = new List<GameObject>();
        }
        currHighlightIndex = highlightIndexCount;
    }

    void Start() {}

    void Update() {
        // Space bar toggles hightlighting mode.
        if (Input.GetButtonDown("Jump")) {
            currHighlightIndex = (currHighlightIndex + 1) % (highlightIndexCount + 1);
            if (currHighlightIndex == highlightIndexCount) {
                highlightModePanel.color = Color.white;
                hightlightPointPanel.color = Color.white;
            } else {
                highlightModePanel.color = highlightColors[currHighlightIndex];
            }
        }

        // Consider an index larger than the max neutral non-highlight mode
        if (currHighlightIndex != highlightIndexCount) {
            Highlight(currHighlightIndex);
        }
    }

    void Highlight(int highlightIndex) {
        // Only collide with objects in layer/tagged 8 ("Highlightable")
        int layerMask = 1 << 8;

        // Draw debug raycast
        Debug.DrawRay(
            transform.position,
            transform.TransformDirection(Vector3.forward) * rayDistance,
            Color.white);

        // Raycast
        RaycastHit hit;
        bool collided = Physics.Raycast(
            transform.position,
            transform.TransformDirection(Vector3.forward),
            out hit,
            rayDistance,
            layerMask);

        // Indicate that this object can be highlighted
        Color highlightColor = highlightColors[highlightIndex];
        hightlightPointPanel.color = collided ? highlightColor : Color.white;

        if (collided) {
            // Draw debug hit raycast
            Debug.DrawRay(
                transform.position,
                transform.TransformDirection(Vector3.forward) * hit.distance,
                hit.transform.gameObject.tag == "Highlightable" ? highlightColor : Color.black
            );

            // Highlight hit object on mouse click
            if (Input.GetMouseButtonDown(0)) {
                HighlightGameObject(hit.transform.gameObject, highlightIndex);
            }
        }
    }

    void HighlightGameObject(GameObject gameObject, int highlightIndex) {
        // Remove all other highlights of this object
        // This prevents an object from being highlighted by more than one color
        for (int i = 0; i < highlightedGameObjects.Length; i++) {
            highlightedGameObjects[i].Remove(gameObject);
        }

        // Unhighlight all other game objects in this highlight index
        foreach (GameObject o in highlightedGameObjects[highlightIndex]) {
            o.GetComponent<Renderer>().material.SetInt("_IsHighlighted", 0);
        }
        highlightedGameObjects[highlightIndex].Clear();
        highlightedGameObjects[highlightIndex].Add(gameObject);

        // Highlight this game object
        Color highlightColor = highlightColors[highlightIndex];
        Renderer gameObjectRenderer = gameObject.GetComponent<Renderer>();
        gameObjectRenderer.material.SetInt("_IsHighlighted", 1);
        gameObjectRenderer.material.SetColor("_HighlightColor", highlightColor);
    }
}