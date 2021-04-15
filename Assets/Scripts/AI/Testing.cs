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
        Debug.Log(SemanticType.Geach(TRUTH_VALUE, TRUTH_VALUE));
        Debug.Log(SemanticType.Geach(INDIVIDUAL, TRUTH_VALUE));

        Debug.Log(SemanticType.Geach(INDIVIDUAL, TRUTH_FUNCTION));
        Debug.Log(SemanticType.Geach(TRUTH_VALUE, TRUTH_FUNCTION));

        Debug.Log(SemanticType.Geach(INDIVIDUAL, QUANTIFIER_PHRASE));

        FrameTimer = gameObject.GetComponent<FrameTimer>();

        // DON'T COMMENT ABOVE THIS LINE
        MentalState.FrameTimer = FrameTimer;

        MentalState.Initialize(new Expression[]{
            new Expression(EVIDENTIALIZER,
                new Expression(RED, SELF), // the basic sentence
                new Expression(new Parameter(TIME, 10)), // the time the sentence is true
                TRULY, // whether the sentence is true or false
                new Expression(KNOW_TENSED, new Empty(TRUTH_VALUE), BOB, new Expression(new Parameter(TIME, 20)))), // source/knower

            new Expression(EVIDENTIALIZER,
                new Expression(RED, SELF),
                new Expression(new Parameter(TIME, 30)),
                TRULY,
                SEE),

            new Expression(EVIDENTIALIZER,
                new Expression(GREEN, SELF),
                new Expression(new Parameter(TIME, 10)),
                TRULY,
                new Expression(KNOW_TENSED, new Empty(TRUTH_VALUE), BOB, new Expression(new Parameter(TIME, 20)))),

            new Expression(EVIDENTIALIZER,
                new Expression(GREEN, SELF),
                new Expression(new Parameter(TIME, 30)),
                NOT,
                SEE),

            new Expression(EVIDENTIALIZER,
                new Expression(AT, ALICE, BOB),
                new Expression(new Parameter(TIME, 40)),
                TRULY,
                SEE)
        });

        MentalState.Timestamp = 50;

        StartCoroutine(LogBasesStream(MentalState, new Expression(RED, SELF)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(RED, SELF))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(NOT, new Expression(RED, SELF)))));

        StartCoroutine(LogBasesStream(MentalState, new Expression(GREEN, SELF)));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(GREEN, SELF))));
        StartCoroutine(LogBasesStream(MentalState, new Expression(NOT, new Expression(NOT, new Expression(NOT, new Expression(GREEN, SELF))))));

        StartCoroutine(LogBasesStream(MentalState, new Expression(AT, ALICE, BOB)));

        StartCoroutine(LogBasesStream(MentalState, new Expression(SEE, new Expression(RED, SELF))));
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
