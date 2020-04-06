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

        Expression bobIsRed     = new Expression(RED, BOB);
        Expression aliceIsBlue  = new Expression(BLUE, ALICE);

        MentalState testState = new MentalState(aliceIsRed, bobIsBlue, aliceIsAtBob, aliceIsAlice, bobIsBob, iCanMakeCharlieBlue);

        // Log(testState.Query(aliceIsRed));
        Log(BasesString(testState.Bases(aliceIsRed, ProofType.Proof)));
        // Log(testState.Query(bobIsBlue));
        // Log(!testState.Query(bobIsRed));
        // Log(!testState.Query(aliceIsBlue));

        // Log("Double Negation Elimination");
        // Log(testState.Query(new Expression(NOT,
        //     new Expression(NOT, aliceIsRed))));
        // Log(testState.Query(new Expression(NOT,
        //     new Expression(NOT, new Expression(NOT,
        //         new Expression(NOT, aliceIsRed))))));
        // Log(testState.Query(
        //     new Expression(NOT, new Expression(NOT,
        //     new Expression(NOT, new Expression(NOT, new Expression(NOT,
        //         new Expression(NOT, aliceIsRed))))))));
        // Log(!testState.Query(new Expression(NOT, new Expression(NOT, bobIsRed))));

        Log("Disjunction Introduction");
        Log(BasesString(testState.Bases(new Expression(OR, aliceIsRed, bobIsBlue),  Proof)));
        Log(BasesString(testState.Bases(new Expression(OR, aliceIsRed, bobIsRed),   Proof)));
        Log(BasesString(testState.Bases(new Expression(OR, aliceIsBlue, bobIsBlue), Proof)));
        Log(BasesString(testState.Bases(new Expression(OR, aliceIsBlue, bobIsRed),  Proof)));

        Log("Conjunction Introduction");
        Log(BasesString(testState.Bases(new Expression(AND, aliceIsRed,  bobIsBlue), Proof)));
        Log(BasesString(testState.Bases(new Expression(AND, aliceIsRed,  bobIsRed), Proof)));
        Log(BasesString(testState.Bases(new Expression(AND, aliceIsBlue, bobIsBlue), Proof)));
        Log(BasesString(testState.Bases(new Expression(AND, aliceIsBlue, bobIsRed), Proof)));

        Log(BasesString(
            testState.Bases(new Expression(AND,
                new Expression(OR, aliceIsRed, bobIsBlue),
                new Expression(OR, aliceIsAlice, bobIsBob)),
            ProofType.Proof)));

        Log("Planning");
        Log(BasesString(testState.Bases(new Expression(BLUE, CHARLIE), Proof)));
        Log(BasesString(testState.Bases(new Expression(BLUE, CHARLIE), Plan)));
        Log(BasesString(testState.Bases(new Expression(AND, new Expression(BLUE, CHARLIE), bobIsBlue), Plan)));
        Log(BasesString(testState.Bases(new Expression(AND, bobIsBlue, new Expression(BLUE, CHARLIE)), Plan)));
        // @TODO Print out and test Bases().
        // @TODO Test Assert().
    }

    private String Verbose(Expression e) {
        return e.ToString() + " : " + e.Type.ToString();
    }

    private String BasesString(HashSet<List<Expression>> bases) {
        StringBuilder s = new StringBuilder();
        s.Append("{");
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
