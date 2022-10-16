using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;
using static ProofType;
using static InferenceRule;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;

public class Testing : MonoBehaviour {
    private FrameTimer FrameTimer;

    public MentalState MentalState;

    Thread thread;

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
            new Expression(IDENTITY, aa, bb),
            new Expression(IDENTITY, bb, cc),
            new Expression(ROUND, aa),
            new Expression(RED, aa),
            // TODO fix unwanted infinite loop
        });

        // Expression nv = VERUM;

        // for (int i = 0; i < 10_000; i++) {
        //     nv = new Expression(NOT, nv);
        // }

        thread = new Thread(() => {
            // PrintProofs(MentalState, new Expression(IDENTITY, aa, aa));
            // PrintProofs(MentalState, new Expression(IDENTITY, aa, bb));
            // PrintProofs(MentalState, new Expression(IDENTITY, bb, aa));
            // PrintProofs(MentalState, new Expression(IDENTITY, aa, cc));
            PrintProofs(MentalState, new Expression(ROUND, cc));
            PrintProofs(MentalState, new Expression(SOME, RED, ROUND));
            PrintProofs(MentalState, new Expression(RED, XE));
        });

        thread.Start();
    }

    void OnApplicationQuit() {
        thread.Abort();
    }

    public static void PrintProofs(MentalState m, Expression e) {
        Debug.Log(e + " is proved by " + m.GetProofs(e));
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

    // public static IEnumerator Assert(MentalState m, Expression content, Expression speaker) {
    //     yield return new WaitForSeconds(5);
    //     m.StartCoroutine(m.ReceiveAssertion(content, speaker));
    // }

    // public static IEnumerator TestEstimateValueFor(MentalState m, List<Expression> goods, Expression goal) {
    //     var estimates = new List<Expression>();
    //     var done = new Container<bool>(false);
    //     m.StartCoroutine(m.EstimateValueFor(goal, goods, estimates, done));

    //     while (!done.Item) {
    //         yield return null;
    //     }

    //     var str = new StringBuilder();
    //     str.Append("u(" + goal + ") is estimated by: {\n");
    //     foreach (var estimate in estimates) {
    //         str.Append("\t" + estimate + "\n");
    //     }
    //     str.Append("}");

    //     Log(str.ToString());
    // }

    // public static IEnumerator TestDecideCurrentPlan(MentalState m) {
    //     var plan = new List<Expression>();
    //     var done = new Container<bool>(false);

    //     m.StartCoroutine(m.DecideCurrentPlan(plan, done));

    //     while (!done.Item) {
    //         yield return null;
    //     }

    //     var planString = new StringBuilder();
    //     planString.Append("<");
    //     foreach (var step in plan) {
    //         planString.Append(step + ", ");
    //     }
    //     if (plan.Count > 0) {
    //         planString.Remove(planString.Length - 2, 2);
    //     }
    //     planString.Append(">");

    //     Debug.Log(planString.ToString());
    // }

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
}
