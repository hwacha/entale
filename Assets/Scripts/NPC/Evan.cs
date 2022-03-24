using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Evan : Agent
{
    protected override void Start()
    {
        MentalState.Initialize(
            new Expression[]{
                // new Expression(GOOD, new Expression(SOME, BANANA, new Expression(AT, SELF))),
                // new Expression(GOOD, new Expression(SOME, TOMATO, new Expression(AT, SELF))),
            }
        );

        base.Start();
    }
}
