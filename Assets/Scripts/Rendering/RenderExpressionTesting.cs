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

    GameObject SpawnArgumentContainer(Expression e, Vector3 pos, Quaternion rot) {
        var argContainer = ArgumentContainer.From(e);
        argContainer.GetComponent<ArgumentContainer>().GenerateVisual();
        argContainer.transform.position = pos;
        argContainer.transform.rotation = rot;

        return argContainer;
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

    //     SpawnExpressionContainer(
    //         new Expression(SOME, GREEN, BLUE), new Vector3(10, 2, 0), Quaternion.identity);

        // SpawnExpressionContainer(
        //         new Expression(IF, new Expression(AT, ALICE, BOB), new Expression(RED, BOB)),
        //         new Vector3(15, 2, 0), Quaternion.identity);

        // ArgumentContainer.From(new Expression(ITSELF, AT, BOB)).GetComponent<ArgumentContainer>().GenerateVisual();

        // SpawnArgumentContainer(
        //     new Expression(IF,
        //         new Expression(IF, new Expression(AT, ALICE, BOB), new Expression(RED, BOB)),
        //      new Expression(SOME, GREEN, BLUE)), new Vector3(25, 2, 0), Quaternion.identity);
        
        // SpawnArgumentContainer(new Expression(IF, new Expression(AT, ALICE, BOB), new Expression(GREEN, BOB)));
        // SpawnArgumentContainer(new Expression(ITSELF, AT, ALICE));
        // SpawnArgumentContainer(new Expression(SOME, BLUE, new Expression(AT, new Empty(SemanticType.INDIVIDUAL), BOB)),
            // new Vector3(0, 2, 1), Quaternion.identity);
        // SpawnArgumentContainer(new Expression(GREEN, BOB));

    //     SpawnExpressionContainer(GREEN, new Vector3(-5, 2, -1), Quaternion.identity);
    //     SpawnExpressionContainer(AT, new Vector3(-2, 2, -1), Quaternion.identity);
    //     SpawnExpressionContainer(SOME, new Vector3(-10, 2, -1), Quaternion.identity);
    //     SpawnExpressionContainer(IF, new Vector3(-15, 2, -1), Quaternion.identity);

    //     SpawnExpressionContainer(new Expression(SOME, BLUE, new Expression(AT, new Empty(SemanticType.INDIVIDUAL), ALICE)),
    //         new Vector3(-20, 2, -1), Quaternion.identity);

    }
}
