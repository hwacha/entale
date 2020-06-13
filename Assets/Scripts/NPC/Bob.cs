using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Bob : Agent
{
    // Start is called before the first frame update
    protected override void Start()
    {
        MentalState.Initialize(
            new Expression(BETTER, new Expression(SOME, TREE, new Expression(AT, SELF)), NEUTRAL)
            );

        base.Start();
    }
}
