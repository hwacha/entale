using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SemanticType;

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

public enum Position {
    Left,
    Center,
    Right
}

public enum FillMode {
    Head,
    Output,
    Complete
}

public class ExpressionContainer : MonoBehaviour
{
    // THERE ARE ALL WAYS TO CUSTOMIZE THE WAY EXPRESSIONS ARE DRAWN
    #region Attributes
    // @Note: the opacity is 1 - transparency
    // The larger the number, the more transparent
    // the object will become
    public float FillTransparency = 0.6f;
    public float BorderTransparency = 0.2f;
    public int BorderSize = 8;
    public int Scale = 80;
    public bool DrawFirstArgumentDiagonally = false;
    public bool DrawInaccessibleArgumentSlot = false;
    public FillMode FillMode = FillMode.Head;
    public Position HeadSymbolPosition = Position.Center;
    public bool ReadVertically = false;
    #endregion

    public Expression Expression;
    

    // Start is called before the first frame update
    void Start()
    {
        GenerateVisual();
    }

    void Update() {
        transform.Rotate(0, 0, 10 * Mathf.Sin(Time.time * 10) * Time.deltaTime);
    }
    

    // because we want empty argument slots
    // that are inaccessible not to be rendered,
    // we have a separate call for recurring.
    private int SecondCallGetWidth(Expression e) {
        int width = 1;

        int emptiesWidth = 0;
        bool allEmptyArguments = true;
        for (int i = 0; i < e.NumArgs; i++) {
            if (e.GetArg(i) is Expression) {
                width += SecondCallGetWidth((Expression) e.GetArg(i));
                allEmptyArguments = false;
            } else {
                emptiesWidth++;
            }
        }

        if (!allEmptyArguments) {
            width += emptiesWidth;
        }

        if (width == 1 || DrawFirstArgumentDiagonally) {
            return width;
        }

        return width - 1;
    }

    // returns the width of this expression.
    private int GetWidth(Argument arg) {
        int width = 1;

        if (arg is Empty) {
            return width;
        }

        Expression e = (Expression) arg;

        for (int i = 0; i < e.NumArgs; i++) {
            if (e.GetArg(i) is Empty) {
                width++;
            } else {
                width += SecondCallGetWidth((Expression) e.GetArg(i));    
            }
        }

        if (width == 1 || DrawFirstArgumentDiagonally) {
            return width;
        }

        return width - 1;
    }

    private int SecondCallGetHeight(Expression e) {
        int maxArgHeight = 0;
        for (int i = 0; i < e.NumArgs; i++) {
            var nextArg = e.GetArg(i);
            if (nextArg is Expression) {
                int argHeight = SecondCallGetHeight((Expression) nextArg);
                if (argHeight > maxArgHeight) {
                    maxArgHeight = argHeight;
                }
            }
        }
        return maxArgHeight + 1;
    }

    private int GetHeight(Argument arg) {
        if (arg is Empty) {
            return 1;
        }

        Expression e = (Expression) arg;

        int maxArgHeight = 0;
        for (int i = 0; i < e.NumArgs; i++) {
            var nextArg = e.GetArg(i);
            if (nextArg is Empty) {
                if (maxArgHeight < 1) {
                    maxArgHeight += 1;    
                }
            } else {
                int argHeight = SecondCallGetHeight((Expression) nextArg);
                if (argHeight > maxArgHeight) {
                    maxArgHeight = argHeight;
                }
            }
        }

        return maxArgHeight + 1;
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
                // TODO (the commented code swaps the key variables)
                // Solution is probably to adjust the values by some
                // negative value.

                // int temp;

                // temp = scaledCurrentX;
                // scaledCurrentX = scaledCurrentY;
                // scaledCurrentY = temp;

                // temp = scaledCurrentWidth;
                // scaledCurrentWidth = scaledCurrentHeight;
                // scaledCurrentHeight = temp;

                // temp = endingY;
                // endingY = endingX;
                // endingX = endingY;

                // temp = startingX;
                // startingX = startingY;
                // startingY = temp;
            }

            // fill with the color of the expression's semantic type.
            for (int y = startingY; y < endingY; y++)
            {
                for (int x = startingX; x < endingX; x++)
                {
                    SemanticType fillType = currentDrawInfo.Argument.Type;
                    if (FillMode == FillMode.Complete) {
                        // already set
                    }

                    if (FillMode == FillMode.Output &&
                        fillType is FunctionalType &&
                        currentDrawInfo.Argument is Expression &&
                        isFirstLevel) {
                        fillType = ((FunctionalType) fillType).Output;   
                    }

                    if (FillMode == FillMode.Head && currentDrawInfo.Argument is Expression) {
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

    private static readonly Dictionary<SemanticType, Color> ColorsByType = new Dictionary<SemanticType, Color>{
        [TRUTH_VALUE] = new Color(0.2f, 0.3f, 0.85f, 1),
        [INDIVIDUAL]  = new Color(0.9f, 0.2f, 0.01f, 1),
        [RELATION_2]  = new Color(0.8f, 0.9f, 0.1f, 1),
        [PREDICATE]   = new Color(0.2f, 0.7f, 0.3f, 1),
        [QUANTIFIER]  = new Color(0.6f, 0.3f, 0.9f, 1),
        [TRUTH_FUNCTION_2] = new Color(0.1f, 0.9f, 0.86f, 1),
        [TRUTH_FUNCTION] = new Color(0.7f, 0.7f, 0.86f, 1),
        [ASSERTION]   = new Color(1, 1, 1, 1),
        [QUESTION]    = new Color(0.9f, 0.2f, 0.9f, 1),
        [TRUTH_ASSERTION_FUNCTION] = new Color(0.9f, 0.9f, 1, 1),
        [TRUTH_QUESTION_FUNCTION]  = new Color(0.9f, 0.4f, 0.7f, 1)
    };
}
