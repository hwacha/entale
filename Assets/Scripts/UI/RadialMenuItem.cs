using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;
using static Icons;

public class RadialMenuItem : MonoBehaviour
{
    UnityEngine.UI.Outline Outline;
    Image Icon;
    public SemanticType Type;
    public Name Name;

    void Awake() {
        Outline = GetComponent<UnityEngine.UI.Outline>();
        Icon = GetComponent<Image>();
        Unhighlight();
    }

    public void Unhighlight() {
        Outline.enabled = false;
    }

    public void Highlight() {
        Outline.enabled = true;
    }

    public void SetTypeIcon() {
        Icon.sprite = Icons.GetTypeIcon();
    }

    public void SetIcon(Name name) {
        Icon.sprite = Icons.GetIcon(name);
    }
}
