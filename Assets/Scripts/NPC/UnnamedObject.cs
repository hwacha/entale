using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public class UnnamedObject : VisibleObject
{
    void Start()
    {
        string characteristicName = gameObject.name.ToLower();
        Characteristic = new Expression(new Name(PREDICATE, characteristicName));
    }

    override protected void OnSendPercept(Agent agent, Vector3 position) {
        agent.EnqueueAction(() => agent.MentalState.ConstructPercept(Characteristic, position));
    }
}
