using UnityEngine;
using System;
using static UnityEngine.Debug;
using static SemanticType;
using static Expression;

public class Testing : MonoBehaviour {
    void Start() {
        Log("Running tests from AI/Testing.cs. " +
            "To turn these off, " +
            "deactivate the 'AITesting' object in the heirarchy.");

        Log("SEMANTIC TYPES: ");
        Log("testing constructors.");
        Log("individual: " + INDIVIDUAL);
        Log("truth value: " + TRUTH_VALUE);
        Log("predicate: " + PREDICATE);
        Log("2-place relation: " + RELATION_2);

        Log("testing removal");
        Log("@TODO");

        Log("EXPRESSIONS: ");

        Log("word constructors: ");
        Log("alice: " + ALICE);
        Log("bob: " + BOB);
        Log("charlie: " + CHARLIE);
        Log("x: " + XE);
        Log("y: " + YE);
        Log("z: " + ZE);
        Log("verum: " + VERUM);
        Log("falsum: " + FALSUM);
        Log("S: " + ST);
        Log("T: " + TT);
        Log("red: " + RED);
        Log("blue: " + BLUE);
        Log("F: " + FET);
        Log("G: " + GET);
        Log("=: " + IDENTITY);
        Log("at: " + AT);
        Log("R: " + REET);

        Log("phrase constructors: ");
        Log(Verbose(new Expression(RED, ALICE)));
        Log(Verbose(new Expression(BLUE, BOB)));
        Log(Verbose(new Expression(IDENTITY, ALICE, ALICE)));
        Log(Verbose(new Expression(AT, ALICE, BOB)));
        Log(Verbose(new Expression(IDENTITY, ALICE)));
        Log(Verbose(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB)));
        Log(Verbose(new Expression(new Expression(IDENTITY, new Empty(INDIVIDUAL), BOB), ALICE)));

        try {
            Log("Failed to catch error: " + Verbose(new Expression(RED, ALICE, BOB)));
        } catch (ArgumentException e) {
            Log("Got expected error: " + e);
        }

        try {
            Log("Failed to catch error: " + new Expression(IDENTITY, ALICE, BOB, CHARLIE));
        } catch (ArgumentException e) {
            Log("Got expected error: " + e);
        }

        try {
            Log("Failed to catch error: " + new Expression(new Expression(IDENTITY, ALICE), BOB, CHARLIE));
        } catch (ArgumentException e) {
            Log("Got expected error: " + e);
        }

        // @TODO Test potential bug in mutating expressions
        
        // Testing mental state.
        Log("Testing mental state.");
        Log("QUERY");
        MentalState testState = new MentalState(
            BeliefRevisionPolicy.Conservative,
            new Expression(RED, ALICE),
            new Expression(BLUE, BOB),
            new Expression(AT, ALICE, BOB));

        Log(testState.Query(new Expression(RED, ALICE)));
        Log(testState.Query(new Expression(BLUE, BOB)));
        Log(!testState.Query(new Expression(RED, BOB)));
        Log(!testState.Query(new Expression(BLUE, ALICE)));
    }

    private String Verbose(Expression e) {
        return e.ToString() + " : " + e.Type.ToString();
    }
}
