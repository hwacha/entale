using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Bob : Agent
{
    // Start is called before the first frame update
    protected override void Start()
    {
        var tree = new Deictic(THAT, GameObject.Find("tree"));

        MentalState.Initialize(
            new Expression(ABLE, SELF, new Expression(AT, SELF, tree)),
            // new Expression(AT, FOREST_KING, tree),
            new Expression(BETTER, new Expression(AT, SELF, tree), NEUTRAL));

        base.Start();
    }
}
