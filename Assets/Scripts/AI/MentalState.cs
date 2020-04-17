using System;
using System.Collections.Generic;
using static SemanticType;
using static Expression;
using static ProofType;
using static BeliefRevisionPolicy;
using static DecisionPolicy;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;
using Basis =
    System.Collections.Generic.KeyValuePair
        <System.Collections.Generic.List<Expression>,
        System.Collections.Generic.Dictionary<Variable, Expression>>;

// The prover and the planner each use the same inference mechanism,
// so this enum specifies some parameters to it.
public enum ProofType {
    Proof,
    Plan
}

// @Note these policies might well
// become sentences in the belief base
// and can change dynamically, just like anything else.

// Simple placeholder policies for how to revise beliefs.
// Can be set with the mental state.
// If there is a conflict between new and old information,
// A conversation policy rejects new information, and
// a liberal policy discards old information to accommodate the new.
public enum BeliefRevisionPolicy {
    Liberal,
    Conservative
}

// @Note: this might be the difference
// between risk aversion, ratinoal calculus, etc.
public enum DecisionPolicy {
    Default
}

// A model of the mental state of an NPC.
// Includes their beliefs, preferences, goals, etc.,
// for use by perception and in inference and action.
// 
// currently represented by sets of sentences,
// (all of which correspond to a belief)
// but the format of representation may change
// as it's clearer what patterns of inference
// should be optimized.
//
// Also includes a proof algorithm which tries to find
// a proof from the NPCs core beliefs to some target sentence,
// returning the free premises of the proof so that
// inconsistencies can be resolved.
// 
public class MentalState {
    public BeliefRevisionPolicy BeliefRevisionPolicy = Conservative;
    public DecisionPolicy DecisionPolicy = Default;
    public ProofType ProofMode = Proof;

    // private HashSet<Expression> BeliefBase = new HashSet<Expression>();
    private Dictionary<SemanticType, Dictionary<Atom, HashSet<Expression>>> BeliefBase =
        new Dictionary<SemanticType, Dictionary<Atom, HashSet<Expression>>>();

    int MaxDepth = 0;

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public MentalState(params Expression[] initialBeliefs) {
        for (int i = 0; i < initialBeliefs.Length; i++) {
            if (!Add(initialBeliefs[i])) {
                throw new ArgumentException("MentalState(): expected sentences for belief base.");
            }
        }
    }

    private bool Contains(Expression sentence) {
        return BeliefBase.ContainsKey(sentence.Head.Type) &&
            BeliefBase[sentence.Head.Type].ContainsKey(sentence.Head) &&
            BeliefBase[sentence.Head.Type][sentence.Head].Contains(sentence);
    }

    // @Note this presupposes the belief is not a formula
    // (it doesn't contain any variables)
    private bool Add(Expression belief) {
        if (belief.Type.Equals(TRUTH_VALUE)) {
            if (!BeliefBase.ContainsKey(belief.Head.Type)) {
                BeliefBase[belief.Head.Type] = new Dictionary<Atom, HashSet<Expression>>();
            }
            if (!BeliefBase[belief.Head.Type].ContainsKey(belief.Head)) {
                BeliefBase[belief.Head.Type][belief.Head] = new HashSet<Expression>();
            }
            BeliefBase[belief.Head.Type][belief.Head].Add(belief);
            if (belief.Depth > MaxDepth) {
                MaxDepth = belief.Depth;
            }
            return true;
        }
        return false;
    }

    private bool Remove(Expression sentence) {
        if (Contains(sentence)) {
            BeliefBase[sentence.Head.Type][sentence.Head as Constant].Remove(sentence);
            return true;
        }
        return false;
    }

    // returns satisfiers in the belief base for this formula
    private HashSet<Basis> Satisfiers(Expression formula) {
        // first, we get the domain to search through.
        // this is going to correspond to all sentences
        // that are structural candidates for matching
        // the formula, given the structure of the formula's
        // variables and semantic types.
        HashSet<Expression> domain = new HashSet<Expression>();
        if (formula.Head is Constant) {
            if (BeliefBase.ContainsKey(formula.Head.Type) &&
                BeliefBase[formula.Head.Type].ContainsKey(formula.Head)) {
                domain.UnionWith(BeliefBase[formula.Head.Type][formula.Head]);
            }
        }

        foreach (var typeMap in BeliefBase) {
            if (formula.Head.Type.IsPartialApplicationOf(typeMap.Key)) {
                foreach (var beliefsByPrefix in typeMap.Value.Values) {
                    domain.UnionWith(beliefsByPrefix);
                }
            }
        }

        // then, we iterate through the domain and pattern match (unify)
        // the formula against the sentences in the belief base.
        // any sentences that match get added, along with the
        // unifying substitution, to the basis set.
        HashSet<Basis> satisfiers = new HashSet<Basis>();
        foreach (Expression belief in domain) {
            HashSet<Substitution> unifiers = formula.Unify(belief);
            foreach (Substitution unifier in unifiers) {
                List<Expression> premiseContainer = new List<Expression>();
                premiseContainer.Add(belief);
                satisfiers.Add(new Basis(premiseContainer, unifier));
            }
        }

        return satisfiers;
    }

