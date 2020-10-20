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
using Basis = System.Collections.Generic.KeyValuePair<System.Collections.Generic.List<Expression>, System.Collections.Generic.Dictionary<Variable, Expression>>;

public class Testing : MonoBehaviour {
    private FrameTimer FrameTimer;

    public MentalState MentalState;

    private IEnumerator TestRoutine() {
        Container<bool> done = new Container<bool>(false);
        StartCoroutine(CalleeRoutine(done));
        while (!done.Item) {
            yield return new WaitForSeconds(1);
        }
        Debug.Log("done.");
        yield break;
    }

    private IEnumerator CalleeRoutine(Container<bool> done) {
        float timeBudget = 0.032f;
        int counter = 0;
        while (counter < 100000000) {
            while (FrameTimer.FrameDuration < timeBudget) {
                counter++;    
            }
            Log(counter);
            yield return null;
        }
        done.Item = true;
        yield break;
    }

    private IEnumerator TestAssertion() {
        StartCoroutine(LogBases(MentalState, new Expression(RED, ALICE)));
        yield return new WaitForSeconds(2);

        StartCoroutine(LogBases(MentalState,
            new Expression(NOT, new Expression(PERCEIVE, SELF,
                new Expression(NOT, new Expression(RED, ALICE))))));

        MentalState.StartCoroutine(MentalState.Assert(
            new Expression(PERCEIVE, SELF,
                new Expression(NOT, new Expression(RED, ALICE)))));
        yield return new WaitForSeconds(2);

        // StartCoroutine(LogBases(MentalState, new Expression(RED, ALICE)));
        // yield return new WaitForSeconds(2);
        // StartCoroutine(LogBases(MentalState, new Expression(NOT, new Expression(RED, ALICE))));
        // yield return new WaitForSeconds(2);
        yield break;
    }

