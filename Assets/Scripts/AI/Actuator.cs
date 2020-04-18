using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Actuator : MonoBehaviour {
    protected Agent Agent;

    public Actuator(Agent agent) {
        Agent = agent;
    }

    // @Note we want this to be interruptible
    public IEnumerator ExecutePlan() {
        while (true) {
            List<Expression> plan = Agent.MentalState.DecideCurrentPlan();
            foreach (Expression action in plan) {

                if (!action.Head.Equals(WILL.Head)) {
                    throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
                }

                var content = action.GetArg(0);

                if (content.Equals(NEUTRAL)) {
                    Debug.Log("Busy doin' nothin'");
                }

                else if (content.Equals(new Expression(BLUE, SELF))) {
                    Agent.IsBlue = true;
                    Debug.Log("I blue myself.");
                }

                var iTried = new Expression(TRIED, SELF, content);
                Debug.Log(iTried);

                // we assert to the mental state that
                // we've tried to perform this action.
                Agent.MentalState.Assert(iTried);

                yield return new WaitForSeconds(2);
            }

            yield return null;
        }
    }
}
