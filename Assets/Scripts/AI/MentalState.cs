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

// a model of the mental state of an NPC.
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
    // each independently prove the goal.
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
    public HashSet<List<Expression>> Basis(Expression goal, ProofType proofType) {
        // goal should be type t.
        if (!goal.Type.Equals(TRUTH_VALUE)) {
            throw new ArgumentException("Basis: goal/conclusion must be a sentence (type t)");
        }

        // @TODO change to an array/list of goals, and account for variables.
        // May require having implemented unification.
        if (Contains(goal)) {
            List<Expression> basis = new List<Expression>();
            basis.Add(goal);
            HashSet<List<Expression>> alternativeBases = new HashSet<List<Expression>>();
            alternativeBases.Add(basis);
            return alternativeBases;
        }

        // INFERENCES
        // ====
        // There are no inferences implemented yet. @TODO

        return null;
    }

    // Asks if the expression is proven by this belief base.
    public bool Query(Expression query) {
        return Basis(query, Proof) != null;
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
        if (Basis(assertion, Proof) != null) {
            return true;
        }

        HashSet<List<Expression>> notAssertionBases = Basis(new Expression(NOT, assertion), Proof);

        // We believe ~A. This is inconsistent with the assertion.
        if (notAssertionBases != null) {
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
