using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;
using static ProofType;

public class Testing : MonoBehaviour {
    void Start() {
        // Log("Running tests from AI/Testing.cs. " +
        //     "To turn these off, " +
        //     "deactivate the 'AITesting' object in the heirarchy.");

        // Log("SEMANTIC TYPES: ");
        // Log("testing constructors.");
        // Log("individual: " + INDIVIDUAL);
        // Log("truth value: " + TRUTH_VALUE);
        // Log("predicate: " + PREDICATE);
        // Log("2-place relation: " + RELATION_2);

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
        Log(Verbose(new Expression(RED, ALICE)));
        Log(Verbose(new Expression(BLUE, BOB)));
        Log(Verbose(new Expression(IDENTITY, ALICE, ALICE)));
        Log(Verbose(new Expression(AT, ALICE, BOB)));
        Log(Verbose(new Expression(IDENTITY, ALICE)));
        Log(Verbose(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB)));
        Log(Verbose(new Expression(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB), ALICE)));

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
        Log("equality");
        // Log(NOT.Equals(NOT));

        // @TODO Test potential bug in mutating expressions
        
        // Testing mental state.
        Log("Testing mental state.");
        Log("QUERY");
        Expression aliceIsRed   = new Expression(RED, ALICE);
        Expression bobIsBlue    = new Expression(BLUE, BOB);
        Expression aliceIsAtBob = new Expression(AT, ALICE, BOB);
        Expression aliceIsAlice = new Expression(IDENTITY, ALICE, ALICE);
        Expression bobIsBob     = new Expression(IDENTITY, BOB, BOB);
        Expression iCanMakeCharlieBlue = new Expression(ABLE, SELF, new Expression(BLUE, CHARLIE));
        Expression iSeeCharlieAsRed = new Expression(PERCEIVE, SELF, new Expression(RED, CHARLIE));

        Expression bobIsRed     = new Expression(RED, BOB);
        Expression aliceIsBlue  = new Expression(BLUE, ALICE);

        MentalState testState = new MentalState(
            aliceIsRed, bobIsBlue, aliceIsAtBob,
            aliceIsAlice, bobIsBob,
            iCanMakeCharlieBlue, iSeeCharlieAsRed);

        // Log(testState.Query(aliceIsRed));
        Log(BasesString(testState, aliceIsRed));
        // Log(testState.Query(bobIsBlue));
        // Log(!testState.Query(bobIsRed));
        // Log(!testState.Query(aliceIsBlue));

        Log("Double Negation Elimination");
        Expression notNotAliceIsRed = new Expression(NOT, new Expression(NOT, aliceIsRed));
        Expression notNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotAliceIsRed));
        Expression notNotNotNotNotNotAliceIsRed = new Expression(NOT, new Expression(NOT, notNotNotNotAliceIsRed));
        Log(Verbose(notNotAliceIsRed));
        Log(BasesString(testState, notNotAliceIsRed));
        Log(Verbose(notNotNotNotAliceIsRed));
        Log(BasesString(testState, notNotNotNotAliceIsRed));
        Log(Verbose(notNotNotNotNotNotAliceIsRed));
        Log(BasesString(testState, notNotNotNotNotNotAliceIsRed));
        Log(BasesString(testState, new Expression(NOT, new Expression(NOT, bobIsRed))));
        Log(BasesString(testState, new Expression(NOT, bobIsBlue)));

        Log("Disjunction Introduction");
        Log(BasesString(testState, new Expression(OR, aliceIsRed, bobIsBlue)));
        Log(BasesString(testState, new Expression(OR, aliceIsRed, bobIsRed)));
        Log(BasesString(testState, new Expression(OR, aliceIsBlue, bobIsBlue)));
        Log(BasesString(testState, new Expression(OR, aliceIsBlue, bobIsRed)));

        Log("Conjunction Introduction");
        Log(BasesString(testState, new Expression(AND, aliceIsRed,  bobIsBlue)));
        Log(BasesString(testState, new Expression(AND, aliceIsRed,  bobIsRed)));
        Log(BasesString(testState, new Expression(AND, aliceIsBlue, bobIsBlue)));
        Log(BasesString(testState, new Expression(AND, aliceIsBlue, bobIsRed)));

        Expression conjunctionOfDisjunctions = new Expression(AND,
                new Expression(OR, aliceIsRed, bobIsBlue),
                new Expression(OR, aliceIsAlice, bobIsBob));

        Log(Verbose(conjunctionOfDisjunctions));

        Log(BasesString(testState, conjunctionOfDisjunctions));

        Log(BasesString(testState, new Expression(RED, CHARLIE)));

        Log("Planning");
        Log(BasesString(testState, new Expression(BLUE, CHARLIE)));
        testState.ProofMode = Plan;
        Log(BasesString(testState, new Expression(BLUE, CHARLIE)));
        Log(BasesString(testState, new Expression(AND, new Expression(BLUE, CHARLIE), bobIsBlue)));
        Log(BasesString(testState, new Expression(AND, bobIsBlue, new Expression(BLUE, CHARLIE))));
        // @TODO Print out and test Bases().
        // @TODO Test Assert().
    }

    private String Verbose(Expression e) {
        return e.ToString() + " : " + e.Type.ToString() + " #" + e.Depth;
    }

    private String BasesString(MentalState m, Expression e) {
        StringBuilder s = new StringBuilder();
        s.Append(Verbose(e));
        s.Append(" is proved by: ");
        s.Append("\n{");
        HashSet<List<Expression>> bases = m.Bases(e);
        foreach (List<Expression> basis in bases) {
            s.Append("\n<");
            if (basis.Count > 0) {
                s.Append(basis[0]);
                for (int i = 1; i < basis.Count; i++) {
                    s.Append(", ");
                    s.Append(basis[i]);
                }
            }
            s.Append(">");
        }
        s.Append("\n}");
        return s.ToString();
    }
}
