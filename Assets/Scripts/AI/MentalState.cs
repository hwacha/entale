using System;
using System.Collections;
using System.Collections.Generic;
using static SemanticType;
using static Expression;
using static ProofType;
using static BeliefRevisionPolicy;
using static DecisionPolicy;
using static InferenceRule;

using UnityEngine;

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
public class MentalState : MonoBehaviour {
    // the time, in seconds, that the mental
    // state is allowed to run search in one frame
    protected static float TIME_BUDGET = 0.16f;

    public BeliefRevisionPolicy BeliefRevisionPolicy = Conservative;
    public DecisionPolicy DecisionPolicy = Default;
    public ProofType ProofMode = Proof;

    public FrameTimer FrameTimer;

    // private HashSet<Expression> BeliefBase = new HashSet<Expression>();
    private Dictionary<SemanticType, Dictionary<Atom, HashSet<Expression>>> BeliefBase;
    int MaxDepth = 0;

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public void Initialize(params Expression[] initialBeliefs) {
        if (BeliefBase != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        BeliefBase = new Dictionary<SemanticType, Dictionary<Atom, HashSet<Expression>>>();
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
            HashSet<Substitution> unifiers = formula.GetMatches(belief);
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

    // gets a variable that's unused in the goal
    private static Variable GetUnusedVariable(SemanticType t, HashSet<Variable> usedVariables) {
        Variable x = new Variable(t, "x_" + t);
        while (usedVariables.Contains(x)) {
            x = new Variable(t, x.ID + "'");
        }
        return x;
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
    // ====
    //
    // @BUG pending expressions shouldn't be mutated in branches
    // that don't have the expression as its ancestor.
    // This'll cause false negatives.
    //
    // ====
    //
    // goal: the sentence to be proved
    // suppositions: the set of sentences supposed at this point in search
    //               due to  a conditional
    // pendingExpressions: expressions that have commenced search but
    //                     haven't completed. Used to prevent mutually
    //                     recursive loops.
    // completeExpressions: expressions that have been tried and found.
    // alternativeBases: the resulting set of bases that each prove the goal.
    // done: a flag that indicates when search has been completed.
    //
    public IEnumerator GetBases(Expression goal,
        HashSet<Expression> suppositions,
        Dictionary<Expression, List<HashSet<Expression>>> pendingExpressions,
        Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>> completeExpressions,
        HashSet<Basis> alternativeBases,
        Container<bool> done) {
        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
            yield return null;
        }

        // goal should be type t.
        if (!goal.Type.Equals(TRUTH_VALUE)) {
            throw new ArgumentException("Bases: goal/conclusion must be a sentence (type t)");
        }

        // if we have completed bases for this goal, go ahead
        // and return with the previous value.
        if (completeExpressions.ContainsKey(goal)) {
            var suppositionsAndBases = completeExpressions[goal];
            if (suppositions.SetEquals(suppositionsAndBases.Key)) {
                alternativeBases.UnionWith(suppositionsAndBases.Value);
                done.Item = true;
                yield break;
            }
        }

        // here, we eliminate repeated attempts to prove the same
        // goal within the same proof.
        if (pendingExpressions.ContainsKey(goal)) {
            var listOfSuppositionSets = pendingExpressions[goal];
            foreach (var suppositionSet in listOfSuppositionSets) {
                if (suppositions.SetEquals(suppositionSet)) {
                    done.Item = true;
                    yield break;
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
            done.Item = true;
            yield break;
        } else {
            foreach (Expression supposition in suppositions) {
                // @Note: now that get matches is unidirectional
                // as opposed to unification, we may now want
                // to make a second call, here, in case we want to make
                // some sort of supposition -> goal match, interpreted
                // universally instead of existentially.
                HashSet<Substitution> unifiers = goal.GetMatches(supposition);
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

        // M's belief base has A => M |- A
        if (Contains(goal)) {
            var premises = new List<Expression>();
            premises.Add(goal);
            alternativeBases.Add(new Basis(premises, new Substitution()));

            // @Note we want all proofs, not just these basic ones,
            // because they may need to be falsified to accept the
            // negation of A.
            // yield return;
        } else {
            // in case the goal is a formula,
            // try to see if there are any satisfying instances in the belief base.
            var satisfiers = Satisfiers(goal);

            if (satisfiers.Count != 0) {
                alternativeBases.UnionWith(satisfiers);
            }
        }

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

        int numRoutines = 0;

        // a function for handling inference rules
        IEnumerator ApplyInferenceRule(InferenceRule rule)
        {
            numRoutines++;

            Expression[] premises = new Expression[rule.Premises.Length];
            Expression[] assumptions = new Expression[rule.Assumptions.Length];
            Expression[] conclusions = new Expression[rule.Conclusions.Length];

            // change out variables in the rule to not collide
            // with the variables in goal.
            var usedVariables = goal.GetVariables();
            var newVariableSubstitution = new Substitution();
            var newUsedVariables = new HashSet<Variable>();
            newUsedVariables.UnionWith(usedVariables);
            foreach (var usedVariable in usedVariables) {
                Variable newVariable = GetUnusedVariable(usedVariable.Type, newUsedVariables);
                newVariableSubstitution.Add(usedVariable, new Expression(newVariable));
            }

            for (int i = 0; i < rule.Premises.Length; i++) {
                premises[i] = rule.Premises[i].Substitute(newVariableSubstitution);
            }

            for (int i = 0; i < rule.Assumptions.Length; i++) {
                assumptions[i] = rule.Assumptions[i].Substitute(newVariableSubstitution);
            }

            for (int i = 0; i < rule.Conclusions.Length; i++) {
                conclusions[i] = rule.Conclusions[i].Substitute(newVariableSubstitution);
            }

            // Now, we go through the conclusions of the rules,
            // trying to match a conclusion.
            for (int i = 0; i < conclusions.Length; i++)
            {
                var unifiers = conclusions[i].GetMatches(goal);

                // for each unifier, we get a different set of bases.
                foreach (var unifier in unifiers) {
                    HashSet<Basis> currentBases = new HashSet<Basis>();
                    currentBases.Add(new Basis(new List<Expression>(), unifier));

                    for (int j = 0; j < premises.Length; j++) {
                        var meetBases = new HashSet<Basis>();
                        foreach (var currentBasis in currentBases) {
                            var premiseBases = new HashSet<Basis>();
                            var doneFlag = new Container<bool>(false);
                            StartCoroutine(GetBases(premises[j].Substitute(currentBasis.Value),
                                    suppositions,
                                    pendingExpressions,
                                    completeExpressions,
                                    premiseBases,
                                    doneFlag));

                            while (!doneFlag.Item) {
                                yield return null;
                            }

                            foreach (var premiseBasis in premiseBases) {
                                List<Expression> meetPremises = new List<Expression>();
                                meetPremises.AddRange(currentBasis.Key);
                                meetPremises.AddRange(premiseBasis.Key);
                                meetBases.Add(new Basis(meetPremises, Compose(currentBasis.Value, premiseBasis.Value)));
                            }
                        }
                        currentBases = meetBases;
                    }

                    // we try to disprove each of the other conclusions
                    for (int j = 0; j < conclusions.Length && j != i; j++) {
                        var meetBases = new HashSet<Basis>();
                        foreach (var currentBasis in currentBases) {
                            var notConclusion = new Expression(NOT, conclusions[j].Substitute(currentBasis.Value));
                            var conclusionBases = new HashSet<Basis>();
                            var doneFlag = new Container<bool>(false);

                            StartCoroutine(GetBases(notConclusion,
                                suppositions,
                                pendingExpressions,
                                completeExpressions,
                                conclusionBases,
                                doneFlag));

                            while (!doneFlag.Item) {
                                yield return null;
                            }

                            foreach (var conclusionBasis in conclusionBases) {
                                List<Expression> meetPremises = new List<Expression>();
                                meetPremises.AddRange(currentBasis.Key);
                                meetPremises.AddRange(conclusionBasis.Key);
                                meetBases.Add(new Basis(meetPremises, Compose(currentBasis.Value, conclusionBasis.Value)));
                            }
                        }
                        currentBases = meetBases;
                    }

                    for (int j = 0; j < assumptions.Length; j++) {
                        var meetBases = new HashSet<Basis>();

                        foreach (var currentBasis in currentBases) {
                            var assumption = assumptions[j].Substitute(currentBasis.Value);
                            // here we want to try to disprove
                            // the assumption. If we can't, then
                            // the inference goes through by default.
                            var assumptionDisbases = new HashSet<Basis>();
                            var doneFlag = new Container<bool>(false);

                            StartCoroutine(GetBases(new Expression(NOT, assumption),
                                    suppositions,
                                    pendingExpressions,
                                    completeExpressions,
                                    assumptionDisbases,
                                    doneFlag));

                            while (!doneFlag.Item) {
                                yield return null;
                            }

                            if (assumptionDisbases.Count == 0) {
                                List<Expression> meetPremises = new List<Expression>();
                                meetPremises.AddRange(currentBasis.Key);
                                meetPremises.Add(assumption);
                                meetBases.Add(new Basis(meetPremises, currentBasis.Value));
                            }
                        }

                        currentBases = meetBases;
                    }

                    var collectedBases = new HashSet<Basis>();
                    foreach (var currentBasis in currentBases) {
                        collectedBases.Add(new Basis(currentBasis.Key, DiscardUnusedAssignments(currentBasis.Value)));
                    }
                    alternativeBases.UnionWith(collectedBases);
                }
            }
            numRoutines--;
        }

        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
            yield return null;
        }

        // INFERENCES
        // ==========

        StartCoroutine(ApplyInferenceRule(VERUM_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(VEROUS_INTRODUCTION));

        StartCoroutine(ApplyInferenceRule(TRULY_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(DOUBLE_NEGATION_INTRODUCTION));

        StartCoroutine(ApplyInferenceRule(LIKES_ALL_TO_LIKES));

        // @Note: not working. Something is up with Unify()
        StartCoroutine(ApplyInferenceRule(ITSELF_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(ITSELF_ELIMINATION));

        StartCoroutine(ApplyInferenceRule(DISJUNCTION_INTRODUCTION_LEFT));
        StartCoroutine(ApplyInferenceRule(DISJUNCTION_INTRODUCTION_RIGHT));

        StartCoroutine(ApplyInferenceRule(CONJUNCTION_INTRODUCTION));

        StartCoroutine(ApplyInferenceRule(EXISTENTIAL_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(UNIVERSAL_ELIMINATION));

        // conjunction elimination
        // A & B |- A; A & B |- B

        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
            yield return null;
        }

        // conditional proof (conditional introduction)
        // @note all of the proof annotations should be written
        // in the sequent calculus, not natural deduction.
        // Usually doesn't matter though.
        // M, A |- B => M |- A -> B
        if (goal.Head.Equals(IF.Head)) {
            var antecedent = goal.GetArgAsExpression(0);

            // @Note as a workaround, we skip if the antecedent
            // is a lone variable, as it won't be helpful to know
            // that's matching, and it causes loops with
            // modus ponens.
            if (!(antecedent.Head is Variable) && !antecedent.Type.Equals(antecedent.Head.Type)) {
                var newSuppositions = new HashSet<Expression>();
                foreach (Expression supposition in suppositions) {
                    newSuppositions.Add(supposition);
                }
                // add the antecedent of the conditional
                // to the list of suppositions.
                newSuppositions.Add(antecedent);

                // add the proofs of the consequent
                // under the supposition of the antecedent.
                var doneFlag = new Container<bool>(false);
                StartCoroutine(GetBases((Expression) goal.GetArg(1),
                    newSuppositions,
                    pendingExpressions,
                    completeExpressions,
                    alternativeBases,
                    doneFlag));

                while (!doneFlag.Item) {
                    yield return null;
                }
            }
        }

        StartCoroutine(ApplyInferenceRule(BETTER_ANTISYMMETRY));
        StartCoroutine(ApplyInferenceRule(BETTER_TRANSITIVITY));
        StartCoroutine(ApplyInferenceRule(SELF_BELIEF_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(NEGATIVE_SELF_BELIEF_INTRODUCTION));

        // StartCoroutine(ApplyInferenceRule(SYMMETRY_OF_LOCATION));
        // StartCoroutine(ApplyInferenceRule(TRANSITIVITY_OF_LOCATION));

        StartCoroutine(ApplyInferenceRule(SOMETIMES_INTRODUCTION));

        StartCoroutine(ApplyInferenceRule(Contrapose(PERCEPTUAL_BELIEF)));

        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
            yield return null;
        }

        // @Note put all contractive rules here
        // (rules whose premises are more complex than their conclusions,
        //  and for which a premise may match the rule as a conclusion)
        //  the depth check only applies if we don't want the size
        //  of the premise to explode. It's fine to prove conclusions
        //  that are very large (i.e. with DNE).
        if (goal.Depth <= MaxDepth) {
            StartCoroutine(ApplyInferenceRule(PERCEPTUAL_BELIEF));
            // StartCoroutine(ApplyInferenceRule(ALWAYS_ELIMINATION));
            StartCoroutine(ApplyInferenceRule(MODUS_PONENS));
            // StartCoroutine(ApplyInferenceRule(Contrapose(DISJUNCTION_INTRODUCTION_LEFT)));
            // StartCoroutine(ApplyInferenceRule(Contrapose(DISJUNCTION_INTRODUCTION_RIGHT)));
            // StartCoroutine(ApplyInferenceRule(Contrapose(CONJUNCTION_INTRODUCTION)));
            StartCoroutine(ApplyInferenceRule(Contrapose(MODUS_PONENS)));

            // PLANNING
            // ====
            // @Note we might in the future add a check to see if there is
            // no other proof of G. This is because you don't want to enact
            // something you already believe to be true.
            //
            // M |- able(self, A),  M :: will(A) => M |- A
            if (ProofMode == Plan) {

                // here we assume it's a logical fact
                // that we can will the neutral state of affairs.
                // M :: will(neutral) |- neutral
                if (goal.Equals(NEUTRAL)) {
                    alternativeBases.Add(new Basis(new List<Expression>{new Expression(WILL, NEUTRAL)}, new Substitution()));
                }

                Expression ableToEnactGoal = new Expression(ABLE, SELF, goal);

                HashSet<Basis> abilityBases = new HashSet<Basis>();
                var doneFlag = new Container<bool>(false);

                StartCoroutine(GetBases(
                    ableToEnactGoal,
                    suppositions,
                    pendingExpressions,
                    completeExpressions,
                    abilityBases,
                    doneFlag));

                while (!doneFlag.Item) {
                    yield return null;
                }

                if (abilityBases.Count != 0) {
                    foreach (Basis abilityBasis in abilityBases) {
                        abilityBasis.Key.Add(new Expression(WILL, goal));
                        alternativeBases.Add(abilityBasis);
                    }
                }
            }
        }

        while (numRoutines > 0) {
            yield return null;
        }

        var completeBases = new HashSet<Basis>();
        completeBases.UnionWith(alternativeBases);

        completeExpressions[goal] =
            new KeyValuePair<HashSet<Expression>, HashSet<Basis>>(suppositions, completeBases);

        done.Item = true;
        yield break;

        // @Note my assumption is: because removing premises
        // occurs within the belief base, it's okay to have
        // duplicate premises, even in a standard proof.
        // If that turns out to be a faulty assumption,
        // we can check if the proof mode is Proof, and
        // if it is go through the list and remove duplicates.
    }

    public IEnumerator GetBases(Expression goal, HashSet<Basis> result, Container<bool> done) {
        yield return StartCoroutine(GetBases(goal, new HashSet<Expression>(),
            new Dictionary<Expression, List<HashSet<Expression>>>(),
            new Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>>(),
            result, done));
    }

    // Asks if the expression is proven by this belief base.
    public IEnumerator Query(Expression query, Container<bool> answer, Container<bool> queryDone) {
        HashSet<Basis> bases = new HashSet<Basis>();
        var doneFlag = new Container<bool>(false);
        ProofMode = Proof;
        IEnumerator basesRoutine = GetBases(query, bases, doneFlag);
        StartCoroutine(basesRoutine);

        while (!doneFlag.Item) {
            if (bases.Count > 0) {
                answer.Item = true;
                queryDone.Item = true;
                StopCoroutine(basesRoutine);
                yield break;
            }
            yield return null;
        }

        answer.Item = bases.Count > 0;
        queryDone.Item = true;
        yield break;
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
    public IEnumerator Assert(Expression assertion) {
        // We already believe assertion A.
        // We accept it, but don't change our belief state.
        var answer = new Container<bool>(false);
        var queryDone = new Container<bool>(false);
        StartCoroutine(Query(assertion, answer, queryDone));

        while (!queryDone.Item) {
            yield return null;
        }

        if (answer.Item) {
            yield break;
        }

        var notAssertionBases = new HashSet<Basis>();
        var done = new Container<bool>(false);
        ProofMode = Proof;
        StartCoroutine(GetBases(new Expression(NOT, assertion), notAssertionBases, done));

        while (!done.Item) {
            yield return null;
        }

        // We're agnostic about A. We add this belief to our base.
        if (notAssertionBases.Count == 0) {
            Add(assertion);
            yield break;
        }

        // We believe ~A. This is inconsistent with the assertion.
        // TODO: implement revision policy whereby we accept or
        // reject information based on its epistemic strength
    }

    // ranks the goals according to this mental state,
    // and then stores the ranking.
    public IEnumerator DecideCurrentPlan(List<Expression> plan, Container<bool> done) {
        // if we don't have any preferences, then
        // we return the empty list
        // (or, equivalently, the list containing just
        // the list to enact the neutral condtion)
        if (!BeliefBase.ContainsKey(TRUTH_FUNCTION_2) ||
            !BeliefBase[TRUTH_FUNCTION_2].ContainsKey(BETTER.Head)) {
            plan.Add(new Expression(WILL, NEUTRAL));
            done.Item = true;
            yield break;
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

                var answer = new Container<bool>(false);
                var doneFlag = new Container<bool>(false);
                StartCoroutine(Query(new Expression(BETTER, preferable, bestSoFar), answer, doneFlag));
                while (!doneFlag.Item) {
                    yield return null;
                }

                if (answer.Item) {
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
            var goalBases = new HashSet<Basis>();
            var doneFlag = new Container<bool>(false);
            StartCoroutine(GetBases(nextBestGoal, goalBases, doneFlag));

            while (!doneFlag.Item) {
                yield return null;
            }

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

        plan.AddRange(bestPlan);
        done.Item = true;
        yield break;
    }
}
