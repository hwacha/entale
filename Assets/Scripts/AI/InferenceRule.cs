using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using static Expression;

public class InferenceRule
{
    public readonly List<Expression> Premises;
    public readonly List<Expression> Conclusions;
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

    private static bool IsExpressionPermissive(Expression e, params Expression[] prejacents) {
        if (e.Head is Variable) {
            return true;
        }

        return e.HeadedBy(prejacents) && (e.GetArgAsExpression(0).Head is Variable);
    }

    // 
    // is the rule premise-expansive?
    // 
    // e.g. very(P) |- P
    // 
    // gives an estimate of how likely
    // a rule is to apply to unwanted lemmas
    // in their conclusion
    // 
    // this rule is especially problematic
    // as the premise applies to its own conclusion.
    // But it's also for rules which are likely to
    // match the premises of other rules and lead
    // to nowhere.
    // 
    // any rule that's expansive is not directly
    // matched against. Instead, they're instantiated
    // when a sentence is added to the knowledge base
    // 
    public bool IsExpansive() {
        // simple check.
        // 
        // A more thorough approach would do some sort
        // of network analysis of which rules matched
        // which sentences on random assumptions and
        // inputs
        // 
        // but here, we just check to see if the head
        // of any of the conclusions is a variable.
        foreach (var conclusion in Conclusions) {
            if (IsExpressionPermissive(conclusion, NOT, STAR, VERY)) {
                return true;
            }
        }

        return false;
    }

    // for now, we don't contrapose because
    // we don't know what to do with conclusion-side
    // star sentences.
    public bool IsContraposable() {
        foreach (var premise in Premises) {
            if (premise.HeadedBy(STAR) || premise.PrejacentHeadedBy(NOT, NOT)) {
                return false;
            }
        }
        foreach (var conclusion in Conclusions) {
            if (conclusion.HeadedBy(STAR) || conclusion.PrejacentHeadedBy(NOT, NOT)) {
                return false;
            }
        }
        return true;
    }

    // 
    public bool IsPlusifiable() {
        foreach (var premise in Premises) {
            if (premise.HeadedBy(STAR, VERY)) {
                return false;
            }
        }
        foreach (var conclusion in Conclusions) {
            if (conclusion.HeadedBy(STAR, VERY)) {
                return false;
            }
        }
        return true;
    }