    // composes two substitutions together a * b,
    // according the rule that the A[a * b] = (A[a])[b] 
    private static Substitution Compose(Substitution a, Substitution b) {
        Substitution composition = new Substitution();
        foreach (KeyValuePair<Variable, Expression> aAssignment in a) {
            composition[aAssignment.Key] = aAssignment.Value;
        }
        foreach (KeyValuePair<Variable, Expression> bAssignment in b) {
            if (!composition.ContainsKey(bAssignment.Key)) {
                composition[bAssignment.Key] = bAssignment.Value;
            }
        }

        return composition;
    }

    // if this mental state, S, can prove the goal
    // directly or via inference, then Basis(goal)
    // returns the set of bases of goal, relative to S:
    // that is, the premises needed to prove the goal,
    // (that is, they aren't lemmas which are derived
    // elsewhere in the proof).
    // 
    // ====
    // 
    // Why a set of lists?
    // 
    // This returns the set of alternative bases that
    // each independently prove the goal. If the set
    // is empty, that means this mental state doesn't
    // prove the goal.
    // 
    // Also includes a substitution in case the goal
    // is a formula.
    // 
    // ====
    // 
    // Why a list, and not a set?
    // 
    // The basis is a partially ordered collection, where the
    // order corresponds to the proof depth of the premise:
    // If one premise P occurs in the application of a rule
    // whose other sentence is a lemma over a second premise Q,
    // then Q comes before P in the list.
    // 
    // This corresponds to the order in which the NPC should perform
    // actions. This is because the premises earlier in the order
    // will serve as preconditions to the success of a later action.
    // Also, duplicates in the sequence are allowed; a sequence
    // where the same sentence occurs multiple times corresponds
    // to the same action being performed again.
    // 
    // As for normal proofs, the order doesn't have a concrete interpretation,
    // and can be ignored. Furthermore, duplicate premises should ultimately be
    // discarded, in case a belief is discard to resolve an inconsistency.
    // 
    // TODO: fix bug of accidentally rejecting a proof of A in loops
    // where we may have to get the proof of A twice. Need to save
    // previous expressions
    // 
    // ====
    public HashSet<Basis> Bases(Expression goal,
        HashSet<Expression> suppositions,
        Dictionary<Expression, List<HashSet<Expression>>> pendingExpressions,
        Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>> completeExpressions) {
        // goal should be type t.
        if (!goal.Type.Equals(TRUTH_VALUE)) {
            throw new ArgumentException("Bases: goal/conclusion must be a sentence (type t)");
        }

        // the set of alternative bases for the goal
        HashSet<Basis> alternativeBases = new HashSet<Basis>();

        // if we have completed bases for this goal, go ahead
        // and return with the previous value.
        if (completeExpressions.ContainsKey(goal)) {
            var suppositionsAndBases = completeExpressions[goal];
            if (suppositions.SetEquals(suppositionsAndBases.Key)) {
                alternativeBases.UnionWith(suppositionsAndBases.Value);
                return alternativeBases;
            }
        }

        // here, we eliminate repeated attempts to prove the same
        // goal within the same proof.
        if (pendingExpressions.ContainsKey(goal)) {
            var listOfSuppositionSets = pendingExpressions[goal];
            foreach (var suppositionSet in listOfSuppositionSets) {
                if (suppositions.SetEquals(suppositionSet)) {
                    return alternativeBases;
                }
            }
        } else {
            pendingExpressions[goal] = new List<HashSet<Expression>>();
        }

        pendingExpressions[goal].Add(suppositions);

        // if suppositions prove the goal, then
        // nothing in our belief base will be under risk
        // of being falsified by ~goal. So, our basis
        // shouldn't include any premises in our basis.
        // 
        // @Note: the basis might include a stack of
        // conclusions proved via supposition, and then
        // pop them off. Maybe? Potentially not necessary.
        if (suppositions.Contains(goal)) {
            alternativeBases.Add(new Basis(new List<Expression>(), new Substitution()));
            return alternativeBases;
        } else {
            foreach (Expression supposition in suppositions) {
                // @Note: unification should change to a one-sided
                // match, so that we stop unifying on formulas in
                // the suppositions, which is screwing up other
                // inference rules.
                HashSet<Substitution> unifiers = supposition.Unify(goal);
                foreach (var unifier in unifiers) {
                    alternativeBases.Add(new Basis(new List<Expression>(), unifier));
                }
                // @Note: because finding a sentence in the
                // supposition set would amount to
                // logical vacuity, I used to return
                // the basis set here. But, we actually
                // probably want to know the other bases, too.
                // 
                // What will happen is that, because there's
                // no premise to discard in this basis,
                // no other sentence from another basis
                // will be discarded.
            }
        }

        // @Note: we should initialize the substitution in a basis of a formula
        // to include all the substitution variables assigned to themselves,
        // etc. etc.

        // belief-base(M) has A => M |- A
        if (Contains(goal)) {
            var premises = new List<Expression>();
            premises.Add(goal);
            alternativeBases.Add(new Basis(premises, new Substitution()));

            // @Note if a belief is found in the belief base,
            // then should we also try to find other supporting
            // premises? This may lead to an infinite loop.
            // So, I'll return prematurely for now, on the assumption
            // that the belief base, as an invariant, doesn't
            // include any beliefs that could be derived elsewhere.
            // return alternativeBases;
        } else {
            // in case the goal is a formula,
            // try to see if there are any satisfying instances in the belief base.
            var satisfiers = Satisfiers(goal);

            if (satisfiers.Count != 0) {
                alternativeBases.UnionWith(satisfiers);
            }
        }

        // INFERENCES
        // ====
        // => M |- Empty(x)
        if (goal.Head.Equals(EMPTY.Head)) {
            Expression arg = (Expression) goal.GetArg(0);
            // this condition is meant to account for the fact that
            // some(empty) would call on this.
            alternativeBases.Add(new Basis(new List<Expression>(), arg.GetSelfSubstitution()));
        }

        // truly introduction
        // M |- S => M |- truly(S)
        if (goal.Head.Equals(TRULY.Head)) {
            Expression subSentence = goal.GetArg(0) as Expression;
            alternativeBases.UnionWith(
                Bases(subSentence,
                    suppositions,
                    pendingExpressions,
                    completeExpressions));
        }

        // Double negation introduction
        // M |- S => M |- not(not(S))
        if (goal.Head.Equals(NOT.Head)) {
            Expression subExpression = goal.GetArg(0) as Expression;
            if (subExpression.Head.Equals(NOT.Head)) {
                subExpression = subExpression.GetArg(0) as Expression;
                alternativeBases.UnionWith(
                    Bases(subExpression,
                        suppositions,
                        pendingExpressions,
                        completeExpressions));
            }
        }

        // itself introduction
        // M |- R(x, x) => M |- itself(R, x)
        if (goal.Head.Equals(ITSELF.Head)) {
            Expression r = (Expression) goal.GetArg(0);
            Expression x = (Expression) goal.GetArg(1);

            alternativeBases.UnionWith(
                Bases(new Expression(r, x, x),
                    suppositions, pendingExpressions, completeExpressions));
        }

        // itself elimination
        // M |- itself(R, x) => M |- R(x, x)
        if (goal.Head.Type.Equals(RELATION_2)) {
            Expression x = (Expression) goal.GetArg(0);
            if (x.Equals((Expression) goal.GetArg(1))) {
                alternativeBases.UnionWith(Bases(
                    new Expression(ITSELF,
                        new Expression(goal.Head), x),
                    suppositions, pendingExpressions, completeExpressions));
            }
        }

        // disjunction introduction
        // M |- A => M |- A v B; M |- B => M |- A v B
        if (goal.Head.Equals(OR.Head)) {
            Expression leftDisjunct = goal.GetArg(0) as Expression;
            Expression rightDisjunct = goal.GetArg(1) as Expression;

            // @Note there's no need to share the substitutions between
            // the proofs of each disjunct: if one disjunct is true,
            // it doesn't matter what gets assigned to the variables in
            // the other. @BUT is that true??
            alternativeBases.UnionWith(Bases(leftDisjunct, suppositions, pendingExpressions, completeExpressions));
            alternativeBases.UnionWith(Bases(rightDisjunct, suppositions, pendingExpressions, completeExpressions));
        }

        // conjunction introduction
        // A, B |- A & B
        if (goal.Head.Equals(AND.Head)) {
            Expression leftConjunct = goal.GetArg(0) as Expression;
            Expression rightConjunct = goal.GetArg(1) as Expression;

            HashSet<Basis> leftConjunctBases = Bases(leftConjunct, suppositions, pendingExpressions, completeExpressions);
            // @Note: this should be saved in tried expressions, but hashing weirdness isn't ideal
            var rightBaseses = new Dictionary<Expression, HashSet<Basis>>();
            foreach (var leftConjunctBasis in leftConjunctBases) {
                Expression substitutedRightConjunct = rightConjunct.Substitute(leftConjunctBasis.Value);
                HashSet<Basis> rightConjunctBases;
                if (rightBaseses.ContainsKey(substitutedRightConjunct)) {
                    rightConjunctBases = rightBaseses[substitutedRightConjunct];
                } else {
                    rightConjunctBases = Bases(substitutedRightConjunct, suppositions, pendingExpressions, completeExpressions);
                    rightBaseses.Add(substitutedRightConjunct, rightConjunctBases);
                }
                foreach (var rightConjunctBasis in rightConjunctBases) {
                    // the set of all possible proofs  of A & B
                    // is the set of all combinations of
                    // the proofs of A and the proofs of B.
                    List<Expression> conjunctionPremises = new List<Expression>();
                    // @Note assumption is that for a plan, a plan to
                    // enact the left conjunct should be performed before
                    // the plan to independently enact the right conjunct.
                    // Ultimately, both conjuncts need to be true simultaneously,
                    // and so this is a faulty assumption to make when it comes to plans.
                    conjunctionPremises.AddRange(leftConjunctBasis.Key);
                    conjunctionPremises.AddRange(rightConjunctBasis.Key);
                    Basis conjunctionBasis = new Basis(conjunctionPremises,
                        Compose(leftConjunctBasis.Value, rightConjunctBasis.Value));
                    alternativeBases.Add(conjunctionBasis);
                }
            }
        }

        // gets a variable that's unused in the goal
        Variable GetUnusedVariable(SemanticType t) {
            Variable x = new Variable(t, "x_" + t);
            while (goal.HasOccurenceOf(x)) {
                x = new Variable(t, x.ID + "'");
            }
            return x;
        }

        // @Note trial for variable "recycling"
        // @Note might want this to take an array instead,
        // and trim out any unused variables.
        Substitution DiscardUnusedAssignments(Substitution substitution) {
            var trimmedSubstitution = new Substitution();
            foreach (var assignment in substitution) {
                if (goal.HasOccurenceOf(assignment.Key)) {
                    trimmedSubstitution[assignment.Key] = assignment.Value;
                }
            }
            return trimmedSubstitution;
        }

        // existential introduction
        // M |- F(x), M |- G(x) => M |- some(F, G)
        if (goal.Head.Equals(SOME.Head)) {
            Expression f = goal.GetArg(0) as Expression;
            Expression g = goal.GetArg(1) as Expression;

            Variable x = GetUnusedVariable(INDIVIDUAL);

            HashSet<Basis> fBases = Bases(new Expression(f, new Expression(x)), suppositions, pendingExpressions, completeExpressions);
            foreach (Basis fBasis in fBases) {
                Expression gc = new Expression(g, fBasis.Value[x]);

                HashSet<Basis> gBases = Bases(gc, suppositions, pendingExpressions, completeExpressions);
                foreach (Basis gBasis in gBases) {
                    List<Expression> fgPremises = new List<Expression>();
                    fgPremises.AddRange(fBasis.Key);
                    fgPremises.AddRange(gBasis.Key);
                    alternativeBases.Add(new Basis(fgPremises, DiscardUnusedAssignments(Compose(fBasis.Value, gBasis.Value))));
                }
            }
        }

        // universal elimination
        // all(F, G), F(x) |- G(x)
        Variable vg = GetUnusedVariable(PREDICATE);
        Variable vx = GetUnusedVariable(INDIVIDUAL);
        Expression predicatePattern = new Expression(new Expression(vg), new Expression(vx));
        var gxUnifiers = predicatePattern.Unify(goal);
        foreach (var gxUnifier in gxUnifiers) {
            if (!gxUnifier.ContainsKey(vg) || !gxUnifier.ContainsKey(vx)) {
                // that means unification succeeded, but not in a way that
                // assigned variables in the right way.
                continue;
            }
            var gValue = gxUnifier[vg];
            var xValue = gxUnifier[vx];
            Variable f = GetUnusedVariable(PREDICATE);
            Expression allFsAreGs = new Expression(ALL, new Expression(f), gValue);
            HashSet<Basis> allFsAreGsBases = Bases(allFsAreGs, suppositions, pendingExpressions, completeExpressions);
            foreach (Basis allFsAreGsBasis in allFsAreGsBases) {
                HashSet<Basis> fxBases = Bases(new Expression(new Expression(f), xValue).Substitute(allFsAreGsBasis.Value), suppositions, pendingExpressions, completeExpressions);
                foreach (Basis fxBasis in fxBases) {
                    List<Expression> premises = new List<Expression>();
                    premises.AddRange(allFsAreGsBasis.Key);
                    premises.AddRange(fxBasis.Key);
                    alternativeBases.Add(new Basis(premises, DiscardUnusedAssignments(Compose(allFsAreGsBasis.Value, fxBasis.Value))));
                }
            }
        }

        // conjunction elimination
        // A & B |- A; A & B |- B

        // @NOTE FOR SOUREN: Implement the following rules
        // contraposition of conjunction elimination
        // ~A |- ~(A & B); ~B |- ~(A & B)

        // contraposition of disjunction elimination
        // ~A, ~B |- ~(A v B)

        // conditional proof (conditional introduction)
        // @note all of the proof annotations should be written
        // in the sequent calculus, not natural deduction.
        // Usually doesn't matter though.
        // M,[A] |- B => M |- A -> B
        if (goal.Head.Equals(IF.Head)) {
            var newSuppositions = new HashSet<Expression>();
            foreach (Expression supposition in suppositions) {
                newSuppositions.Add(supposition);
            }
            // add the antecedent of the conditional
            // to the list of suppositions.
            newSuppositions.Add((Expression) goal.GetArg(0));
            // add the proofs of the consequent
            // under the supposition of the antecedent.
            alternativeBases.UnionWith(Bases((Expression) goal.GetArg(1),
                newSuppositions,
                pendingExpressions,
                completeExpressions));
        }

        // antisymmetry of better
        // M |- better(A, B) => M |- ~better(B, A)
        if (goal.Head.Equals(NOT.Head)) {
            Expression subExpression = (Expression) goal.GetArg(0);
            if (subExpression.Head.Equals(BETTER.Head)) {
                Expression converse = new Expression(BETTER,
                        (Expression) subExpression.GetArg(1),
                        (Expression) subExpression.GetArg(0));
                alternativeBases.UnionWith(Bases(converse,
                    suppositions, pendingExpressions, completeExpressions));
            }
        }

        // transitivity of better
        // M |- better(A, B); M |- better(B, C) => M |- better(A, C)
        if (goal.Head.Equals(BETTER.Head)) {
            Variable b = GetUnusedVariable(TRUTH_VALUE);
            var betterABBases =
                Bases(new Expression(BETTER, (Expression) goal.GetArg(0), new Expression(b)), 
                suppositions, pendingExpressions, completeExpressions);

            foreach (var betterABBasis in betterABBases) {
                if (!betterABBasis.Value.ContainsKey(b)) {
                    continue;
                }
                var betterBC =
                    (new Expression(BETTER,
                        betterABBasis.Value[b],
                        (Expression) goal.GetArg(1))).Substitute(betterABBasis.Value);

                var betterBCBases = Bases(betterBC,
                    suppositions,
                    pendingExpressions,
                    completeExpressions);

                foreach (var betterBCBasis in betterBCBases) {
                    var premises = new List<Expression>();
                    premises.AddRange(betterABBasis.Key);
                    premises.AddRange(betterBCBasis.Key);
                    var substitution = DiscardUnusedAssignments(Compose(betterABBasis.Value, betterBCBasis.Value));
                    alternativeBases.Add(new Basis(premises, substitution));
                }

            }
        }

        // @Note put all contractive rules here
        // (rules whose premises are more complex than their conclusions,
        //  and for which a premise may match the rule as a conclusion)
        //  the depth check only applies if we don't want the size
        //  of the premise to explode. It's fine to prove conclusions
        //  that are very large (i.e. with DNE).
        if (goal.Depth <= MaxDepth) {
            // truly elimination
            // M |- truly(S) => M |- S
            // @Note: have to limit the 'truly's for now...
            // Expression trulyGoal = new Expression(TRULY, goal);
            // alternativeBases.UnionWith(Bases(trulyGoal, suppositions, pendingExpressions, completeExpressions));

            // Double negation elimination
            // M |- not(not(S)) => M |- S
            // Expression notNotGoal = new Expression(NOT, new Expression(NOT, goal));
            // alternativeBases.UnionWith(Bases(notNotGoal, suppositions, pendingExpressions, completeExpressions));
            
            // perceptual belief: back in for now
            // M |- perceive(self, S); veridical(self, S) => M |- S
            var perceptionBases = 
                Bases(new Expression(PERCEIVE, SELF, goal),
                    suppositions, pendingExpressions, completeExpressions);
            foreach (var perceptionBasis in perceptionBases) {
                var premises = new List<Expression>();
                premises.AddRange(perceptionBasis.Key);
                premises.Add(new Expression(VERIDICAL, SELF, goal));
                alternativeBases.Add(new Basis(premises, perceptionBasis.Value));
            }
            
            // sometimes introduction
            // M |- TF(S), M |- TG(S) => M |- sometimes(TF, TG)
            if (goal.Head.Equals(SOMETIMES.Head)) {
                Expression tf = goal.GetArg(0) as Expression;
                Expression tg1 = goal.GetArg(1) as Expression;

                Variable s1 = GetUnusedVariable(TRUTH_VALUE);

                HashSet<Basis> tfBases =
                    Bases(new Expression(tf, new Expression(s1)),
                        suppositions,
                        pendingExpressions,
                        completeExpressions);
                foreach (Basis tfBasis in tfBases) {
                    if (!tfBasis.Value.ContainsKey(s1)) {
                        UnityEngine.Debug.Log(goal + Testing.BasesString(tfBases));
                        throw new Exception("sometimes introduction: failing now");
                    }
                    Expression tg1c = new Expression(tg1, tfBasis.Value[s1]);

                    HashSet<Basis> tgBases = Bases(tg1c, suppositions, pendingExpressions, completeExpressions);
                    foreach (Basis tgBasis in tgBases) {
                        List<Expression> fgPremises = new List<Expression>();
                        fgPremises.AddRange(tfBasis.Key);
                        fgPremises.AddRange(tgBasis.Key);
                        alternativeBases.Add(
                            new Basis(fgPremises,
                                DiscardUnusedAssignments(Compose(tfBasis.Value, tgBasis.Value))));
                    }
                }
            }

            // @Note @Bug @TODO commented out because it's a hot mess
            // // Modus Ponens (conditional elimination)
            // // M |- A -> B, M |- A => M |- B
            // Variable a = GetUnusedVariable(TRUTH_VALUE);
            // // we surround this to check if we're in the middle of a
            // // conditional proof of B, which cuts down the number of
            // // searches by at least an order of magnitude.
            // if (!suppositions.Contains(new Expression(a))) {
            //     Expression ifAThenGoal = new Expression(IF, new Expression(a), goal);
            //     var ifAThenGoalBases = Bases(ifAThenGoal, suppositions, pendingExpressions, completeExpressions);
            //     foreach (var ifAThenGoalBasis in ifAThenGoalBases) {
            //         // if we don't have a value for a, that means
            //         // we must have proved A -> B from B alone. In which
            //         // case we don't want to use modus ponens here, lest
            //         // every proof involve every possible antecedent
            //         if (!ifAThenGoalBasis.Value.ContainsKey(a)) {
            //             continue;
            //         }

            //         Expression antecedent = ifAThenGoalBasis.Value[a];
            //         var antecedentBases = Bases(antecedent, suppositions, pendingExpressions, completeExpressions);
            //         foreach (var antecedentBasis in antecedentBases) {
            //             var premises = new List<Expression>();
            //             premises.AddRange(ifAThenGoalBasis.Key);
            //             premises.AddRange(antecedentBasis.Key);
            //             alternativeBases.Add(new Basis(premises,
            //                 DiscardUnusedAssignments(Compose(ifAThenGoalBasis.Value, antecedentBasis.Value))));
            //         }
            //     }
            // }

            // @Note: we want to 'trulify' bare sentence if this check fails.
            // TODO
            // always elimination
            // M |- always(TF, TG), M |- TF(S) => M |- TG(S)
            var tg = GetUnusedVariable(TRUTH_FUNCTION);
            var ss = GetUnusedVariable(TRUTH_VALUE);

            var tgsUnifiers = (new Expression(new Expression(tg), new Expression(ss))).Unify(goal);

            foreach (var tgsUnifier in tgsUnifiers) {
                if (!tgsUnifier.ContainsKey(tg) || !tgsUnifier.ContainsKey(ss)) {
                    // that means unification succeeded, but not in a way that
                    // assigned variables in the right way.
                    continue;
                }
                var tgValue = tgsUnifier[tg];
                var ssValue = tgsUnifier[ss];
                var tf = GetUnusedVariable(TRUTH_FUNCTION);

                Expression alwaysTfTg = new Expression(ALWAYS, new Expression(tf), tgValue);
                HashSet<Basis> alwaysTfTgBases = Bases(alwaysTfTg, suppositions, pendingExpressions, completeExpressions);
                foreach (Basis alwaysTfTgBasis in alwaysTfTgBases) {
                    HashSet<Basis> tfsBases = Bases(new Expression(new Expression(tf), ssValue)
                        .Substitute(alwaysTfTgBasis.Value), suppositions, pendingExpressions, completeExpressions);
                    foreach (Basis tfsBasis in tfsBases) {
                        List<Expression> premises = new List<Expression>();
                        premises.AddRange(alwaysTfTgBasis.Key);
                        premises.AddRange(tfsBasis.Key);
                        alternativeBases.Add(new Basis(premises, DiscardUnusedAssignments(Compose(alwaysTfTgBasis.Value, tfsBasis.Value))));
                    }
                }
            }

            // PLANNING
            // ====
            // @Note we might in the future add a check to see if there is
            // no other proof of G. This is because you don't want to enact
            // something you already believe to be true.
            // 
            // M |- able(self, A),  M :: will(A) => M |- A
            // 
            if (ProofMode == Plan) {

                // here we assume it's a logical fact
                // that we can will the neutral state of affairs.
                // M :: will(neutral) |- neutral
                if (goal.Equals(NEUTRAL)) {
                    alternativeBases.Add(new Basis(new List<Expression>{new Expression(WILL, NEUTRAL)}, new Substitution()));
                }

                Expression ableToEnactGoal = new Expression(ABLE, SELF, goal);

                HashSet<Basis> abilityBases = Bases(ableToEnactGoal, suppositions, pendingExpressions, completeExpressions);

                if (abilityBases.Count != 0) {
                    foreach (Basis abilityBasis in abilityBases) {
                        abilityBasis.Key.Add(new Expression(WILL, goal));
                        alternativeBases.Add(abilityBasis);
                    }
                }
            }
        }

        // @Note my assumption is: because removing premises
        // occurs within the belief base, it's okay to have
        // duplicate premises, even in a standard proof.
        // If that turns out to be a faulty assumption,
        // we can check if the proof mode is Proof, and
        // if it is go through the list and remove duplicates.
            
        var completeBases = new HashSet<Basis>();
        completeBases.UnionWith(alternativeBases);

        completeExpressions.Add(goal, new KeyValuePair<HashSet<Expression>, HashSet<Basis>>(suppositions, completeBases));

        return alternativeBases;
    }

