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
    public SemanticType semanticType { get; set; }
    public Constant constant { get; set; }

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

    public void setSemanticType(SemanticType semantic_type_in) {
        semanticType = semantic_type_in;
        setIcon(semantic_type_in);
    }

    public void setConstant(Constant constant_in) {
        constant = constant_in;
        setIcon(constant_in);
    }

    public void setIcon(SemanticType semantic_type) {
        if (semantic_type == INDIVIDUAL) {
            icon.sprite = Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (semantic_type == PREDICATE) {
            icon.sprite = Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (semantic_type == RELATION_2) {
            icon.sprite = Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (semantic_type == TRUTH_VALUE) {
            icon.sprite = Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else {
            Debug.Log("No sprite available for this semantic type!");
        }
    }

    public void setIcon(Constant constant) {
        if (constant == ALICE.Head || constant == RED.Head) {
            icon.sprite = Resources.Load<Sprite>("Sprites/red_circle") as Sprite;
        } else if (constant == BOB.Head || constant == BLUE.Head) {
            icon.sprite = Resources.Load<Sprite>("Sprites/blue_circle") as Sprite;
        } else if (constant == VERUM.Head) {
            icon.sprite = Resources.Load<Sprite>("Sprites/orange_circle") as Sprite;
        } else if (constant == AT.Head) {
            icon.sprite = Resources.Load<Sprite>("Sprites/green_circle") as Sprite;
        } else if (constant == IDENTITY.Head) { 
            icon.sprite = Resources.Load<Sprite>("Sprites/pink_circle") as Sprite;
        }
        else {
            Debug.Log("No sprite available for this semantic type!");
        }
    }
}
