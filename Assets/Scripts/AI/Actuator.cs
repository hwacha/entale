using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

using static Expression;
using static SemanticType;

public class Actuator : MonoBehaviour {
    public Agent Agent;

    protected bool actionInProgress = false;

    public NavMeshAgent NavMeshAgent { protected set; get; }
    protected Queue<Expression> expressionsToSay;
    protected Queue<Vector3> destinations;

    void Start() {
        expressionsToSay = new Queue<Expression>();
        destinations = new Queue<Vector3>();
        NavMeshAgent = Agent.GetComponent<NavMeshAgent>();
    }

    void Update() {
        if (!actionInProgress && destinations.Count > 0) {
            actionInProgress = true;
            Walk(destinations.Dequeue());
        }
        if (!actionInProgress && expressionsToSay.Count > 0) {
            actionInProgress = true;
            StartCoroutine(Say(expressionsToSay.Dequeue(), 5));
        }
    }

    protected IEnumerator Walk(Vector3 destination) {
        NavMeshAgent.SetDestination(destination);
        while (Vector3.Distance(transform.position, destination) > NavMeshAgent.stoppingDistance + 1) {
            NavMeshAgent.SetDestination(destination);
            yield return new WaitForSeconds(0.5f);
        }
        actionInProgress = false;
    }

    protected IEnumerator Say(Expression e, float time) {
        var eContainer = ArgumentContainer.From(e);
        ArgumentContainer eContainerScript = eContainer.GetComponent<ArgumentContainer>();

        eContainerScript.GenerateVisual();

        var display = Agent.gameObject.transform.Find("Display");
        eContainer.transform.position = display.position;
        eContainer.transform.rotation = display.rotation;
        eContainer.transform.localScale *= 0.5f;
        eContainer.transform.SetParent(display);

        yield return new WaitForSeconds(time);
        Destroy(eContainer);
        actionInProgress = false;
    }

    // @Note this should be removed when the planner is better.
    public void RespondTo(Expression utterance, Expression speaker) {
        if (utterance.Type.Equals(ASSERTION)) {
            if (utterance.Head.Equals(ASSERT.Head)) {
                Agent.MentalState.ReceiveAssertion(utterance.GetArgAsExpression(0), speaker);
                expressionsToSay.Enqueue(YES);
            } else if (utterance.Head.Equals(DENY.Head)) {
                Agent.MentalState.ReceiveAssertion(
                    new Expression(NOT, utterance.GetArgAsExpression(0)), speaker);
                expressionsToSay.Enqueue(NO);
            }
            return;
        }

        if (utterance.Type.Equals(QUESTION)) {
            if (utterance.Head.Equals(ASK.Head)) {
                var positiveProofs = Agent.MentalState.GetProofs(utterance.GetArgAsExpression(0));
                var negativeProofs = Agent.MentalState.GetProofs(
                        new Expression(NOT, utterance.GetArgAsExpression(0)));

                if (positiveProofs.IsEmpty() && negativeProofs.IsEmpty()) {
                    expressionsToSay.Enqueue(NEGIGN);
                } else if (positiveProofs.IsEmpty()) {
                    expressionsToSay.Enqueue(NO);
                } else if (negativeProofs.IsEmpty()) {
                    expressionsToSay.Enqueue(YES);
                } else {
                    expressionsToSay.Enqueue(POSIGN);
                }
                return;
            }
        }

        if (utterance.Type.Equals(CONFORMITY_VALUE)) {
            if (utterance.Head.Equals(WOULD.Head)) {
                var content = utterance.GetArgAsExpression(0);
                var abilityProofs = Agent.MentalState.GetProofs(new Expression(IF, content, new Expression(DF, MAKE, content, SELF)));

                if (abilityProofs.IsEmpty()) {
                    expressionsToSay.Enqueue(REFUSE);
                } else {
                    expressionsToSay.Enqueue(ACCEPT);
                    Agent.MentalState.ReceiveRequest(content, speaker);
                }
            }
            return;
        }
    }


    // @Note we want this to be interruptible
    public void ExecutePlan() {
        List<Expression> plan = Agent.MentalState.DecideCurrentPlan();

        foreach (Expression action in plan) {
            if (!action.Head.Equals(WILL.Head)) {
                throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
            }

            // Debug.Log(action);
            // expressionsToSay.Enqueue(action);

            while (actionInProgress) {
                Thread.Sleep(1000);
            }

            var content = action.GetArgAsExpression(0);

            if (content.Equals(NEUTRAL)) {
                // Debug.Log("Busy doin' nothin'");
            }

            // at(self, X)
            else if (content.Head.Equals(AT.Head) && content.GetArgAsExpression(0).Equals(SELF)) {
                var destination = content.GetArgAsExpression(1);
                // assumption: if we find this in a plan,
                // then the location of X should be known.
                if (Agent.MentalState.Locations.ContainsKey(destination)) {
                    destinations.Enqueue(Agent.MentalState.Locations[destination]);
                } else {
                    break;
                }
            }

            else if (content.Head.Equals(INFORMED.Head)) {
                var message = new Expression(ASSERT, content.GetArgAsExpression(0));
                // Debug.Log("saying " + message);
                expressionsToSay.Enqueue(message);
            }

            while (actionInProgress) {
                Thread.Sleep(1000);
            }
        }
    }

}
