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
        FrameTimer = gameObject.GetComponent<FrameTimer>();

        // DON'T COMMENT ABOVE THIS LINE

        // Log("End percept ordering");
        Log("Bounds ordering");
        var bounds = new Expression[]{
            new Expression(new Top(TRUTH_VALUE)),
            new Expression(new Bottom(TRUTH_VALUE)),
            VERUM, FALSUM, new Expression(RED, SELF),
            SELF, ASK,
            new Expression(AT, ALICE, ALICE),
            new Expression(AT, ALICE, BOB),
            new Expression(AT, BOB, ALICE),
            new Expression(AT, BOB, BOB),
            new Expression(AT, new Expression(SELECTOR, RED), new Expression(SELECTOR, BLUE)),
            new Expression(ALL, RED, new Expression(AT, new Empty(INDIVIDUAL), ALICE)),
        //  new Expression(AT, ALICE, new Expression(new Top(INDIVIDUAL))),
        //  new Expression(AT, ALICE, new Expression(new Bottom(INDIVIDUAL))),
        //  new Expression(AT, new Expression(new Top(INDIVIDUAL)), new Expression(new Top(INDIVIDUAL))),
        //  new Expression(AT, new Expression(new Bottom(INDIVIDUAL)), new Expression(new Bottom(INDIVIDUAL))),
        //  new Expression(AT, new Expression(new Top(INDIVIDUAL)), BOB),
        //  new Expression(AT, new Expression(new Bottom(INDIVIDUAL)), BOB),
            new Expression(new Expression(new Top(PREDICATE)), new Expression(new Top(INDIVIDUAL))),
            new Expression(new Expression(new Bottom(PREDICATE)), new Expression(new Bottom(INDIVIDUAL))),
            new Expression(ALL, RED, new Expression(new Bottom(PREDICATE))),
            new Expression(ALL, RED, new Expression(new Top(PREDICATE)))
        };
        Array.Sort(bounds);
        for (int i = 0; i < bounds.Length; i++) {
            Log(bounds[i]);
        }
        Log("End bounds ordering");

        MentalState.FrameTimer = FrameTimer;

        // MentalState.Initialize(
        //     aliceIsRed,
        //     aliceIsAnApple,
        //     bobIsBlue,
        //     aliceIsAtBob,
        //     aliceIsAlice,
        //     bobIsBob,
        //     charlieIsAMacintosh,
        //     allApplesAreRed,
        //     allMacintoshesAreApples,
        //     iCanMakeCharlieBlue,
        //     charlieIsBlue,
        //     iSeeCharlieAsRed,
        //     ifCharlieIsBlueIAmGreen,
        //     bobIsAtCharlie,
        //     everythingIsSelfIdentical,
        //     whatIseeIsAlwaysTrue,
        //     new Expression(BETTER, new Expression(BLUE, SELF), NEUTRAL),
        //     new Expression(BETTER, new Expression(RED, SELF), new Expression(BLUE, SELF)));
        
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

    public static IEnumerator LogBasesStream(MentalState m, Expression e, ProofType pt = Proof) {
        var result = new ProofBases();
        var done = new Container<bool>(false);

        m.StartCoroutine(m.StreamBasesIteratedDFS(result, e, done, pt));

        var waitingString = "waiting for '" + e + "' to be proved...";
        var isProvedByString = "'" + e + "'" + " is proved by: ";
        while (!done.Item) {
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
