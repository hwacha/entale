﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Kirby : Agent
{
    protected override void Start() {
        MentalState.Initialize(new Expression[]{});
            // new Expression(ABLE, SELF, new Expression(AT, SELF, tree)),
            // // new Expression(AT, FOREST_KING, tree),
            // new Expression(BETTER, new Expression(AT, SELF, tree), NEUTRAL));
        base.Start();
    }
}
