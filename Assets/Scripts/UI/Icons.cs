using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public abstract class Icons {
    public static Sprite GetTypeIcon() {
        return Resources.Load<Sprite>("Sprites/white_circle") as Sprite;
    }

    public static Sprite GetIcon(Name name) {
        return Resources.Load<Sprite>("Textures/Symbols/" + name.ID) as Sprite;
    }
}