    public (List<Expression>, Expression, Expression) Apply(Expression e) {
        // UnityEngine.Debug.Assert(IsContraposable() || Conclusions.Count == 1);

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
                        if (!conclusionMatchedOnce && otherConclusion.Equals(conclusion)) {
                            conclusionMatchedOnce = true;
                            continue;
                        }
                        var subbedConclusion = otherConclusion.Substitute(match);
                        var negConclusion =
                            subbedConclusion.HeadedBy(NOT) ?
                            subbedConclusion.GetArgAsExpression(0) :
                            new Expression(NOT, subbedConclusion);
                        lemmas.Add(negConclusion);
                    }
                }
            }
            if (matches.Count > 0) {
                return (lemmas,
                    Require == null ? null : Require.Substitute(matches.First()),
                    Supposition == null ? null : Supposition.Substitute(matches.First()));
            }
        }

        return (null, null, null);
    }

    public InferenceRule Contrapose() {
        var newPremises = new List<Expression>();
        foreach (var conclusion in Conclusions) {
            newPremises.Add(conclusion.HeadedBy(NOT) ? conclusion.GetArgAsExpression(0) : new Expression(NOT, conclusion));
        }
        var newConclusions = new List<Expression>();
        foreach (var premise in Premises) {
            newConclusions.Add(premise.HeadedBy(NOT) ? premise.GetArgAsExpression(0) : new Expression(NOT, premise));
        }
        return new InferenceRule(newPremises, newConclusions, Require, Supposition);
    }

    public InferenceRule Plusify() {
        var newPremises = new List<Expression>();
        foreach (var premise in Premises) {
            newPremises.Add(new Expression(VERY, premise));
        }
        var newConclusions = new List<Expression>();
        foreach (var conclusion in Conclusions) {
            newConclusions.Add(new Expression(VERY, conclusion));
        }
        return new InferenceRule(newPremises, newConclusions, Require, Supposition);
    }

    public InferenceRule Instantiate(Expression e) {
        if ((e.Head.Type.Equals(SemanticType.TRUTH_VALUE)) ||
            e.HeadedBy(NOT, STAR) &&
            (e.GetArgAsExpression(0).Head.Type.Equals(SemanticType.TRUTH_VALUE))) {
            return null;
        }
        Dictionary<Variable, Expression> match = null;
        foreach (var premise in Premises) {
            if (IsExpressionPermissive(premise, NOT, STAR)) {
                continue;
            }
            var matches = premise.Unify(e);
            if (matches.Count == 0) {
                continue;
            }
            match = matches.First();
            break;
        }
        if (Conclusions.Count > 1) {
            foreach (var conclusion in Conclusions) {
                if (IsExpressionPermissive(conclusion, NOT, STAR)) {
                    continue;
                }
                var negE = e.HeadedBy(NOT) ? e.GetArgAsExpression(0) : new Expression(NOT, e);
                var matches = conclusion.Unify(negE);
                if (matches.Count == 0) {
                    continue;
                }
                match = matches.First();
                break;
            }
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

        var instantiatedRule = new InferenceRule(
            instantiatedPremises,
            instantiatedConclusions,
            instantiatedRequire,
            instantiatedSupposition);

        if (instantiatedRule.IsExpansive()) {
            return null;
        }

        // UnityEngine.Debug.Log(this + " <- " + e + " := " + instantiatedRule);

        return instantiatedRule;
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

    private static readonly InferenceRule[] BASE_RULES = new InferenceRule[]{
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
            new List<Expression>{new Expression(STAR, ST)}, new List<Expression>{new Expression(NOT, new Expression(IF, ST, TT))},
            require: TT, 
            supposition: TT),
        new InferenceRule(
            new List<Expression>{new Expression(IF, ST, TT), TT},
            new List<Expression>{ST}),

        // therefore
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(THEREFORE, ST, TT)}, require: TT),
        new InferenceRule(new List<Expression>{new Expression(STAR, ST)}, new List<Expression>{new Expression(NOT, new Expression(THEREFORE, ST, TT))}, require: TT),

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

        // omega
        new InferenceRule(new List<Expression>{new Expression(OMEGA, FTF, ST)},
            new List<Expression>{new Expression(FTF, ST)}),
        // contraposition causes problems, IsExpansive() should rule it out
        // new InferenceRule(new List<Expression>{new Expression(OMEGA, FTF, ST)},
        //     new List<Expression>{new Expression(OMEGA, FTF, new Expression(FTF, ST))}),

        // =
        new InferenceRule(new List<Expression>{}, new List<Expression>{new Expression(IDENTITY, XE, XE)}),
        new InferenceRule(new List<Expression>{new Expression(IDENTITY, YE, XE)}, new List<Expression>{new Expression(IDENTITY, XE, YE)}),
        new InferenceRule(
            new List<Expression>{new Expression(STAR,new Expression(IDENTITY, XE, YE))},
            new List<Expression>{new Expression(NOT, new Expression(IDENTITY, XE, YE))}),

        // factive
        new InferenceRule(new List<Expression>{new Expression(ITET, ST, XE)}, new List<Expression>{new Expression(DF, ITET, ST, XE)}),
        new InferenceRule(new List<Expression>{new Expression(ITET, ST, XE)}, new List<Expression>{new Expression(IF, ST, new Expression(DF, ITET, ST, XE))}),

        // converse
        new InferenceRule(
            new List<Expression>{new Expression(REET, YE, XE)},
            new List<Expression>{new Expression(CONVERSE, REET, XE, YE)}),
        new InferenceRule(
            new List<Expression>{new Expression(CONVERSE, REET, XE, YE)},
            new List<Expression>{new Expression(REET, YE, XE)}),

        // past
        new InferenceRule(new List<Expression>{ST}, new List<Expression>{new Expression(PAST, ST)}),

        // since
        new InferenceRule(new List<Expression>{new Expression(SINCE, ST, TT)}, new List<Expression>{ST}),
        new InferenceRule(new List<Expression>{new Expression(SINCE, ST, TT)}, new List<Expression>{new Expression(PAST, TT)}),

        // good
        new InferenceRule(
            new List<Expression>{new Expression(GOOD, new Expression(NOT, ST))},
            new List<Expression>{new Expression(NOT, new Expression(GOOD, ST))}),

        // at
        // reflexivity
        new InferenceRule(
            new List<Expression>{},
            new List<Expression>{new Expression(AT, XE, XE)}),
        // symmetry
        new InferenceRule(
            new List<Expression>{new Expression(AT, XE, YE)},
            new List<Expression>{new Expression(AT, YE, XE)}),

        // fruit
        new InferenceRule(new List<Expression>{new Expression(TOMATO, XE)}, new List<Expression>{new Expression(FRUIT, XE)}),
        new InferenceRule(new List<Expression>{new Expression(BANANA, XE)}, new List<Expression>{new Expression(FRUIT, XE)}),

        // abilities (TODO)
        new InferenceRule(
            new List<Expression>{new Expression(DF, MAKE, new Expression(AT, SELF, XE), SELF)},
            new List<Expression>{new Expression(AT, SELF, XE)}),
        new InferenceRule(
            new List<Expression>{
                new Expression(AT, SELF, XE),
                new Expression(DF, MAKE, new Expression(INFORMED, ST, XE), SELF)
            },
            new List<Expression>{new Expression(INFORMED, ST, XE)}),
    };

    private static (List<InferenceRule>, List<InferenceRule>) SortRules() {
        var contractiveRules = new List<InferenceRule>();
        var expansiveRules   = new List<InferenceRule>();
        for (int i = 0; i < BASE_RULES.Length; i++) {
            var rule = BASE_RULES[i];
            if (rule.IsExpansive()) {
                expansiveRules.Add(rule);
            } else {
                contractiveRules.Add(rule);
            }

            if (rule.IsContraposable()) {
                var contraposedRule = rule.Contrapose();
                if (contraposedRule.IsExpansive()) {
                    expansiveRules.Add(contraposedRule);
                } else {
                    contractiveRules.Add(contraposedRule);
                }
            }

            if (rule.IsPlusifiable()) {
                var plusifiedRule = rule.Plusify();
                if (plusifiedRule.IsExpansive()) {
                    expansiveRules.Add(plusifiedRule);
                } else {
                    contractiveRules.Add(plusifiedRule);
                }
            }
        }

        return (contractiveRules, expansiveRules);
    }

    public static readonly (List<InferenceRule>, List<InferenceRule>) RULES = SortRules();
}
