using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Kirby : Agent
{
    protected override void Start() {
        var tree = new Deictic(THAT, GameObject.Find("tree"));
        MentalState = new MentalState(
            new Expression(ABLE, SELF, new Expression(AT, SELF, tree)),
            // new Expression(AT, FOREST_KING, tree),
            new Expression(BETTER, new Expression(AT, SELF, tree), NEUTRAL));
        base.Start();
    }
}
