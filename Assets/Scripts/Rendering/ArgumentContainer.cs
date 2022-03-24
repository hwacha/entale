using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static RenderingOptions;

public class ArgumentContainer : MonoBehaviour {
    #region Fields
    public Argument Argument {get; protected set;}
    public int Width {get; protected set;}
    public int Height {get; protected set;}
    #endregion

    #region Presets
    // public Camera camera;
    #endregion

    public static GameObject From(Argument argument, int depth = 0) {
        GameObject argumentContainerPrefab = Resources.Load("Prefabs/ArgumentContainer") as GameObject;
        GameObject argumentContainerInstance =
            Instantiate(argumentContainerPrefab, new Vector3(0, 0, 0), Quaternion.identity);

        argumentContainerInstance.name =
            (argument is Expression)
            ? ((Expression) argument).Head.ToString()
            : argument.ToString();

        var argumentContainerScript = argumentContainerInstance.GetComponent<ArgumentContainer>();

        argumentContainerScript.Argument = argument;
        argumentContainerScript.Width = depth > 0 && (argument is Expression)
            ? SecondCallGetWidth(argument as Expression)
            : GetWidth(argument);

        argumentContainerScript.Height = depth > 0 && (argument is Expression)
            ? SecondCallGetHeight(argument as Expression)
            :  GetHeight(argument);

        argumentContainerInstance.transform.position = new Vector3(0, argumentContainerScript.Height * 0.5f, 0);

        argumentContainerInstance.transform.localScale =
            new Vector3(argumentContainerScript.Width, argumentContainerScript.Height, 0.01f);

        if (argument is Empty) {
            argumentContainerInstance.tag = "EmptyArgument";
            if (depth >= 2) {
                argumentContainerInstance.SetActive(false);
            }
        } else {
            var topLeftX =
                argumentContainerInstance.transform.position.x -
                (0.5f * argumentContainerInstance.transform.localScale.x);
            var topLeftY =
                argumentContainerInstance.transform.position.y +
                (0.5f * argumentContainerInstance.transform.localScale.y);

            Expression expression = (Expression) argument;
            int currentX = 0;
            if (DrawFirstArgumentDiagonally) {
                currentX++;
            }
            for (int i = 0; i < expression.NumArgs; i++) {
                var subArgument = From(expression.GetArg(i), depth + 1);

                var subArgumentScript = subArgument.GetComponent<ArgumentContainer>();
                var subArgumentWidth = subArgumentScript.Width;
                var subArgumentHeight = subArgumentScript.Height;

                var argTopLeftX = topLeftX + currentX;
                var argTopLeftY = topLeftY - 1;

                // this adjusts for center pivot.
                var xPos = (float) argTopLeftX + (0.5f * subArgumentWidth);
                var yPos = argTopLeftY - (0.5f * subArgumentHeight);

                subArgument.transform.position = new Vector3(xPos, yPos, -depth - 1);
                subArgument.transform.parent = argumentContainerInstance.transform;
                currentX += subArgumentWidth;
                
            }
        }

        return argumentContainerInstance;
    }

    private void SpawnFill(int depth = 0) {
        if (Argument is Expression || depth < 2) {
            gameObject.layer = LayerMask.NameToLayer("Pre-render Expression");

            var fillType = Argument.Type;
            if (RenderingOptions.FillMode == FillMode.Head) {
                if (Argument is Expression) {
                    fillType = ((Expression) Argument).Head.Type;    
                }
            } else if (RenderingOptions.FillMode == FillMode.Output) {
                if (fillType is FunctionalType) {
                    fillType = ((FunctionalType) fillType).Output;
                }
            } else if (RenderingOptions.FillMode == FillMode.Complete) {
                // Do nothing, already set
            }

            gameObject.GetComponent<Renderer>().material.SetColor("_Color", ColorsByType[fillType]);

            foreach (Transform childArgumentContainer in gameObject.transform) {
                ArgumentContainer childArgumentContainerScript =
                    childArgumentContainer.gameObject.GetComponent<ArgumentContainer>();
                if (childArgumentContainerScript != null) {
                    childArgumentContainerScript.SpawnFill(depth + 1);    
                }
            }
        }
    }

