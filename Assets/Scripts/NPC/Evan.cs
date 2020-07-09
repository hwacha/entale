using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Evan : Agent
{
    // Start is called before the first frame update
    protected override void Start()
    {
        MentalState.Initialize(
            new Expression(IDENTITY, SELF, EVAN),
            new Expression(BLUE, SELF),
            new Expression(RED, BOB));

        base.Start();
    }
}
