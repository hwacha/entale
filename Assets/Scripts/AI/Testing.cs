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

        var es = new SortedSet<Expression>{
            new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  20))),
                ALICE), new Expression(new Parameter(TIME, 25))),
            new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  10))),
                EVAN), new Expression(new Parameter(TIME, 28))),
            new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  10))),
                BOB), new Expression(new Parameter(TIME, 20))),
            new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  15))),
                SELF), new Expression(new Parameter(TIME, 40))),
        };

        // suppose we're trying to prove when(red(self), 30)
        var bottom = new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  0))),
                new Expression(new Bottom(INDIVIDUAL))), new Expression(new Parameter(TIME, 0)));

        var top = new Expression(WHEN, new Expression(KNOW,
                new Expression(WHEN, new Expression(RED, SELF), new Expression(new Parameter(TIME,  30))),
                new Expression(new Top(INDIVIDUAL))), new Expression(new Parameter(TIME, 30)));

        var timespan = es.GetViewBetween(bottom, top);

        foreach (var x in timespan) {
            Debug.Log(x);
        }

        MentalState.Initialize(new Expression[]{
            new Expression(SEE, new Expression(RED, SELF))
        });
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
            if (startTime + timeout <= Time.time) {
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
