using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public abstract class Agent : MonoBehaviour
{
    protected Sensor Sensor;
    public MentalState MentalState;
    protected Actuator Actuator;

    protected virtual void Start()
    {
        // @Note: is the best way to customize this
        // class inheretance?

        Sensor = new Sensor(this);
        Actuator = new Actuator(this);

        StartCoroutine(Sensor.ReceiveStimulus());
        StartCoroutine(Actuator.ExecutePlan());
    }
}
