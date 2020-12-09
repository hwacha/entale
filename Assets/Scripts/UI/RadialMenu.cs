using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static SemanticType;
using static Expression;
using static MouseLook;

public class RadialMenu : MonoBehaviour {
    public RadialMenuItem radialMenuItemPrefab;
    public Camera playerCamera;
    MouseLook playerMouseLook;
    const double INITIAL_ANGLE_OFFSET = Math.PI / 2f;
    const double RADIUS = 150;
    Vector2 SCREEN_CENTER = new Vector2(Screen.width, Screen.height);

    public Dictionary<SemanticType, HashSet<Name>> Lexicon =
        new Dictionary<SemanticType, HashSet<Name>>(){
            [TRUTH_VALUE] = new HashSet<Name>{
                VERUM.Head as Name,
                FALSUM.Head as Name,
                NEUTRAL.Head as Name
            },
            [ASSERTION] = new HashSet<Name>(){
                YES.Head as Name,
                NO.Head  as Name
            },
            [CONFORMITY_VALUE] = new HashSet<Name>(){
                ACCEPT.Head as Name,
                REFUSE.Head as Name
            },
            [TRUTH_ASSERTION_FUNCTION] = new HashSet<Name>(){
                ASSERT.Head as Name,
                DENY.Head as Name
            },
            [TRUTH_QUESTION_FUNCTION] = new HashSet<Name>(){
                ASK.Head as Name
            },
            [TRUTH_CONFORMITY_FUNCTION] = new HashSet<Name>(){
                WILL.Head as Name,
                WOULD.Head as Name
            },
            [INDIVIDUAL] = new HashSet<Name>(){
                SELF.Head as Name,
                BOB.Head  as Name,
                THIS.Head as Name
            },
            [PREDICATE] = new HashSet<Name>(){
                RED.Head  as Name,
                BLUE.Head as Name,
                TOMATO.Head as Name,
                BANANA.Head as Name
            },
            [RELATION_2] = new HashSet<Name>(){
                IDENTITY.Head as Name,
                AT.Head       as Name
            },
            [DETERMINER] = new HashSet<Name>(){
                SELECTOR.Head as Name
            },
            [TRUTH_FUNCTION] = new HashSet<Name>(){
                TRULY.Head as Name,
                NOT.Head as Name
            },
            [TRUTH_FUNCTION_2] = new HashSet<Name>(){
                AND.Head as Name,
                OR.Head  as Name,
                IF.Head  as Name,
                BETTER.Head as Name,
                AS_GOOD_AS.Head  as Name
            },
            [INDIVIDUAL_TRUTH_RELATION] = new HashSet<Name>(){
                BELIEVE.Head as Name,
            },
            [QUANTIFIER] = new HashSet<Name>(){
                SOME.Head as Name,
                ALL.Head as Name
            }
        };
    List<RadialMenuItem> radialMenuItems = new List<RadialMenuItem>(); 

    int currentSliceIndex = -1;
    bool semanticMenuOpen = false;
    bool constantMenuOpen = false;

    public delegate void WordCallback(Name name); 
    public event WordCallback wordCallback = null;

    // Start is called before the first frame update
    void Start() {
        playerMouseLook = playerCamera.GetComponent<MouseLook>();
    }

    // Update is called once per frame
    void Update() {
        HandleMenuOpen();
        if (semanticMenuOpen || constantMenuOpen) {
            HighlightMenuItems();
            HandleMenuItemClick();
        }
    }

    public void HandleMenuOpen() {
        if (!semanticMenuOpen && !constantMenuOpen) {
            OpenSemanticMenu();
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ExitMenu() {
        CloseSemanticMenu();
        CloseConstantMenu();
        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.lockState = CursorLockMode.Locked;
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
            radialMenuItem.Type = semanticType;
            radialMenuItem.SetTypeIcon();
            radialMenuItem.gameObject.GetComponent<CanvasRenderer>().SetColor(RenderingOptions.ColorsByType[semanticType]);
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
        foreach (Name name in Lexicon[semanticType]) {
            theta = theta % (2f * Math.PI);
            var radialMenuItem = Instantiate(radialMenuItemPrefab);
            radialMenuItem.GetComponent<Transform>().SetParent(gameObject.transform);
            radialMenuItem.GetComponent<Transform>().localPosition = new Vector2(
                (float)(Math.Cos(theta) * RADIUS),
                (float)(Math.Sin(theta) * RADIUS)
            );
            radialMenuItem.Name = name;
            radialMenuItem.SetIcon(name);
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
            if (semanticMenuOpen) {
                CloseSemanticMenu();
                OpenConstantMenu(rmi.Type);
            } else {
                // Debug.Log(rmi.constant);
                if (wordCallback != null) {
                    wordCallback(rmi.Name);
                }
                ExitMenu();
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

    public void setWordSelectCallback(WordCallback callback) {
        wordCallback = callback;
    }
}
