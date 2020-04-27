using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Sensor : MonoBehaviour {
    public Agent Agent;

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
            var tree = GameObject.Find("tree");
            if (Vector3.Distance(tree.transform.position,
                Agent.gameObject.transform.position) < 2) {
                Agent.MentalState.StartCoroutine(Agent.MentalState.Assert(
                    new Expression(PERCEIVE, SELF,
                        new Expression(AT, SELF, new Deictic(THAT, tree)))));
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
