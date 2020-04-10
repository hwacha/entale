using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;

public class RadialMenu : MonoBehaviour {
    public RadialMenuItem radialMenuItemPrefab;
    const double INITIAL_ANGLE_OFFSET = Math.PI / 2f;
    const double RADIUS = 100;
    Vector2 SCREEN_CENTER = new Vector2(Screen.width, Screen.height);

    Dictionary<SemanticType, HashSet<Constant>> Lexicon = new Dictionary<SemanticType, HashSet<Constant>> {
        [INDIVIDUAL] = new HashSet<Constant> { ALICE.Head as Constant, BOB.Head as Constant },
        [PREDICATE] = new HashSet<Constant> { RED.Head as Constant, BLUE.Head as Constant },
        [TRUTH_VALUE] = new HashSet<Constant> { VERUM.Head as Constant },
        [RELATION_2] = new HashSet<Constant> { AT.Head as Constant, IDENTITY.Head as Constant },
    };
    List<RadialMenuItem> radialMenuItems = new List<RadialMenuItem>(); 

    int currentSliceIndex = -1;
    bool semanticMenuOpen = false;
    bool constantMenuOpen = false;

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
        HandleMenuOpen();
        if (semanticMenuOpen || constantMenuOpen) {
            HighlightMenuItems();
            HandleMenuItemClick();
        }
    }

    void HandleMenuOpen() {
        if (Input.GetKeyDown(KeyCode.M)) {
            if (!semanticMenuOpen && !constantMenuOpen) {
                OpenSemanticMenu();
            } else {
                CloseSemanticMenu();
                CloseConstantMenu();
            }
        }
    }

    void OpenSemanticMenu() {
        semanticMenuOpen = true;
        double sliceTheta = 2f * Math.PI / Lexicon.Count;
        double theta = INITIAL_ANGLE_OFFSET;
        foreach (SemanticType semanticType in Lexicon.Keys) {
            theta = theta % (2f * Math.PI);
            var radialMenuItem = Instantiate(radialMenuItemPrefab);
            radialMenuItem.GetComponent<Transform>().SetParent(gameObject.transform);
            radialMenuItem.GetComponent<Transform>().localPosition = new Vector2(
                (float)(Math.Cos(theta) * RADIUS),
                (float)(Math.Sin(theta) * RADIUS)
            );
            radialMenuItem.semanticType = semanticType;
            radialMenuItem.setIcon(semanticType);
            radialMenuItems.Add(radialMenuItem);
            theta += sliceTheta;
        }
    }

    void CloseSemanticMenu() {
        semanticMenuOpen = false;
        foreach(var rmi in radialMenuItems) {
            Destroy(rmi.gameObject);
        }
        radialMenuItems.Clear();
    }

    void OpenConstantMenu(SemanticType semanticType) {
        constantMenuOpen = true;
        double sliceTheta = 2f * Math.PI / Lexicon[semanticType].Count;
        double theta = INITIAL_ANGLE_OFFSET;
        foreach (Constant constant in Lexicon[semanticType]) {
            theta = theta % (2f * Math.PI);
            var radialMenuItem = Instantiate(radialMenuItemPrefab);
            radialMenuItem.GetComponent<Transform>().SetParent(gameObject.transform);
            radialMenuItem.GetComponent<Transform>().localPosition = new Vector2(
                (float)(Math.Cos(theta) * RADIUS),
                (float)(Math.Sin(theta) * RADIUS)
            );
            radialMenuItem.constant = constant;
            radialMenuItem.setIcon(constant);
            radialMenuItems.Add(radialMenuItem);
            theta += sliceTheta;
        }
    }

    void CloseConstantMenu() {
        constantMenuOpen = false;
        foreach(var rmi in radialMenuItems) {
            Destroy(rmi.gameObject);
        }
        radialMenuItems.Clear();
    }

    void HighlightMenuItems() {
        int sliceIndex = GetSliceIndex(radialMenuItems.Count, INITIAL_ANGLE_OFFSET, GetMouseAngle());
        if (sliceIndex != currentSliceIndex) {
            foreach(var rmi in radialMenuItems) {
                rmi.Unhighlight();
            }
            currentSliceIndex = sliceIndex;
        }
        radialMenuItems[currentSliceIndex].Highlight();
    }

    void HandleMenuItemClick() {
        if (Input.GetMouseButtonDown(0)) {
            int sliceIndex = GetSliceIndex(radialMenuItems.Count, INITIAL_ANGLE_OFFSET, GetMouseAngle());
            var rmi = radialMenuItems[currentSliceIndex];
            if(semanticMenuOpen) {
                CloseSemanticMenu();
                OpenConstantMenu(rmi.semanticType);
            } else {
                CloseConstantMenu();
                Debug.Log(rmi.constant);
            }
        }
    }

    // Returns the mouse angle from the cetner of the screen relative to the x axis in counter clockwise
    double GetMouseAngle() {
        Vector2 mousePosition = GetMousePosition(); 
        double angle = Mathf.Atan2(mousePosition.y, mousePosition.x);
        return angle > 0 ? angle : ((2f * Math.PI) + angle);
    }

    // Returns the mouse position from the center of the screen normalized by the screen's size
    Vector2 GetMousePosition() {
        return ((Input.mousePosition / SCREEN_CENTER) * 2) - Vector2.one; 
    }

    // Returns the index of the slice
    int GetSliceIndex(int sliceCount, double inital_angle_offset, double angle_in) {
        double sliceAngle = 2f * Math.PI / sliceCount;
        double angle = angle_in - inital_angle_offset + (sliceAngle/2);
        angle = angle > 0 ? angle : ((2f* Math.PI) + angle);
        return (int) Math.Floor(angle / sliceAngle) % sliceCount;
    }
}
