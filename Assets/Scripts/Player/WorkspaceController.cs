﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenArgumentInfo {
    public int ParentX {get; private set;}
    public int ParentY {get; private set;}
    public SemanticType Type {get; private set;}
    public GameObject ArgumentSlotContainer {get; private set;}
    public GameObject ParentContainer {get; private set;}
    public Argument[] Arguments;

    public OpenArgumentInfo(int parentX, int parentY,
        GameObject argumentSlotContainer,
        GameObject parentContainer,
        List<Argument> arguments) {
        ParentX = parentX;
        ParentY = parentY;
        ArgumentSlotContainer = argumentSlotContainer;
        ParentContainer = parentContainer;
        
        Arguments = new Argument[arguments.Count];
        for (int i = 0; i < arguments.Count; i++) {
            Arguments[i] = arguments[i];
        }
        Type = Arguments[arguments.Count - 1].Type;
    }
}

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
    OpenArgumentInfo[,] openArgumentSlots = new OpenArgumentInfo[slotsY, slotsX];
    int cursorX = 0;
    int cursorY = 0;
    
    float timeBetweenSteps = 0.25f;
    float lastStepHorizontal = 0.25f;
    float lastStepVertical = 0.25f;

    GameObject SelectedExpression = null;

    #endregion

    public void SpawnWord(Constant word) {
        SpawnExpression(new Expression(word));
    }

    public GameObject SpawnExpression(Expression expression) {
        GameObject wordContainer = ArgumentContainer.From(expression);
        
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

                // we've found a rectangular region
                // that can fit this new container
                if (empty) {
                    // here, we add the open arguments in this expression
                    // to a map which will be used when expressions of the
                    // right type are selected.
                    if (wordContainerScript.Argument is Expression) {
                        List<Argument> arguments = new List<Argument>();
                        Expression e = (Expression) wordContainerScript.Argument;

                        int index = 0;
                        foreach (Transform child in wordContainer.transform) {
                            var script = child.gameObject.GetComponent<ArgumentContainer>();
                            if (script == null) {
                                continue;
                            }
                            if (script.Argument is Empty) {
                                var newArguments = new List<Argument>();
                                foreach (Argument arg in newArguments) {
                                    newArguments.Add(new Empty(arg.Type));
                                }
                                arguments.Add(script.Argument);
                                openArgumentSlots[i + 1, j + index] = new OpenArgumentInfo(j, i, script.gameObject, wordContainer, arguments);
                            }
                            index += script.Width;
                        }
                    }

                    // here, we set the container's position to the first available grid position.
                    wordContainer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + width / 16.0f + 0.125f * j),
                            0.95f * ((0.5f - height / 16.0f) - 0.125f * i), -0.01f);

                    // if no pointer is active on the workspace, we set the pointer active
                    // to this expression.
                    if (!Pointer.active) {
                        Pointer.active = true;
                        Pointer.transform.localPosition =
                            new Vector3(wordContainer.transform.localPosition.x,
                                wordContainer.transform.localPosition.y + (3.0f * height / 32.0f),
                                -0.01f);
                    }

                    // fill the slots in the grid with the ID
                    // of this container.
                    for (int h = 0; h < height; h++) {
                        for (int w = 0; w < width; w++) {
                            slots[i + h, j + w] = wordContainer;
                        }
                    }
                    return wordContainer;
                }
            }
        }
        Destroy(wordContainer);
        return null;

        // @NOTE the workspace should expand when there's
        // no room for another word.
        // Need to implement scroll and zoom.
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

        // @NOTE: for each of cursor movements, the traversal should
        // be different from how it is. It should follow a zig-zag
        // traversal pattern that covers the whole rectangular region
        // starting from the position of the cursor.

        // move the cursor to the expression to the right
        // of the current expression
        if (Input.GetAxis("Horizontal") > 0 && Time.time - lastStepHorizontal > timeBetweenSteps) {
            lastStepHorizontal = Time.time;

            if (SelectedExpression == null) {
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
            } else {
                for (int i = cursorX + 1; i < slotsX; i++) {
                    if (openArgumentSlots[cursorY, i] == null ||
                        !openArgumentSlots[cursorY, i].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                        continue;
                    }

                    cursorX = i;
                    Pointer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + 1 / 16.0f + 0.125f * cursorX),
                            0.95f * ((0.5f + 1 / 16.0f) - 0.125f * cursorY), -0.02f);
                    break;
                }
            }
        }

        // move the cursor to the expression to the left
        // of the current expression
        if (Input.GetAxis("Horizontal") < 0 && Time.time - lastStepHorizontal > timeBetweenSteps) {
            lastStepHorizontal = Time.time;
            if (SelectedExpression == null) {
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
            } else {
                for (int i = cursorX - 1; i >= 0; i--) {
                    if (openArgumentSlots[cursorY, i] == null ||
                        !openArgumentSlots[cursorY, i].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                        continue;
                    }

                    cursorX = i;
                    Pointer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + 1 / 16.0f + 0.125f * cursorX),
                            0.95f * ((0.5f + 1 / 16.0f) - 0.125f * cursorY), -0.02f);
                    break;
                }
            }
        }

        // move the cursor to the expression downard
        // from the current expression
        if (Input.GetAxis("Vertical") < 0 && Time.time - lastStepVertical > timeBetweenSteps) {
            lastStepVertical = Time.time;
            if (SelectedExpression == null) {
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
            } else {
                for (int i = cursorY + 1; i < slotsY; i++) {
                    if (openArgumentSlots[i, cursorX] == null ||
                        !openArgumentSlots[i, cursorX].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                        continue;
                    }

                    cursorY = i;
                    Pointer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + 1 / 16.0f + 0.125f * cursorX),
                            0.95f * ((0.5f + 1 / 16.0f) - 0.125f * cursorY), -0.02f);
                    break;
                }
            }
        }

        // move the cursor to the expression upward
        // from the current expression
        if (Input.GetAxis("Vertical") > 0 && Time.time - lastStepVertical > timeBetweenSteps) {
            lastStepVertical = Time.time;
            if (SelectedExpression == null) {
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
            } else {
                for (int i = cursorY - 1; i >= 0; i--) {
                    if (openArgumentSlots[i, cursorX] == null ||
                        !openArgumentSlots[i, cursorX].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                        continue;
                    }

                    cursorY = i;
                    Pointer.transform.localPosition =
                        new Vector3(0.95f * (-0.5f + 1 / 16.0f + 0.125f * cursorX),
                            0.95f * ((0.5f + 1 / 16.0f) - 0.125f * cursorY), -0.02f);
                    break;
                }
            }
        }

        if (Input.GetButtonDown("Submit") && IsWorkspaceActive) {
            if (SelectedExpression == null) {
                SelectedExpression = slots[cursorY, cursorX];

                var selectedType = SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type;

                bool oneOpenSlot = false;
                for (int i = 0; i < slotsY; i++) {
                    if (oneOpenSlot) {
                        break;
                    }
                    for (int j = 0; j < slotsX; j++) {
                        OpenArgumentInfo openArgumentSlot = openArgumentSlots[i, j];
                        if (openArgumentSlot == null || !openArgumentSlot.Type.Equals(selectedType)) {
                            continue;
                        }

                        if (openArgumentSlot.Arguments.Length > 0 &&
                            openArgumentSlot.Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                            cursorX = j;
                            cursorY = i;
                        }

                        Pointer.transform.localPosition =
                            new Vector3(0.95f * (-0.5f + 1 / 16.0f + 0.125f * j),
                                0.95f * ((0.5f + 1 / 16.0f) - 0.125f * i), -0.02f);

                        oneOpenSlot = true;
                        break;
                    }
                }
            } else {
                // Debug.Log("Combining " + SelectedExpression + " at " + CurrentArguments[ArgumentIndex]);
                var openArgumentSlot = openArgumentSlots[cursorY, cursorX];
                Argument selectedArgument = SelectedExpression.GetComponent<ArgumentContainer>().Argument;
                if (openArgumentSlot != null && openArgumentSlot.Type.Equals(selectedArgument.Type)) {
                    openArgumentSlot.Arguments[openArgumentSlot.Arguments.Length - 1] = selectedArgument;

                    var parentContainerScript = openArgumentSlot.ParentContainer.GetComponent<ArgumentContainer>();

                    for (int i = openArgumentSlot.ParentY; i < openArgumentSlot.ParentY + parentContainerScript.Height; i++) {
                        for (int j = openArgumentSlot.ParentX; j < openArgumentSlot.ParentX + parentContainerScript.Width; j++) {
                            slots[i, j] = null;
                            openArgumentSlots[i, j] = null;
                        }
                    }
                    var combinedExpression =
                        new Expression(parentContainerScript.Argument as Expression,
                            openArgumentSlot.Arguments);
                    
                    Destroy(openArgumentSlot.ParentContainer);
                    
                    var o = SpawnExpression(combinedExpression);
                    
                    Pointer.transform.localPosition =
                        new Vector3(o.transform.localPosition.x,
                            o.transform.localPosition.y + (3.0f * o.GetComponent<ArgumentContainer>().Height / 32.0f),
                            -0.01f);

                    // TODO: make this more efficient
                    for (int i = 0; i < slotsY; i++) {
                        for (int j = 0; j < slotsX; j++) {
                            if (slots[i, j] == SelectedExpression) {
                                slots[i, j] = null;
                            }
                        }
                    }

                    Destroy(SelectedExpression);
                }
            }
        }

        if (Input.GetButtonDown("Cancel") && IsWorkspaceActive) {
            if (IsRadialMenuActive) {
                RadialMenu.ExitMenu();
                RadialMenu.enabled = false;
                IsRadialMenuActive = false;
            } else {
                if (SelectedExpression != null) {
                    Pointer.transform.localPosition =
                        new Vector3(SelectedExpression.transform.localPosition.x,
                            SelectedExpression.transform.localPosition.y +
                            (3.0f * SelectedExpression.GetComponent<ArgumentContainer>().Height / 32.0f),
                            -0.01f);
                    SelectedExpression = null;
                } else {
                    IsWorkspaceActive = false;
                    Workspace.SetActive(false);
                    PlayerMovement.enabled = true;
                    MouseLook.enabled = true;                    
                }

                // HighlightPoint.SetActive(true);
            }
        }
    }
}
