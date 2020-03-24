using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Highlight : MonoBehaviour
{   
    public float rayDistance = 3f;
    private bool pointingModeEnabled = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // space bar toggles pointing mode.
        if (Input.GetButtonDown("Jump"))
        {
            pointingModeEnabled = !pointingModeEnabled;
            if (pointingModeEnabled) {
                Debug.Log("Pointing enabled");
            } else {
                Debug.Log("Pointing disabled");
            }
        }

        if (pointingModeEnabled)
        {
            // Bit shift the index of the layer (8) to get a bit mask
            // Only objects tagged "Highlightable"
            int layerMask = 1 << 8;

            RaycastHit hit;
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * rayDistance, Color.yellow);
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, rayDistance, layerMask))
            {
                Color c = hit.transform.gameObject.tag == "Highlightable" ? Color.blue : Color.red;
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, c);

                
            }
        }
    }
}
