using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class RenderExpressionTesting : MonoBehaviour
{
    public GameObject ExpressionContainerPrefab;

    GameObject SpawnExpressionContainer(Expression e, Vector3 pos, Quaternion rot) {
        GameObject expressionContainerInstance =
            Instantiate(ExpressionContainerPrefab, pos, rot);
        ExpressionContainer expressionContainerScript =
            expressionContainerInstance.GetComponent<ExpressionContainer>();

        expressionContainerScript.Expression = e;

        return expressionContainerInstance;
    }

    // Start is called before the first frame update
    void Start()
    {
        // SpawnExpressionContainer(VERUM, new Vector3(0, 2, 0), Quaternion.identity);
        // SpawnExpressionContainer(
        //     new Expression(RED, BOB),
        //     new Vector3(2, 2, 0), Quaternion.identity);
        // SpawnExpressionContainer(
        //     new Expression(AT, ALICE, BOB),
        //     new Vector3(6, 2, 0), Quaternion.identity);

        SpawnExpressionContainer(
            new Expression(SOME, GREEN, BLUE), new Vector3(10, 2, 0), Quaternion.identity);

        SpawnExpressionContainer(
                new Expression(IF, new Expression(AT, ALICE, BOB), new Expression(RED, BOB)),
                new Vector3(15, 2, 0), Quaternion.identity);

        SpawnExpressionContainer(
            new Expression(IF,
                new Expression(IF, new Expression(AT, ALICE, BOB), new Expression(RED, BOB)),
             new Expression(SOME, GREEN, BLUE)), new Vector3(25, 2, 0), Quaternion.identity);

        SpawnExpressionContainer(GREEN, new Vector3(-5, 2, -1), Quaternion.identity);
        SpawnExpressionContainer(AT, new Vector3(-2, 2, -1), Quaternion.identity);
        SpawnExpressionContainer(SOME, new Vector3(-10, 2, -1), Quaternion.identity);
        SpawnExpressionContainer(IF, new Vector3(-15, 2, -1), Quaternion.identity);

    }
}