    // @Bug this is being called twice for lower levels,
    // because GetChildren(), etc. don't just look one level deep.
    // change to an array or another method of getting the children
    // to prevent this.
    public void SpawnBordersAndSymbols(int depth = 0) {
        if (Argument is Expression || depth < 2) {
            gameObject.layer = LayerMask.NameToLayer("Visible");

            if (Argument is Expression) {
                GameObject symbol = Instantiate(Resources.Load<GameObject>("Prefabs/Symbol"),
                    gameObject.transform.position + new Vector3(0, 0.5f * (Height - 1), 0),
                    gameObject.transform.rotation);

                symbol.transform.localScale =
                    Vector3.Scale(symbol.transform.localScale,
                        new Vector3(1 - (2 * BorderRatio), 1 - (2 * BorderRatio), 1));

                symbol.transform.parent = gameObject.transform;

                symbol.layer = LayerMask.NameToLayer("Pre-render Expression");

                var head = ((Expression) Argument).Head;
                var nameString = head is Name ? ((Name) head).ID : "";
                Texture2D symbolTexture =
                    Resources.Load<Texture2D>("Textures/Symbols/" + nameString);

                symbol.GetComponent<Renderer>().material.SetTexture("_MainTex", symbolTexture);
            }

            var fillType = Argument.Type;

            GameObject borderPrefab = Resources.Load("Prefabs/Border") as GameObject;

            GameObject westBorder =
                Instantiate(borderPrefab,
                    gameObject.transform.position,
                    gameObject.transform.rotation);
            GameObject eastBorder =
                Instantiate(borderPrefab,
                    gameObject.transform.position,
                    gameObject.transform.rotation);
            GameObject northBorder =
                Instantiate(borderPrefab,
                    gameObject.transform.position,
                    gameObject.transform.rotation);
            GameObject southBorder =
                Instantiate(borderPrefab,
                    gameObject.transform.position,
                    gameObject.transform.rotation);

            westBorder.transform.localScale =
                Vector3.Scale(westBorder.transform.localScale, new Vector3(BorderRatio, Height, 1));
            eastBorder.transform.localScale =
                Vector3.Scale(eastBorder.transform.localScale, new Vector3(BorderRatio, Height, 1));
            northBorder.transform.localScale =
                Vector3.Scale(northBorder.transform.localScale, new Vector3(Width, BorderRatio, 1));
            southBorder.transform.localScale =
                Vector3.Scale(southBorder.transform.localScale, new Vector3(Width, BorderRatio, 1));

            westBorder.transform.position += new Vector3(0.5f * (-Width + BorderRatio), 0, 0);
            eastBorder.transform.position += new Vector3(0.5f * (Width - BorderRatio), 0, 0);
            northBorder.transform.position += new Vector3(0, 0.5f * (Height - BorderRatio), 0);
            southBorder.transform.position += new Vector3(0, 0.5f * (-Height + BorderRatio), 0);

            westBorder.transform.SetParent(this.gameObject.transform);
            eastBorder.transform.SetParent(this.gameObject.transform);
            northBorder.transform.SetParent(this.gameObject.transform);
            southBorder.transform.SetParent(this.gameObject.transform);

            westBorder.layer = LayerMask.NameToLayer("Pre-render Expression");
            eastBorder.layer = LayerMask.NameToLayer("Pre-render Expression");
            northBorder.layer = LayerMask.NameToLayer("Pre-render Expression");
            southBorder.layer = LayerMask.NameToLayer("Pre-render Expression");

            westBorder.GetComponent<Renderer>().material.SetColor("_Color", ColorsByType[fillType]);
            eastBorder.GetComponent<Renderer>().material.SetColor("_Color", ColorsByType[fillType]);
            northBorder.GetComponent<Renderer>().material.SetColor("_Color", ColorsByType[fillType]);
            southBorder.GetComponent<Renderer>().material.SetColor("_Color", ColorsByType[fillType]);

            foreach (Transform childArgumentContainer in gameObject.transform) {
                ArgumentContainer childArgumentContainerScript =
                    childArgumentContainer.gameObject.GetComponent<ArgumentContainer>();
                if (childArgumentContainerScript != null) {
                    childArgumentContainerScript.SpawnBordersAndSymbols(depth + 1);    
                }
            }
        }
    }

