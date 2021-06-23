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
        });

        // // testing custom ordering for evidentials, negations, tense, etc.
        // SortedSet<Expression> sequence = new SortedSet<Expression>{
        //     new Expression(RED, SELF),
        //     new Expression(GREEN, SELF),
        //     new Expression(BLUE, SELF),
        //     new Expression(NOT, new Expression(RED, SELF)),
        //     new Expression(NOT, new Expression(GREEN, SELF)),
        //     new Expression(NOT, new Expression(BLUE, SELF)),
        //     new Expression(NOT, new Expression(NOT, new Expression(RED, SELF))),
        //     new Expression(NOT, new Expression(NOT, new Expression(GREEN, SELF))),
        //     new Expression(NOT, new Expression(NOT, new Expression(BLUE, SELF))),
        //     new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 0))),
        //     new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 0))),
        //     new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 0))),
        //     new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 1))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 0)))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME, 1)))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 0)))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(GREEN, SELF), new Expression(new Parameter(TIME, 1)))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 0)))),
        //     new Expression(NOT, new Expression(WHEN, new Expression(BLUE, SELF), new Expression(new Parameter(TIME, 1)))),
        //     new Expression(KNOW, new Expression(RED, SELF), BOB),
        //     new Expression(KNOW, new Expression(GREEN, SELF), BOB),
        //     new Expression(KNOW, new Expression(BLUE, SELF), BOB),
        //     new Expression(KNOW, new Expression(RED, SELF), ALICE),
        //     new Expression(KNOW, new Expression(GREEN, SELF), ALICE),
        //     new Expression(KNOW, new Expression(BLUE, SELF), ALICE),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
        //         new Expression(new Parameter(TIME, 0))), BOB), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(GREEN, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
        //     new Expression(WHEN, new Expression(KNOW, new Expression(WHEN, new Expression(BLUE, SELF),
        //         new Expression(new Parameter(TIME, 0))), ALICE), new Expression(new Parameter(TIME, 1))),
        // };

        // var sort = sequence.GetViewBetween(
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Bottom(TIME))), BOB),
        //     new Expression(KNOW, new Expression(WHEN, new Expression(RED, SELF), new Expression(new Top(TIME))), BOB)
        // );

        // foreach (var e in sort) {
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

        StartCoroutine(LogBasesStream(MentalState,
            new Expression(KNOW,
                new Expression(KNOW,
                    new Expression(AT, SELF, ALICE),
                    ALICE),
                CHARLIE)));

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
