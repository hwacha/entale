using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Actuator : MonoBehaviour
{
    public MentalState MentalState;
    private bool ActionInProgress;

    private bool IsBlue = false;

    // Okay, so the way I see the actuator working is
    // that it is constantly polling the mental state
    // (and maybe sensor) to update what it does,
    // using coroutines or something similar to
    // make its execution of plans interuptable.
    // 
    // For now, however, assume that it exuctes
    // a plan from the mental state serially and
    // non-interuptably.
    // 
    void Start()
    {
        // TODO: Structure things better so
        // there's a good way of encapsulating.
        MentalState = new MentalState(
            new Expression(BETTER, new Expression(BLUE, SELF), NEUTRAL),
            new Expression(ABLE, SELF, new Expression(BLUE, SELF)));
        ActionInProgress = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsBlue) {
            MentalState.Assert(new Expression(PERCEIVE, SELF, new Expression(BLUE, SELF)));
        }
        if (!ActionInProgress) {
            StartCoroutine(ExecutePlan());
        }
    }

    public IEnumerator ExecutePlan() {
        Debug.Log(MentalState.Query(new Expression(BLUE, SELF)));
        List<Expression> plan = MentalState.DecideCurrentPlan();
        ActionInProgress = true;
        foreach (Expression action in plan) {
            
            if (!action.Head.Equals(WILL.Head)) {
                throw new Exception("ExecutePlan(): expected sentences to start with 'will'");
            }

            var content = action.GetArg(0);

            var iTried = new Expression(TRIED, SELF, content);

            if (content.Equals(NEUTRAL)) {
                Debug.Log("Busy doin' nothin'");
            }

            else if (content.Equals(new Expression(BLUE, SELF))) {
                Debug.Log("I blue myself.");
                IsBlue = true;
            }

            // we assert to the mental state that
            // we've tried to perform this action.
            MentalState.Assert(new Expression(TRIED, SELF, content));

            yield return new WaitForSeconds(2);
        }
        ActionInProgress = false;
        yield return null;
    }
}
