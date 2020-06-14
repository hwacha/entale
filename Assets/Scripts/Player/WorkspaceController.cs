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
    #endregion

    #region Fields
    bool IsWorkspaceActive = false;
    bool IsRadialMenuActive = false;
    static int slotsX = 8;
    static int slotsY = 8;
    bool[,] slots = new bool[slotsY, slotsX];
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
            new Vector3(0.125f * width, 0.125f * height, 1);

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
                        if (slots[i + h, j + w]) {
                            empty = false;
                        }
                    }
                }
                if (empty) {
                    wordContainer.transform.localPosition =
                        new Vector3(-0.5f + width / 16.0f + 0.125f * j,
                            (0.5f - height / 16.0f) - 0.125f * i, -0.01f);

                    for (int h = 0; h < height; h++) {
                        for (int w = 0; w < width; w++) {
                            slots[i + h, j + w] = true;
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
                slots[i, j] = false;
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
                HighlightPoint.SetActive(false);

                // set the workspace to active
                IsWorkspaceActive = true;
                Workspace.SetActive(true);
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
                HighlightPoint.SetActive(true);
            }
        }
    }
}
