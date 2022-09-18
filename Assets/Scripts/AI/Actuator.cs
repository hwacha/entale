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

    protected Queue<Action> todo;

    void Start() {
        todo = new Queue<Action>();
        NavMeshAgent = Agent.GetComponent<NavMeshAgent>();
    }

    void Update() {
        if (!actionInProgress && todo.Count > 0) {
            actionInProgress = true;
            todo.Dequeue()();
        }
    }

    public bool IsBusy() {
        return todo.Count > 0;
    }

    protected IEnumerator WalkTo(Vector3 destination) {
        NavMeshAgent.SetDestination(destination);
        while (Vector3.Distance(transform.position, destination) > NavMeshAgent.stoppingDistance + 1) {
            NavMeshAgent.SetDestination(destination);
            yield return new WaitForSeconds(0.2f);
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
                todo.Enqueue(() => StartCoroutine(Say(YES, 5000)));
            } else if (utterance.Head.Equals(DENY.Head)) {
                Agent.MentalState.ReceiveAssertion(
                    new Expression(NOT, utterance.GetArgAsExpression(0)), speaker);
                todo.Enqueue(() => StartCoroutine(Say(NO, 5000)));
            }
            return;
        }

        if (utterance.Type.Equals(QUESTION)) {
            if (utterance.Head.Equals(ASK.Head)) {
                var positiveProofs = Agent.MentalState.GetProofs(utterance.GetArgAsExpression(0));
                var negativeProofs = Agent.MentalState.GetProofs(
                        new Expression(NOT, utterance.GetArgAsExpression(0)));

                if (positiveProofs.IsEmpty() && negativeProofs.IsEmpty()) {
                    todo.Enqueue(() => StartCoroutine(Say(NEGIGN, 5000)));
                } else if (positiveProofs.IsEmpty()) {
                    todo.Enqueue(() => StartCoroutine(Say(NO, 5000)));
                } else if (negativeProofs.IsEmpty()) {
                    todo.Enqueue(() => StartCoroutine(Say(YES, 5000)));
                } else {
                    todo.Enqueue(() => StartCoroutine(Say(POSIGN, 5000)));
                }
                return;
            }
        }

        if (utterance.Type.Equals(CONFORMITY_VALUE)) {
            if (utterance.Head.Equals(WOULD.Head)) {
                var content = utterance.GetArgAsExpression(0);
                var abilityProofs = Agent.MentalState.GetProofs(new Expression(IF, content, new Expression(DF, MAKE, content, SELF)));

                if (abilityProofs.IsEmpty()) {
                    todo.Enqueue(() => StartCoroutine(Say(REFUSE, 5000)));
                } else {
                    todo.Enqueue(() => StartCoroutine(Say(ACCEPT, 5000)));
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
            // todo.Enqueue(() => StartCoroutine(Say(action, 5000)));

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
                    todo.Enqueue(() => StartCoroutine(WalkTo(Agent.MentalState.Locations[destination])));
                } else {
                    break;
                }
            }

            else if (content.Head.Equals(INFORMED.Head)) {
                var message = new Expression(ASSERT, content.GetArgAsExpression(0));
                // Debug.Log("saying " + message);
                todo.Enqueue(() => StartCoroutine(Say(message, 5000)));
            }

            while (actionInProgress) {
                Thread.Sleep(1000);
            }
        }
    }

}
