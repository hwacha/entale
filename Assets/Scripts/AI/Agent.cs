using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public abstract class Agent : MonoBehaviour
{
    public MentalState MentalState;
    public Actuator Actuator;

    protected virtual void Start()
    {
        // @Note: is the best way to customize this
        // class inheretance?
        StartCoroutine(Actuator.ExecutePlan());
    }
}
