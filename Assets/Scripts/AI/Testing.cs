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

        MentalState.Initialize(new Expression[]{
            new Expression(KNOW, new Expression(RED, ALICE), BOB),
            // new Expression(GOOD, p),
            // new Expression(GOOD, new Expression(NOT, new Expression(VERY, p))),
            // new Expression(ABLE, p, SELF),
            // new Expression(ABLE, new Expression(VERY, p), SELF),
        });

        StartCoroutine(LogBasesStream(MentalState, new Expression(RED, ALICE)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(KNOW, new Expression(RED, ALICE), BOB)));

        // StartCoroutine(LogBasesStream(MentalState, p, Plan));
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
