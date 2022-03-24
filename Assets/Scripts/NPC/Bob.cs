using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Bob : Agent
{
    protected override void Start()
    {
        MentalState.Initialize(
            new Expression[]{
                new Expression(GOOD, new Expression(SOME, FRUIT, new Expression(AT, SELF))),
                // new Expression(ALL, BANANA,
                //     new Expression(GEACH_E_TRUTH_FUNCTION,
                //         new Expression(GEACH_T_TRUTH_FUNCTION,
                //             GOOD,
                //             new Expression(GEACH_T_TRUTH_FUNCTION,
                //                 NOT,
                //                 new Expression(MAKE,
                //                     new Empty(SemanticType.TRUTH_VALUE),
                //                     SELF))),
                //             new Expression(AT, SELF))),
            }
        );
        base.Start();
    }
}
