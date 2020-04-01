using UnityEngine;
using static SemanticType;

public class Testing : MonoBehaviour {
    void Start() {
        Debug.Log("Running tests from AI/Testing.cs. " +
            "To turn these off, " +
            "deactivate the 'AITesting' object in the heirarchy.");
        Debug.Log("SEMANTIC TYPES: ");
        Debug.Log("individual: " + INDIVIDUAL);
        Debug.Log("truth value: " + TRUTH_VALUE);
        Debug.Log("predicate: " + PREDICATE);
        Debug.Log("2-place relation: " + RELATION_2);
    }
}
