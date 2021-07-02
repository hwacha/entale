using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;
using static ProofType;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;

public class Testing : MonoBehaviour {
    private FrameTimer FrameTimer;

    public MentalState MentalState;

    void Start() {
        // Debug.Log(SemanticType.Geach(TRUTH_VALUE, TRUTH_VALUE));
        // Debug.Log(SemanticType.Geach(INDIVIDUAL, TRUTH_VALUE));

        // Debug.Log(SemanticType.Geach(INDIVIDUAL, TRUTH_FUNCTION));
        // Debug.Log(SemanticType.Geach(TRUTH_VALUE, TRUTH_FUNCTION));

        // Debug.Log(SemanticType.Geach(INDIVIDUAL, QUANTIFIER_PHRASE));

        FrameTimer = gameObject.GetComponent<FrameTimer>();

        // DON'T COMMENT ABOVE THIS LINE
        MentalState.FrameTimer = FrameTimer;

        MentalState.Initialize(new Expression[]{
            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(RED, SELF),
                        new Expression(new Parameter(TIME, 10))),
                    BOB),
                new Expression(new Parameter(TIME, 20))),

            new Expression(SEE,
                new Expression(WHEN,
                    new Expression(RED, SELF),
                    new Expression(new Parameter(TIME, 30))),
                SELF),

