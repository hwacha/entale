using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;
using static Icons;

public class RadialMenuItem : MonoBehaviour
{
    UnityEngine.UI.Outline outline;
    Image Icon;
    public SemanticType semanticType { get; set; }
    public Constant constant { get; set; }

    void Awake() {
        outline = GetComponent<UnityEngine.UI.Outline>();
        Icon = GetComponent<Image>();
        Unhighlight();
    }

    public void Unhighlight() {
        outline.enabled = false;
    }

    public void Highlight() {
        outline.enabled = true;
    }

    public void SetTypeIcon() {
        Icon.sprite = Icons.GetTypeIcon();
    }

    public void SetIcon(Constant constantIn) {
        Icon.sprite = Icons.GetIcon(constantIn);
    }
}
