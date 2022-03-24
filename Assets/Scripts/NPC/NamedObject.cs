using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public class NamedObject : MonoBehaviour
{
    protected Expression Name;

    void Start()
    {
        string iName = gameObject.name.ToLower();
        if (iName.Equals("cylinder")) {
            iName = "player";
        } else if (iName.Equals("ethanbody")) {
            iName = transform.parent.gameObject.name.ToLower();
        }
        Name = new Expression(new Name(INDIVIDUAL, iName));
    }

    void OnWillRenderObject() {
        var mentalStateRef = Camera.current.GetComponent<ReferenceToMentalState>();
        if (mentalStateRef != null) {
            // check if this object is actually visible
            int layerMask = 1 << 11;

            var seerPosition = mentalStateRef.transform.position;
            var objPosition  = transform.position;

            bool collided = Physics.Raycast(seerPosition, objPosition - seerPosition, Mathf.Infinity, layerMask);

            // @Bug the raycasting is yielding false negatives
            if (collided || true) {
                mentalStateRef.MentalState.AddNamedPercept(Name, gameObject.transform.position);
            }
        }
    }
}
