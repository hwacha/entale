using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SemanticType;
using static RenderingOptions;

class DrawInfo {
    public Argument Argument { get; }
    public int X { get; }
    public int Y { get; }

    public DrawInfo(Argument argument, int x, int y) {
        Argument = argument;
        X = x;
        Y = y;
    }
}

public class ExpressionContainer : MonoBehaviour
{
    public Expression Expression;

    // Start is called before the first frame update
    void Start()
    {
        GenerateVisual();
    }

    void Update() {
        transform.Rotate(0, 0, 10 * Mathf.Sin(Time.time * 10) * Time.deltaTime);
    }

    // Resizes the quad according to the dimensions of the
    // expression, and attaches a texture made by
    // drawing the expression recursively.
    void GenerateVisual() {
        int width = GetWidth(Expression);
        int height = GetHeight(Expression);

        gameObject.transform.localScale = new Vector3(
                gameObject.transform.localScale.x * width,
                gameObject.transform.localScale.y * height,
                gameObject.transform.localScale.z);

        // mipmap levels turned off. May want to turn this on later.
        var texture = new Texture2D(width * Scale, height * Scale);

        var drawStack = new Stack<DrawInfo>();
        drawStack.Push(new DrawInfo(Expression, 0, 0));

        bool isFirstLevel = true;

        while (drawStack.Count != 0)
        {
            DrawInfo currentDrawInfo = drawStack.Pop();
            int currentWidth;
            int currentHeight;

            if (currentDrawInfo.Argument is Expression && !isFirstLevel) {
                currentWidth = SecondCallGetWidth((Expression) currentDrawInfo.Argument);
                currentHeight = SecondCallGetHeight((Expression) currentDrawInfo.Argument);
            } else {
                 currentWidth = GetWidth(currentDrawInfo.Argument);
                 currentHeight = GetHeight(currentDrawInfo.Argument);
            }

            int scaledHeight = height * Scale;
            
            int scaledCurrentX = currentDrawInfo.X * Scale;
            int scaledCurrentY = currentDrawInfo.Y * Scale;

            int scaledCurrentWidth = currentWidth * Scale;
            int scaledCurrentHeight = currentHeight * Scale;

            int endingY = scaledHeight - scaledCurrentY;
            int startingY = endingY - scaledCurrentHeight;

            int startingX = scaledCurrentX;
            int endingX = startingX + scaledCurrentWidth;

            if (ReadVertically) {
                Debug.Log("ReadVertically: Not yet working. Defaulting to horizontal reading order.");
                // TODO (the previous code swaps the key variables)
            }

            // fill with the color of the expression's semantic type.
            for (int y = startingY; y < endingY; y++)
            {
                for (int x = startingX; x < endingX; x++)
                {
                    SemanticType fillType = currentDrawInfo.Argument.Type;
                    if (RenderingOptions.FillMode == FillMode.Complete) {
                        // already set
                    }

                    if (RenderingOptions.FillMode == FillMode.Output &&
                        fillType is FunctionalType &&
                        currentDrawInfo.Argument is Expression &&
                        isFirstLevel) {
                        fillType = ((FunctionalType) fillType).Output;   
                    }

                    if (RenderingOptions.FillMode == FillMode.Head && currentDrawInfo.Argument is Expression) {
                        fillType = ((Expression) currentDrawInfo.Argument).Head.Type;
                    }

                    Color typeColor = ColorsByType[fillType] - new Color(0, 0, 0, FillTransparency);
                    texture.SetPixel(x, y, typeColor);
                }
            }

            // northern border and southern border
            for (int y = startingY; y < startingY + BorderSize; y++)
            {
                for (int x = startingX; x < endingX; x++)
                {
                    Color typeColor =
                        ColorsByType[currentDrawInfo.Argument.Type] - new Color(0, 0, 0, BorderTransparency);
                    // northern border
                    texture.SetPixel(x, y + scaledCurrentHeight - BorderSize, typeColor);
                    // southern border
                    texture.SetPixel(x, y, typeColor);
                    
                }
            }

            // eastern and western border
            for (int y = startingY; y < endingY; y++) {
                for (int x = startingX; x < startingX + BorderSize; x++) {
                    Color typeColor =
                        ColorsByType[currentDrawInfo.Argument.Type] - new Color(0, 0, 0, BorderTransparency);
                    // west border
                    texture.SetPixel(x, y, typeColor);
                    // east border
                    texture.SetPixel(x + scaledCurrentWidth - BorderSize, y, typeColor);
                }
            }

            // We're at an empty argument slot. Nothing left to do here.
            // @Note: we could draw the inaccessible argument slot, but that
            // involves more changes than I want to make.
            if (currentDrawInfo.Argument is Empty) {
                continue;
            }

            Expression currentExpression = (Expression) currentDrawInfo.Argument;
            Texture2D headTexture = Resources.Load<Texture2D>("Textures/Symbols/" + ((Expression) currentDrawInfo.Argument).Head.ID);

            // if we're an expression, we want to draw the head symbol
            // @Note this has to be in a specific RGBA format to work.
            if (headTexture != null) {
                int symbolSize = Scale - BorderSize * 2;

                // @Note Unity is being picky about these.
                // Seems like we just have give a texture of the
                // right size and format for now.
                // headTexture.Resize(Scale - BorderSize * 2, Scale - BorderSize * 2);
                // headTexture.Apply();

                int xOffset = 0;
                if (HeadSymbolPosition == Position.Left) {
                    xOffset = 0;
                }
                if (HeadSymbolPosition == Position.Center) {
                    xOffset = (currentWidth / 2) * Scale;
                    if (currentWidth % 2 == 0) {
                        xOffset -= Scale / 2;    
                    }
                }
                if (HeadSymbolPosition == Position.Right) {
                    xOffset = (currentWidth - 1) * Scale;
                }
                for (int y = 0; y < symbolSize; y++) {
                    for (int x = 0; x < symbolSize; x++) {
                        Color headPixelColor = headTexture.GetPixel(x, y) - new Color(0, 0, 0, BorderTransparency);
                        if (headPixelColor.a > 0) {
                            texture.SetPixel(
                                startingX + xOffset + x + BorderSize,
                                -Scale + startingY + scaledCurrentHeight + y + BorderSize,
                                headPixelColor);                            
                        }

                    }
                }
            }

            if (currentDrawInfo.Argument is Empty) {
                continue;
            }

            // Now, we push the arguments on to the draw stack.
            var nextX = currentDrawInfo.X;
            if (DrawFirstArgumentDiagonally && HeadSymbolPosition != Position.Right) {
                nextX++;
            }

            if (DrawInaccessibleArgumentSlot) {
                Debug.Log("DrawInaccessibleArgumentSlot: not implemented yet. Default to not drawing it.");
            }


            // We only skip empties if all arguments are empty.
            bool canSkipEmpties = true;
            for (int i = 0; i < currentExpression.NumArgs; i++) {
                if (currentExpression.GetArg(i) is Expression) {
                    canSkipEmpties = false;
                    break;
                }
            }

            for (int i = 0; i < currentExpression.NumArgs; i++) {
                var arg = currentExpression.GetArg(i);

                // we skip moving forward, because we won't draw this empty slot.
                if (arg is Empty && !isFirstLevel) {
                    if (!canSkipEmpties) {
                        nextX++;
                    }
                    continue;
                }
                
                drawStack.Push(new DrawInfo(arg, nextX, currentDrawInfo.Y + 1));

                if (arg is Expression) {
                    nextX += SecondCallGetWidth((Expression) arg);    
                } else {
                    nextX += GetWidth(arg);
                }
                
            }

            isFirstLevel = false;
        }

        texture.Apply();
        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
