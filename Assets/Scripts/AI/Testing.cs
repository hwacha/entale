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

        var a = new Expression(new Name(TRUTH_VALUE, "A"));
        var b = new Expression(new Name(TRUTH_VALUE, "B"));
        var c = new Expression(new Name(TRUTH_VALUE, "C"));

        var d = new Expression(new Name(TRUTH_VALUE, "D"));
        var e = new Expression(new Name(TRUTH_VALUE, "E"));
        var f = new Expression(new Name(TRUTH_VALUE, "F"));
        var g = new Expression(new Name(TRUTH_VALUE, "G"));
        
        // var item1 = new Expression(new Name(INDIVIDUAL, "item1"));
        // var item2 = new Expression(new Name(INDIVIDUAL, "item2"));
        
        // 
        // MentalState.Initialize(new Expression[]{
        //     new Expression(IF, p, q),
        //     new Expression(IF, q, r),
        //     new Expression(IF, p, r),

        //     new Expression(IF, a, c),
        //     new Expression(IF, b, c),

        //     new Expression(IF, d, e),
        //     new Expression(IF, d, f),
        //     new Expression(IF, e, g),
        //     new Expression(IF, f, g),
        // });

        // var goods = new List<Expression>{p, q, r, a, b, c, d, e, f, g};
        
        Debug.Log(MentalState.Conjunctify(new List<Expression>{a, b, c, d, e, f, g, p, q, r}));

        // StartCoroutine(TestEstimateValueFor(MentalState, goods, p));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, q));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, r));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, a));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, b));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, c));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, d));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, e));
        // StartCoroutine(TestEstimateValueFor(MentalState, goods, f));

        // MentalState.Initialize(new Expression[]{
        //     new Expression(IDENTITY, item2, item1),
        //     new Expression(GOOD, p),
        //     new Expression(VERY, new Expression(GOOD, new Expression(NOT, q))),
        //     new Expression(OMEGA, VERY, new Expression(GOOD, r)),
        //     new Expression(VERY, new Expression(OMEGA, VERY, new Expression(OMEGA, new Expression(OMEGA, VERY), new Expression(GOOD, r)))),
        //     new Expression(VERY, new Expression(VERY, new Expression(VERY, new Expression(GOOD, new Expression(RED, SELF))))),
        //     new Expression(VERY, new Expression(OMEGA, VERY, new Expression(GOOD, new Expression(OR, a, b)))),
        //     new Expression(SEE, new Expression(RED, ALICE), SELF),
        //     new Expression(SEE, new Expression(TREE, ALICE), SELF),
        //     new Expression(IF, new Expression(BLUE, ALICE), new Expression(RED, ALICE)),
        // });

        // StartCoroutine(LogBasesStream(MentalState, new Expression(SOME, TREE, RED)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(BLUE, ALICE)));
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
