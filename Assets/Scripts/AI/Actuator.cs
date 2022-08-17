using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using static Expression;
using static SemanticType;

public class Actuator : MonoBehaviour {
    public Agent Agent;
    public NavMeshAgent NavMeshAgent { protected set; get; }

    void Start() {
        NavMeshAgent = Agent.GetComponent<NavMeshAgent>();
    }

    protected IEnumerator Say(Expression e, float time) {
        // TODO: figure this stuff out
        // var eWithoutParameters = new Container<Expression>(null);
        // Agent.MentalState.StartCoroutine(Agent.MentalState.ReplaceParameters(e, eWithoutParameters));

        // while (eWithoutParameters.Item == null) {
        //     yield return null;
        // }

        // GameObject eContainer = ArgumentContainer.From(eWithoutParameters.Item);

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
        yield break;
    }

    // @Note this should be removed when the planner is better.
    public IEnumerator RespondTo(Expression utterance, Expression speaker) {
        if (utterance.Type.Equals(ASSERTION)) {
            if (utterance.Head.Equals(ASSERT.Head)) {
                Agent.MentalState.ReceiveAssertion(
                    utterance.GetArgAsExpression(0),
                    speaker);

                var negativeProofs = new ProofBases();
                var doneNegative = new Container<bool>(false);

                Agent.MentalState.StartCoroutine(
                    Agent.MentalState.StreamProofs(
                        negativeProofs,
                        new Expression(NOT, utterance.GetArgAsExpression(0)),
                        doneNegative));

                while (!doneNegative.Item) {
                    yield return new WaitForSeconds(0.5f);
                }

                if (negativeProofs.IsEmpty()) {
                    StartCoroutine(Say(YES, 5));
                    yield break;
                } else {
                    StartCoroutine(Say(NO, 5));
                }

                
            }
            if (utterance.Head.Equals(DENY.Head)) {
                Agent.MentalState.ReceiveAssertion(
                    new Expression(NOT, utterance.GetArgAsExpression(0)), speaker);

                var positiveProofs = new ProofBases();
                var donePositive = new Container<bool>(false);

                Agent.MentalState.StartCoroutine(
                    Agent.MentalState.StreamProofs(
                        positiveProofs,
                        new Expression(NOT, utterance.GetArgAsExpression(0)),
                        donePositive));

                while (!donePositive.Item) {
                    yield return new WaitForSeconds(0.5f);
                }

                if (positiveProofs.IsEmpty()) {
                    StartCoroutine(Say(NO, 5));
                } else {
                    StartCoroutine(Say(YES, 5));
                }
            }
            yield break;
        }

        if (utterance.Type.Equals(QUESTION)) {
            if (utterance.Head.Equals(ASK.Head)) {
                var positiveProofs = new ProofBases();
                var donePositive = new Container<bool>(false);
                Agent.MentalState.StartCoroutine(Agent.MentalState.StreamProofs(
                    positiveProofs,
                    utterance.GetArgAsExpression(0),
                    donePositive));

                var negativeProofs = new ProofBases();
                var doneNegative = new Container<bool>(false);
                
                Agent.MentalState.StartCoroutine(
                    Agent.MentalState.StreamProofs(
                        negativeProofs,
                        new Expression(NOT, utterance.GetArgAsExpression(0)),
                        doneNegative));

                while (!donePositive.Item || !doneNegative.Item) {
                    yield return new WaitForSeconds(0.5f);
                }

                if (!positiveProofs.IsEmpty()) {
                    StartCoroutine(Say(YES, 5));
                    yield break;
                }

                if (!negativeProofs.IsEmpty()) {
                    StartCoroutine(Say(NO, 5));
                    yield break;
                }

                StartCoroutine(Say(MAYBE, 5));
            }

            yield break;
        }

        if (utterance.Type.Equals(CONFORMITY_VALUE)) {
            if (utterance.Head.Equals(WOULD.Head)) {
                var content = utterance.GetArgAsExpression(0);
                var proofs = new ProofBases();
                var done = new Container<bool>(false);

                Agent.MentalState.StartCoroutine(
                    Agent.MentalState.StreamProofs(proofs, content, done, ProofType.Plan));

                while (!done.Item) {
                    yield return new WaitForSeconds(0.5f);
                }

                if (!proofs.IsEmpty()) {
                    StartCoroutine(Say(ACCEPT, 5));
                    Agent.MentalState.ReceiveRequest(content, speaker);
                } else {
                    StartCoroutine(Say(REFUSE, 5));
                }
            }
            
            yield break;
        }

        yield break;
    }


    // @Note we want this to be interruptible
    public IEnumerator ExecutePlan() {
        while (true) {
            List<Expression> plan = new List<Expression>();
            var done = new Container<bool>(false);
            Agent.MentalState.StartCoroutine(Agent.MentalState.DecideCurrentPlan(plan, done));

            while (!done.Item) {
                yield return null;
            }

            foreach (Expression action in plan) {
                if (!action.Head.Equals(WILL.Head)) {
                    throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
                }

                // Debug.Log(action);
                // StartCoroutine(Say(action, 1));

                var content = action.GetArgAsExpression(0);

                if (content.Equals(NEUTRAL)) {
                    // Debug.Log("Busy doin' nothin'");
                }

                // at(self, X)
                else if (content.Head.Equals(AT.Head) && content.GetArgAsExpression(0).Equals(SELF)) {
                    var destination = content.GetArgAsExpression(1);
                    // assumption: if we find this in a plan,
                    // then the location of X should be known.
                    var location = Agent.MentalState.Locations[destination];
                    NavMeshAgent.SetDestination(location);
                    while (Vector3.Distance(transform.position, location) > NavMeshAgent.stoppingDistance + 1) {
                        location = Agent.MentalState.Locations[destination];
                        NavMeshAgent.SetDestination(location);
                        yield return null;
                    }
                }

                else if (content.Head.Equals(INFORMED.Head)) {
                    var message = new Expression(ASSERT, content.GetArgAsExpression(0));
                    // Debug.Log("saying " + message);
                    StartCoroutine(Say(message, 1.5f));
                }

                yield return new WaitForSeconds(2);
            }

            yield return null;
        }
    }

}
