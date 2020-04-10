using System;
using System.Collections.Generic;
using static SemanticType;
using static Expression;
using static ProofType;
using static BeliefRevisionPolicy;

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

// Simple placeholder policies for how to revise beliefs.
// Can be set with the mental state.
// If there is a conflict between new and old information,
// A conversation policy rejects new information, and
// a liberal policy discards old information to accommodate the new.
public enum BeliefRevisionPolicy {
    Liberal,
    Conservative
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
        HashSet<Expression> domain = new HashSet<Expression>();
        if (formula.Head is Constant) {
            if (BeliefBase.ContainsKey(formula.Head.Type) &&
                BeliefBase[formula.Head.Type].ContainsKey(formula.Head)) {
                domain.UnionWith(BeliefBase[formula.Head.Type][formula.Head]);
            }
        } else if (BeliefBase.ContainsKey(formula.Head.Type)) {
            Dictionary<Atom, HashSet<Expression>> beliefBaseByHeadType = BeliefBase[formula.Head.Type];
            foreach (var beliefBaseByHead in beliefBaseByHeadType) {
                domain.UnionWith(beliefBaseByHead.Value);
            }
        } else if (formula.Type.Equals(TRUTH_VALUE) && formula.Head.Type.Equals(TRUTH_VALUE)) {
            foreach (var typeMap in BeliefBase.Values) {
                foreach (var prefixMap in typeMap.Values) {
                    domain.UnionWith(prefixMap);
                }
            }
        }

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
    // ====
    // 
    // @Note the semantics of the substitution outputted is somewhat unclear,
    // as variables are reused at different stages of the proof. Should switch
    // to a way of incrementing variable expressions, either with a Church-like
    // encoding or by changing the variables to have an integer identifier which
    // can be incremented?
    // TODO make this well-defined by incrementing variables at each recursive step,
    // add maxVariable parameter to Bases()?
    // OR, instead of incrementing variables, could the variables be recycled
    // by trimming the substitutions to only those variable assignments for
    // variables which still occur in goal?
    //
    public HashSet<Basis> Bases(Expression goal, HashSet<Expression> triedExpressions) {
        // goal should be type t.
        if (!goal.Type.Equals(TRUTH_VALUE)) {
            throw new ArgumentException("Bases: goal/conclusion must be a sentence (type t)");
        }

        // the set of alternative bases for the goal
        HashSet<Basis> alternativeBases = new HashSet<Basis>();

        // here, we eliminate repeated attempts to prove the same
        // goal within the same session.
        if (triedExpressions.Contains(goal)) {
            return alternativeBases;    
        }
        triedExpressions.Add(goal);

        // A |- A
        if (Contains(goal)) {
            List<Expression> premises = new List<Expression>();
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
        // Double negation elimination
        // S |- not(not(S))
        if (goal.Head.Equals(NOT.Head)) {
            Expression subExpression = goal.GetArg(0) as Expression;
            if (subExpression.Head.Equals(NOT.Head)) {
                subExpression = subExpression.GetArg(0) as Expression;
                alternativeBases.UnionWith(Bases(subExpression, triedExpressions));
            }
        }

        // @Note we want to coordinate variables
        // for these cases. That isn't happening right now.

        // @NOTE FOR SOUREN:
        // contraposition of conjunction elimination will look a lot like this
        // disjunction introduction
        // A |- A v B; B |- A v B
        if (goal.Head.Equals(OR.Head)) {
            Expression leftDisjunct = goal.GetArg(0) as Expression;
            Expression rightDisjunct = goal.GetArg(1) as Expression;

            // @Note there's no need to share the substitutions between
            // the proofs of each disjunct: if one disjunct is true,
            // it doesn't matter what gets assigned to the variables in
            // the other. @BUT is that true??
            alternativeBases.UnionWith(Bases(leftDisjunct, triedExpressions));
            alternativeBases.UnionWith(Bases(rightDisjunct, triedExpressions));
        }

        // @NOTE FOR SOUREN
        // contraposition of disjunction elimination will look a lot like this
        // conjunction introduction
        // A, B |- A & B
        if (goal.Head.Equals(AND.Head)) {
            Expression leftConjunct = goal.GetArg(0) as Expression;
            Expression rightConjunct = goal.GetArg(1) as Expression;

            HashSet<Basis> leftConjunctBases = Bases(leftConjunct, triedExpressions);
            foreach (var leftConjunctBasis in leftConjunctBases) {
                Expression substitutedRightConjunct = rightConjunct.Substitute(leftConjunctBasis.Value);
                HashSet<Basis> rightConjunctBases = Bases(substitutedRightConjunct, triedExpressions);
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
            Variable x = new Variable(t, "x");
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
        // F(x), G(x) |- some(F, G)
        if (goal.Head.Equals(SOME.Head)) {
            Expression f = goal.GetArg(0) as Expression;
            Expression g = goal.GetArg(1) as Expression;

            Variable x = GetUnusedVariable(INDIVIDUAL);

            HashSet<Basis> fBases = Bases(new Expression(f, new Expression(x)), triedExpressions);
            foreach (Basis fBasis in fBases) {
                Expression gc = new Expression(g, fBasis.Value[x]);

                HashSet<Basis> gBases = Bases(gc, triedExpressions);
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
        if (goal.Head.Type.Equals(PREDICATE)) {
            Expression c = (Expression) goal.GetArg(0);
            Variable f = GetUnusedVariable(PREDICATE);
            Expression allFsAreGs = new Expression(ALL, new Expression(f), new Expression(goal.Head));
            HashSet<Basis> allFsAreGsBases = Bases(allFsAreGs, triedExpressions);
            foreach (Basis allFsAreGsBasis in allFsAreGsBases) {
                HashSet<Basis> fcBases = Bases(new Expression(new Expression(f), c).Substitute(allFsAreGsBasis.Value));
                foreach (Basis fcBasis in fcBases) {
                    List<Expression> premises = new List<Expression>();
                    premises.AddRange(allFsAreGsBasis.Key);
                    premises.AddRange(fcBasis.Key);
                    alternativeBases.Add(new Basis(premises, DiscardUnusedAssignments(Compose(allFsAreGsBasis.Value, fcBasis.Value))));
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

        // @Note put all contractive rules here
        // (rules whose premises are more complex than their conclusions,
        //  and for which a premise may match the rule as a conclusion)
        //  the depth check only applies if we don't want the size
        //  of the premise to explode. It's fine to prove conclusions
        //  that are very large (i.e. with DNE).
        if (goal.Depth <= MaxDepth) {
            // @TEST for loop prevention
            // Double negation introduction
            // not(not(S)) |- S
            Expression notNotGoal = new Expression(NOT, new Expression(NOT, goal));
            alternativeBases.UnionWith(Bases(notNotGoal, triedExpressions));

            // @Note eventually replace this with
            // normality
            // normally(A, B) | normal(A, B), A |- B
            // plus a sentence normally(perceive(self, S), S) in the belief base
            // (quantify over sentences?)
            // 
            // perceptual belief
            // normally(perceive(self, any(S)), any(S))
            // perceive(self, S) | normal(perceive(self, S), S) |- S
            Expression iPerceiveGoal = new Expression(PERCEIVE, SELF, goal);
            Expression veridicalityAssumption = new Expression(NORMAL, iPerceiveGoal, goal);
            if (Bases(new Expression(NOT, veridicalityAssumption), triedExpressions).Count == 0) {
                HashSet<Basis> perceptionBases = Bases(iPerceiveGoal, triedExpressions);
                foreach (Basis perceptionBasis in perceptionBases) {
                    perceptionBasis.Key.Add(veridicalityAssumption);
                    alternativeBases.Add(perceptionBasis);
                }
            }

            // PLANNING
            // ====
            // @Note we might in the future add a check to see if there is
            // no other proof of G. This is because you don't want to enact
            // something you already believe to be true.
            if (ProofMode == Plan) {
                Expression ableToEnactGoal = new Expression(ABLE, SELF, goal);

                HashSet<Basis> abilityBases = Bases(ableToEnactGoal, triedExpressions);

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

        return alternativeBases;
    }

    public HashSet<Basis> Bases(Expression goal) {
        return Bases(goal, new HashSet<Expression>());
    }

    // Asks if the expression is proven by this belief base.
    public bool Query(Expression query) {
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
}
