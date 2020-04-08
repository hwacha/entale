using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public class RadialMenuItem : MonoBehaviour
{
    Outline outline;
    Image icon;

    void Awake() {
        outline = GetComponent<Outline>();
        icon = GetComponent<Image>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Unhighlight();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Unhighlight() {
        outline.enabled = false;
    }

    public void Highlight() {
        outline.enabled = true;
    }

    public void setIcon(SemanticType semanticType) {
        if (semanticType == INDIVIDUAL) {
            icon.sprite = Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (semanticType == PREDICATE) {
            icon.sprite = Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else if (semanticType == RELATION_2) {
            icon.sprite = Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (semanticType == TRUTH_VALUE) {
            icon.sprite = Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (semanticType == TRUTH_FUNCTION) {
            icon.sprite = Resources.Load<Sprite>("Sprites/pink_circle") as Sprite;
        }
        else {
            Debug.Log("No sprite available for this semantic type!");
        }
    }
}
