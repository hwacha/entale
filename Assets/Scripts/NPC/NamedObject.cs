using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public class NamedObject : VisibleObject
{
    void Start()
    {
        string iName = gameObject.name.ToLower();
        if (iName.Equals("cylinder")) {
            iName = "player";
        } else if (iName.Equals("ethanbody")) {
            iName = transform.parent.gameObject.name.ToLower();
        }
        Characteristic = new Expression(new Name(INDIVIDUAL, iName));
    }

    override protected void OnSendPercept(MentalState m, Vector3 position) {
        m.AddNamedPercept(Characteristic, gameObject.transform.position);
    }
}
