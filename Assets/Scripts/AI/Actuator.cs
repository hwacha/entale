﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using static Expression;

public class Actuator : MonoBehaviour {
    public Agent Agent;
    public NavMeshAgent NavMeshAgent { protected set; get; }

    void Start() {
        NavMeshAgent = Agent.GetComponent<NavMeshAgent>();
    }

    protected IEnumerator Say(Expression e, float time) {
        GameObject eContainer = ArgumentContainer.From(e);
        ArgumentContainer eContainerScript = eContainer.GetComponent<ArgumentContainer>();

        eContainerScript.GenerateVisual();
        eContainer.transform.rotation = Agent.gameObject.transform.rotation;
        eContainer.transform.Rotate(0, 180, 0);
        eContainer.transform.position =
            Agent.gameObject.transform.position +
            2f * Vector3.up +
            0.75f * Vector3.forward;
        eContainer.transform.SetParent(Agent.gameObject.transform);

        yield return new WaitForSeconds(time);
        Destroy(eContainer);
        yield break;
        yield return null;
    }


    // @Note we want this to be interruptible
    public IEnumerator ExecutePlan() {
        while (true) {
            List<Expression> plan = new List<Expression>();
            var done = new Container<bool>(false);
            Agent.MentalState.StartCoroutine(Agent.MentalState.DecideCurrentPlan(plan, done));

            while (!done.Item) {
                yield return new WaitForSeconds(0.5f);
            }

            foreach (Expression action in plan) {
                if (!action.Head.Equals(WILL.Head)) {
                    throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
                }

                StartCoroutine(Say(action, 2));

                var content = action.GetArgAsExpression(0);

                if (content.Equals(NEUTRAL)) {
                    Debug.Log("Busy doin' nothin'");
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
                                NavMeshAgent.ResetPath();
                            }
                        }
                    }
                }

                var iTried = new Expression(TRIED, SELF, content);

                // we assert to the mental state that
                // we've tried to perform this action.
                Agent.MentalState.Assert(iTried);

                yield return new WaitForSeconds(2);
            }

            yield return null;
        }
    }

}
