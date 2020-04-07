using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public class WordSelectMenu : MonoBehaviour {

    const double INITIAL_OFFSET = Math.PI / 2;
    const double RADIUS = 100;

    Dictionary<SemanticType, HashSet<Constant>> Lexicon;

    // Start is called before the first frame update
    void Start() {
        // Create lexicon
        Lexicon = new Dictionary<SemanticType, HashSet<Constant>> {
            [INDIVIDUAL] = new HashSet<Constant> { ALICE.Head as Constant, BOB.Head as Constant },
            [PREDICATE] = new HashSet<Constant> { RED.Head as Constant, BLUE.Head as Constant },
            [TRUTH_VALUE] = new HashSet<Constant> { VERUM.Head as Constant },
            [RELATION_2] = new HashSet<Constant> { AT.Head as Constant, IDENTITY.Head as Constant }
        };

        double sliceTheta = 2.0 * Math.PI / Lexicon.Keys.Count;
        double theta = INITIAL_OFFSET;
        foreach (SemanticType semanticType in Lexicon.Keys) {
            theta += sliceTheta;
            GameObject imageContainer = new GameObject();
            imageContainer.transform.parent = this.gameObject.transform;
            Image typeImage = imageContainer.AddComponent<Image>();
            if (semanticType == INDIVIDUAL) {
                typeImage.sprite = Resources.Load<Sprite>("Sprites/individual") as Sprite;
            } else if (semanticType == PREDICATE) {
                typeImage.sprite = Resources.Load<Sprite>("Sprites/predicate") as Sprite;
            } else if (semanticType == RELATION_2) {
                typeImage.sprite = Resources.Load<Sprite>("Sprites/relation_2") as Sprite;
            } else if (semanticType == TRUTH_VALUE) {
                typeImage.sprite = Resources.Load<Sprite>("Sprites/truth_value") as Sprite;
            } else {
                Debug.Log("No sprite available for this semantic type!");
            }
            typeImage.GetComponent<RectTransform>().localPosition = new Vector2(
                (float)(Math.Cos(theta) * RADIUS),
                (float)(Math.Sin(theta) * RADIUS));
        }
    }

    // Update is called once per frame
    void Update() {

    }
}
