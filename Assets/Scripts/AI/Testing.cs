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
        var h = new Expression(new Name(TRUTH_VALUE,  "h"));
        var j = new Expression(new Name(TRUTH_VALUE,  "j"));
        var k = new Expression(new Name(TRUTH_VALUE,  "k"));
        var l = new Expression(new Name(TRUTH_VALUE,  "l"));
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

        MentalState.Initialize(new Expression[]{
            new Expression(OMEGA, VERY, a),
            new Expression(OMEGA, new Expression(DF, KNOW, new Empty(TRUTH_VALUE), ALICE), b),
            new Expression(OMEGA, new Expression(OMEGA, VERY), c),
            d,
            new Expression(NOT, e),
        });

        var va = a;

        for (int i = 0; i < 5; i++) {
            StartCoroutine(LogBasesStream(MentalState, va));
            va = new Expression(VERY, va);
        }

        StartCoroutine(LogBasesStream(MentalState, new Expression(OMEGA, TRULY, d)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(OMEGA, new Expression(AND, d), d)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(OMEGA, new Expression(OMEGA, TRULY), d)));

        // this should be working
        StartCoroutine(LogBasesStream(MentalState, new Expression(OMEGA, new Expression(KNOW, new Empty(TRUTH_VALUE), SELF), d)));

        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(OMEGA, NOT, d))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(OMEGA, TRULY, e))));

        StartCoroutine(LogBasesStream(MentalState, new Expression(IF, new Expression(OMEGA, TRULY, f), f)));

        // var bb = b;
        // for (int i = 0; i < 5; i++) {
        //     StartCoroutine(LogBasesStream(MentalState, bb));
        //     bb = new Expression(DF, KNOW, bb, ALICE);
        // }
        
        // var ovb = b;
        // for (int i = 0; i < 5; i++) {
        //     ovb = new Expression(VERY, ovb);
            
        // }
        // for (int i = 0; i < 5; i++) {
        //     ovb = new Expression(OMEGA, VERY, ovb);
        //     StartCoroutine(LogBasesStream(MentalState, ovb));
        // }

        // var vc = c;
        // for (int i = 0; i < 5; i++) {
        //     StartCoroutine(LogBasesStream(MentalState, vc));
        //     vc = new Expression(VERY, vc);
        // }

        // var oc = c;
        // for (int i = 0; i < 5; i++) {
        //     StartCoroutine(LogBasesStream(MentalState, oc));
        //     oc = new Expression(OMEGA, VERY, oc);
        // }

        // var ovc = c;
        // for (int i = 0; i < 5; i++) {
        //     ovc = new Expression(VERY, ovc);
            
        // }
        // for (int i = 0; i < 5; i++) {
        //     ovc = new Expression(OMEGA, VERY, ovc);
        //     StartCoroutine(LogBasesStream(MentalState, ovc));
        // }

        // var voc = c;
        // for (int i = 0; i < 5; i++) {
        //     voc = new Expression(OMEGA, VERY, voc);
        // }
        // for (int i = 0; i < 5; i++) {
        //     voc = new Expression(VERY, voc);
        //     StartCoroutine(LogBasesStream(MentalState, voc));
        // }
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
