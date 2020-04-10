using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;
using static Icons;

public class RadialMenuItem : MonoBehaviour
{
    Outline outline;
    Image icon;
    public SemanticType semanticType { get; set; }
    public Constant constant { get; set; }

    void Awake() {
        outline = GetComponent<Outline>();
        icon = GetComponent<Image>();
        Unhighlight();
    }

    public void Unhighlight() {
        outline.enabled = false;
    }

    public void Highlight() {
        outline.enabled = true;
    }

    public void setIcon(SemanticType semanticTypeIn) {
        icon.sprite = Icons.getIcon(semanticTypeIn);
    }

    public void setIcon(Constant constantIn) {
        icon.sprite = Icons.getIcon(constantIn);
    }
}