    public void DisableRenderers() {
        gameObject.layer = LayerMask.NameToLayer("Visible");
        gameObject.GetComponent<Renderer>().enabled = false;

        foreach (Transform child in gameObject.transform) {
            ArgumentContainer childArgumentContainer = child.gameObject.GetComponent<ArgumentContainer>();
            if (childArgumentContainer != null) {
                childArgumentContainer.DisableRenderers();
            } else {
                child.gameObject.layer = gameObject.layer = LayerMask.NameToLayer("Visible");
                var r = child.gameObject.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }
    }

    public void GenerateVisual() {
        // @Note this may have to be a prefab.
        GameObject prePassCamera = GameObject.Find("ExpressionPrePass");
        prePassCamera.transform.parent = this.gameObject.transform;
        prePassCamera.transform.localPosition =
            Vector3.Scale(prePassCamera.transform.localPosition, new Vector3(0, 0, 0));
        prePassCamera.transform.position += new Vector3(0, 0, -Argument.Depth * 100);

        Camera prePassCameraComponent = prePassCamera.GetComponent<Camera>();
        prePassCameraComponent.enabled = true;
        prePassCameraComponent.orthographicSize = 0.5f * (Height > Width ? Height : Width);

        int textureWidth = Scale * Width;
        int textureHeight = Scale * Height;

        // place fills in front of the prepass camera
        SpawnFill();
        RenderTexture fillTexture =
            new RenderTexture(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32);

        // phase down its transparency
        // according to fill transparency
        prePassCameraComponent.targetTexture = fillTexture;
        prePassCamera.GetComponent<ApplyTransparency>().Opacity = 1 - FillTransparency;
        prePassCameraComponent.Render();

        // place borders and symbols in front of the prepass camera
        SpawnBordersAndSymbols();
        RenderTexture borderAndSymbolTexture =
            new RenderTexture(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32);

        // phase down its transparency
        // according to border transparency
        prePassCameraComponent.targetTexture = borderAndSymbolTexture;
        prePassCamera.GetComponent<ApplyTransparency>().Opacity = 1 - BorderTransparency;
        prePassCameraComponent.Render();

        DisableRenderers();

        // Now, we combine the two textures.
        GameObject fillQuad = Instantiate(Resources.Load<GameObject>("Prefabs/Symbol"),
                    gameObject.transform.position + new Vector3(0, 0, 1),
                    gameObject.transform.rotation);
        GameObject borderAndSymbolQuad = Instantiate(Resources.Load<GameObject>("Prefabs/Symbol"),
                    gameObject.transform.position,
                    gameObject.transform.rotation);

        fillQuad.transform.localScale =
            Vector3.Scale(fillQuad.transform.localScale, new Vector3(Width, Height, 1));
        borderAndSymbolQuad.transform.localScale =
            Vector3.Scale(borderAndSymbolQuad.transform.localScale, new Vector3(Width, Height, 1));

        fillQuad.layer = LayerMask.NameToLayer("Pre-render Expression");
        borderAndSymbolQuad.layer = LayerMask.NameToLayer("Pre-render Expression");

        fillQuad.GetComponent<Renderer>().material.SetTexture("_MainTex", fillTexture);
        borderAndSymbolQuad.GetComponent<Renderer>().material.SetTexture("_MainTex", borderAndSymbolTexture);

        RenderTexture finalTexture =
            new RenderTexture(textureWidth, textureHeight, 32, RenderTextureFormat.ARGB32);

        prePassCameraComponent.targetTexture = finalTexture;
        prePassCamera.GetComponent<ApplyTransparency>().Opacity = 1;
        prePassCameraComponent.Render();

        fillQuad.layer = LayerMask.NameToLayer("Visible");
        borderAndSymbolQuad.layer = LayerMask.NameToLayer("Visible");

        Destroy(fillQuad);
        Destroy(borderAndSymbolQuad);

        var renderer = gameObject.GetComponent<Renderer>();
        renderer.enabled = true;

        renderer.material = Resources.Load<Material>("Materials/ExpressionContainer");
        renderer.material.SetTexture("_MainTex", finalTexture);

        // @Note I can't set it inactive again, or else I can't reference it.
        // Maybe I should have a reference to the camera?
        prePassCamera.transform.parent = null;
        prePassCameraComponent.enabled = false;
        prePassCameraComponent.targetTexture = null;
    }

    // to be seen
    void OnWillRenderObject() {
        var mentalStateRef = Camera.current.GetComponent<ReferenceToMentalState>();
        if (mentalStateRef != null) {
            // check if this object is actually visible
            int layerMask = 1 << 11;

            var seerPosition = mentalStateRef.transform.position;
            var objPosition  = transform.position;

            bool collided = Physics.Raycast(seerPosition, objPosition - seerPosition, Mathf.Infinity, layerMask);

            // @Bug the raycasting is yielding false negatives
            if (collided || true) {
                Debug.Log("TODO: see expression");
            }
        }
    }
}
