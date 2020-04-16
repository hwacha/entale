using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;
using static ProofType;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;
using Basis = System.Collections.Generic.KeyValuePair<System.Collections.Generic.List<Expression>, System.Collections.Generic.Dictionary<Variable, Expression>>;

public class Testing : MonoBehaviour {
    void Start() {
        // Log("Running tests from AI/Testing.cs. " +
        //     "To turn these off, " +
        //     "deactivate the 'AITesting' object in the heirarchy.");

        Log("SEMANTIC TYPES: ");
        Log("testing constructors.");
        Log("individual: " + INDIVIDUAL);
        Log("truth value: " + TRUTH_VALUE);
        Log("predicate: " + PREDICATE);
        Log("2-place relation: " + RELATION_2);
        Log("Testing semantic type partial application predicate.");
        Log(TRUTH_VALUE.IsPartialApplicationOf(TRUTH_VALUE));
        Log(TRUTH_VALUE.IsPartialApplicationOf(PREDICATE));
        Log(TRUTH_VALUE.IsPartialApplicationOf(RELATION_2));
        Log(PREDICATE.IsPartialApplicationOf(PREDICATE));
        Log(PREDICATE.IsPartialApplicationOf(RELATION_2));
        Log(PREDICATE.IsPartialApplicationOf(INDIVIDUAL_TRUTH_RELATION));
        Log(!RELATION_2.IsPartialApplicationOf(PREDICATE));
        Log(!INDIVIDUAL_TRUTH_RELATION.IsPartialApplicationOf(PREDICATE));

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
        // Log("equality");
        // Log(NOT.Equals(NOT));

        // Log("Unification");
        // Log(UnificationString(ALICE, ALICE));
        // Log(UnificationString(XE, ALICE));
        // Log(UnificationString(ALICE, XE));
        // Log(UnificationString(XE, XE));
        // Log(UnificationString(ST, new Expression(AT, ALICE, BOB)));

        // Log(UnificationString(new Expression(RED, XE), new Expression(RED, ALICE)));
        // Log(UnificationString(new Expression(RED, ALICE), new Expression(RED, XE)));

        // Log(UnificationString(new Expression(AT, XE, YE), new Expression(AT, ALICE, BOB)));
        // Log(UnificationString(new Expression(AT, XE, XE), new Expression(AT, ALICE, ALICE)));
        // Log(UnificationString(new Expression(AT, XE, XE), new Expression(AT, ALICE, BOB)));

        // Log(UnificationString(new Expression(FET, ALICE), new Expression(AT, ALICE, BOB)));
        // Log(UnificationString(new Expression(FET, BOB), new Expression(AT, ALICE, BOB)));

        // Log(UnificationString(new Expression(AT, ALICE, BOB), new Expression(FET, ALICE)));
        // Log(UnificationString(new Expression(AT, ALICE, BOB), new Expression(FET, BOB)));

        // Log(UnificationString(new Expression(AT, XE, BOB), new Expression(AT, ALICE, YE)));

        // Log(UnificationString(new Expression(FET, XE), new Expression(REET, ALICE, BOB)));
        // Log(UnificationString(new Expression(FET, XE), new Expression(GET, YE)));

        // @TODO Test potential bug in mutating expressions
        
        // Testing mental state.
        Log("Testing mental state.");
        // Log("QUERY");
        // Expression aliceIsRed   = new Expression(RED, ALICE);
        // Expression aliceIsAnApple = new Expression(APPLE, ALICE);
        // Expression bobIsBlue    = new Expression(BLUE, BOB);
        Expression aliceIsAtBob = new Expression(AT, ALICE, BOB);
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

        // Expression everythingIsSelfIdentical = new Expression(ALL, EMPTY, new Expression(ITSELF, IDENTITY));

        // var whatIseeIsAlwaysTrue = new Expression(ALWAYS, new Expression(PERCEIVE, SELF), TRULY);

        // MentalState testState = new MentalState(
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
        //     whatIseeIsAlwaysTrue);

        // Log(BasesString(testState, aliceIsRed));
        // Log(BasesString(testState, bobIsBlue));
        // Log(BasesString(testState, bobIsRed));
        // Log(BasesString(testState, aliceIsBlue));

        // Log("Double Negation Elimination");
        // Expression notNotAliceIsRed = new Expression(NOT, new Expression(NOT, aliceIsRed));
        // Expression notNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotAliceIsRed));
        // Expression notNotNotNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotNotNotAliceIsRed));
        // Log(BasesString(testState, notNotAliceIsRed));
        // Log(BasesString(testState, notNotNotNotAliceIsRed));
        // Log(BasesString(testState, notNotNotNotNotNotAliceIsRed));
        // Log(BasesString(testState, new Expression(NOT, new Expression(NOT, bobIsRed))));
        // Log(BasesString(testState, new Expression(NOT, bobIsBlue)));

        // Log("Disjunction Introduction");
        // Log(BasesString(testState, new Expression(OR, aliceIsRed, bobIsBlue)));
        // Log(BasesString(testState, new Expression(OR, aliceIsRed, bobIsRed)));
        // Log(BasesString(testState, new Expression(OR, aliceIsBlue, bobIsBlue)));
        // Log(BasesString(testState, new Expression(OR, aliceIsBlue, bobIsRed)));

        // Log("Conjunction Introduction");
        // Log(BasesString(testState, new Expression(AND, aliceIsRed,  bobIsBlue)));
        // Log(BasesString(testState, new Expression(AND, aliceIsRed,  bobIsRed)));
        // Log(BasesString(testState, new Expression(AND, aliceIsBlue, bobIsBlue)));
        // Log(BasesString(testState, new Expression(AND, aliceIsBlue, bobIsRed)));

        // Expression conjunctionOfDisjunctions = new Expression(AND,
        //         new Expression(OR, aliceIsRed, bobIsBlue),
        //         new Expression(OR, aliceIsAlice, bobIsBob));

        // Log(BasesString(testState, conjunctionOfDisjunctions));

        // Log("Planning");
        // Log(BasesString(testState, new Expression(BLUE, CHARLIE)));
        // testState.ProofMode = Plan;
        // Log(BasesString(testState, new Expression(BLUE, CHARLIE)));
        // Log(BasesString(testState, new Expression(AND, new Expression(BLUE, CHARLIE), bobIsBlue)));
        // Log(BasesString(testState, new Expression(AND, bobIsBlue, new Expression(BLUE, CHARLIE))));
        // testState.ProofMode = Proof;

        // Log("Formula satisfaction");
        // Expression xIsBlue = new Expression(BLUE, XE);
        // Log(BasesString(testState, xIsBlue));
        // Log(BasesString(testState, new Expression(OR, aliceIsBlue, xIsBlue)));

        // Log("existential introduction");
        // Log(BasesString(testState, new Expression(SOME, APPLE, RED)));
        // Log(BasesString(testState, new Expression(SOME, APPLE, BLUE)));

        // Log("universal elimination");
        // Log(BasesString(testState, new Expression(RED, CHARLIE)));
        // Log(BasesString(testState, new Expression(FET, CHARLIE)));

        // Log("variable coordination in disjunctions:");
        // Log(BasesString(testState, new Expression(OR, ST, new Expression(NOT, ST))));
        // Log(BasesString(testState, new Expression(OR, new Expression(RED, XE), new Expression(BLUE, XE))));

        // Log("variable coordination in conjunctions: TODO");
        // Log(BasesString(testState, new Expression(RED, XE)));
        // Log(BasesString(testState, new Expression(APPLE, CHARLIE)));
        // Log(BasesString(testState, new Expression(AND, new Expression(RED, XE), new Expression(APPLE, XE))));

        // Log("Modus ponens: TODO - bug");
        // Log(BasesString(testState, new Expression(GREEN, SELF)));

        // Log("Conditional proof");
        // Log(BasesString(testState, new Expression(IF, new Expression(GREEN, BOB), new Expression(GREEN, BOB))));
        // Log(BasesString(testState, new Expression(IF, new Expression(GREEN, BOB), new Expression(AND, new Expression(GREEN, BOB), new Expression(BLUE, BOB)))));
        // Log(BasesString(testState, new Expression(IF, new Expression(ALL, BLUE, GREEN), new Expression(GREEN, BOB))));

        // Log("itself");
        // Log(BasesString(testState, new Expression(ITSELF, IDENTITY, BOB)));
        // Log(BasesString(testState, new Expression(IDENTITY, CHARLIE, CHARLIE)));

        // Log("truly");
        // Log(BasesString(testState, new Expression(TRULY, new Expression(RED, ALICE))));
        // Log(BasesString(testState, new Expression(SOMETIMES, TRULY, NOT)));
        // Log(BasesString(testState, new Expression(TRULY, new Expression(RED, CHARLIE))));
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

    public static String UnificationString(Expression a, Expression b) {
        return a + ", " + b + ": " + SubstitutionString(a.Unify(b));
    }

    public static String BasesString(MentalState m, Expression e) {
        return Verbose(e) + " is proved by: " + BasesString(m.Bases(e));
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
