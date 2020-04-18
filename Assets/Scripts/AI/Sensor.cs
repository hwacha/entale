using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Sensor : MonoBehaviour {
    protected Agent Agent;

    public Sensor(Agent agent) {
        Agent = agent;
    }

    // @Note for separate sense modalities,
    // we'd want this to become an abstract class,
    // making some sort of abstract method like
    // CaptureModality() or something which
    // does the processing for a specific sense.
    // 
    // Especially important may be so-called
    // "proprioception" or self-perception of movement,
    // do be directly linked to the actuator.
    // 
    // Right now, assume it's a vision module.
    public IEnumerator ReceiveStimulus() {
        while (true) {
            // right now, simply receive the attributes of
            // the agent. This is just a test!
            if (Agent.IsBlue) {
                UnityEngine.Debug.Log("My proprioceptive senses are tingling... I truly am blue!");
                Agent.MentalState.Assert(new Expression(PERCEIVE, SELF, new Expression(BLUE, SELF)));
            } else {
                // @Note: this should be uncommented once Assert() is working better.
                Agent.MentalState.Assert(new Expression(PERCEIVE, SELF, new Expression(NOT, new Expression(BLUE, SELF))));
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
