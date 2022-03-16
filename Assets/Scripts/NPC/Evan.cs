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

        var tomato = GameObject.Find("Tomato");
        var tomatoParam = MentalState.ConstructPercept(TOMATO, tomato.transform.position);
        // MentalState.AddToKnowledgeBase(new Expression(GOOD, new Expression(AT, SELF, tomatoParam)));
        // MentalState.AddToKnowledgeBase(new Expression(ABLE, new Expression(AT, SELF, tomatoParam), SELF));

        var banana = GameObject.Find("Banana");
        var bananaParam = MentalState.ConstructPercept(BANANA, banana.transform.position);
        // MentalState.AddToKnowledgeBase(new Expression(VERY, new Expression(GOOD, new Expression(AT, SELF, bananaParam))));
        // MentalState.AddToKnowledgeBase(new Expression(ABLE, new Expression(AT, SELF, bananaParam), SELF));

        base.Start();
    }
}
