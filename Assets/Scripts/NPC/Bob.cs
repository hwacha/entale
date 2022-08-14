using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Bob : Agent
{
    protected override void Start()
    {
        MentalState.Initialize(
            new Expression[] {
                new Expression(IDENTITY, SELF, BOB),
                new Expression(RED, SELF),
                new Expression(GEN, TOMATO, RED),
                new Expression(GEN, BANANA, YELLOW),
            }
        );
        base.Start();
    }
}
