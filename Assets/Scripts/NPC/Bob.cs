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
            new Expression[]{
                new Expression(IDENTITY, SELF, BOB),
                new Expression(RED, SELF),
                new Expression(BLUE, EVAN)
            },
            new Expression[]{
                new Expression(SAY, SELF, new Expression(RED, SELF))
            }
        );

        // SPECIAL CASING THE TEST ROOM
        var tomato = GameObject.Find("Tomato");
        var tomatoParam = MentalState.ConstructPercept(TOMATO, tomato.transform.position);

        var banana = GameObject.Find("Banana");
        var bananaParam = MentalState.ConstructPercept(BANANA, banana.transform.position);

        // the player knows that there's a tomato on the table, but doesn't know the word for tomato.
        // The player doesn't know anything about how the language works, but will probably make
        // a few assumptions, implicitly, about how it works.
        // 
        // say a sentence 'X.Y' while directing their attention to an apple, and say the sentence
        // 'Z.Y' while directing their attention to an orange. The player will assume 'X' means
        // apple and 'Z' means orange, because that's what's most obviously different
        // about the two objects. They may not yet understand that Y is a kind of demonstrative pronoun,
        // but they'll be able to figure out that it must be something that's appropriate to use in both
        // the presence of an apple and an orange. They migvht assume it means 'fruit' or something.
        // The trick is to use the pronoun in enough contexts where those hypotheses is ruled out.
        // 

        // MentalState.StartCoroutine(MentalState.Assert(new Expression(BETTER,
        //     new Expression(AT, SELF, tomatoParam),
        //     new Expression(SAY, SELF, new Expression(TOMATO, tomatoParam)))));

        // MentalState.StartCoroutine(MentalState.Assert(
        //     new Expression(BETTER,
        //         new Expression(SAY, SELF, new Expression(TOMATO, tomatoParam)),
        //         NEUTRAL)));
        // MentalState.StartCoroutine(MentalState.Assert(new Expression(BETTER, new Expression(AT, SELF, bananaParam), new Expression(AT, SELF, tomatoParam))));
        // MentalState.StartCoroutine(MentalState.Assert(
        //     new Expression(BETTER, new Expression(AT, SELF,
        //         new Expression(SELECTOR, TOMATO)), NEUTRAL)));

        // GameObject.Find("Test").GetComponent<Testing>().StartCoroutine(Testing.LogBases(MentalState,
        //     new Expression(ABLE, SELF, new Expression(AT, SELF,
        //         new Expression(SELECTOR, TOMATO)))));
        base.Start();
    }
}