    public HashSet<Basis> Bases(Expression goal) {
        return Bases(goal, new HashSet<Expression>(),
            new Dictionary<Expression, List<HashSet<Expression>>>(),
            new Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>>());
    }

    // Asks if the expression is proven by this belief base.
    public bool Query(Expression query) {
        ProofMode = Proof;
        return Bases(query).Count != 0;
    }

    // Asserts a sentence to this mental state.
    // 
    // If the assertion is incompatible (i.e. inconsistent)
    // with the NPC's current beliefs, then the NPC's
    // are resolved by either rejecting the assertion,
    // or discarding one of the NPC's beliefs
    // that is incompatible with the assertion.
    // 
    // If accepted, then the sentence is added to
    // the state's beliefs.
    // 
    // Assert() returns true if the assertion is
    // accepted, false if it is rejected.
    public bool Assert(Expression assertion) {
        ProofMode = Proof;
        // We already believe assertion A.
        // We accept it, but don't change our belief state.
        if (Query(assertion)) {
            return true;
        }

        HashSet<Basis> notAssertionBases = Bases(new Expression(NOT, assertion));

        // We believe ~A. This is inconsistent with the assertion.
        if (notAssertionBases.Count != 0) {
            // if our belief revision policy is conservative,
            // we reject the new information in favor of the old.
            if (BeliefRevisionPolicy == Conservative) {
                return false;
            }
            // otherwise, the belief revision policy is liberal.
            // So we accept the new information and reject the old.
            // 
            // @Note: we want to check for zero-premise proofs,
            // and refuse to discard information if we do.
            foreach (Basis basis in notAssertionBases) {
                // right now, we simply randomly select a premise
                // to discard from each basis.
                var random = new Random();
                int index = random.Next(basis.Key.Count);
                Remove(basis.Key[index]);
            }
            Add(assertion);
            return true;
        }

        // We're agnostic about A. We accept it and add it to our belief state.
        Add(assertion);
        return true;
    }

