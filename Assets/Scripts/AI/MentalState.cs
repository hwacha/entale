using System;
using System.Collections.Generic;
using static SemanticType;
using static Expression;
using static ProofType;
using static BeliefRevisionPolicy;

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
    private HashSet<Expression> beliefBase = new HashSet<Expression>();
    private BeliefRevisionPolicy beliefRevisionPolicy;

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public MentalState(BeliefRevisionPolicy beliefRevisionPolicy, params Expression[] initialBeliefs) {
        this.beliefRevisionPolicy = beliefRevisionPolicy;
        for (int i = 0; i < initialBeliefs.Length; i++) {
            if (!Add(initialBeliefs[i])) {
                throw new ArgumentException("MentalState(): expected sentences for belief base.");
            }
        }
    }

    public MentalState(params Expression[] initialBeliefs) : this(Conservative, initialBeliefs) {}

    private bool Contains(Expression belief) {
        return beliefBase.Contains(belief);
    }

    private bool Add(Expression belief) {
        if (belief.Type.Equals(TRUTH_VALUE)) {
            beliefBase.Add(belief);
            return true;
        }
        return false;
    }

    private bool Remove(Expression belief) {
        if (beliefBase.Contains(belief)) {
            beliefBase.Remove(belief);
            return true;
        }
        return false;
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
    public HashSet<List<Expression>> Bases(Expression goal, ProofType proofType) {
        // goal should be type t.
        if (!goal.Type.Equals(TRUTH_VALUE)) {
            throw new ArgumentException("Bases: goal/conclusion must be a sentence (type t)");
        }

        // the set of alternative bases for the goal
        HashSet<List<Expression>> alternativeBases = new HashSet<List<Expression>>();

        // @TODO change to an array/list of goals, and account for variables.
        // May require having implemented unification.
        if (Contains(goal)) {
            List<Expression> basis = new List<Expression>();
            basis.Add(goal);
            alternativeBases.Add(basis);

            // @Note if a belief is found in the belief base,
            // then should we also try to find other supporting
            // premises? This may lead to an infinite loop.
            // So, I'll return prematurely for now, on the assumption
            // that the belief base, as an invariant, doesn't
            // include any beliefs that could be derived elsewhere.
            return alternativeBases;
        }

        // INFERENCES
        // ====
        // Double negation elimination
        // S |- not(not(S))
        if (goal.Head.Equals(NOT.Head)) {
            Expression subExpression = goal.GetArg(0) as Expression;
            if (subExpression.Head.Equals(NOT.Head)) {
                subExpression = subExpression.GetArg(0) as Expression;
                alternativeBases.UnionWith(Bases(subExpression, proofType));
            }
        }

        // disjunction introduction
        // A |- A v B; B |- A v B
        if (goal.Head.Equals(OR.Head)) {
            Expression leftDisjunct = goal.GetArg(0) as Expression;
            Expression rightDisjunct = goal.GetArg(1) as Expression;

            alternativeBases.UnionWith(Bases(leftDisjunct,  proofType));
            alternativeBases.UnionWith(Bases(rightDisjunct, proofType));
        }

        // conjunction introduction
        // A, B |- A & B
        if (goal.Head.Equals(AND.Head)) {
            Expression leftConjunct = goal.GetArg(0) as Expression;
            HashSet<List<Expression>> leftConjunctBases = Bases(leftConjunct, proofType);
            if (leftConjunctBases.Count != 0) {
                Expression rightConjunct = goal.GetArg(1) as Expression;
                HashSet<List<Expression>> rightConjunctBases = Bases(rightConjunct, proofType);

                if (rightConjunctBases.Count != 0) {
                    // if both conjuncts have proofs, then the set of all possible
                    // proofs is the set of all combinations of the proofs of A and
                    // the proofs of B.
                    foreach (List<Expression> leftConjunctBasis in leftConjunctBases) {
                        foreach (List<Expression> rightConjunctBasis in rightConjunctBases) {
                            // @Note assumption is that for a plan, a plan to
                            // enact the left conjunct should be performed before
                            // the plan to independently enact the right conjunct.
                            // Ultimately, both conjuncts need to be true simultaneously,
                            // and so this is a faulty assumption to make when it comes to plans.
                            List<Expression> conjunctionBasis = new List<Expression>();
                            conjunctionBasis.AddRange(leftConjunctBasis);
                            conjunctionBasis.AddRange(rightConjunctBasis);
                            alternativeBases.Add(conjunctionBasis);
                        }
                    }
                }
            }
        }

        // PLANNING
        // ====
        // @Note right now, I only resort to planning if there is
        // no proof of G. This is because you shouldn't try to
        // enact something you already believe to be true.
        if (proofType == Plan && alternativeBases.Count == 0) {
            Expression ableToEnactGoal = new Expression(ABLE, SELF, goal);

            HashSet<List<Expression>> abilityBases = Bases(ableToEnactGoal, Plan);

            if (abilityBases.Count != 0) {
                foreach (List<Expression> abilityBasis in abilityBases) {
                    abilityBasis.Add(new Expression(WILL, goal));
                    alternativeBases.Add(abilityBasis);
                }
            }
        }

        return alternativeBases;
    }

    // Asks if the expression is proven by this belief base.
    public bool Query(Expression query) {
        return Bases(query, Proof).Count != 0;
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

        HashSet<List<Expression>> notAssertionBases = Bases(new Expression(NOT, assertion), Proof);

        // We believe ~A. This is inconsistent with the assertion.
        if (notAssertionBases.Count != 0) {
            // if our belief revision policy is conservative,
            // we reject the new information in favor of the old.
            if (beliefRevisionPolicy == Conservative) {
                return false;
            }
            // otherwise, the belief revision policy is liberal.
            // So we accept the new information and reject the old.
            foreach (List<Expression> basis in notAssertionBases) {
                // right now, we simply randomly select a premise
                // to discard from each basis.
                var random = new Random();
                int index = random.Next(basis.Count);
                basis.RemoveAt(index);
            }
            Add(assertion);
            return true;
        }

        // We're agnostic about A. We accept it and add it to our belief state.
        Add(assertion);
        return true;
    }
}
