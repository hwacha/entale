using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Agent : MonoBehaviour
{
    protected Sensor Sensor;
    public MentalState MentalState { protected set; get; }
    protected Actuator Actuator;

    public bool IsBlue = false; // test

    void Start()
    {
        // @Note: is the best way to customize this
        // class inheretance?
        MentalState = new MentalState(
            new Expression(BETTER, new Expression(BLUE, SELF), NEUTRAL),
            new Expression(ABLE, SELF, new Expression(BLUE, SELF))
        );

        Sensor = new Sensor(this);
        Actuator = new Actuator(this);

        StartCoroutine(Sensor.ReceiveStimulus());
        StartCoroutine(Actuator.ExecutePlan());
    }
}
