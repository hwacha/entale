using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

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
    public GameObject Camera;
    PlayerMovement PlayerMovement;
    MouseLook MouseLook;
    RadialMenu RadialMenu;
    GameObject HighlightPoint;
    public GameObject Pointer;

    public AudioSource MenuOpen;
    public AudioSource MenuClose;
    public AudioSource Error;
    public AudioSource PlaceExpression;
    public AudioSource CombineExpression;
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

    float prevAxisX = 0;
    float prevAxisY = 0;

    GameObject SelectedExpression = null;
    GameObject EquippedExpression = null;

    #endregion

    void SetPointerPosition(GameObject obj) {
        Pointer.transform.parent = obj.transform;

        var y = 0.5f + 0.3f / obj.GetComponent<ArgumentContainer>().Height;

        Pointer.transform.localPosition = new Vector3(0, y, 0);
        Pointer.transform.parent = Workspace.transform;

    }

    void SetPointerPosition(int x, int y) {
        var pointerX = 0.9375f * (-0.5f + 1 / 16.0f + 0.125f * x);
        var pointerY = 0.9375f * ((0.5f + 1 / 16.0f) - 0.125f * y) - 0.03f;
        Pointer.transform.localPosition = new Vector3(pointerX, pointerY, -0.02f);
    }

    public void SpawnWord(Constant word) {
        PlaceExpression.Play();
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

        wordContainer.transform.localScale = new Vector3(0.125f * 0.875f * width, 0.125f * 0.875f * height, 1);

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
                    if (!Pointer.activeSelf) {
                        cursorX = j;
                        cursorY = i;
                        Pointer.SetActive(true);
                        SetPointerPosition(wordContainer);
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
        // @NOTE the workspace should expand when there's
        // no room for another word.
        // Need to implement scroll and zoom.
        Debug.Log("no more room :'(");

        Error.Play();

        // TODO 12/30

        for (int i = 0; i < slotsY; i++) {
            for (int j = 0; j < slotsX; j++) {
                if (slots[i, j] != null) {
                    cursorX = j;
                    cursorY = i;
                    goto LoopEnd;
                }
            }
        }
        LoopEnd:
            SetPointerPosition(cursorX, cursorY);
            return null;
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

        SpawnDebugExpressions(
            new Expression(ASK, new Expression(SOME, BANANA, BANANA)),
            new Expression(ASK, new Expression(SOME, TOMATO, TOMATO)),
            new Expression(ASK, new Expression(SOME, BLUE, BLUE)),
            new Expression(ASK, new Expression(SOME, RED, RED))
        );

        Workspace.SetActive(false);
        PlayerMovement = GetComponent<PlayerMovement>();
        MouseLook = GameObject.Find("Main Camera").GetComponent<MouseLook>();
        RadialMenu = GameObject.Find("RadialMenu Canvas").GetComponent<RadialMenu>();
        HighlightPoint = GameObject.Find("Highlight Pointer");

        RadialMenu.setWordSelectCallback(SpawnWord);
        RadialMenu.enabled = false;
        Pointer.SetActive(false);
    }

    public void SpawnDebugExpressions(params Expression[] expressions) {
        for (int i = 0; i < expressions.Length; i++) {
            SpawnExpression(expressions[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // the menu button was pressed
        if (Input.GetButtonDown("Menu")) {
            // the workspace is already open
            if (IsWorkspaceActive) {
                IsRadialMenuActive = true;
                IsWorkspaceActive = false;
                RadialMenu.enabled = true;
                // do stuff with the radial menu
                RadialMenu.HandleMenuOpen();
                // play menu open sound
                MenuOpen.Play();
            } else if (!IsRadialMenuActive) {
                // the workspace isn't open yet
                if (EquippedExpression != null) {
                    SpawnExpression(EquippedExpression.GetComponent<ArgumentContainer>().Argument as Expression);
                    Destroy(EquippedExpression);
                }

                // lock the player's movement
                PlayerMovement.enabled = false;
                MouseLook.enabled = false;
                // HighlightPoint.SetActive(false);

                // set the workspace to active
                IsWorkspaceActive = true;
                Workspace.SetActive(true);

                // play menu open sound
                MenuOpen.Play();
            }
        }

        var axisX = Input.GetAxis("Horizontal");
        var axisY = Input.GetAxis("Vertical");

        bool newAxisX = System.Math.Sign(axisX) != System.Math.Sign(prevAxisX);
        bool newAxisY = System.Math.Sign(axisY) != System.Math.Sign(prevAxisY);

        prevAxisX = axisX;
        prevAxisY = axisY;

        // @NOTE: for each of cursor movements, the traversal should
        // be different from how it is. It should follow a zig-zag
        // traversal pattern that covers the whole rectangular region
        // starting from the position of the cursor.

        // move the cursor to the expression to the right
        // of the current expression
        if (IsWorkspaceActive && newAxisX && Input.GetAxis("Horizontal") > 0) {

            int bestX = -1;
            int bestY = -1;
            int bestNorm = int.MaxValue;
            GameObject bestObject = null;
            if (SelectedExpression == null) {
                GameObject currentlySelected = slots[cursorY, cursorX];
                for (int i = 0; i < slotsY; i++) {
                    for (int j = cursorX + 1; j < slotsX; j++) {
                        GameObject o = slots[i, j];
                        if (o == null || o == currentlySelected) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                            bestObject = o;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;
                
                    SetPointerPosition(bestObject);
                }
            } else {
                for (int i = 0; i < slotsY; i++) {
                    for (int j = cursorX + 1; j < slotsX; j++) {
                        if (openArgumentSlots[i, j] == null ||
                            !openArgumentSlots[i, j].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;

                    SetPointerPosition(cursorX, cursorY);
                }
            }
        }

        // move the cursor to the expression to the left
        // of the current expression
        if (IsWorkspaceActive && newAxisX && Input.GetAxis("Horizontal") < 0) {
            
            int bestX = -1;
            int bestY = -1;
            int bestNorm = int.MaxValue;
            GameObject bestObject = null;
            if (SelectedExpression == null) {
                GameObject currentlySelected = slots[cursorY, cursorX];
                for (int i = 0; i < slotsY; i++) {
                    for (int j = 0; j < cursorX; j++) {
                        GameObject o = slots[i, j];
                        if (o == null || o == currentlySelected) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                            bestObject = o;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;
                
                    SetPointerPosition(bestObject);
                }
            } else {
                for (int i = 0; i < slotsY; i++) {
                    for (int j = 0; j < cursorX; j++) {
                        if (openArgumentSlots[i, j] == null ||
                            !openArgumentSlots[i, j].Type
                                .Equals(
                                    SelectedExpression
                                    .GetComponent<ArgumentContainer>()
                                    .Argument.Type)) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;

                    SetPointerPosition(cursorX, cursorY);
                }
            }
        }

        // move the cursor to the expression downard
        // from the current expression
        if (IsWorkspaceActive && newAxisY && Input.GetAxis("Vertical") < 0) {
            int bestX = -1;
            int bestY = -1;
            int bestNorm = int.MaxValue;
            GameObject bestObject = null;
            if (SelectedExpression == null) {
                GameObject currentlySelected = slots[cursorY, cursorX];
                for (int i = cursorY + 1; i < slotsY; i++) {
                    for (int j = 0; j < slotsX; j++) {
                        GameObject o = slots[i, j];
                        if (o == null || o == currentlySelected) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                            bestObject = o;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;
                
                    SetPointerPosition(bestObject);
                }
            } else {
                for (int i = cursorY + 1; i < slotsY; i++) {
                    for (int j = 0; j < slotsX; j++) {
                        if (openArgumentSlots[i, j] == null ||
                            !openArgumentSlots[i, j].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;

                    SetPointerPosition(cursorX, cursorY);
                }
            }
        }

        // move the cursor to the expression upward
        // from the current expression
        if (IsWorkspaceActive && newAxisY && Input.GetAxis("Vertical") > 0) {

            int bestX = -1;
            int bestY = -1;
            int bestNorm = int.MaxValue;
            GameObject bestObject = null;
            if (SelectedExpression == null) {
                GameObject currentlySelected = slots[cursorY, cursorX];
                for (int i = 0; i < cursorY; i++) {
                    for (int j = 0; j < slotsX; j++) {
                        GameObject o = slots[i, j];
                        if (o == null || o == currentlySelected) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                            bestObject = o;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;
                
                    SetPointerPosition(bestObject);
                }
            } else {
                for (int i = 0; i < cursorY; i++) {
                    for (int j = 0; j < slotsX; j++) {
                        if (openArgumentSlots[i, j] == null ||
                            !openArgumentSlots[i, j].Type.Equals(SelectedExpression.GetComponent<ArgumentContainer>().Argument.Type)) {
                            continue;
                        }

                        int dx = j - cursorX;
                        int dy = i - cursorY;

                        int norm = (dx * dx) + (dy * dy);

                        if (norm < bestNorm) {
                            bestX = j;
                            bestY = i;
                            bestNorm = norm;
                        }
                    }
                }

                if (bestX != -1 && bestY != -1) {
                    cursorX = bestX;
                    cursorY = bestY;

                    SetPointerPosition(cursorX, cursorY);
                }
            }
        }

        if (IsWorkspaceActive && Input.GetButtonDown("Select")) {
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

                        SetPointerPosition(j, i);

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

                    Pointer.SetActive(false);

                    CombineExpression.Play();
                    SpawnExpression(combinedExpression);

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

        if (Input.GetButtonDown("Use")) {
            if (IsWorkspaceActive) {
                if (SelectedExpression == null) {
                    SelectedExpression = slots[cursorY, cursorX];
                }
                if (SelectedExpression != null) {
                    // set expression to center of screen
                    SelectedExpression.transform.SetParent(Workspace.transform.parent);
                    SelectedExpression.transform.localPosition = new Vector3(0, 0, 0.5f);

                    // @TODO move the pointer to another expression, if one exists
                    // (this should probably be its own method)

                    // disable the workspace
                    IsWorkspaceActive = false;
                    Workspace.SetActive(false);
                    PlayerMovement.enabled = true;
                    MouseLook.enabled = true;

                    bool foundNewExpression = false;
                    for (int i = 0; i < slotsY; i++) {
                        for (int j = 0; j < slotsX; j++) {
                            if (slots[i, j] == SelectedExpression) {
                                slots[i, j] = null;
                            } else if (!foundNewExpression && slots[i, j] != null) {
                                cursorX = j;
                                cursorY = i;
                                foundNewExpression = true;
                            }
                        }
                    }

                    // set equipped expression
                    EquippedExpression = SelectedExpression;
                    SelectedExpression = null;

                    if (foundNewExpression) {
                        SetPointerPosition(slots[cursorY, cursorX]);
                    } else {
                        Pointer.SetActive(false);
                    }
                }
            } else if (!IsRadialMenuActive) {
                if (EquippedExpression != null) {
                    // Raycast
                    RaycastHit hit;                    
                    bool collided = Physics.Raycast(
                        Camera.transform.position,
                        // Camera.transform.forward,
                        Camera.transform.TransformDirection(Vector3.forward),
                        out hit,
                        10,
                        1 << 10);

                    if (collided) {
                        var targetedObject = hit.transform.gameObject;
                        // @Note the object should be displayed
                        // and received by other NPC's sensors,
                        // not sent directly. But this is less
                        // error-prone to start out with.
                        var targetedMentalState = targetedObject.GetComponent<MentalState>();
                        var targetedActuator = targetedObject.GetComponent<Actuator>();

                        if (targetedMentalState != null && targetedActuator != null) {
                            targetedActuator.StartCoroutine(
                                targetedActuator.RespondTo(EquippedExpression.GetComponent<ArgumentContainer>().Argument as Expression, Expression.CHARLIE));

                            Destroy(EquippedExpression);
                            EquippedExpression = null;
                        }
                    }
                }
            }
        }

        if (Input.GetButtonDown("Cancel")) {
            if (IsRadialMenuActive) {
                RadialMenu.ExitMenu();
                RadialMenu.enabled = false;
                IsRadialMenuActive = false;
                IsWorkspaceActive = true;
                MenuClose.Play();
            } else if (SelectedExpression != null) {
                SetPointerPosition(SelectedExpression);
                SelectedExpression = null;
                MenuClose.Play();
            } else if (IsWorkspaceActive) {
                IsWorkspaceActive = false;
                Workspace.SetActive(false);
                PlayerMovement.enabled = true;
                MouseLook.enabled = true;
                MenuClose.Play();
            }

            if (!IsWorkspaceActive && !IsRadialMenuActive && EquippedExpression != null) {
                // SpawnExpression(EquippedExpression.GetComponent<ArgumentContainer>().Argument as Expression);
                Destroy(EquippedExpression);
                MenuClose.Play();
            }
            
            // HighlightPoint.SetActive(true);
        }
    }
}
