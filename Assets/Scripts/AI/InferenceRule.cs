using System;
using System.Text;

using static Expression;

public class InferenceRule
{
    public Expression[] Premises;
    public Expression[] Assumptions;
    public Expression[] Conclusions;

    // This means that the rule will recur infinitely
    // if no upper bound is place on its application.
    // The premises yield a different expression than
    // the input, but will still apply as input.
    public bool IsExpansive { get; private set; } = false;

    public InferenceRule(Expression[] premises, Expression[] assumptions, Expression[] conclusions)
    {
        Premises = premises;
        Assumptions = assumptions;
        Conclusions = conclusions;
    }

    public static InferenceRule Contrapose(InferenceRule rule) {
        Expression[] newConclusions = new Expression[rule.Premises.Length];
        for (int i = 0; i < rule.Premises.Length; i++) {
            newConclusions[i] = new Expression(Expression.NOT, rule.Premises[i]);
        }
        Expression[] newPremises = new Expression[rule.Conclusions.Length];
        for (int i = 0; i < rule.Conclusions.Length; i++) {
            newPremises[i] = new Expression(Expression.NOT, rule.Conclusions[i]);
        }

        return new InferenceRule(newPremises, rule.Assumptions, newConclusions);
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();
        for (int i = 0; i < Premises.Length; i++) {
            s.Append("M |- ");
            s.Append(Premises[i]);
            s.Append(";");
        }

        for (int i = 0; i < Assumptions.Length; i++) {
            s.Append("M :: ");
            s.Append(Assumptions[i]);
            s.Append(";");
        }

        s.Append(" => ");

        for (int i = 0; i < Conclusions.Length; i++) {
            s.Append("M |- ");
            s.Append(Conclusions[i]);
            s.Append(";");
        }

        s.Append("\n");
        return s.ToString();
    }

    public static readonly InferenceRule VERUM_INTRODUCTION =
        new InferenceRule(
            new Expression[]{},
            new Expression[]{},
            new Expression[]{VERUM});

    public static readonly InferenceRule VEROUS_INTRODUCTION =
        new InferenceRule(
            new Expression[]{},
            new Expression[]{},
            new Expression[]{new Expression(VEROUS, XE)});

    public static readonly InferenceRule TRULY_INTRODUCTION =
        new InferenceRule(
            new Expression[]{ST},
            new Expression[]{},
            new Expression[]{new Expression(TRULY, ST)});

    public static readonly InferenceRule DOUBLE_NEGATION_INTRODUCTION =
        new InferenceRule(
            new Expression[]{ST},
            new Expression[]{},
            new Expression[]{new Expression(NOT, new Expression(NOT, ST))});

    public static readonly InferenceRule CONJUNCTION_INTRODUCTION =
        new InferenceRule(
            new Expression[]{ST, TT},
            new Expression[]{},
            new Expression[]{new Expression(AND, ST, TT)});

    public static readonly InferenceRule DISJUNCTION_INTRODUCTION_LEFT =
        new InferenceRule(
            new Expression[]{ST},
            new Expression[]{},
            new Expression[]{new Expression(OR, ST, TT)});

    public static readonly InferenceRule DISJUNCTION_INTRODUCTION_RIGHT =
        new InferenceRule(
            new Expression[]{TT},
            new Expression[]{},
            new Expression[]{new Expression(OR, ST, TT)});

    public static readonly InferenceRule EXISTENTIAL_INTRODUCTION =
        new InferenceRule(
            new Expression[]{new Expression(FET, XE), new Expression(GET, XE)},
            new Expression[]{},
            new Expression[]{new Expression(SOME, FET, GET)});

    public static readonly InferenceRule UNIVERSAL_ELIMINATION =
        new InferenceRule(
            new Expression[]{new Expression(ALL, FET, GET), new Expression(FET, XE)},
            new Expression[]{},
            new Expression[]{new Expression(GET, XE)});

    public static readonly InferenceRule MODUS_PONENS =
        new InferenceRule(
            new Expression[]{new Expression(IF, ST, TT), ST},
            new Expression[]{},
            new Expression[]{TT});

    public static readonly InferenceRule ITSELF_INTRODUCTION =
        new InferenceRule(
            new Expression[]{new Expression(REET, XE, XE)},
            new Expression[]{},
            new Expression[]{new Expression(ITSELF, REET, XE)});

    public static readonly InferenceRule ITSELF_ELIMINATION =
        new InferenceRule(
            new Expression[]{new Expression(ITSELF, REET, XE)},
            new Expression[]{},
            new Expression[]{new Expression(REET, XE, XE)});

    public static readonly InferenceRule BETTER_ANTISYMMETRY =
        new InferenceRule(
            new Expression[]{new Expression(BETTER, ST, TT)},
            new Expression[]{},
            new Expression[]{new Expression(NOT, new Expression(BETTER, TT, ST))});

    public static readonly InferenceRule BETTER_TRANSITIVITY =
        new InferenceRule(
            new Expression[]{new Expression(BETTER, ST, TT), new Expression(BETTER, TT, PT)},
            new Expression[]{},
            new Expression[]{new Expression(BETTER, ST, PT)});

    public static readonly InferenceRule PERCEPTUAL_BELIEF =
        new InferenceRule(
            new Expression[]{new Expression(PERCEIVE, SELF, ST)},
            new Expression[]{new Expression(VERIDICAL, SELF, ST)},
            new Expression[]{ST});

    public static readonly InferenceRule SOMETIMES_INTRODUCTION =
        new InferenceRule(
            new Expression[]{new Expression(FTF, ST), new Expression(GTF, ST)},
            new Expression[]{},
            new Expression[]{new Expression(SOMETIMES, FTF, GTF)});

    public static readonly InferenceRule ALWAYS_ELIMINATION =
        new InferenceRule(
            new Expression[]{new Expression(ALWAYS, FTF, GTF), new Expression(FTF, ST)},
            new Expression[]{},
            new Expression[]{new Expression(GTF, ST)});

    public static readonly InferenceRule SELF_BELIEF_INTRODUCTION =
        new InferenceRule(new Expression[]{ST},
            new Expression[]{},
            new Expression[]{new Expression(BELIEVE, SELF, ST)});

    public static readonly InferenceRule NEGATIVE_SELF_BELIEF_INTRODUCTION =
        new InferenceRule(new Expression[]{},
            new Expression[]{new Expression(NOT, ST)},
            new Expression[]{new Expression(NOT, new Expression(BELIEVE, SELF, ST))});

    public static readonly InferenceRule SYMMETRY_OF_LOCATION =
        new InferenceRule(new Expression[]{
            new Expression(AT, XE, YE)},
            new Expression[]{},
            new Expression[]{new Expression(AT, YE, XE)});

    // able(self, at(self, x)), at(x, y) => able(self, at(self, y))
    public static readonly InferenceRule TRANSITIVITY_OF_LOCATION =
        new InferenceRule(
            new Expression[]{
                new Expression(AT, XE, YE),
                new Expression(AT, YE, ZE)
            },
            new Expression[]{},
            new Expression[]{new Expression(AT, XE, ZE)});

    public static readonly InferenceRule LIKES_ALL_TO_LIKES =
        new InferenceRule(
            new Expression[]{
                new Expression(LIKES_ALL, XE, FET),
                new Expression(FET, YE)
            },
            new Expression[]{},
            new Expression[]{new Expression(LIKES, XE, YE)}
        );
}
