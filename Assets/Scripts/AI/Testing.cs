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

        MentalState.Initialize(new Expression[]{
            // new Expression(OMEGA, VERY, new Expression(GOOD, a)),
            // new Expression(OMEGA, new Expression(OMEGA, VERY), b)
            // new Expression(KNOW, a, ALICE),
            // new Expression(IF, c, b),
            // b,
            // new Expression(IF, new Expression(KNOW, new Expression(RED, SELF), CHARLIE), b),
            // new Expression(ALL, PERSON, new Expression(KNOW, new Expression(GREEN, SELF))),
            // new Expression(ALL, RED, new Expression(GEACH_E_TRUTH_FUNCTION, NOT, BLUE)),
            // new Expression(RED, ALICE),
            // new Expression(NOT, new Expression(GREEN, BOB)),
            // new Expression(NOT, new Expression(YELLOW, BOB)),
            new Expression(OMEGA,
                new Expression(GEACH_T_QUANTIFIER_PHRASE,
                    new Expression(ALL, PERSON), KNOW),
                new Expression(GREEN, SELF)),
            new Expression(PERSON, ALICE),
            new Expression(GEACH_T_QUANTIFIER_PHRASE, new Expression(ALL, PERSON), KNOW, new Expression(GREEN, SELF)),

            // new Expression(ABLE, new Expression(BLUE, SELF), SELF),
            new Expression(OMEGA, VERY, new Expression(GOOD, a)),
            // new Expression(OMEGA, new Expression(OMEGA, VERY), new Expression(GOOD, b)),// Mental State line 809
            // new Expression(OMEGA, new Expression(OMEGA, new Expression(OMEGA, VERY)), new Expression(GOOD, c)),
            // new Expression(AND, d, e)
        });

        // StartCoroutine(LogBasesStream(MentalState, new Expression(GOOD, a)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(GOOD, a))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(VERY, new Expression(GOOD, a)))));

        // StartCoroutine(LogBasesStream(MentalState, b));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, b)));
        // // this is failing because: the search would need to check the LINKS
        // // and not just the knowledge base for omega sentences.
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(VERY, b))));

        // var orLeftIntroduction = new InferenceRule("OR+L", e => e.HeadedBy(OR),
        //     e => new List<Expression>{
        //         e.GetArgAsExpression(0)
        //     });
        // var orRightIntroduction = new InferenceRule("OR+R", e => e.HeadedBy(OR),
        //     e => new List<Expression>{
        //         e.GetArgAsExpression(1)
        //     });
        // var andIntroduction = new InferenceRule("AND+", e => e.HeadedBy(AND),
        //     e => new List<Expression>{
        //         e.GetArgAsExpression(0), e.GetArgAsExpression(1)
        //     });

        // TestInferenceRule(orLeftIntroduction, new Expression(OR, a, b)); // A
        // TestInferenceRule(orLeftIntroduction, new Expression(AND, a, b)); // null
        // TestInferenceRule(orLeftIntroduction, a); // null

        // TestInferenceRule(orRightIntroduction, new Expression(OR, a, b)); // B
        // TestInferenceRule(orRightIntroduction, new Expression(AND, a, b)); // null
        // TestInferenceRule(orRightIntroduction, a); // null

        // TestInferenceRule(andIntroduction, new Expression(OR, a, b)); // null
        // TestInferenceRule(andIntroduction, new Expression(AND, a, b)); // A, B
        // TestInferenceRule(andIntroduction, a); // A, B

        // var knowledgeElim = new InferenceRule("K-", e => e.Equals(a),
        //     e => new List<Expression>{
        //         new Expression(KNOW, a, BOB)
        //     });

        // TestInferenceRule(knowledgeElim, a);

        // StartCoroutine(LogBasesStream(MentalState, a));
        // StartCoroutine(LogBasesStream(MentalState, c));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(RED, SELF)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(GREEN, SELF)));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(BLUE, SELF), Plan));

        // StartCoroutine(LogBasesStream(MentalState, d));
        // StartCoroutine(LogBasesStream(MentalState, e));

        // StartCoroutine(LogBasesStream(MentalState, new Expression(GOOD, a)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(GOOD, b)));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(GOOD, c)));

        StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(GOOD, a))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(GOOD, b))));
        // StartCoroutine(LogBasesStream(MentalState, new Expression(VERY, new Expression(GOOD, c))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(VERY, new Expression(VERY, new Expression(GOOD, a)))));
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(VERY, new Expression(VERY, new Expression(GOOD, b))))); // LINE 831
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(VERY, new Expression(VERY, new Expression(GOOD, c)))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(OMEGA, VERY, new Expression(OMEGA, VERY, new Expression(GOOD, b)))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(OMEGA, VERY, new Expression(OMEGA, VERY, new Expression(GOOD, c)))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(VERY, new Expression(VERY,
        //         new Expression(OMEGA, VERY, new Expression(OMEGA, VERY, new Expression(GOOD, b)))))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(VERY,
        //         new Expression(OMEGA, VERY,
        //             new Expression(OMEGA, new Expression(OMEGA, VERY),
        //                 new Expression(GOOD, c))))));

        StartCoroutine(LogBasesStream(MentalState,
            new Expression(KNOW, new Expression(GREEN, SELF), ALICE)));
        
        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(NOT, new Expression(BLUE, ALICE))));

        // StartCoroutine(LogBasesStream(MentalState,
        //     new Expression(SOME,
        //         new Expression(GEACH_E_TRUTH_FUNCTION, NOT, GREEN),
        //         new Expression(GEACH_E_TRUTH_FUNCTION, NOT, YELLOW))));

        // every(person, knows(A)), person(x) => knows(A, x) => A
        
        // omega(F, P) => F(P)
        // M |- omega(F, P) -> X => M |- F(X)
        
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