            new Expression(SEE,
                new Expression(WHEN,
                    new Expression(RED, SELF),
                    new Expression(new Parameter(TIME, 75))),
                SELF),

            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(GREEN, SELF),
                        new Expression(new Parameter(TIME, 10))),
                    BOB),
                new Expression(new Parameter(TIME, 20))),

            new Expression(SEE,
                new Expression(NOT,
                    new Expression(WHEN,
                        new Expression(GREEN, SELF),
                        new Expression(new Parameter(TIME, 30)))),
                SELF),

            new Expression(SEE,
                new Expression(WHEN,
                    new Expression(GREEN, SELF),
                    new Expression(new Parameter(TIME, 75))),
                SELF),

            new Expression(SEE,
                new Expression(WHEN,
                    new Expression(AT, ALICE, BOB),
                    new Expression(new Parameter(TIME, 40))),
                SELF),

            new Expression(SEE,
                new Expression(WHEN,
                    new Expression(BLUE, SELF),
                    new Expression(new Parameter(TIME, 35))),
                SELF),

            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(KNOW,
                            new Expression(WHEN,
                                new Expression(AT, SELF, ALICE),
                                new Expression(new Parameter(TIME, 37))),
                            ALICE),
                        new Expression(new Parameter(TIME, 38))),
                    BOB),
                new Expression(new Parameter(TIME, 39))),

            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(KNOW,
                            new Expression(WHEN,
                                new Expression(KNOW,
                                    new Expression(WHEN,
                                        new Expression(AT, SELF, ALICE),
                                        new Expression(new Parameter(TIME, 37))),
                                    ALICE),
                                new Expression(new Parameter(TIME, 38))),
                            BOB),
                        new Expression(new Parameter(TIME, 39))),
                    CHARLIE),
                new Expression(new Parameter(TIME, 40))),

            // for testing admissibility for present tense
            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(TOMATO, SELF),
                        new Expression(new Parameter(TIME, 27))),
                    BOB),
                new Expression(new Parameter(TIME, 28))),

            new Expression(NOT,
                new Expression(WHEN,
                    new Expression(KNOW,
                        new Expression(WHEN,
                            new Expression(TOMATO, SELF),
                            new Expression(new Parameter(TIME, 27))),
                        BOB),
                    new Expression(new Parameter(TIME, 30)))),

            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(TOMATO, SELF),
                        new Expression(new Parameter(TIME, 27))),
                    BOB),
                new Expression(new Parameter(TIME, 60))),

            new Expression(WHEN,
                new Expression(KNOW,
                    new Expression(WHEN,
                        new Expression(TOMATO, SELF),
                        new Expression(new Parameter(TIME, 70))),
                    BOB),
                new Expression(new Parameter(TIME, 80))),

            // new Expression(BEFORE,
            //     new Expression(SPICY, SELF),
            //     new Expression(new Parameter(TIME, 20))),

            // new Expression(AFTER,
            //     new Expression(SPICY, BOB),
            //     new Expression(new Parameter(TIME, 60))),

            // new Expression(GEACH_E_TRUTH_FUNCTION,
            //     new Expression(GEACH_T_TRUTH_FUNCTION, GOOD, NOT),
            //     SPICY,
            //     CHARLIE),

            new Expression(BLUE, CHARLIE),

            new Expression(ALL, BLUE, YELLOW),

            new Expression(ALL, BLUE,
                new Expression(GEACH_E_TRUTH_FUNCTION,
                    new Expression(GEACH_T_TRUTH_FUNCTION, GOOD, NOT), SPICY)),

            new Expression(ALL, RED, APPLE),

            new Expression(ALL, APPLE, new Expression(GEACH_E_TRUTH_FUNCTION, NOT, SPICY)),

            new Expression(BANANA, ALICE),

            // @Note the left-associative order of the T-geaches
            // isn't arbitrary: the search doesn't work
            // if the geach is made to be right-associative instead.
            // 
            // This seems like a problem if we don't constrain the
            // player's ability to make the expressions. Intuitively,
            // they should be equivalent.
            new Expression(ALL, BANANA,
                new Expression(GEACH_E_TRUTH_FUNCTION,
                    new Expression(GEACH_T_TRUTH_FUNCTION,
                        new Expression(GEACH_T_TRUTH_FUNCTION,
                            GOOD,
                            NOT),
                        new Expression(MAKE,
                            new Empty(SemanticType.TRUTH_VALUE),
                            SELF)),
                        new Expression(AT, SELF))),

        });

        // testing custom ordering for factives, negations, tense, etc.
        SortedSet<Expression> sequence = new SortedSet<Expression>{
            // new Expression(RED, SELF),
            // new Expression(GREEN, SELF),
            // new Expression(BLUE, SELF),
            // new Expression(NOT, new Expression(RED, SELF)),
            // new Expression(NOT, new Expression(GREEN, SELF)),
            // new Expression(NOT, new Expression(BLUE, SELF)),
            // new Expression(NOT, new Expression(NOT, new Expression(RED, SELF))),
            // new Expression(NOT, new Expression(NOT, new Expression(GREEN, SELF))),
            // new Expression(NOT, new Expression(NOT, new Expression(BLUE, SELF))),
            // new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 0))),
            // new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 0))),
            // new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 0))),
            // new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 1))),
            // new Expression(NOT, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 0)))),
            // new Expression(NOT, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 1)))),
            // new Expression(NOT, new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 0)))),
            // new Expression(NOT, new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 1)))),
            // new Expression(NOT, new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 0)))),
            // new Expression(NOT, new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 1)))),
            // new Expression(KNOW, new Expression(RED, SELF), BOB),
            // new Expression(KNOW, new Expression(GREEN, SELF), BOB),
            // new Expression(KNOW, new Expression(BLUE, SELF), BOB),
            // new Expression(KNOW, new Expression(RED, SELF), ALICE),
            // new Expression(KNOW, new Expression(GREEN, SELF), ALICE),
            // new Expression(KNOW, new Expression(BLUE, SELF), ALICE),
            // new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB),
            // new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB),
            // new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB),
            // new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE),
            // new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE),
            // new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
            //     new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
            // new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
            //     new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
        };

        // var sort = sequence.GetViewBetween(
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Bottom(TIME))), BOB),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Top(TIME))), BOB)
        // );

        // foreach (var e in sequence) {
        //     Debug.Log(e);
        // }

        // var r = new Expression(WHEN, new Expression(RED, SELF),
        //         new Expression(new Parameter(TIME, 0)));

        // var k = new Expression(WHEN, new Expression(KNOW, r, BOB), new Expression(new Parameter(TIME, 1)));

        // var bot = new Expression(WHEN, new Expression(RED, SELF), new Expression(new Bottom(TIME)));

        // var top = new Expression(WHEN, new Expression(RED, SELF), new Expression(new Top(TIME)));

        // Debug.Log(k + " compared to " + bot + " : " + k.CompareTo(bot));
        // Debug.Log(k + " compared to " + top + " : " + k.CompareTo(top));

        MentalState.Timestamp = 50;

        // var aliceIsRed = new Expression(RED, ALICE);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(aliceIsRed));

        // var bobKnowsAliceIsRed = new Expression(KNOW, aliceIsRed, BOB);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(bobKnowsAliceIsRed));

        // var charlieKnowsBobKnowsAliceIsRed = new Expression(KNOW, bobKnowsAliceIsRed, CHARLIE);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(charlieKnowsBobKnowsAliceIsRed));

        // var iSeeAliceIsRed = new Expression(SEE, aliceIsRed, SELF);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(iSeeAliceIsRed));

        // var aliceIsntRed = new Expression(NOT, aliceIsRed);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(aliceIsntRed));

        // var bobKnowsAliceIsntRed = new Expression(KNOW, aliceIsntRed, BOB);
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(bobKnowsAliceIsntRed));

        // var bobDoesntKnowAliceIsRed = new Expression(NOT, new Expression(KNOW, aliceIsRed, BOB));
        // MentalState.StartCoroutine(MentalState.AddToKnowledgeBase(bobDoesntKnowAliceIsRed));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(RED, SELF)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(RED, SELF))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(NOT, new Expression(RED, SELF)))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(GREEN, SELF)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(GREEN, SELF))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, new Expression(GREEN, SELF))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, new Expression(NOT,  new Expression(GREEN, SELF)))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(FUTURE, new Expression(GREEN, SELF))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(FUTURE, new Expression(NOT, new Expression(GREEN, SELF)))));
        
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(NOT, new Expression(NOT, new Expression(GREEN, SELF))))));
        
        // StartCoroutine(LogBasesStream(MentalState, new Expression(BLUE, SELF)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(AT, ALICE, BOB)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(SEE, new Expression(RED, SELF), SELF)));

        // check MentalState.cs #429
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(RED, SELF), BOB)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(SOME, RED, BLUE)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(AT, SELF, ALICE)));

        // StartCoroutine(LogBasesStream(MentalState,
            // new Expression(KNOW, new Expression(AT, SELF, ALICE), BOB)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW, new Expression(AT, SELF, ALICE), ALICE)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             ALICE),
        //         BOB)));
        
        // var selfIsRed = new Expression(RED, SELF);
        // var aliceKnowsSelfIsRed = new Expression(KNOW, selfIsRed, ALICE);

        // StartCoroutine(TestFactiveContains(MentalState, aliceKnowsSelfIsRed, selfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, selfIsRed, aliceKnowsSelfIsRed, false));

        // var bobKnowsAliceKnowsSelfIsRed = new Expression(KNOW, aliceKnowsSelfIsRed, BOB);

        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceKnowsSelfIsRed, aliceKnowsSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceKnowsSelfIsRed, selfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceKnowsSelfIsRed, new Expression(GREEN, SELF), false));

        // var charlieKnows = new Expression(KNOW, bobKnowsAliceKnowsSelfIsRed, CHARLIE);

        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, bobKnowsAliceKnowsSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, aliceKnowsSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, selfIsRed, true));

        // var bobKnowsSelfIsRed = new Expression(KNOW, selfIsRed, BOB);

        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, bobKnowsSelfIsRed, true));

        // var aliceKnowsBobKnowsSelfIsRed = new Expression(KNOW, bobKnowsSelfIsRed, ALICE);

        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, aliceKnowsBobKnowsSelfIsRed, false));

        // var charlieKnowsAliceKnowsSelfIsRed = new Expression(KNOW, aliceKnowsSelfIsRed, CHARLIE);

        // StartCoroutine(TestFactiveContains(MentalState, charlieKnows, charlieKnowsAliceKnowsSelfIsRed, true));
        
        // var selfIsntRed = new Expression(NOT, selfIsRed);

        // StartCoroutine(TestFactiveContains(MentalState, selfIsRed, selfIsntRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, selfIsntRed, selfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, aliceKnowsSelfIsRed, selfIsntRed, true));

        // var aliceDoesntKnowSelfIsRed = new Expression(NOT, aliceKnowsSelfIsRed);
        // var aliceKnowsSelfIsntRed = new Expression(KNOW, selfIsntRed, ALICE);

        // StartCoroutine(TestFactiveContains(MentalState, aliceDoesntKnowSelfIsRed, selfIsRed, false));
        // StartCoroutine(TestFactiveContains(MentalState, aliceDoesntKnowSelfIsRed, selfIsRed, false));

        // StartCoroutine(TestFactiveContains(MentalState, aliceDoesntKnowSelfIsRed, aliceKnowsSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, aliceKnowsSelfIsRed, aliceDoesntKnowSelfIsRed, true));
        
        // StartCoroutine(TestFactiveContains(MentalState, aliceDoesntKnowSelfIsRed, aliceKnowsSelfIsntRed, false));

        // var bobDoesntKnowAliceKnowsSelfIsRed = new Expression(NOT, bobKnowsAliceKnowsSelfIsRed);

        // StartCoroutine(TestFactiveContains(MentalState, bobDoesntKnowAliceKnowsSelfIsRed, selfIsRed, false));
        // StartCoroutine(TestFactiveContains(MentalState, bobDoesntKnowAliceKnowsSelfIsRed, aliceKnowsSelfIsRed, false));
        // StartCoroutine(TestFactiveContains(MentalState, bobDoesntKnowAliceKnowsSelfIsRed, bobKnowsAliceKnowsSelfIsRed, true));

        // var bobKnowsAliceDoesntKnowSelfIsRed = new Expression(KNOW, aliceDoesntKnowSelfIsRed, BOB);

        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceDoesntKnowSelfIsRed, aliceDoesntKnowSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceDoesntKnowSelfIsRed, aliceKnowsSelfIsRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceDoesntKnowSelfIsRed, aliceKnowsSelfIsntRed, false));

        // var aliceDoesntKnowSelfIsntRed = new Expression(NOT, aliceKnowsSelfIsntRed);
        // var bobKnowsAliceDoesntKnowSelfIsntRed = new Expression(KNOW, aliceDoesntKnowSelfIsntRed, BOB);

        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceKnowsSelfIsRed, bobKnowsAliceDoesntKnowSelfIsntRed, true));
        // StartCoroutine(TestFactiveContains(MentalState, bobKnowsAliceDoesntKnowSelfIsntRed, bobKnowsAliceKnowsSelfIsRed, false));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             ALICE),
        //         CHARLIE)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             BOB),
        //         CHARLIE)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             BOB),
        //         ALICE)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             CHARLIE),
        //         BOB)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW,
        //         new Expression(KNOW,
        //             new Expression(AT, SELF, ALICE),
        //             CHARLIE),
        //         ALICE)));
        

        // StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, new Expression(RED, SELF))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(FUTURE, new Expression(RED, SELF))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(FUTURE,
        //         new Expression(KNOW,
        //             new Expression(RED, SELF),
        //             BOB))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(PAST, new Expression(KNOW, new Expression(TOMATO, SELF), BOB))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW, new Expression(TOMATO, SELF), BOB)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(FUTURE,
        //         new Expression(KNOW,
        //             new Expression(PAST,
        //                 new Expression(TOMATO, SELF)),
        //             BOB))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(FUTURE,
        //         new Expression(KNOW,
        //             new Expression(FUTURE,
        //                 new Expression(TOMATO, SELF)),
        //             BOB))));
        
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(PAST, new Expression(SPICY, SELF))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(SPICY, SELF)));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(FUTURE, new Expression(SPICY, BOB))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(AND,
        //         new Expression(PAST, new Expression(SPICY, SELF)),
        //         new Expression(PRESENT, new Expression(SPICY, SELF))
        //     )));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(KNOW, new Expression(SPICY, SELF), SELF)));


        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(NOT, new Expression(IDENTITY, ALICE, BOB))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(NOT,
        //         new Expression(KNOW,
        //             new Expression(SPICY, ALICE),
        //             SELF))));
        
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(YELLOW, CHARLIE)));
        
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(GOOD,
        //         new Expression(NOT,
        //             new Expression(MAKE,
        //                 new Expression(AT, SELF, ALICE),
        //                 SELF)))));

        // StartCoroutine(LogBasesStream(MentalState,
            // new Expression(GOOD, new Expression(NOT, new Expression(SPICY, CHARLIE)))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(NOT, new Expression(SPICY, SELF))));
        
    }

    public static IEnumerator TestFactiveContains(MentalState m, Expression factive, Expression content, bool expect) {
        var matches = new HashSet<Substitution>();
        var answer = new Container<bool>(false);
        var parityAligned = new Container<bool>(true);
        var done = new Container<bool>(false);

        m.StartCoroutine(m.FactiveContains(factive, content, matches, answer, parityAligned, done));

        while (!done.Item) {
            yield return null;
        }

        Log((answer.Item == expect ? "SUCCESS: " : "FAILURE: ") +
            factive +
            (answer.Item ? " contains " : " does not contain ") +
            content);
    }

    public static String Verbose(Expression e) {
        return e.ToString() + " : " + e.Type.ToString() + " #" + e.Depth;
    }

    public static String SubstitutionString(HashSet<Substitution> subs) {
        StringBuilder s = new StringBuilder();
        s.Append("{\n");
        foreach (var sub in subs) {
            s.Append("\t{\n");
            foreach (KeyValuePair<Variable, Expression> assignments in sub) {
                s.Append("\t\t");
                s.Append(assignments.Key);
                s.Append(" -> ");
                s.Append(assignments.Value);
                s.Append(",\n");
            }
            s.Append("\t}\n");
        }
        s.Append("}");
        return s.ToString();
    }

    public static String MatchesString(Expression a, Expression b) {
        return a + ", " + b + ": " + SubstitutionString(a.GetMatches(b));
    }

    public static IEnumerator LogBasesStream(MentalState m, Expression e, ProofType pt = Proof, float timeout = -1) {
        var result = new ProofBases();
        var done = new Container<bool>(false);
        
        var startTime = Time.time;
        var proofRoutine = m.StreamProofs(result, e, done, pt);
        m.StartCoroutine(proofRoutine);

        var waitingString = "waiting for '" + e + "' to be proved...";
        var isProvedByString = "'" + e + "'" + " is proved by: ";
        while (!done.Item) {
            if (startTime + timeout >= Time.time) {
                m.StopCoroutine(proofRoutine);
                break;
            }
            // Log(waitingString);
            if (!result.IsEmpty()) {
                Log(isProvedByString + result);
            }
            yield return null;
        }
        Log(isProvedByString + result);
        yield break;
    }
}
