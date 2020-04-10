using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public abstract class Icons {
    public static Sprite getIcon(SemanticType semanticType) {
        if (semanticType == INDIVIDUAL) {
            return Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (semanticType == PREDICATE) {
            return Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (semanticType == RELATION_2) {
            return Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (semanticType == TRUTH_VALUE) {
            return Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else {
            Debug.Log("No sprite available for the semantic type " + semanticType);
            return null;
        }
    }

    public static Sprite getIcon(Constant constant) {
        if (constant == ALICE.Head || constant == RED.Head) {
            return Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (constant == BOB.Head || constant == BLUE.Head) {
            return Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (constant == VERUM.Head) {
            return Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (constant == AT.Head) {
            return Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else if (constant == IDENTITY.Head) { 
            return Resources.Load<Sprite>("Sprites/pink_circle") as Sprite;
        }
        else {
            Debug.Log("No sprite available for the constant " + constant);
            return null;
        }
    }
}