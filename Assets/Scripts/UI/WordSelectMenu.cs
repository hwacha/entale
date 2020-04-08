using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public class WordSelectMenu : MonoBehaviour {
    public RadialMenuItem radialMenuItemPrefab;
    const double RADIUS = 100;
    Vector2 SCREEN_CENTER = new Vector2(Screen.width, Screen.height);

    Dictionary<SemanticType, HashSet<Constant>> Lexicon = new Dictionary<SemanticType, HashSet<Constant>> {
        [INDIVIDUAL] = new HashSet<Constant> { ALICE.Head as Constant, BOB.Head as Constant },
        [PREDICATE] = new HashSet<Constant> { RED.Head as Constant, BLUE.Head as Constant },
        [TRUTH_VALUE] = new HashSet<Constant> { VERUM.Head as Constant },
        [RELATION_2] = new HashSet<Constant> { AT.Head as Constant, IDENTITY.Head as Constant },
        [TRUTH_FUNCTION] = new HashSet<Constant> { AT.Head as Constant, IDENTITY.Head as Constant }
    };

    List<RadialMenuItem> radialMenuItems = new List<RadialMenuItem>(); 
    int currentSliceIndex = -1;
    double INITIAL_ANGLE_OFFSET = Math.PI / 2f;

    // Start is called before the first frame update
    void Start() {
        DrawMenu(INITIAL_ANGLE_OFFSET, RADIUS);
    }

    void DrawMenu(double initial_angle_offset, double radius) {
        double sliceTheta = 2.0 * Math.PI / Lexicon.Keys.Count;
        double theta = initial_angle_offset;
        foreach (SemanticType semanticType in Lexicon.Keys) {
            theta = theta % (Math.PI * 2);
            var radialMenuItem = Instantiate(radialMenuItemPrefab);
            radialMenuItem.GetComponent<Transform>().SetParent(gameObject.transform);
            radialMenuItem.GetComponent<Transform>().localPosition = new Vector2(
                (float)(Math.Cos(theta) * radius),
                (float)(Math.Sin(theta) * radius)
            );
            radialMenuItem.setIcon(semanticType);
            radialMenuItems.Add(radialMenuItem);
            theta += sliceTheta;
        }
    }

    // Update is called once per frame
    void Update() {
        int sliceIndex = GetSliceIndex(radialMenuItems.Count, INITIAL_ANGLE_OFFSET, GetMouseAngle());
        if (sliceIndex != currentSliceIndex) {
            foreach(var rmi in radialMenuItems) {
                rmi.Unhighlight();
            }
            currentSliceIndex = sliceIndex;
        }
        radialMenuItems[currentSliceIndex].Highlight();
    }

    // Returns the mouse angle from the cetner of the screen relative to the x axis in counter clockwise
    double GetMouseAngle() {
        Vector2 mousePosition = GetMousePosition(); 
        double angle = Mathf.Atan2(mousePosition.y, mousePosition.x);
        return angle > 0 ? angle : (2*Math.PI + angle);
    }

    // Returns the mouse position from the center of the screen normalized by the screen's size
    Vector2 GetMousePosition() {
        return ((Input.mousePosition / SCREEN_CENTER) * 2) - Vector2.one; 
    }

    // If the slices are 
    int GetSliceIndex(int sliceCount, double inital_angle_offset, double angle_in) {
        double sliceAngle = Math.PI * 2 / sliceCount;
        double angle = angle_in - inital_angle_offset + (sliceAngle/2);
        angle = angle > 0 ? angle : (2*Math.PI + angle);
        return (int) Math.Floor(angle / sliceAngle) % sliceCount;
    }
}
