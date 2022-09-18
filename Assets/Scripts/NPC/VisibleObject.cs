using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public abstract class VisibleObject : MonoBehaviour
{
    protected Expression Characteristic;

    static int PERIOD_IN_FRAMES = 60;

    protected int currentFrame = 0;

    protected abstract void OnSendPercept(Agent agent, Vector3 position);

    void Update() {
        currentFrame++;
        currentFrame %= PERIOD_IN_FRAMES;
    }

    void OnWillRenderObject() {
        if (currentFrame > 0) {
            return;
        }

        var agentRef = Camera.current.GetComponent<ReferenceToAgent>();
        if (agentRef != null) {
            // check if this object is actually visible
            int layerMask = 1 << 11;

            var seerPosition = agentRef.transform.position;
            var objPosition  = transform.position;
            var difference = objPosition - seerPosition;

            // for now, just assume any NPC is in frame
            if (gameObject.name.Equals("EthanBody")) {
                // Debug.Log(gameObject);
                OnSendPercept(agentRef.Agent, gameObject.transform.position);
                Debug.DrawRay(seerPosition, difference, Color.green);
                return;
            }

            Debug.DrawRay(seerPosition, difference, Color.black);

            RaycastHit hit;
            Physics.Raycast(seerPosition, difference, out hit, Mathf.Infinity, layerMask);
            
            // @Bug the raycasting is yielding false negatives
            if (hit.transform != null) {
                bool expectedObject = hit.transform.gameObject.Equals(gameObject);
                expectedObject = expectedObject || hit.transform.gameObject.name.Equals("Player") && gameObject.name.Equals("Cylinder");

                if (expectedObject) {
                    Debug.DrawRay(seerPosition, difference, Color.blue);
                    OnSendPercept(agentRef.Agent, gameObject.transform.position);
                } else {
                    // Debug.Log("hit " + hit.transform.gameObject.name + ", expected " + gameObject.name);
                    Debug.DrawRay(seerPosition, difference, Color.red);
                }
            } else {
                Debug.DrawRay(seerPosition, difference, Color.yellow);    
            }
        }
    }
}
