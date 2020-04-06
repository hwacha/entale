using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SemanticType;
using static Expression;

public class WordSelectMenu : MonoBehaviour
{
    Dictionary<SemanticType, HashSet<Constant>> Lexicon;

    // Start is called before the first frame update
    void Start()
    {
        Lexicon = new Dictionary<SemanticType, HashSet<Constant>>
            {
                [INDIVIDUAL] = new HashSet<Constant>{ALICE.Head as Constant, BOB.Head as Constant},
                [PREDICATE]  = new HashSet<Constant>{RED.Head as Constant, BLUE.Head as Constant},
                [RELATION_2] = new HashSet<Constant>{AT.Head as Constant, IDENTITY.Head as Constant}
            };

        Dictionary<SemanticType, HashSet<Constant>>.KeyCollection types = Lexicon.Keys;

        double sliceTheta = 2.0 * Math.PI / types.Count;
        for (double theta = Math.PI / 4; theta <= 9 * Math.PI / 4; theta += sliceTheta) {
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
