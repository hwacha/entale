using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public abstract class Icons {
    public static Sprite getIcon(SemanticType semanticType) {
        if (semanticType.Equals(ASSERTION)) {
            return Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (semanticType.Equals(PREDICATE)) {
            return Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (semanticType.Equals(RELATION_2)) {
            return Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (semanticType.Equals(TRUTH_VALUE)) {
            return Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else {
            Debug.Log("No sprite available for the semantic type " + semanticType);
            return null;
        }
    }

    public static Sprite getIcon(Constant constant) {
        return Resources.Load<Sprite>("Sprites/" + constant.ToString()) as Sprite;
    }
}
