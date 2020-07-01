using System.Collections.Generic;
using UnityEngine;

using static SemanticType;
using static Expression;

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

public class RenderingOptions {
    // THESE ARE ALL WAYS TO CUSTOMIZE THE WAY EXPRESSIONS ARE DRAWN
    #region Attributes
    // @Note: the opacity is 1 - transparency
    // The larger the number, the more transparent
    // the object will become
    public static float FillTransparency = 0.6f;
    public static float BorderTransparency = 0.2f;
    public static int BorderSize = 8;
    public static float BorderRatio = 0.125f;
    public static int Scale = 320;
    public static bool DrawFirstArgumentDiagonally = false;
    public static bool DrawInaccessibleArgumentSlot = false;
    public static FillMode FillMode = FillMode.Head;
    public static Position HeadSymbolPosition = Position.Center;
    public static bool ReadVertically = false;

    public static readonly Dictionary<SemanticType, Color> ColorsByType =
        new Dictionary<SemanticType, Color>{
            [TRUTH_VALUE] = new Color(0.2f, 0.3f, 0.85f, 1),
            [INDIVIDUAL]  = new Color(0.9f, 0.2f, 0.01f, 1),
            [RELATION_2]  = new Color(0.8f, 0.9f, 0.1f, 1),
            [PREDICATE]   = new Color(0.2f, 0.7f, 0.3f, 1),
            [DETERMINER]  = new Color(0.8f, 0.3f, 0.7f, 1),
            [QUANTIFIER]  = new Color(0.6f, 0.3f, 0.9f, 1),
            [QUANTIFIER_PHRASE] = new Color(0.4f, 0.5f, 0.6f, 1),
            [TRUTH_FUNCTION_2] = new Color(0.1f, 0.9f, 0.86f, 1),
            [TRUTH_FUNCTION] = new Color(0.7f, 0.7f, 0.86f, 1),
            [ASSERTION]   = new Color(1, 1, 1, 1),
            [QUESTION]    = new Color(0.9f, 0.2f, 0.9f, 1),
            [CONFORMITY_VALUE] = new Color(0.0f, 0.1f, 0.1f, 1),
            [TRUTH_ASSERTION_FUNCTION] = new Color(0.9f, 0.9f, 1, 1),
            [TRUTH_QUESTION_FUNCTION]  = new Color(0.9f, 0.4f, 0.7f, 1),
            [TRUTH_CONFORMITY_FUNCTION] = new Color(0.4f, 0.5f, 0.5f, 1),
            [RELATION_2_REDUCER] = new Color(0.8f, 0.6f, 0.3f, 1),
            [INDIVIDUAL_TRUTH_RELATION] = new Color(0.6f, 0.3f, 0.2f, 1)
        };
    #endregion

    #region UtilityFunctions
    // because we want empty argument slots
    // that are inaccessible not to be rendered,
    // we have a separate call for recurring.
    public static int SecondCallGetWidth(Expression e) {
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
    public static int GetWidth(Argument arg) {
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

    public static int SecondCallGetHeight(Expression e) {
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

    public static int GetHeight(Argument arg) {
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
    #endregion
}
