using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using static Expression;

public class InferenceRule
{
    public bool IsContractive { protected set; get; }
    protected List<Expression> Premises;
    protected List<Expression> Conclusions;
    protected bool IsContraposable;
    public Expression Require     { protected set; get; }
    public Expression Supposition { protected set; get; }
    

    public InferenceRule(
        List<Expression> premises,
        List<Expression> conclusions,
        Expression require = null,
        Expression supposition = null) {
        Premises = premises;
        Conclusions = conclusions;
        Require = require;
        Supposition = supposition;
    }

    public List<Expression> Apply(Expression e) {
        UnityEngine.Debug.Assert(IsContraposable || Conclusions.Count == 1);

        var lemmas = new List<Expression>();

        foreach (var conclusion in Conclusions) {
            var matches = conclusion.Unify(e);
            foreach (var match in matches) {
                foreach (var premise in Premises) {
                    lemmas.Add(premise.Substitute(match));
                }
                if (Conclusions.Count > 1) {
                    // in case strengthening structural rule fails :)
                    bool conclusionMatchedOnce = false;
                    foreach (var otherConclusion in Conclusions) {
                        if (!conclusionMatchedOnce && !otherConclusion.Equals(conclusion)) {
                            conclusionMatchedOnce = true;
                            continue;
                        }
                        lemmas.Add(new Expression(NOT, otherConclusion.Substitute(match)));
                    }
                }
            }
            if (matches.Count > 0) {
                return lemmas;
            }
        }

        return null;
    }

    public InferenceRule Contrapose() {
        var newPremises = new List<Expression>();
        foreach (var conclusion in Conclusions) {
            newPremises.Add(conclusion.HeadedBy(NOT) ?
                conclusion.GetArgAsExpression(0) :
                new Expression(NOT, conclusion));
        }
        var newConclusions = new List<Expression>();
        foreach (var premise in Premises) {
            newConclusions.Add(premise.HeadedBy(NOT) ?
                premise.GetArgAsExpression(0) :
                new Expression(NOT, premise));
        }

        return new InferenceRule(newPremises, newConclusions, Require, Supposition);
    }

    public InferenceRule Instantiate(Expression e) {
        Dictionary<Variable, Expression> match = null;
        foreach (var premise in Premises) {
            var matches = premise.Unify(e);
            if (matches.Count == 0) {
                continue;
            }
            match = matches.First();
            break;
        }
        if (match == null) {
            return null;
        }
        var instantiatedPremises = new List<Expression>();

        foreach (var premise in Premises) {
            instantiatedPremises.Add(premise.Substitute(match));
        }
        var instantiatedConclusions = new List<Expression>();
        foreach (var conclusion in Conclusions) {
            instantiatedConclusions.Add(conclusion.Substitute(match));
        }
        var instantiatedRequire = Require == null ? null : Require.Substitute(match);
        var instantiatedSupposition = Supposition == null ? null : Supposition.Substitute(match);

        return new InferenceRule(
            instantiatedPremises,
            instantiatedConclusions,
            instantiatedRequire,
            instantiatedSupposition);
    }

    public override string ToString() {
        StringBuilder s = new StringBuilder();
        foreach (var premise in Premises) {
            s.Append(premise);
            s.Append(", ");
        }
        if (Premises.Count > 0) {
            s.Remove(s.Length - 2, 2);
        }
        if (Require != null || Supposition != null) {
            s.Append(" [");
            s.Append(Supposition);
            s.Append("|");
            s.Append(Require);
            s.Append("]");
        }
        s.Append(" |- ");
        foreach (var conclusion in Conclusions) {
            s.Append(conclusion);
            s.Append(", ");
        }
        if (Premises.Count > 0) {
            s.Remove(s.Length - 2, 2);
        }

        return s.ToString();
    }

    public static readonly InferenceRule[] DEFAULT_RULES = new InferenceRule[]{
        // verum
        new InferenceRule(new List<Expression>{}, new List<Expression>{VERUM}),
        // falsum
        new InferenceRule(new List<Expression>{FALSUM}, new List<Expression>{}),
        
        // truly
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(TRULY, ST)}),
        new InferenceRule(new List<Expression>{new Expression(TRULY, ST)}, new List<Expression>{ST}),
        // not
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(NOT, new Expression(NOT, ST))}),
        new InferenceRule(new List<Expression>{new Expression(NOT, new Expression(NOT, ST))}, new List<Expression>{ST}),

        // *
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(NOT, new Expression(STAR, ST))}),

        // or
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(OR, ST, TT)}),
        new InferenceRule(new List<Expression>{TT}, new List<Expression>{new Expression(OR, ST, TT)}),
        new InferenceRule(new List<Expression>{new Expression(OR, ST, TT)}, new List<Expression>{ST, TT}),
        // and
        new InferenceRule(new List<Expression>{ST, TT}, new List<Expression>{new Expression(AND, ST, TT)}),
        new InferenceRule(new List<Expression>{new Expression(AND, ST, TT)}, new List<Expression>{ST}),
        new InferenceRule(new List<Expression>{new Expression(AND, ST, TT)}, new List<Expression>{TT}),

        // if
        new InferenceRule(
            new List<Expression>{ST}, new List<Expression>{new Expression(IF, ST, TT)},
            require: TT,
            supposition: TT),
        new InferenceRule(
            new List<Expression>{new Expression(IF, ST, TT), TT},
            new List<Expression>{ST}),

        // therefore
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(THEREFORE, ST, TT)}, require: TT),

        // some
        new InferenceRule(
            new List<Expression>{new Expression(FET, XE), new Expression(GET, XE)},
            new List<Expression>{new Expression(SOME, FET, GET)}),
        // all
        new InferenceRule(
            new List<Expression>{new Expression(ALL, FET, GET), new Expression(FET, XE)},
            new List<Expression>{new Expression(GET, XE)}),

        // very
        new InferenceRule(new List<Expression>{new Expression(VERY, ST)}, new List<Expression>{ST}),

        // =
        new InferenceRule(new List<Expression>{}, new List<Expression>{new Expression(IDENTITY, XE, XE)}),
        new InferenceRule(new List<Expression>{new Expression(IDENTITY, YE, XE)}, new List<Expression>{new Expression(IDENTITY, XE, YE)}),
        new InferenceRule(
            new List<Expression>{new Expression(STAR, new Expression(NOT, new Expression(IDENTITY, XE, YE)))},
            new List<Expression>{new Expression(NOT, new Expression(IDENTITY, XE, YE))}),

        // converse
        new InferenceRule(
            new List<Expression>{new Expression(REET, YE, XE)},
            new List<Expression>{new Expression(CONVERSE, REET, XE, YE)}),
        new InferenceRule(
            new List<Expression>{new Expression(CONVERSE, REET, XE, YE)},
            new List<Expression>{new Expression(REET, YE, XE)}),

        // past
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(PAST, ST)}),

        // good
        new InferenceRule(
            new List<Expression>{new Expression(GOOD, new Expression(NOT, ST))},
            new List<Expression>{new Expression(NOT, new Expression(GOOD, ST))}),

        // fruit
        new InferenceRule(new List<Expression>{new Expression(TOMATO, XE)}, new List<Expression>{new Expression(FRUIT, XE)}),
        new InferenceRule(new List<Expression>{new Expression(BANANA, XE)}, new List<Expression>{new Expression(FRUIT, XE)}),
    };
}
