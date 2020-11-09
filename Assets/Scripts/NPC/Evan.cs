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

        var banana = GameObject.Find("Banana");
        var bananaParam = MentalState.ConstructPercept(BANANA, banana.transform.position);

        var tomato = GameObject.Find("Tomato");
        var tomatoParam = MentalState.ConstructPercept(TOMATO, tomato.transform.position);

        // MentalState.StartCoroutine(MentalState.Assert(new Expression(BETTER, new Expression(AT, SELF, bananaParam), NEUTRAL)));
        // MentalState.StartCoroutine(MentalState.Assert(new Expression(BETTER, new Expression(AT, SELF, tomatoParam),
        //     new Expression(AT, SELF, bananaParam))));

        base.Start();
    }
}