    void Start() {
        FrameTimer = gameObject.GetComponent<FrameTimer>();

        // DON'T COMMENT ABOVE THIS LINE

        // Log(FrameTimer);
        // Log("Testing coroutines.");
        // StartCoroutine(TestRoutine());

        // Log("Running tests from AI/Testing.cs. " +
        //     "To turn these off, " +
        //     "deactivate the 'AITesting' object in the heirarchy.");

        // Log("SEMANTIC TYPES: ");
        // Log("testing constructors.");
        // Log("individual: " + INDIVIDUAL);
        // Log("truth value: " + TRUTH_VALUE);
        // Log("predicate: " + PREDICATE);
        // Log("2-place relation: " + RELATION_2);
        // Log("Testing semantic type partial application predicate.");
        // Log(TRUTH_VALUE.IsPartialApplicationOf(TRUTH_VALUE));
        // Log(TRUTH_VALUE.IsPartialApplicationOf(PREDICATE));
        // Log(TRUTH_VALUE.IsPartialApplicationOf(RELATION_2));
        // Log(PREDICATE.IsPartialApplicationOf(PREDICATE));
        // Log(PREDICATE.IsPartialApplicationOf(RELATION_2));
        // Log(PREDICATE.IsPartialApplicationOf(INDIVIDUAL_TRUTH_RELATION));
        // Log(!RELATION_2.IsPartialApplicationOf(PREDICATE));
        // Log(!INDIVIDUAL_TRUTH_RELATION.IsPartialApplicationOf(PREDICATE));

        // Log("testing removal");
        // Log("@TODO");

        // Log("EXPRESSIONS: ");

        // Log("word constructors: ");
        // Log("alice: " + ALICE);
        // Log("bob: " + BOB);
        // Log("charlie: " + CHARLIE);
        // Log("x: " + XE);
        // Log("y: " + YE);
        // Log("z: " + ZE);
        // Log("verum: " + VERUM);
        // Log("falsum: " + FALSUM);
        // Log("S: " + ST);
        // Log("T: " + TT);
        // Log("red: " + RED);
        // Log("blue: " + BLUE);
        // Log("F: " + FET);
        // Log("G: " + GET);
        // Log("=: " + IDENTITY);
        // Log("at: " + AT);
        // Log("R: " + REET);

        // Log("phrase constructors: ");
        // Log(Verbose(new Expression(RED, ALICE)));
        // Log(Verbose(new Expression(BLUE, BOB)));
        // Log(Verbose(new Expression(IDENTITY, ALICE, ALICE)));
        // Log(Verbose(new Expression(AT, ALICE, BOB)));
        // Log(Verbose(new Expression(IDENTITY, ALICE)));
        // Log(Verbose(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB)));
        // Log(Verbose(new Expression(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB), ALICE)));

        // try {
        //     Log("Failed to catch error: " + Verbose(new Expression(RED, ALICE, BOB)));
        // } catch (ArgumentException e) {
        //     Log("Got expected error: " + e);
        // }

        // try {
        //     Log("Failed to catch error: " + new Expression(IDENTITY, ALICE, BOB, CHARLIE));
        // } catch (ArgumentException e) {
        //     Log("Got expected error: " + e);
        // }

        // try {
        //     Log("Failed to catch error: " + new Expression(new Expression(IDENTITY, ALICE), BOB, CHARLIE));
        // } catch (ArgumentException e) {
        //     Log("Got expected error: " + e);
        // }
        
        // Log("Deictic Constructor");
        // Deictic thatEmpty = new Deictic(THAT, new GameObject());
        // Log(Verbose(thatEmpty));
        
        // Log("equality");
        // Log(thatEmpty.Equals(thatEmpty));
        // Deictic thatTree = new Deictic(THAT, GameObject.Find("tree"));
        // Deictic thatTree2 = new Deictic(THAT, GameObject.Find("tree"));

        // Log(thatTree.Equals(thatTree));
        // Log(thatTree2.Equals(thatTree2));
        // Log(thatTree.Equals(thatTree2));
        // Log(thatTree2.Equals(thatTree));
        // Log(!thatEmpty.Equals(thatTree));
        // Log(!thatEmpty.Equals(thatTree2));

        // Log(NOT.Equals(NOT));

        // Log("Unification");
        // Log(MatchesString(ALICE, ALICE));
        // Log(MatchesString(XE, ALICE));
        // Log(MatchesString(ALICE, XE));
        // Log(MatchesString(XE, XE));
        // Log(MatchesString(ST, new Expression(AT, ALICE, BOB)));

        // Log(MatchesString(new Expression(RED, XE), new Expression(RED, ALICE)));
        // Log(MatchesString(new Expression(RED, ALICE), new Expression(RED, XE)));

        // Log(MatchesString(new Expression(AT, XE, YE), new Expression(AT, ALICE, BOB)));
        // Log(MatchesString(new Expression(AT, XE, XE), new Expression(AT, ALICE, ALICE)));
        // Log(MatchesString(new Expression(AT, XE, XE), new Expression(AT, ALICE, BOB)));

        // Log(MatchesString(new Expression(FET, ALICE), new Expression(AT, ALICE, BOB)));
        // Log(MatchesString(new Expression(FET, BOB), new Expression(AT, ALICE, BOB)));

        // Log(MatchesString(new Expression(AT, ALICE, BOB), new Expression(FET, ALICE)));
        // Log(MatchesString(new Expression(AT, ALICE, BOB), new Expression(FET, BOB)));

        // Log(MatchesString(new Expression(AT, XE, BOB), new Expression(AT, ALICE, YE)));

        // Log(MatchesString(new Expression(FET, XE), new Expression(REET, ALICE, BOB)));
        // Log(MatchesString(new Expression(FET, XE), new Expression(GET, YE)));

        // Log(MatchesString(new Expression(FET, XE), new Expression(ITSELF, REET, XE)));
        // Log(MatchesString(new Expression(ITSELF, REET, XE), new Expression(FET, BOB)));

        // @TODO Test potential bug in mutating expressions
        
        // Testing mental state.
        // Log("Testing mental state.");
        // Log("QUERY");
        // Expression aliceIsRed   = new Expression(RED, ALICE);
        // Expression aliceIsAnApple = new Expression(APPLE, ALICE);
        // Expression bobIsBlue    = new Expression(BLUE, BOB);
        // Expression aliceIsAtBob = new Expression(AT, ALICE, BOB);
        // Expression aliceIsAlice = new Expression(IDENTITY, ALICE, ALICE);
        // Expression bobIsBob     = new Expression(IDENTITY, BOB, BOB);
        // Expression allMacintoshesAreApples = new Expression(ALL, new Expression(new Constant(PREDICATE, "macintosh")), APPLE);
        // Expression allApplesAreRed = new Expression(ALL, APPLE, RED);
        // Expression charlieIsAMacintosh = new Expression(new Expression(new Constant(PREDICATE, "macintosh")), CHARLIE);
        // Expression iCanMakeCharlieBlue = new Expression(ABLE, SELF, new Expression(BLUE, CHARLIE));
        // Expression iSeeCharlieAsRed = new Expression(PERCEIVE, SELF, new Expression(RED, CHARLIE));

        // Expression bobIsRed     = new Expression(RED, BOB);
        // Expression aliceIsBlue  = new Expression(BLUE, ALICE);
        // Expression charlieIsBlue  = new Expression(BLUE, CHARLIE);

        // Expression ifCharlieIsBlueIAmGreen =
        //     new Expression(IF, charlieIsBlue, new Expression(GREEN, SELF));

        // Expression bobIsAtCharlie = new Expression(AT, BOB, CHARLIE);

        // Expression everythingIsSelfIdentical = new Expression(ALL, VEROUS, new Expression(ITSELF, IDENTITY));

        // var whatIseeIsAlwaysTrue = new Expression(ALWAYS, new Expression(PERCEIVE, SELF), TRULY);

        MentalState.FrameTimer = FrameTimer;

        // MentalState.Initialize(
        //     aliceIsRed,
        //     aliceIsAnApple,
        //     bobIsBlue,
        //     aliceIsAtBob,
        //     aliceIsAlice,
        //     bobIsBob,
        //     charlieIsAMacintosh,
        //     allApplesAreRed,
        //     allMacintoshesAreApples,
        //     iCanMakeCharlieBlue,
        //     charlieIsBlue,
        //     iSeeCharlieAsRed,
        //     ifCharlieIsBlueIAmGreen,
        //     bobIsAtCharlie,
        //     everythingIsSelfIdentical,
        //     whatIseeIsAlwaysTrue,
        //     new Expression(BETTER, new Expression(BLUE, SELF), NEUTRAL),
        //     new Expression(BETTER, new Expression(RED, SELF), new Expression(BLUE, SELF)));
        
        // Testing base query
        MentalState.Initialize(new Expression(RED, SELF), new Expression(NOT, new Expression(BLUE, SELF)));
        Log(MentalState.BaseQuery(TensedQueryType.Exact, new Expression(RED, SELF), 0));
        Log(MentalState.BaseQuery(TensedQueryType.Exact, new Expression(RED, SELF), 1));
        Log(MentalState.BaseQuery(TensedQueryType.Exact, new Expression(BLUE, SELF), 0));
        Log(MentalState.BaseQuery(TensedQueryType.Intertial, new Expression(RED, SELF), 0));

        // MentalState.ProofMode = Proof;
        // MentalState.Initialize(
        //     new Expression(ABLE, SELF,  new Expression(AT, SELF, BOB)),
        //     new Expression(IF, new Expression(AT, SELF, BOB), new Expression(PERCEPTUALLY_CLOSED, SELF, new Expression(GREEN, BOB))),
        //     new Expression(PERCEIVE, SELF, new Expression(GREEN, BOB))
        // );

        // MentalState.ProofMode = Plan;
        // StartCoroutine(LogBases(MentalState, new Expression(NOT, new Expression(GREEN, BOB))));
        
        // MentalState.Initialize(
        //     new Expression(PERCEIVE, SELF, new Expression(RED, ALICE))
        // );

        // StartCoroutine(TestAssertion());
        // MentalState.Initialize(
        //     new Expression(AT, ALICE, BOB),
        //     new Expression(NOT, new Expression(RED, ALICE)),
        //     new Expression(BLUE, CHARLIE),
        //     new Expression(GREEN, CHARLIE),
        //     new Expression(BLUE, BOB),
        //     new Expression(AT, BOB, CHARLIE),
        //     new Expression(RED, SELF),
        //     new Expression(Expression.QUANTIFIER_PHRASE_COORDINATOR_2,
        //         AT, new Expression(ALL, RED), new Expression(ALL, GREEN)));
        
        // MentalState.Initialize(new Expression(WHEN,
        //     new Expression(new Vector(INDIVIDUAL, new float[]{(float) 0})),
        //     new Expression(PERCEIVE, SELF, new Expression(RED, SELF))));
        // StartCoroutine(LogBases(MentalState, new Expression(WHEN,
        //     XE,
        //     new Expression(PERCEIVE, SELF, new Expression(RED, SELF)))));

        // StartCoroutine(LogBases(MentalState, new Expression(AT, ALICE, BOB)));

        // StartCoroutine(LogBases(MentalState, new Expression(CONVERSE, AT, BOB, ALICE)));
        // StartCoroutine(LogBases(MentalState, new Expression(CONVERSE, AT, ALICE, BOB)));

        // StartCoroutine(LogBases(MentalState, new Expression(Expression.GEACH_TRUTH_FUNCTION, NOT, RED, ALICE)));
        // StartCoroutine(LogBases(MentalState, new Expression(Expression.GEACH_TRUTH_FUNCTION_2, AND, BLUE, GREEN, CHARLIE)));
        // StartCoroutine(LogBases(MentalState, new Expression(SOME, BLUE,
        //     new Expression(Expression.GEACH_QUANTIFIER_PHRASE, new Expression(SOME, GREEN), AT))));
        // StartCoroutine(LogBases(MentalState, new Expression(AT, SELF, CHARLIE)));

        // StartCoroutine(LogBases(MentalState, aliceIsRed));
        // StartCoroutine(LogBases(MentalState, bobIsBlue));
        // StartCoroutine(LogBases(MentalState, bobIsRed));
        // StartCoroutine(LogBases(MentalState, aliceIsBlue));
        
        // StartCoroutine(LogBases(MentalState, new Expression(BETTER, new Expression(RED, SELF), NEUTRAL)));
        // StartCoroutine(LogBases(MentalState, new Expression(NOT, new Expression(BETTER, NEUTRAL, new Expression(RED, SELF)))));

        // StartCoroutine(LogBases(MentalState, new Expression(RED, CHARLIE)));
        // Log("Double Negation Elimination");
        // Expression notNotAliceIsRed = new Expression(NOT, new Expression(NOT, aliceIsRed));
        // Expression notNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotAliceIsRed));
        // Expression notNotNotNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotNotNotAliceIsRed));
        // StartCoroutine(LogBases(MentalState, notNotAliceIsRed));
        // StartCoroutine(LogBases(MentalState, notNotNotNotAliceIsRed));
        // StartCoroutine(LogBases(MentalState, notNotNotNotNotNotAliceIsRed));
        // StartCoroutine(LogBases(MentalState, new Expression(NOT, new Expression(NOT, bobIsRed))));
        // StartCoroutine(LogBases(MentalState, new Expression(NOT, bobIsBlue)));

        // Log("Disjunction Introduction");
        // StartCoroutine(LogBases(MentalState, new Expression(OR, aliceIsRed, bobIsBlue)));
        // StartCoroutine(LogBases(MentalState, new Expression(OR, aliceIsRed, bobIsRed)));
        // StartCoroutine(LogBases(MentalState, new Expression(OR, aliceIsBlue, bobIsBlue)));
        // StartCoroutine(LogBases(MentalState, new Expression(OR, aliceIsBlue, bobIsRed)));

        // Log("Conjunction Introduction");
        // StartCoroutine(LogBases(MentalState, new Expression(AND, aliceIsRed,  bobIsBlue)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, aliceIsRed,  bobIsRed)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, aliceIsBlue, bobIsBlue)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, aliceIsBlue, bobIsRed)));

        // Expression conjunctionOfDisjunctions = new Expression(AND,
        //         new Expression(OR, aliceIsRed, bobIsBlue),
        //         new Expression(OR, aliceIsAlice, bobIsBob));

        // StartCoroutine(LogBases(MentalState, conjunctionOfDisjunctions));

        // Log("Planning");
        // StartCoroutine(LogBases(MentalState, new Expression(BLUE, CHARLIE)));
        // MentalState.ProofMode = Plan;
        // StartCoroutine(LogBases(MentalState, new Expression(BLUE, CHARLIE)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, new Expression(BLUE, CHARLIE), bobIsBlue)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, bobIsBlue, new Expression(BLUE, CHARLIE))));
        // MentalState.ProofMode = Proof;

        // Log("Formula satisfaction");
        // Expression xIsBlue = new Expression(BLUE, XE);
        // StartCoroutine(LogBases(MentalState, xIsBlue));
        // StartCoroutine(LogBases(MentalState, new Expression(OR, aliceIsBlue, xIsBlue)));

        // Log("existential introduction");
        // StartCoroutine(LogBases(MentalState, new Expression(SOME, APPLE, RED)));
        // StartCoroutine(LogBases(MentalState, new Expression(SOME, APPLE, BLUE)));

        // Log("universal elimination");
        // StartCoroutine(LogBases(MentalState, new Expression(RED, CHARLIE)));
        // StartCoroutine(LogBases(MentalState, new Expression(FET, CHARLIE)));

        // Log("variable coordination in conjunctions");
        // StartCoroutine(LogBases(MentalState, new Expression(RED, XE)));
        // StartCoroutine(LogBases(MentalState, new Expression(APPLE, CHARLIE)));
        // StartCoroutine(LogBases(MentalState, new Expression(AND, new Expression(RED, XE), new Expression(APPLE, XE))));

        // Log("Modus ponens: TODO - bug");
        // StartCoroutine(LogBases(MentalState, new Expression(GREEN, SELF)));

        // Log("Conditional proof");
        // StartCoroutine(LogBases(MentalState, new Expression(IF, new Expression(GREEN, BOB), new Expression(GREEN, BOB))));
        // StartCoroutine(LogBases(MentalState, new Expression(IF, new Expression(GREEN, BOB), new Expression(AND, new Expression(GREEN, BOB), new Expression(BLUE, BOB)))));
        // StartCoroutine(LogBases(MentalState, new Expression(IF, new Expression(ALL, BLUE, GREEN), new Expression(GREEN, BOB))));

        // Log("itself");
        // StartCoroutine(LogBases(MentalState, new Expression(ITSELF, IDENTITY, BOB)));
        // StartCoroutine(LogBases(MentalState, new Expression(IDENTITY, CHARLIE, CHARLIE)));

        // Log("truly");
        // StartCoroutine(LogBases(MentalState, new Expression(TRULY, new Expression(RED, ALICE))));
        // StartCoroutine(LogBases(MentalState, new Expression(SOMETIMES, TRULY, NOT)));
        // StartCoroutine(LogBases(MentalState, new Expression(TRULY, new Expression(RED, CHARLIE))));
        
        // StartCoroutine(LogBases(MentalState, new Expression(SOMETIMES, new Expression(PERCEIVE, SELF), TRULY)));
        
        // StartCoroutine(LogBases(MentalState, VERUM));
        // StartCoroutine(LogBases(MentalState, new Expression(VEROUS, BOB)));
        
        // Log("contraposition of perceptual belief");
        // MentalState ps = new MentalState(new Expression(PERCEIVE, SELF, new Expression(GREEN, SELF)));
        // Log(BasesString(ps, new Expression(GREEN, SELF)));
        // Log(BasesString(ps, new Expression(NOT, new Expression(NOT, new Expression(GREEN, SELF)))));
        // Log(BasesString(ps, new Expression(NOT,
        //     new Expression(PERCEIVE, SELF,
        //         new Expression(NOT, new Expression(GREEN, SELF))))));
        
        // MentalState bs = new MentalState(new Expression(RED, SELF));
        // Log(BasesString(bs, new Expression(BELIEVE, SELF, new Expression(RED, SELF))));
        // Log(BasesString(bs, new Expression(NOT, new Expression(BELIEVE, SELF, new Expression(GREEN, SELF)))));
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

    public static IEnumerator LogBases(MentalState m, Expression e) {
        var result = new HashSet<Basis>();
        var done = new Container<bool>(false);

        m.StartCoroutine(m.GetBases(Proof, e, result, done));

        while (!done.Item) {
            yield return null;
        }
        Log("'" + e + "'" + " is proved by: " + BasesString(result));
        yield break;
    }

    public static String BasesString(HashSet<Basis> bases) {
        StringBuilder s = new StringBuilder();
        s.Append("\n{");
        foreach (Basis basis in bases) {
            List<Expression> premises = basis.Key;
            s.Append("\n<");
            if (premises.Count > 0) {
                s.Append(premises[0]);
                for (int i = 1; i < premises.Count; i++) {
                    s.Append(", ");
                    s.Append(premises[i]);
                }
            }
            s.Append("> with {");
            Substitution substitution = basis.Value;
            foreach (KeyValuePair<Variable, Expression> assignments in substitution) {
                s.Append(assignments.Key);
                s.Append(" -> ");
                s.Append(assignments.Value);
                s.Append(", ");
            }
            s.Append("}");
        }
        s.Append("\n}");
        return s.ToString();
    }
}
