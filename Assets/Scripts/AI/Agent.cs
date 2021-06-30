using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public abstract class Agent : MonoBehaviour
{
    public Sensor Sensor;
    public MentalState MentalState;
    public Actuator Actuator;

    protected virtual void Start()
    {
        // @Note: is the best way to customize this
        // class inheretance?
        StartCoroutine(Sensor.ReceiveStimulus());
        StartCoroutine(Actuator.ExecutePlan());
    }

    protected Expression Tense(Expression e) {
        Debug.Assert(e.Type.Equals(SemanticType.TRUTH_VALUE));
        return new Expression(WHEN, e, new Expression(new Parameter(SemanticType.TIME, 1)));
    }

}
