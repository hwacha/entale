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
        var h = new Expression(new Name(INDIVIDUAL,  "h"));
        var j = new Expression(new Name(INDIVIDUAL,  "j"));
        var k = new Expression(new Name(INDIVIDUAL,  "k"));
        var l = new Expression(new Name(INDIVIDUAL,  "l"));
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

        var aa = new Expression(new Name(TRUTH_VALUE, "AA"));
        var bb = new Expression(new Name(TRUTH_VALUE, "BB"));
        var cc = new Expression(new Name(TRUTH_VALUE, "CC"));
        var dd = new Expression(new Name(TRUTH_VALUE, "DD"));
        var ee = new Expression(new Name(TRUTH_VALUE, "EE"));
        var ff = new Expression(new Name(TRUTH_VALUE, "FF"));
        var gg = new Expression(new Name(TRUTH_VALUE, "GG"));
        var hh = new Expression(new Name(TRUTH_VALUE, "HH"));
        var ii = new Expression(new Name(TRUTH_VALUE, "II"));
        var jj = new Expression(new Name(TRUTH_VALUE, "JJ"));
        var kk = new Expression(new Name(TRUTH_VALUE, "KK"));
        var ll = new Expression(new Name(TRUTH_VALUE, "LL"));
        var mm = new Expression(new Name(TRUTH_VALUE, "MM"));
        var nn = new Expression(new Name(TRUTH_VALUE, "NN"));
        var oo = new Expression(new Name(TRUTH_VALUE, "OO"));
        var pp = new Expression(new Name(INDIVIDUAL, "pp"));
        var qq = new Expression(new Name(INDIVIDUAL, "qq"));

        MentalState.Initialize(new Expression[]{
            a,
            b,
            new Expression(NOT, c),
            new Expression(NOT, d),
            new Expression(KNOW, e, ALICE),
            new Expression(GOOD, new Expression(NOT, f)),
            new Expression(RED, ALICE),
            new Expression(BLUE, ALICE),
            new Expression(NOT, new Expression(GREEN, ALICE)),
            new Expression(IDENTITY, h, j),
            new Expression(NOT, new Expression(IDENTITY, k, l)),
            new Expression(BANANA, h),
            new Expression(TOMATO, j),
            new Expression(NOT, new Expression(FRUIT, k)),
            new Expression(VERY, new Expression(VERY, m)),
            new Expression(KNOW, new Expression(KNOW, m, h), j),
            new Expression(SEE, new Expression(SEE, m, h), j),
            new Expression(INFORMED, new Expression(INFORMED, m, h), j),
            new Expression(MAKE, new Expression(MAKE, m, h), j),
            new Expression(OMEGA, VERY, n),
            new Expression(AND, o, p),
            new Expression(NOT, new Expression(OR, r, s)),
            new Expression(IF, t, a),
            new Expression(ALL, PEPPER, SPICY),
            new Expression(PEPPER, h),
            new Expression(NOT, new Expression(SPICY, pp)),
            new Expression(APPLE, j),
            new Expression(NOT, new Expression(SOME, APPLE, SPICY)),
            new Expression(SPICY, qq),
            new Expression(ALL, BLUE, new Expression(KNOW, u)),
            new Expression(SINCE, w, x),
            new Expression(IF, new Expression(MAKE, o, SELF), new Expression(DF, MAKE, o, SELF)),
            new Expression(OR, aa, bb),
            new Expression(NOT, aa),
            new Expression(OR, cc, dd),
            new Expression(NOT, dd),
            new Expression(NOT, new Expression(AND, ee, ff)),
            ee,
            new Expression(NOT, new Expression(AND, gg, hh)),
            hh,
            new Expression(TRULY, new Expression(TRULY, ii)),
            new Expression(NOT, new Expression(NOT, new Expression(NOT, new Expression(NOT, jj)))),
            new Expression(NOT, new Expression(DF, KNOW, a, BOB)),
            new Expression(NOT, new Expression(PAST, kk)),
            new Expression(IF, mm, ll),
            new Expression(NOT, mm),
            new Expression(NOT, new Expression(STAR, oo)),
        });

        // StartCoroutine(LogBasesStream(MentalState, VERUM));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, FALSUM)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(IDENTITY, SELF, SELF)));
        // StartCoroutine(LogBasesStream(MentalState, a));
        // var trulyA = a;
        // for (int i = 0; i < 5; i++) {
        //     trulyA = new Expression(TRULY, trulyA);
        // }
        // StartCoroutine(LogBasesStream(MentalState, trulyA));
        // var notNotA = a;
        // for (int i = 0; i < 6; i++) {
        //     notNotA = new Expression(NOT, notNotA);
        // }
        // StartCoroutine(LogBasesStream(MentalState, notNotA));
        // StartCoroutine(LogBasesStream(MentalState, notNotA.GetArgAsExpression(0)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(STAR, z)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(IDENTITY, ALICE, BOB))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(VERY, c))));
        // StartCoroutine(LogBasesStream(MentalState, e));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(DF, KNOW, e, ALICE)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, a)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(GOOD, f))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(AND, a, b)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(OR, c, d))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(OR, a, b)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(AND, c, d))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(SOME, RED, BLUE)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(ALL, RED, GREEN))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(IF,
        //         new Expression(SOME, BLUE, YELLOW),
        //         new Expression(YELLOW, ALICE))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(IF, a, b)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(IF, a, g)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(IDENTITY, j, h)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(IDENTITY, l, k))));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(FRUIT, h)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(FRUIT, j)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(BANANA, k))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(TOMATO, k))));

        // StartCoroutine(LogBasesStream(MentalState, m));

        // var veryN = n;
        // for (int i = 0; i < 15; i++) {
        //     veryN = new Expression(VERY, veryN);
        // }
        // StartCoroutine(LogBasesStream(MentalState, veryN));
        // // @Note higher order omega logic is still broken
        
        // StartCoroutine(LogBasesStream(MentalState, o));
        // StartCoroutine(LogBasesStream(MentalState, p));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, r)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, s)));

        // StartCoroutine(LogBasesStream(MentalState, t));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(SPICY, h)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(SWEET, j)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(SPICY, j))));

        // StartCoroutine(LogBasesStream(MentalState, u));
        // StartCoroutine(LogBasesStream(MentalState, v));
        
        // StartCoroutine(LogBasesStream(MentalState, w));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(PAST, x)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(KNOW, a, SELF)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(KNOW, y, SELF))));

        // StartCoroutine(LogBasesStream(MentalState, o, Plan));

        // StartCoroutine(LogBasesStream(MentalState, bb));
        // StartCoroutine(LogBasesStream(MentalState, cc));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, ff)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, gg)));

        // StartCoroutine(LogBasesStream(MentalState, ii));
        // StartCoroutine(LogBasesStream(MentalState, jj));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(KNOW, a, BOB))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, kk)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, ll)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(IF, nn, a))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(STAR, c)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(oo)));

        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(PEPPER, pp))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(APPLE, qq))));
    }

    public static void TestInferenceRule(InferenceRule rule, Expression e) {
        var premises = rule.Apply(e);

        var s = new StringBuilder();

        s.Append(rule + " applied to " + e + " yields ");

        if (premises == null) {
            s.Append("NONE\n");
            Debug.Log(s.ToString());
            return;
        }

        foreach (var premise in premises) {
            s.Append(premise + ", ");
        }
        s.Append("\n");
        Debug.Log(s.ToString());
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
            yield return null;
        }
        Log(isProvedByString + result);
        yield break;
    }
}
