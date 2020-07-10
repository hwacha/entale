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
            new Expression(IDENTITY, SELF, BOB),
            new Expression(RED, SELF),
            new Expression(BLUE, EVAN)
            //new Expression(BETTER, new Expression(SOME, TREE, new Expression(AT, SELF)), NEUTRAL)
            );

        // SPECIAL CASING THE TEST ROOM
        var tomato = GameObject.Find("Tomato");
        var tomatoParam = MentalState.ConstructPercept(TOMATO, tomato.transform.position);

        MentalState.StartCoroutine(MentalState.Assert(new Expression(BETTER, new Expression(AT, SELF, tomatoParam), NEUTRAL)));

        base.Start();
    }
}
