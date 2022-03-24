using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public class TestCameraVision : MonoBehaviour
{
    protected Expression Characteristic;

    void Start()
    {
        string characteristicName = gameObject.name.ToLower();
        if (characteristicName.Equals("cylinder")) {
            characteristicName = "player";
        }
        Characteristic = new Expression(new Name(PREDICATE, characteristicName));
    }

    void OnWillRenderObject() {
        var mentalStateRef = Camera.current.GetComponent<ReferenceToMentalState>();
        if (mentalStateRef != null) {
            // check if this object is actually visible
            int layerMask = 1 << 11;

            var seerPosition = mentalStateRef.transform.position;
            var objPosition  = transform.position;

            bool collided = Physics.Raycast(seerPosition, objPosition - seerPosition, Mathf.Infinity, layerMask);

            if (collided) {
                mentalStateRef.MentalState.ConstructPercept(Characteristic, gameObject.transform.position);
            }
        }
    }
}
