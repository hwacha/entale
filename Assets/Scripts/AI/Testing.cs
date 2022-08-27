using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;
using static ProofType;
using static InferenceRule;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;

public class Testing : MonoBehaviour {
    private FrameTimer FrameTimer;

    public MentalState MentalState;

    void Start() {
        FrameTimer = gameObject.GetComponent<FrameTimer>();

        // DON'T COMMENT ABOVE THIS LINE
        MentalState.FrameTimer = FrameTimer;

        var a = new Expression(new Name(TRUTH_VALUE, "A"));
        var b = new Expression(new Name(TRUTH_VALUE, "B"));
        var c = new Expression(new Name(TRUTH_VALUE, "C"));
        var d = new Expression(new Name(TRUTH_VALUE, "D"));
        var e = new Expression(new Name(TRUTH_VALUE, "E"));
        var f = new Expression(new Name(TRUTH_VALUE, "F"));
        var g = new Expression(new Name(TRUTH_VALUE, "G"));
        var h = new Expression(new Name(TRUTH_VALUE, "H"));
        var j = new Expression(new Name(TRUTH_VALUE, "J"));
        var k = new Expression(new Name(TRUTH_VALUE, "K"));
        var l = new Expression(new Name(TRUTH_VALUE, "L"));
        var m = new Expression(new Name(TRUTH_VALUE, "M"));
        var n = new Expression(new Name(TRUTH_VALUE, "N"));
        var o = new Expression(new Name(TRUTH_VALUE, "O"));
        var p = new Expression(new Name(TRUTH_VALUE, "P"));
        var q = new Expression(new Name(TRUTH_VALUE, "Q"));
        var r = new Expression(new Name(TRUTH_VALUE, "R"));
        var s = new Expression(new Name(TRUTH_VALUE, "S"));
        var t = new Expression(new Name(TRUTH_VALUE, "T"));
        var u = new Expression(new Name(TRUTH_VALUE, "U"));
        var v = new Expression(new Name(TRUTH_VALUE, "V"));
        var w = new Expression(new Name(TRUTH_VALUE, "W"));
        var x = new Expression(new Name(TRUTH_VALUE, "X"));
        var y = new Expression(new Name(TRUTH_VALUE, "Y"));
        var z = new Expression(new Name(TRUTH_VALUE, "Z"));

        var aa = new Expression(new Name(INDIVIDUAL, "a"));
        var bb = new Expression(new Name(INDIVIDUAL, "b"));
        var cc = new Expression(new Name(INDIVIDUAL, "c"));
        var dd = new Expression(new Name(INDIVIDUAL, "d"));
        var ee = new Expression(new Name(INDIVIDUAL, "e"));
        var ff = new Expression(new Name(INDIVIDUAL, "f"));
        var gg = new Expression(new Name(INDIVIDUAL, "g"));

        MentalState.Initialize(new Expression[]{
            a,
            b,
            new Expression(NOT, c),
            new Expression(NOT, d),
            new Expression(TRULY, e),
            new Expression(NOT, new Expression(NOT, f)),
            new Expression(AND, g, h),
            new Expression(NOT, new Expression(OR, j, k)),
            new Expression(IF, l, a),
            new Expression(RED, aa),
            new Expression(ROUND, aa),
            new Expression(ROUND, bb),
            new Expression(ALL, FRUIT, RED),
            new Expression(TOMATO, cc),
            new Expression(VERY, m),
            new Expression(IDENTITY, BOB, SELF),
            new Expression(YIELDS, dd, ee),
            new Expression(CONVERSE, YIELDS, ff, gg),
            new Expression(SINCE, n, o),
            new Expression(GOOD, p),
            new Expression(GOOD, new Expression(NOT, q)),
            new Expression(VERY, new Expression(AND, r, s)),
            new Expression(ALL, PERSON, new Expression(KNOW, t)),
            new Expression(PERSON, ALICE),
        });

        StartCoroutine(LogBasesStream(MentalState, VERUM));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, FALSUM)));
        StartCoroutine(LogBasesStream(MentalState, a));
        StartCoroutine(LogBasesStream(MentalState, new Expression(TRULY, a)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(NOT, a))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(OR, a, b)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(OR, b, a)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(AND, c, d))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(AND, a, b)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(AND, b, a)));
        StartCoroutine(LogBasesStream(MentalState, e));
        StartCoroutine(LogBasesStream(MentalState, f));
        StartCoroutine(LogBasesStream(MentalState, g));
        StartCoroutine(LogBasesStream(MentalState, h));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, j)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, k)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(IF, a, a)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(IF, a, new Expression(AND, a, b))));
        StartCoroutine(LogBasesStream(MentalState, l));
        StartCoroutine(LogBasesStream(MentalState, new Expression(SOME, RED, ROUND)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(RED, cc)));
        StartCoroutine(LogBasesStream(MentalState, m));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(VERY, c))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(IDENTITY, ALICE, ALICE)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(IDENTITY, ALICE, BOB))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(IDENTITY, SELF, BOB)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(CONVERSE, YIELDS, ee, dd)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(YIELDS, gg, ff)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, a)));
        StartCoroutine(LogBasesStream(MentalState, n));
        StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, o)));
        StartCoroutine(LogBasesStream(MentalState, o));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(GOOD, q))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(GOOD, new Expression(NOT, p)))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(OR, m, a))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, r)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, s)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, s)));
        StartCoroutine(LogBasesStream(MentalState, t));
    }

    public static string ValueString(List<int> value) {
        StringBuilder s = new StringBuilder();

        if (value == null || value.Count == 0) {
            return "0";
        }

        s.Append(value[0]);

        for (int i = 1; i < value.Count; i++) {
            s.Append(" + " + value[i] + "*ω^" + i);
        }
        return s.ToString();
    }

    public static void TestConvertToValue(Expression e) {
        var eAsNumber = MentalState.ConvertToValue(e);

        Debug.Log("u(" + e + ") = " + ValueString(eAsNumber));
    }

    public static IEnumerator Assert(MentalState m, Expression content, Expression speaker) {
        yield return new WaitForSeconds(5);
        m.StartCoroutine(m.ReceiveAssertion(content, speaker));
    }

    public static IEnumerator TestEstimateValueFor(MentalState m, List<Expression> goods, Expression goal) {
        var estimates = new List<Expression>();
        var done = new Container<bool>(false);
        m.StartCoroutine(m.EstimateValueFor(goal, goods, estimates, done));

        while (!done.Item) {
            yield return null;
        }

        var str = new StringBuilder();
        str.Append("u(" + goal + ") is estimated by: {\n");
        foreach (var estimate in estimates) {
            str.Append("\t" + estimate + "\n");
        }
        str.Append("}");

        Log(str.ToString());
    }

    public static IEnumerator TestDecideCurrentPlan(MentalState m) {
        var plan = new List<Expression>();
        var done = new Container<bool>(false);

        m.StartCoroutine(m.DecideCurrentPlan(plan, done));

        while (!done.Item) {
            yield return null;
        }

        var planString = new StringBuilder();
        planString.Append("<");
        foreach (var step in plan) {
            planString.Append(step + ", ");
        }
        if (plan.Count > 0) {
            planString.Remove(planString.Length - 2, 2);
        }
        planString.Append(">");

        Debug.Log(planString.ToString());
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
        return a + ", " + b + ": " + SubstitutionString(a.Unify(b));
    }

    public static IEnumerator LogBasesStream(MentalState m, Expression e, ProofType pt = Proof, float timeout = -1) {
        var result = new ProofBases();
        var done = new Container<bool>(false);
        
        var startTime = Time.time;
        var proofRoutine = m.StreamProofs(result, e, done, pt);
        m.StartCoroutine(proofRoutine);

        var waitingString = "waiting for '" + e + "' to be proved...";
        var foundResult = "found partial result. go to LogBasesStream() to see it.";
        var isProvedByString = "'" + e + "'" + " is " + (pt == Proof ? "proved" : "planned") + " by: ";
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

            if (m.FrameTimer.FrameDuration >= MentalState.TIME_BUDGET) {
                yield return null;
            }
        }
        Log(isProvedByString + result);
        yield break;
    }
}
