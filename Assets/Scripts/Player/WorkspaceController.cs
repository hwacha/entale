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
    #endregion

    public void SpawnWord(Constant word) {
        GameObject wordContainer = ArgumentContainer.From(new Expression(word));
        wordContainer.GetComponent<ArgumentContainer>().GenerateVisual();
        wordContainer.transform.SetParent(Workspace.transform);
        wordContainer.transform.localScale = new Vector3(0.125f, 0.125f, 1);
        wordContainer.transform.localPosition = new Vector3(-0.5f + 0.125f, 0.5f - 0.125f, -0.01f);
        wordContainer.transform.localRotation = Quaternion.identity;
        
    }

    // Start is called before the first frame update
    void Start()
    {
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
