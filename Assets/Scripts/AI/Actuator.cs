﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using static Expression;

public class Actuator : MonoBehaviour {
    protected Agent Agent;
    public NavMeshAgent NavMeshAgent { protected set; get; }

    public Actuator(Agent agent) {
        Agent = agent;
        NavMeshAgent = Agent.GetComponent<NavMeshAgent>();
    }

    // @Note we want this to be interruptible
    public IEnumerator ExecutePlan() {
        while (true) {
            List<Expression> plan = Agent.MentalState.DecideCurrentPlan();
            foreach (Expression action in plan) {

                if (!action.Head.Equals(WILL.Head)) {
                    throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
                }

                var content = action.GetArgAsExpression(0);

                if (content.Equals(NEUTRAL)) {
                    Debug.Log("Busy doin' nothin'");
                }

                else if (content.Equals(new Expression(BLUE, SELF))) {
                    Agent.IsBlue = true;
                    Debug.Log("I blue myself.");
                }

                // at(self, that ~> #forest1)
                else if (content.Head.Equals(AT.Head)) {
                    if (content.GetArgAsExpression(0).Equals(SELF)) {
                        var destination = content.GetArg(1);
                        if (destination is Deictic) {
                            Deictic destinationD = (Deictic) destination;
                            NavMeshPath path = new NavMeshPath();

                            NavMeshAgent.CalculatePath(destinationD.Referent.transform.position, path);

                            if (path.status != NavMeshPathStatus.PathPartial) {
                                NavMeshAgent.SetPath(path);
                                while (NavMeshAgent.remainingDistance > 1) {
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                        }
                    }
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
