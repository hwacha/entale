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
        MentalState.FrameTimer = FrameTimer;

        var p = new Expression(new Name(TRUTH_VALUE, "P"));
        var q = new Expression(new Name(TRUTH_VALUE, "Q"));
        var r = new Expression(new Name(TRUTH_VALUE, "R"));

        MentalState.Initialize(new Expression[]{
            // new Expression(KNOW, new Expression(RED, ALICE), BOB),
            // new Expression(KNOW, new Expression(KNOW, new Expression(RED, ALICE), BOB), CHARLIE),
            // // new Expression(PAST, new Expression(SEE, p, SELF)),
            // p,
            // new Expression(IF, q, p),
            // new Expression(IF, r, q),
            
            // new Expression(AND, p, new Expression(AND, q, r)),

            // new Expression(OR, p, new Expression(OR, q, r)),
            new Expression(VERY, new Expression(VERY, new Expression(OMEGA, new Expression(OMEGA, VERY), p))),

            // new Expression(SEE, new Expression(BLUE, SELF), SELF),

            // new Expression(ABLE, new Expression(GREEN, SELF), SELF),
        });

        // MentalState.RemoveFromKnowledgeBase(new Expression(AND, p, new Expression(AND, q, r)));
        // MentalState.RemoveFromKnowledgeBase(new Expression(OR, p, new Expression(OR, q, r)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(BLUE, SELF)));

        // StartCoroutine(Assert(MentalState, new Expression(NOT, new Expression(BLUE, SELF)), BOB));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(GREEN, SELF), Plan));

        var ovp = p;
        var omega = VERY;
        for (int i = 1; i <= 2; i++) {
            omega = new Expression(OMEGA, omega);
        }

        while (true) {
            for (int i = 1; i <= 3; i++) {
                ovp = new Expression(omega, ovp);
            }
            if (!omega.HeadedBy(OMEGA)) {
                break;
            }
            omega = omega.GetArgAsExpression(0);
        }
        var zero = new Expression(GOOD, p);
        var minusZero = new Expression(GOOD, new Expression(NOT, p));
        var one = new Expression(VERY, zero);
        var minusOne = new Expression(VERY, minusZero);

        var five = zero;
        var minusFive = minusZero;

        for (int i = 1; i <= 5; i++) {
            five = new Expression(VERY, five);
            minusFive = new Expression(VERY, minusFive);
        }

        var w = new Expression(OMEGA, VERY, zero);
        var minusW = new Expression(OMEGA, VERY, minusZero);

        var wSquared = new Expression(OMEGA, new Expression(OMEGA, VERY), zero);
        var wCubed = new Expression(OMEGA, new Expression(OMEGA, new Expression(OMEGA, VERY)), zero);

        var wSquaredPlusOne = new Expression(VERY, wSquared);

        // TestConvertToValue(zero);
        // TestConvertToValue(minusZero);
        // TestConvertToValue(one);
        // TestConvertToValue(minusOne);
        // TestConvertToValue(five);
        // TestConvertToValue(minusFive);

        TestConvertToValue(w);
        TestConvertToValue(minusW);

        TestConvertToValue(wSquared);
        TestConvertToValue(wCubed);
        TestConvertToValue(wSquaredPlusOne);

        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(OMEGA, new Expression(OMEGA, VERY), p))));
        
    }

    public static void TestConvertToValue(Expression e) {
        var eAsNumber = MentalState.ConvertToValue(e);

        StringBuilder s = new StringBuilder();

        foreach (var placeValue in eAsNumber) {
            s.Append(placeValue + ", ");
        }

        Debug.Log(s.ToString());
    }

    public static IEnumerator Assert(MentalState m, Expression content, Expression speaker) {
        yield return new WaitForSeconds(5);
        m.StartCoroutine(m.ReceiveAssertion(content, speaker));
    }

    public static IEnumerator TestFindMostSpecificConjunction(MentalState m, List<Expression> conjunction) {
        var result = new List<List<Expression>>();
        var done   = new Container<bool>(false);
        m.StartCoroutine(m.FindMostSpecificConjunction(conjunction, result, done));

        while (!done.Item) {
            yield return null;
        }

        StringBuilder s = new StringBuilder();
        foreach (var c in result) {
            s.Append(MentalState.Conjunctify(c) + "\n");
        }

        Log(s.ToString());
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
        var foundResult = "found partial result. go to LogBasesStream() to see it.";
        var isProvedByString = "'" + e + "'" + " is proved by: ";
        while (!done.Item) {
            if (startTime + timeout >= Time.time) {
                m.StopCoroutine(proofRoutine);
                break;
            }
            // Log(waitingString);
            if (!result.IsEmpty()) {
                // Log(foundResult);
                // Log(isProvedByString + result);
            }
            yield return null;
        }
        Log(isProvedByString + result);
        yield break;
    }
}
