using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkspaceController : MonoBehaviour
{
    #region References
    GameObject Workspace;
    PlayerMovement PlayerMovement;
    MouseLook MouseLook;
    RadialMenu RadialMenu;
    GameObject HighlightPoint;
    public GameObject Pointer;
    #endregion

    #region Fields
    bool IsWorkspaceActive = false;
    bool IsRadialMenuActive = false;
    static int slotsX = 8;
    static int slotsY = 8;
    GameObject[,] slots = new GameObject[slotsY, slotsX];
    int cursorX = 0;
    int cursorY = 0;

    float lastStep = 0.0f;
    #endregion

    public void SpawnWord(Constant word) {
        GameObject wordContainer = ArgumentContainer.From(new Expression(word));
        
        ArgumentContainer wordContainerScript =
            wordContainer.GetComponent<ArgumentContainer>();
        
        wordContainerScript.GenerateVisual();

        wordContainer.transform.SetParent(Workspace.transform);
        wordContainer.transform.localRotation = Quaternion.identity;

        int width = wordContainerScript.Width;
        int height = wordContainerScript.Height;

        wordContainer.transform.localScale =
            new Vector3(0.125f * 0.875f * width, 0.125f * 0.875f * height, 1);

        // find the next available slot that
        // can accommodate the expression
        for (int i = 0; i < slotsY; i++) {
            for (int j = 0; j < slotsX; j++) {
                bool empty = true;
                for (int h = 0; h < height; h++) {
                    if (!empty) {
                        break;
                    }
                    if (i + h >= slotsY) {
                        empty = false;
                        break;
                    }
                    for (int w = 0; w < width; w++) {
                        if (j + w >= slotsX) {
                            empty = false;
                            break;
                        }
                        if (slots[i + h, j + w] != null) {
                            empty = false;
                        }
                    }
                }
                if (empty) {
                    wordContainer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + width / 16.0f + 0.125f * j),
                            0.95f * ((0.5f - height / 16.0f) - 0.125f * i), -0.01f);

                    if (!Pointer.active) {
                        Pointer.active = true;
                        Pointer.transform.localPosition =
                            new Vector3(wordContainer.transform.localPosition.x,
                                wordContainer.transform.localPosition.y + (3.0f * height / 32.0f),
                                -0.01f);
                    }

                    for (int h = 0; h < height; h++) {
                        for (int w = 0; w < width; w++) {
                            slots[i + h, j + w] = wordContainer;
                        }
                    }
                    return;
                }
            }
        }

        Debug.Log("no more room :'(");
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < slotsY; i++) {
            for (int j = 0; j < slotsX; j++) {
                slots[i, j] = null;
            }
        }

        Workspace = GameObject.Find("Workspace");
        Workspace.SetActive(false);
        PlayerMovement = GetComponent<PlayerMovement>();
        MouseLook = GameObject.Find("Main Camera").GetComponent<MouseLook>();
        RadialMenu = GameObject.Find("RadialMenu Canvas").GetComponent<RadialMenu>();
        HighlightPoint = GameObject.Find("Highlight Point");

        RadialMenu.setWordSelectCallback(SpawnWord);
        RadialMenu.enabled = false;
        Pointer.active = false;
    }

    // Update is called once per frame
    void Update()
    {
        // the menu button was pressed
        if (Input.GetButtonDown("Menu")) {
            // the workspace is already open
            if (IsWorkspaceActive) {
                IsRadialMenuActive = true;
                RadialMenu.enabled = true;
                // do stuff with the radial menu
                RadialMenu.HandleMenuOpen();
            } else {
                // the workspace isn't open yet

                // lock the player's movement
                PlayerMovement.enabled = false;
                MouseLook.enabled = false;
                // HighlightPoint.SetActive(false);

                // set the workspace to active
                IsWorkspaceActive = true;
                Workspace.SetActive(true);
            }
        }

        // move the cursor to the expression to the right
        // of the current expression
        if (Input.GetAxis("Horizontal") > 0) {
            GameObject currentlySelected = slots[cursorY, cursorX];
            for (int i = cursorX; i < slotsX; i++) {
                GameObject o = slots[cursorY, i];
                if (o == null || o == currentlySelected) {
                    continue;
                }

                cursorX = i;
                Pointer.transform.localPosition =
                    new Vector3(o.transform.localPosition.x,
                        o.transform.localPosition.y + (3.0f * o.GetComponent<ArgumentContainer>().Height / 32.0f),
                        -0.01f);
                break;
            }
        }

        // move the cursor to the expression to the left
        // of the current expression
        if (Input.GetAxis("Horizontal") < 0) {
            GameObject currentlySelected = slots[cursorY, cursorX];
            for (int i = cursorX; i >= 0; i--) {
                GameObject o = slots[cursorY, i];
                if (o == null || o == currentlySelected) {
                    continue;
                }

                cursorX = i;
                Pointer.transform.localPosition =
                    new Vector3(o.transform.localPosition.x,
                        o.transform.localPosition.y + (3.0f * o.GetComponent<ArgumentContainer>().Height / 32.0f),
                        -0.01f);
                break;
            }
        }

        // move the cursor to the expression downard
        // from the current expression
        if (Input.GetAxis("Vertical") < 0) {
            GameObject currentlySelected = slots[cursorY, cursorX];
            for (int i = cursorY; i < slotsY; i++) {
                GameObject o = slots[i, cursorX];
                if (o == null || o == currentlySelected) {
                    continue;
                }

                cursorY = i;
                Pointer.transform.localPosition =
                    new Vector3(o.transform.localPosition.x,
                        o.transform.localPosition.y + (3.0f * o.GetComponent<ArgumentContainer>().Height / 32.0f),
                        -0.01f);
                break;
            }
        }

        // move the cursor to the expression upward
        // from the current expression
        if (Input.GetAxis("Vertical") > 0) {
            GameObject currentlySelected = slots[cursorY, cursorX];
            for (int i = cursorY; i >= 0; i--) {
                GameObject o = slots[i, cursorX];
                if (o == null || o == currentlySelected) {
                    continue;
                }

                cursorY = i;
                Pointer.transform.localPosition =
                    new Vector3(o.transform.localPosition.x,
                        o.transform.localPosition.y + (3.0f * o.GetComponent<ArgumentContainer>().Height / 32.0f),
                        -0.01f);
                break;
            }
        }

        if (Input.GetButtonDown("Cancel") && IsWorkspaceActive) {
            if (IsRadialMenuActive) {
                RadialMenu.ExitMenu();
                RadialMenu.enabled = false;
                IsRadialMenuActive = false;
            } else {
                IsWorkspaceActive = false;
                Workspace.SetActive(false);
                PlayerMovement.enabled = true;
                MouseLook.enabled = true;
                // HighlightPoint.SetActive(true);
            }
        }
    }
}