    // ranks the goals according to this mental state,
    // and then stores the ranking.
    public List<Expression> DecideCurrentPlan() {
        // if we don't have any preferences, then
        // we return the empty list
        // (or, equivalently, the list containing just
        // the list to enact the neutral condtion)
        if (!BeliefBase.ContainsKey(TRUTH_FUNCTION_2) || !BeliefBase[TRUTH_FUNCTION_2].ContainsKey(BETTER.Head)) {
            var nothing = new List<Expression>();
            nothing.Add(new Expression(WILL, NEUTRAL));
            return nothing;
        }

        // first, we get our domain of elements for which preferences are defined.
        HashSet<Expression> preferencesInBeliefBase = BeliefBase[TRUTH_FUNCTION_2][BETTER.Head];
        var preferables = new HashSet<Expression>();

        foreach (var preference in preferencesInBeliefBase) {
            preferables.Add((Expression) preference.GetArg(0));
            preferables.Add((Expression) preference.GetArg(1));
        }

        // @Note: we'd probably want to consider AS_GOOD_AS in the future. For now,
        // we ignore that detail. We're also ignoring DERIVED preferences which
        // might not be in the belief base, but are still relevant to decision making.
        // @Q: How do we get those?

        // the plan: find the sentence which is strictly better than any other sentence.
        // NOTE: this strategy excludes any sentences not known to be better than
        // netural, because they might be worse than neutral, for all we know.
        // (Hence "risk averse")
        
        // @Note right now, the ranking is represented as a stack.
        // This is wrong, as our preferences are only a partial
        // ordering. Instead, we should have a tree structure
        // where the sibling nodes are of undefined preference
        // to one another/assumed indifferent. This way, we
        // don't discard a goal that might be better than
        // neutral but indifferent/unknown to something
        // better than neutral.
        Stack<Expression> ranking = new Stack<Expression>();
        ranking.Push(NEUTRAL);
        var storage = new Stack<Expression>();
        ProofMode = Proof;
        foreach (var preferable in preferables) {
            while (ranking.Count != 0) {
                var bestSoFar = ranking.Peek();
                if (Query(new Expression(BETTER, preferable, bestSoFar))) {
                    ranking.Push(preferable);
                    break;
                } else {
                    storage.Push(ranking.Pop());
                }
            }
            while (storage.Count != 0) {
                ranking.Push(storage.Pop());
            }
        }

        ProofMode = Plan;
        var bestPlan = new List<Expression>();
        while (ranking.Count != 0) {
            Expression nextBestGoal = ranking.Pop();
            var goalBases = Bases(nextBestGoal);

            bool alreadyTrue = true;
            // we go through each basis of the goal and
            // find the best plan. If there is no plan,
            // then we either can't enact it, or it's
            // already true. In either case, we discard it.
            foreach (var goalBasis in goalBases) {
                // we decide the best plan for our goal
                // and keep track of it.
                // @Note: right now it's very simple;
                // we simply choose the plan that takes
                // the least actions.
                // we'll want to incorporate the cost
                // of actions in later.
                var currentPlan = new List<Expression>();
                
                foreach (var premise in goalBasis.Key) {
                    var boundPremise = premise.Substitute(goalBasis.Value);
                    if (boundPremise.Head.Equals(WILL.Head)) {
                        // @Note: we could send the argument
                        // of the expression to reduce what we're
                        // sending to the actuator. Not doing that
                        // now for pedantic reasons.
                        currentPlan.Add(boundPremise);
                        alreadyTrue = false;
                    }
                }

                if (alreadyTrue) {
                    break;
                }

                if (currentPlan.Count == 0) {
                    continue;
                }

                if (bestPlan.Count == 0 || currentPlan.Count < bestPlan.Count) {
                    bestPlan = currentPlan;
                }
            }
            if (bestPlan.Count != 0) {
                break;
            }
        }

        return bestPlan;
    }
}
