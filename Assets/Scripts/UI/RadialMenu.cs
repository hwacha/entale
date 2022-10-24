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
    const double RADIUS = 300;
    const float ITEM_SCALE = 2;
    Vector2 SCREEN_CENTER = new Vector2(Screen.width, Screen.height);

    public Dictionary<SemanticType, HashSet<Name>> Lexicon =
        new Dictionary<SemanticType, HashSet<Name>>(){
            [TRUTH_VALUE] = new HashSet<Name>{
                VERUM.Head as Name,
                FALSUM.Head as Name,
                NEUTRAL.Head as Name
            },
            [TRUTH_ASSERTION_FUNCTION] = new HashSet<Name>(){
                ASSERT.Head as Name,
                DENY.Head as Name
            },
            [TRUTH_CONFORMITY_FUNCTION] = new HashSet<Name>(){
                WILL.Head as Name,
                WOULD.Head as Name
            },
            [INDIVIDUAL] = new HashSet<Name>(){
                SELF.Head as Name,
                BOB.Head  as Name,
                EVAN.Head as Name,
                THIS.Head as Name
            },
            [PREDICATE] = new HashSet<Name>(){
                RED.Head    as Name,
                BLUE.Head   as Name,
                YELLOW.Head as Name,
                TOMATO.Head as Name,
                BANANA.Head as Name
            },
            [RELATION_2] = new HashSet<Name>(){
                IDENTITY.Head as Name,
                AT.Head       as Name,
                YIELDS.Head   as Name,
            },
            [TRUTH_FUNCTION] = new HashSet<Name>(){
                TRULY.Head  as Name,
                NOT.Head    as Name,
                STAR.Head   as Name,
                VERY.Head   as Name,
                GOOD.Head   as Name,
                PAST.Head   as Name,
                FUTURE.Head as Name,
            },
            [TRUTH_FUNCTION_2] = new HashSet<Name>(){
                AND.Head       as Name,
                OR.Head        as Name,
                IF.Head        as Name,
                THEREFORE.Head as Name,
                SINCE.Head     as Name,
                UNTIL.Head     as Name,
            },
            [INDIVIDUAL_TRUTH_RELATION] = new HashSet<Name>(){
                KNOW.Head as Name,
                MAKE.Head as Name,
            },
            [QUANTIFIER] = new HashSet<Name>(){
                SOME.Head as Name,
                ALL.Head  as Name
            },
            [MISC] = new HashSet<Name>(){
                ASK.Head as Name,
                SELECTOR.Head as Name,
                OMEGA.Head as Name,
                ITSELF.Head   as Name,
                CONVERSE.Head as Name,
                BETTER_BY_MORE.Head as Name,
                Expression.Geach(INDIVIDUAL, TRUTH_FUNCTION).Head as Name,
            },
        };
    List<RadialMenuItem> radialMenuItems = new List<RadialMenuItem>(); 

    int currentSliceIndex = -1;
    bool typeMenuOpen = false;
    bool constantMenuOpen = false;

    public delegate void WordCallback(Name name); 
    public event WordCallback wordCallback = null;

    void Start() {
        playerMouseLook = playerCamera.GetComponent<MouseLook>();
    }

    void Update() {
        HandleMenuOpen();
        if (typeMenuOpen || constantMenuOpen) {
            HighlightMenuItems();
            HandleMenuItemClick();
        }
    }

    public void HandleMenuOpen() {
        if (!typeMenuOpen && !constantMenuOpen) {
            OpenTypeMenu();
            // Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ExitMenu() {
        CloseTypeMenu();
        CloseConstantMenu();
        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OpenTypeMenu() {
        typeMenuOpen = true;
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
            radialMenuItem.transform.localScale *= new Vector2(ITEM_SCALE, ITEM_SCALE);
            radialMenuItem.Type = semanticType;
            radialMenuItem.SetTypeIcon();
            radialMenuItem.gameObject.GetComponent<CanvasRenderer>().SetColor(RenderingOptions.ColorsByType[semanticType]);
            radialMenuItems.Add(radialMenuItem);
            theta += sliceTheta;
        }
    }

    void CloseTypeMenu() {
        typeMenuOpen = false;
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
            radialMenuItem.transform.localScale *= new Vector2(ITEM_SCALE, ITEM_SCALE);
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
        var (r, theta) = GetMousePolar();

        if (r == 0) {
            foreach(var rmi in radialMenuItems) {
                rmi.Unhighlight();
            }
            return;
        }

        int sliceIndex = GetSliceIndex(radialMenuItems.Count, INITIAL_ANGLE_OFFSET, theta);
        if (sliceIndex != currentSliceIndex) {
            foreach(var rmi in radialMenuItems) {
                rmi.Unhighlight();
            }
            currentSliceIndex = sliceIndex;
        }
        radialMenuItems[currentSliceIndex].Highlight();
    }

    void HandleMenuItemClick() {
        if (Input.GetButtonDown("Select")) {
            var (r, theta) = GetMousePolar();

            if (r == 0) {
                return; 
            }

            int sliceIndex = GetSliceIndex(radialMenuItems.Count, INITIAL_ANGLE_OFFSET, theta);
            var rmi = radialMenuItems[currentSliceIndex];
            if (typeMenuOpen) {
                CloseTypeMenu();
                OpenConstantMenu(rmi.Type);
            } else {
                if (wordCallback != null) {
                    wordCallback(rmi.Name);
                }
                CloseConstantMenu();
                // ExitMenu();
            }
        }
    }

    // Returns the mouse angle from the cetner of the screen relative to the x axis in counter clockwise
    (double, double) GetMousePolar() {
        Vector2 mousePosition = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // if (mousePosition.x == 0f && mousePosition.y == 0f) {
        //     mousePosition = GetMousePosition();     
        // }
        double magnitude = mousePosition.magnitude;
        double angle = Mathf.Atan2(mousePosition.y, mousePosition.x);
        if (angle < 0) {
            angle += 2f * Math.PI;
        }
        return (magnitude, angle);
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
