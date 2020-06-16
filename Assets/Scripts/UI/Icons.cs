using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public abstract class Icons {
    public static Sprite getIcon(SemanticType semanticType) {
        if (semanticType.Equals(INDIVIDUAL)) {
            return Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (semanticType.Equals(PREDICATE)) {
            return Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else if (semanticType.Equals(RELATION_2)) {
            return Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else {
            Debug.Log("No sprite available for the semantic type " + semanticType);
            return null;
        }
    }

    public static Sprite getIcon(Constant constant) {
        var res = Resources.Load<Sprite>("Textures/Symbols/" + constant.ID) as Sprite;
        Debug.Log(res);
        return res;
    }
}
