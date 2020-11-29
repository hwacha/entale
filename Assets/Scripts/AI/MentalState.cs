using System;
using System.Linq;
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
using Basis = System.Collections.Generic.KeyValuePair<System.Collections.Generic.List<Expression>,
    System.Collections.Generic.Dictionary<Variable, Expression>>;

// variant ways of quering the belief base
// given a timestamp
public enum TensedQueryType {
    Exact, // directly query the exact timestamp
    Inertial, // checks the most recent timestamp between P and ~P
    Eventive // @TODO figure it out
}

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
// between risk aversion, rational calculus, etc.
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
    protected static float TIME_BUDGET = 0.016f;

    public BeliefRevisionPolicy BeliefRevisionPolicy = Conservative;
    public DecisionPolicy DecisionPolicy = Default;

    public FrameTimer FrameTimer;

    protected uint ParameterID;

    // private HashSet<Expression> BeliefBase = new HashSet<Expression>();
    private Dictionary<SemanticType, Dictionary<Atom,
        Dictionary<Expression, SortedSet<uint>>>> BeliefBase;

    // @Note we may want to replace this with another 'private symbol' scheme like
    // the parameters, but for now, spatial/time points/intervals aren't represented
    // explicitly in the language.
    // 
    // Keyword: @space
    // 
    // @Note: this should be a readonly collection to all outside this
    // class, but I don't know how the access modifiers work on that.
    // I'll just make it public for now.
    //
    public Dictionary<Expression, Vector3> Locations;
    protected uint Timestamp = 0;
    int MaxDepth = 0;

    void Update() {
        //Timestamp++;
    }

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public void Initialize(params Expression[] initialBeliefs) {
        ParameterID = 0;
        Locations = new Dictionary<Expression, Vector3>();

        if (BeliefBase != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        BeliefBase = new Dictionary<SemanticType, Dictionary<Atom,
            Dictionary<Expression, SortedSet<uint>>>>();

        for (int i = 0; i < initialBeliefs.Length; i++) {
            if (!Add(initialBeliefs[i], 0)) {
                throw new ArgumentException("MentalState(): expected sentences for belief base.");
            }
        }
    }

    // this returns true if this model sampled
    // sentence at the specified timestamp.
    public (bool, uint) BaseQuery(TensedQueryType tensedQueryType, Expression sentence, uint timestamp) {
        Debug.Assert(sentence.Type.Equals(TRUTH_VALUE));

        if (BeliefBase.ContainsKey(sentence.Head.Type) &&
            BeliefBase[sentence.Head.Type].ContainsKey(sentence.Head) &&
            BeliefBase[sentence.Head.Type][sentence.Head].ContainsKey(sentence)) {

            if (tensedQueryType == TensedQueryType.Exact) {
                if (BeliefBase[sentence.Head.Type][sentence.Head][sentence].Contains(timestamp)) {
                    return (true, timestamp);
                }
            }

            // here, we check that the most recent positive sample of S
            // is not counterveiled by a more recent negative sample.
            if (tensedQueryType == TensedQueryType.Inertial) {
                // This gets a range of positive samples from 0 to given timestamp (inclusive)
                
                var positiveSampleRange = BeliefBase[sentence.Head.Type][sentence.Head][sentence].GetViewBetween(0, timestamp);

                if (positiveSampleRange.Count == 0) {
                    // @Note do we want to return the most recent negative sample here instead?
                    return (false, 0);
                }

                uint latestPositiveSample = positiveSampleRange.Max;

                Expression negatedSentence = sentence.Head.Equals(NOT.Head) ? sentence.GetArgAsExpression(0) : new Expression(NOT, sentence);
                
                SortedSet<uint> negativeSampleRange = null;
                if (BeliefBase.ContainsKey(negatedSentence.Head.Type) &&
                    BeliefBase[negatedSentence.Head.Type].ContainsKey(negatedSentence.Head) &&
                    BeliefBase[negatedSentence.Head.Type][negatedSentence.Head].ContainsKey(negatedSentence)) {
                    
                    negativeSampleRange =
                        BeliefBase[negatedSentence.Head.Type][negatedSentence.Head][negatedSentence]
                        .GetViewBetween(latestPositiveSample, timestamp);
                }


                if (negativeSampleRange == null || negativeSampleRange.Count == 0) {
                    return (true, latestPositiveSample);
                }

                uint latestNegativeSample = negativeSampleRange.Max;

                if (latestPositiveSample > latestNegativeSample) {
                    return (true, latestPositiveSample);
                } else {
                    return (false, latestNegativeSample);
                }
            }
        }

        return (false, 0);
    }

    // returns false if this belief is already in the belief base
    // at the given timestamp
    private bool Add(Expression belief, uint timestamp) {
        Debug.Assert(belief.Type.Equals(TRUTH_VALUE));
        Debug.Assert(belief.GetVariables().Count == 0);

        if (!BeliefBase.ContainsKey(belief.Head.Type)) {
            BeliefBase[belief.Head.Type] =
                new Dictionary<Atom, Dictionary<Expression, SortedSet<uint>>>();
        }
        if (!BeliefBase[belief.Head.Type].ContainsKey(belief.Head)) {
            BeliefBase[belief.Head.Type][belief.Head] =
                new Dictionary<Expression, SortedSet<uint>>();
        }

        if (!BeliefBase[belief.Head.Type][belief.Head].ContainsKey(belief)) {
            BeliefBase[belief.Head.Type][belief.Head][belief] =
                new SortedSet<uint>();
        }
        if (BeliefBase[belief.Head.Type][belief.Head][belief].Contains(timestamp)) {
            return false;
        }

        BeliefBase[belief.Head.Type][belief.Head][belief].Add(timestamp);

        if (belief.Depth > MaxDepth) {
            MaxDepth = belief.Depth;
        }
        return true;
    }

    private bool Remove(Expression sentence, uint timestamp) {
        if (BaseQuery(TensedQueryType.Exact, sentence, timestamp).Item1) {
            var times = BeliefBase[sentence.Head.Type][sentence.Head as Constant][sentence];
            
            var removeSuccess = times.Remove(timestamp);

            if (!removeSuccess) {
                return false;
            }

            // @Note finish this code to remove a key from the belief base
            // if its value is empty. Right now it doesn't seem to break
            // anything if it's left in.
            // 
            // if (times.Count == 0) {
            //     BeliefBase[sentence.Head.Type][sentence.Head as Constant].Remove(sentence);
            // }
            
            return true;
        }
        return false;
    }

    // returns satisfiers in the belief base for this formula
    // at a given timestamp
    private HashSet<Basis> Satisfiers(Expression formula, uint timestamp) {
        // first, we get the domain to search through.
        // this is going to correspond to all sentences
        // that are structural candidates for matching
        // the formula, given the structure of the formula's
        // variables and semantic types.
        var domain = new HashSet<Expression>();
        if (formula.Head is Constant) {
            if (BeliefBase.ContainsKey(formula.Head.Type) &&
                BeliefBase[formula.Head.Type].ContainsKey(formula.Head)) {
                foreach (var sentenceAndTimes in BeliefBase[formula.Head.Type][formula.Head]) {
                    if (sentenceAndTimes.Value.Contains(timestamp)) {
                        domain.Add(sentenceAndTimes.Key);
                    }
                }
            }
        }

        foreach (var typeMap in BeliefBase) {
            if (formula.Head.Type.IsPartialApplicationOf(typeMap.Key)) {
                foreach (var beliefsByPrefix in typeMap.Value.Values) {
                    foreach (var sentenceAndTimes in beliefsByPrefix) {
                        if (sentenceAndTimes.Value.Contains(timestamp)) {
                            domain.Add(sentenceAndTimes.Key);
                        }
                    }
                }
            }
        }

        // then, we iterate through the domain and pattern match (unify)
        // the formula against the sentences in the belief base.
        // any sentences that match get added, along with the
        // unifying substitution, to the basis set.
        HashSet<Basis> satisfiers = new HashSet<Basis>();
        foreach (Expression belief in domain) {
            // Debug.Log("domain includes " + belief);
            HashSet<Substitution> unifiers = formula.GetMatches(belief);
            foreach (Substitution unifier in unifiers) {
                List<Expression> premiseContainer = new List<Expression>();
                premiseContainer.Add(belief);
                satisfiers.Add(new Basis(premiseContainer, unifier));
            }
        }

        // Debug.Log("satisfiers for " + formula + " at t=" + timestamp);
        // Debug.Log(Testing.BasesString(satisfiers));

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

    // gets a parameter that's unused in the mental state
    public uint GetNextParameterID() {
        var param = ParameterID;
        ParameterID++;
        return param;
    }

    private class ProofNode {
        #region parameters
        protected int ID;
        public Expression Formula {protected set; get;}
        public int OutgoingID;
        public bool IsDependent {protected set; get;}
        public bool IsAssumption {protected set; get;}
        public Func<Basis, Basis, Basis> OnMerge;
        #endregion

        // @C#: is there a way to make a collection
        // readonly for public, but allow modifications
        // within a class?
        #region variables
        public bool Visited;
        public HashSet<Basis> OldMeetBases;
        public HashSet<Basis> OldJoinBases;
        public HashSet<Basis> NewMeetBases;
        public HashSet<Basis> NewJoinBases;
        #endregion

        public ProofNode(int id, Expression formula, int outgoingID, bool isDependent,
            Func<Basis, Basis, Basis> onMerge) {
            ID = id;
            Formula = formula;
            OutgoingID = outgoingID;
            IsDependent = isDependent;
            OnMerge = onMerge;

            Visited = false;

            OldMeetBases = new HashSet<Basis>();
            // this means this node is receiving
            // bases to something out of the
            // scope of an inference rule.
            // So, we don't have to worry about
            // variable assignments.
            // 
            // For these nodes, we give an empty
            // substitution.
            // 
            // For nodes which must receive an
            // assignment before they're queried,
            // we want the for loop for assignments
            // not to trigger an queries.
            if (!isDependent) {
                OldMeetBases.Add(new Basis(new List<Expression>(), new Substitution()));
            }
            
            OldJoinBases = new HashSet<Basis>();

            NewMeetBases = new HashSet<Basis>();
            NewJoinBases = new HashSet<Basis>();
        }

        public ProofNode(int id, Expression formula, int outgoingID, bool isDependent) :
            this(id, formula, outgoingID, isDependent, null) {}

        public void ReceiveBases(HashSet<Basis> bases, int incomingID) {
            // if the queue id of the incoming ID is lower
            // than this node's queue ID, this must mean it
            // is being passed on in an inference rule.
            // So, we should add it to our meet bases.
            if (incomingID < ID) {
                if (Visited) {
                    NewMeetBases.UnionWith(bases);
                } else {
                    OldMeetBases.UnionWith(bases);
                }
            } else {
                var newBases = new HashSet<Basis>();
                foreach (var basis in bases) {
                    // here, we discard any unused variable assignments.
                    var trimmedSubstitution = new Substitution();
                    foreach (var assignment in basis.Value) {
                        if (Formula.HasOccurenceOf(assignment.Key)) {
                            trimmedSubstitution[assignment.Key] = assignment.Value;
                        }
                    }

                    newBases.Add(new Basis(basis.Key, trimmedSubstitution));
                }
                NewJoinBases.UnionWith(newBases);
            }
        }
    }

    public IEnumerator StreamBasesBreadthFirst(
        ProofType proofType,
        Expression goal,
        HashSet<Basis> alternativeBases, Container<bool> done) {
        // this will be the queue for a breadth-first search.
        var queue = new List<ProofNode>();

        // this, we keep track of in order to go back to the
        // first value at a given depth. That way, our rules
        // add nodes to be visited in a breadth-first order.
        int depthLowerBound = 0;
        int depthUpperBound = 1;

        // first, we set up a node for the goal.
        var goalNode = new ProofNode(0, goal,  0, false);
        queue.Add(goalNode);

        while (true) {
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            // then, we go through each node in our execution queue.
            // More nodes will be added as inference rules are matched.
            for (int queueIndex = depthLowerBound; queueIndex < depthUpperBound; queueIndex++) {
                if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                    yield return null;
                }
                ProofNode cur = queue[queueIndex];
                cur.Visited = true;

                Debug.Log(queueIndex + ": " + cur.Formula + " -> " + cur.OutgoingID);

                var assignedFormulas = new HashSet<Expression>();

                // here, we pass assignments on from earlier
                // checked premises in an inference rule
                // (if there are any). If a sentence should
                // be proven outside the context of an inference rule,
                // then an empty assignment will be given ahead of time.
                foreach (Basis b in cur.OldMeetBases) {
                    if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                        yield return null;
                    }
                    var currentFormula = cur.Formula.Substitute(b.Value);

                    if (!cur.IsDependent) {
                        if (assignedFormulas.Contains(currentFormula)) {
                            continue;
                        } else {
                            assignedFormulas.Add(currentFormula);
                        }
                    }

                    var searchBases = new HashSet<Basis>();
                    
                    // we do a base query for the formula.
                    var (success, time) = BaseQuery(TensedQueryType.Inertial, cur.Formula, Timestamp);
                    if (success) {
                        var premise = new List<Expression>();
                        premise.Add(cur.Formula);
                        var basis = new Basis(premise, new Substitution());
                        searchBases.Add(basis);
                    } else if (cur.Formula.GetVariables().Count > 0) {
                        // TODO: inline Satisfiers()
                        // so that it can be chunked.
                        searchBases.UnionWith(Satisfiers(cur.Formula, Timestamp));
                    }

                    if (searchBases.Count > 0) {
                        cur.ReceiveBases(searchBases, queueIndex);

                        if (queueIndex == 0) {
                            alternativeBases.UnionWith(searchBases);
                            yield return null;
                        } else {
                            // this index keeps track of a 'call stack' for
                            // when a proof basis is found inferentially, the
                            // results of which need to be merged through
                            // meet or join operations down to a base call.
                            int mergeIndex = queueIndex;

                            // here, we execute any basis
                            // merging that needs to happen.
                            while (mergeIndex != 0) {
                                if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                                    yield return null;
                                }
                                ProofNode currentMergeNode = queue[mergeIndex];

                                // we want to wait until it's this node's turn.
                                // the ripple merges should start only after
                                // a search has been attempted.
                                if (!currentMergeNode.Visited) {
                                    break;
                                }

                                // old meets matched with old joins, ALREADY DONE.
                                // old meets matched with new joins,
                                // new meets matched with old joins,
                                // new meets matched with new joins, NOT POSSIBLE.
                                
                                // if there aren't any new, then the previous meet
                                // wasn't successful: no proofs.
                                if (currentMergeNode.NewMeetBases.Count == 0 &&
                                    currentMergeNode.NewJoinBases.Count == 0) {
                                    break;
                                }

                                HashSet<Basis> meetBases, joinBases;

                                // if we have new meet bases, we pass the assignments on
                                // and spawn new execution nodes.
                                if (currentMergeNode.NewMeetBases.Count > 0) {

                                    meetBases = currentMergeNode.NewMeetBases;

                                    foreach (var meetBasis in meetBases) {
                                        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                                            yield return null;
                                        }

                                        var assignedFormula = currentMergeNode.Formula.Substitute(meetBasis.Value);

                                        // otherwise, we spawn an independent node with this assignment.
                                        var assignedNode =
                                            new ProofNode(queue.Count, assignedFormula,
                                                mergeIndex, false);

                                        queue.Add(assignedNode);
                                    }

                                    currentMergeNode.OldMeetBases.UnionWith(meetBases);
                                    currentMergeNode.NewMeetBases.Clear();
                                } else {
                                    // if we have new join bases, we link up the meet and join
                                    // bases and pass them on the outgoing node, and ripple through.
                                    meetBases = currentMergeNode.OldMeetBases;
                                    joinBases = currentMergeNode.NewJoinBases;
                                
                                    var productBases = new HashSet<Basis>();
                                    foreach (var meetBasis in meetBases) {
                                        foreach (var joinBasis in joinBases) {
                                            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                                                yield return null;
                                            }

                                            if (currentMergeNode.OnMerge == null) {
                                                List<Expression> productPremises = new List<Expression>();
                                                productPremises.AddRange(meetBasis.Key);
                                                productPremises.AddRange(joinBasis.Key);
                                                var productSubstitution = Compose(meetBasis.Value, joinBasis.Value);
                                                productBases.Add(new Basis(productPremises, productSubstitution));
                                            } else {
                                                productBases.Add(currentMergeNode.OnMerge(meetBasis, joinBasis));
                                            }
                                        }
                                    }

                                    // the outgoing node receives the new bases.
                                    queue[currentMergeNode.OutgoingID].ReceiveBases(productBases, mergeIndex);

                                    // recur on the outgoing node.
                                    mergeIndex = currentMergeNode.OutgoingID;

                                    currentMergeNode.OldJoinBases.UnionWith(joinBases);
                                    currentMergeNode.NewJoinBases.Clear();
                                }
                            }
                            // since we special case the root node,
                            // we check the new join bases and clear it out.
                            alternativeBases.UnionWith(queue[0].NewJoinBases);
                            queue[0].NewJoinBases.Clear();
                            yield return null;
                        }
                    }
                }
            }

            for (int queueIndex = depthLowerBound; queueIndex < depthUpperBound; queueIndex++) {
                if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                    yield return null;
                }
                if (queue[queueIndex].IsDependent) {
                    continue;
                }

                var currentFormula = queue[queueIndex].Formula;
                // we check against inference rules.

                // // this gives us a collection of proof nodes for
                // // the given inference rule.
                // void SpawnNodes(InferenceRule rule) {
                //     Expression[] premises = new Expression[rule.Premises.Length];
                //     Expression[] assumptions = new Expression[rule.Assumptions.Length];
                //     Expression[] conclusions = new Expression[rule.Conclusions.Length];
                    
                //     // change out variables in the rule to not collide
                //     // with the variables in currentFormula.
                //     var usedVariables = currentFormula.GetVariables();
                //     var newVariableSubstitution = new Substitution();
                //     var newUsedVariables = new HashSet<Variable>();
                //     newUsedVariables.UnionWith(usedVariables);
                //     foreach (var usedVariable in usedVariables) {
                //         Variable newVariable = GetUnusedVariable(usedVariable.Type, newUsedVariables);
                //         newVariableSubstitution.Add(usedVariable, new Expression(newVariable));
                //     }

                //     for (int i = 0; i < rule.Premises.Length; i++) {
                //         premises[i] = rule.Premises[i].Substitute(newVariableSubstitution);
                //     }

                //     for (int i = 0; i < rule.Assumptions.Length; i++) {
                //         assumptions[i] = rule.Assumptions[i].Substitute(newVariableSubstitution);
                //     }

                //     for (int i = 0; i < rule.Conclusions.Length; i++) {
                //         conclusions[i] = rule.Conclusions[i].Substitute(newVariableSubstitution);
                //     }

                //     bool isFirstPremise = true;

                //     for (int i = 0; i < conclusions.Length; i++) {
                //         var unifiers = conclusions[i].GetMatches(currentFormula);

                //         // for each unifier, we get a different set of bases.
                //         foreach (var unifier in unifiers) {
                //             for (int j = 0; j < premises.Length; j++) {
                //                 queue.Add(new ProofNode(queue.Count,
                //                     premises[j].Substitute(unifier),
                //                     queue.Count + 1, !isFirstPremise, false));
                //                 isFirstPremise = false;
                //             }

                //             // we try to disprove each of the other conclusions
                //             for (int j = 0; j < conclusions.Length; j++) {
                //                 if (j == i) {
                //                     continue;
                //                 }
                //                 queue.Add(new ProofNode(queue.Count,
                //                     new Expression(NOT, conclusions[j].Substitute(unifier)),
                //                     queue.Count + 1, !isFirstPremise, false));
                //                 isFirstPremise = false;
                //             }

                //             // TODO
                //             // // @Note: the behavior of assumptions in this
                //             // // method isn't yet specially implemented.
                //             // // It probably will not give the right results.
                //             // for (int j = 0; j < assumptions.Length; j++) {
                //             //     queue.Add(new ProofNode(queue.Count,
                //             //         premises[j].Substitute(unifier),
                //             //         queue.Count + 1, !isFirstPremise, true));
                //             //     isFirstPremise = false;
                //             // }
                //             // TODO

                //             queue[queue.Count - 1].OutgoingID = queueIndex;
                //         }
                //     }
                // }

                // SpawnNodes(TRULY_INTRODUCTION);
                // SpawnNodes(DOUBLE_NEGATION_INTRODUCTION);
                // SpawnNodes(CONJUNCTION_INTRODUCTION);
                // SpawnNodes(DISJUNCTION_INTRODUCTION_LEFT);
                // SpawnNodes(DISJUNCTION_INTRODUCTION_RIGHT);
                // SpawnNodes(EXISTENTIAL_INTRODUCTION);

                // truth +
                if (currentFormula.Head.Equals(TRULY.Head)) {
                    queue.Add(new ProofNode(queue.Count, currentFormula.GetArgAsExpression(0), queueIndex, false));
                }

                // double negation +
                if (currentFormula.Head.Equals(NOT.Head)) {
                    var subClause = currentFormula.GetArgAsExpression(0);
                    if (subClause.Head.Equals(NOT.Head)) {
                        var subSubClause = subClause.GetArgAsExpression(0);
                        queue.Add(new ProofNode(queue.Count, subSubClause, queueIndex, false));
                    }
                }

                // conjunction +
                if (currentFormula.Head.Equals(AND.Head)) {
                    queue.Add(new ProofNode(queue.Count, currentFormula.GetArgAsExpression(0), queue.Count + 1, false));
                    queue.Add(new ProofNode(queue.Count, currentFormula.GetArgAsExpression(1), queueIndex, true));
                }

                // disjunction +
                if (currentFormula.Head.Equals(OR.Head)) {
                    queue.Add(new ProofNode(queue.Count, currentFormula.GetArgAsExpression(0), queueIndex, false));
                    queue.Add(new ProofNode(queue.Count, currentFormula.GetArgAsExpression(1), queueIndex, false));
                }

                // existential +
                if (currentFormula.Head.Equals(SOME.Head)) {
                    var x = new Expression(GetUnusedVariable(INDIVIDUAL, currentFormula.GetVariables()));

                    var fx = new Expression(currentFormula.GetArgAsExpression(0), x);
                    var gx = new Expression(currentFormula.GetArgAsExpression(1), x);

                    queue.Add(new ProofNode(queue.Count, fx, queue.Count + 1, false));
                    queue.Add(new ProofNode(queue.Count, fx, queueIndex, true));
                }

                // TODO assumptions.
                // will need to move forward with an
                // assumption, and then ripple defeat
                // if ~assumption is proven.

                if (currentFormula.Depth < MaxDepth) {
                    if (proofType == Plan) {
                        queue.Add(new ProofNode(
                            queue.Count,
                            new Expression(ABLE, SELF, currentFormula),
                            queueIndex, false,
                            (meetBasis, joinBasis) => {
                                var productPremises = new List<Expression>();
                                productPremises.AddRange(meetBasis.Key);
                                productPremises.AddRange(joinBasis.Key);
                                productPremises.Add(new Expression(WILL, currentFormula));
                                var productSubstitution = Compose(meetBasis.Value, joinBasis.Value);
                                return new Basis(productPremises, productSubstitution);
                        }));                        
                    }
                }
            }

            // no new nodes were added, so quit!
            if (depthUpperBound == queue.Count) {
                break;
            }

            depthLowerBound = depthUpperBound;
            depthUpperBound = queue.Count;
        }

        done.Item = true;
        yield break;
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
    public IEnumerator GetBases(
        ProofType proofType,
        Expression goal,
        HashSet<Expression> suppositions,
        Dictionary<Expression, List<HashSet<Expression>>> pendingExpressions,
        Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>> completeExpressions,
        HashSet<Basis> alternativeBases,
        Container<bool> done) {
        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
            yield return null;
        }

        // Debug.Log("trying to prove " + goal);

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

        // @Note: a bug where different formulas return the same proof
        // allow duplicate proofs. We'd want a unify check here to fix that,
        // or even something more sophisticated. It doesn't seem like the duplication
        // is causing a problem, so no worry yet.
        // 
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
        // 
        // right now, we do an intertial query: if
        // M |- A at i and M |- ~A at j and j < i, then M |- A
        var (success, sample) = BaseQuery(TensedQueryType.Inertial, goal, Timestamp);
        if (success) {
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
            var satisfiers = Satisfiers(goal, Timestamp);

            if (satisfiers.Count != 0) {
                alternativeBases.UnionWith(satisfiers);
            }
        }

        // M.locations has X => M |- able(self, at(self, X))
        if (goal.Head.Equals(ABLE.Head) && goal.GetArgAsExpression(0).Head.Equals(SELF.Head)) {
            var abilityContent = goal.GetArgAsExpression(1);
            if (abilityContent.Head.Equals(AT.Head) && abilityContent.GetArgAsExpression(0).Head.Equals(SELF.Head)) {
                var location = abilityContent.GetArgAsExpression(1);
                if (Locations.ContainsKey(location)) {
                    var premises = new List<Expression>();
                    premises.Add(goal);
                    alternativeBases.Add(new Basis(premises, new Substitution()));
                }
            }
        }

        // dist(M.locations(x), M.locations(y)) < 1 => M |- at(x, y)
        if (goal.Head.Equals(AT.Head) || 
           (goal.Head.Equals(NOT.Head) && goal.GetArgAsExpression(0).Head.Equals(AT.Head))) {
            float cutoffDistance = 5;

            Expression nameA;
            Expression nameB;

            if (goal.Head.Equals(AT.Head)) {
                nameA = goal.GetArgAsExpression(0);
                nameB = goal.GetArgAsExpression(1);
            } else {
                nameA = goal.GetArgAsExpression(0).GetArgAsExpression(0);
                nameB = goal.GetArgAsExpression(0).GetArgAsExpression(1);
            }

            if (Locations.ContainsKey(nameA) && Locations.ContainsKey(nameB)) {
                var locationA = Locations[nameA];
                var locationB = Locations[nameB];

                var dx = locationA.x - locationB.x;
                var dy = locationA.y - locationB.y;
                var dz = locationA.z - locationB.z;

                if (dx * dx + dy * dy + dz * dz <= cutoffDistance) {
                    if (goal.Head.Equals(AT.Head)) {
                        var premises = new List<Expression>();
                        premises.Add(goal);
                        alternativeBases.Add(new Basis(premises, new Substitution()));
                    }
                } else {
                    if (goal.Head.Equals(NOT.Head)) {
                        var premises = new List<Expression>();
                        premises.Add(goal);
                        alternativeBases.Add(new Basis(premises, new Substitution()));
                    }
                }
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
        IEnumerator ApplyInferenceRule(InferenceRule rule) {
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
                            StartCoroutine(
                                    GetBases(proofType,
                                    premises[j].Substitute(currentBasis.Value),
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
                    for (int j = 0; j < conclusions.Length; j++) {
                        if (j == i) {
                            continue;
                        }
                        var meetBases = new HashSet<Basis>();
                        foreach (var currentBasis in currentBases) {
                            var notConclusion = new Expression(NOT, conclusions[j].Substitute(currentBasis.Value));
                            var conclusionBases = new HashSet<Basis>();
                            var doneFlag = new Container<bool>(false);

                            StartCoroutine(GetBases(proofType,
                                notConclusion,
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

                            StartCoroutine(GetBases(proofType,
                                    new Expression(NOT, assumption),
                                    suppositions,
                                    pendingExpressions,
                                    completeExpressions,
                                    assumptionDisbases,
                                    doneFlag));

                            bool disproven = false;

                            while (!doneFlag.Item) {
                                // if we have even one proof of ~A,
                                // then we're good to break out of the search.
                                if (assumptionDisbases.Count > 0) {
                                    disproven = true;
                                    break;
                                }
                                yield return null;
                            }

                            if (!disproven) {
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

        StartCoroutine(ApplyInferenceRule(ITSELF_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(ITSELF_ELIMINATION));

        // StartCoroutine(ApplyInferenceRule(CONVERSE_INTRODUCTION));
        // StartCoroutine(ApplyInferenceRule(GEACH_TRUTH_FUNCTION_INTRODUCTION));
        // StartCoroutine(ApplyInferenceRule(GEACH_TRUTH_FUNCTION_2_INTRODUCTION));
        // StartCoroutine(ApplyInferenceRule(GEACH_QUANTIFIER_PHRASE_INTRODUCTION));
        // StartCoroutine(ApplyInferenceRule(QUANTIFIER_PHRASE_COORDINATOR_2_INTRODUCTION));
        // StartCoroutine(ApplyInferenceRule(QUANTIFIER_PHRASE_COORDINATOR_2_ELIMINATION));
        
        StartCoroutine(ApplyInferenceRule(DISJUNCTION_INTRODUCTION_LEFT));
        StartCoroutine(ApplyInferenceRule(DISJUNCTION_INTRODUCTION_RIGHT));
        
        StartCoroutine(ApplyInferenceRule(CONJUNCTION_INTRODUCTION));

        StartCoroutine(ApplyInferenceRule(EXISTENTIAL_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(UNIVERSAL_ELIMINATION));

        StartCoroutine(ApplyInferenceRule(SELECTOR_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(SELECTOR_INTRODUCTION_MODAL));
        
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
                StartCoroutine(GetBases(proofType,
                    (Expression) goal.GetArg(1),
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
        StartCoroutine(ApplyInferenceRule(CLOSED_QUESTION_ASSUMPTION));
        StartCoroutine(ApplyInferenceRule(PERCEPTUALLY_CLOSED_ASSUMPTION));

        // @BUG this rule causes planning not to work.
        // StartCoroutine(ApplyInferenceRule(SYMMETRY_OF_LOCATION));

        StartCoroutine(ApplyInferenceRule(SOMETIMES_INTRODUCTION));
        StartCoroutine(ApplyInferenceRule(Contrapose(PERCEPTUAL_BELIEF)));

        StartCoroutine(ApplyInferenceRule(SPEECH_ABILITY));

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
            StartCoroutine(ApplyInferenceRule(ALWAYS_ELIMINATION));
            StartCoroutine(ApplyInferenceRule(MODUS_PONENS));
            StartCoroutine(ApplyInferenceRule(Contrapose(DISJUNCTION_INTRODUCTION_LEFT)));
            StartCoroutine(ApplyInferenceRule(Contrapose(DISJUNCTION_INTRODUCTION_RIGHT)));
            StartCoroutine(ApplyInferenceRule(Contrapose(CONJUNCTION_INTRODUCTION)));
            // StartCoroutine(ApplyInferenceRule(MODUS_TOLLENS));
            StartCoroutine(ApplyInferenceRule(TRANSITIVITY_OF_LOCATION));
            
            StartCoroutine(ApplyInferenceRule(CONVERSE_ELIMINATION));
            StartCoroutine(ApplyInferenceRule(GEACH_TRUTH_FUNCTION_ELIMINATION));
            StartCoroutine(ApplyInferenceRule(GEACH_TRUTH_FUNCTION_2_ELIMINATION));
            StartCoroutine(ApplyInferenceRule(GEACH_QUANTIFIER_PHRASE_ELIMINATION));

            // PLANNING
            // ====
            // @Note we might in the future add a check to see if there is
            // no other proof of G. This is because you don't want to enact
            // something you already believe to be true.
            // 
            // M |- able(self, A),  M :: will(A) => M |- A
            if (proofType == Plan && alternativeBases.Count == 0) {

                // here we assume it's a logical fact
                // that we can will the neutral state of affairs.
                // M :: will(neutral) |- neutral
                if (goal.Equals(NEUTRAL)) {
                    alternativeBases.Add(new Basis(new List<Expression>{new Expression(WILL, NEUTRAL)}, new Substitution()));
                }

                Expression ableToEnactGoal = new Expression(ABLE, SELF, goal);

                HashSet<Basis> abilityBases = new HashSet<Basis>();
                var doneFlag = new Container<bool>(false);

                StartCoroutine(GetBases(proofType,
                    ableToEnactGoal,
                    suppositions,
                    pendingExpressions,
                    completeExpressions,
                    abilityBases,
                    doneFlag));

                while (!doneFlag.Item) {
                    yield return null;
                }

                foreach (Basis abilityBasis in abilityBases) {
                    abilityBasis.Key.Add(new Expression(WILL, goal));
                    alternativeBases.Add(abilityBasis);
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

    public IEnumerator GetBases(ProofType proofType, Expression goal, HashSet<Basis> result, Container<bool> done) {
        yield return StartCoroutine(GetBases(proofType, goal, new HashSet<Expression>(),
            new Dictionary<Expression, List<HashSet<Expression>>>(),
            new Dictionary<Expression, KeyValuePair<HashSet<Expression>, HashSet<Basis>>>(),
            result, done));
    }

    // Asks if the expression is proven by this belief base.
    public IEnumerator Query(Expression query, Container<bool> answer, Container<bool> queryDone) {
        HashSet<Basis> bases = new HashSet<Basis>();
        var doneFlag = new Container<bool>(false);
        IEnumerator basesRoutine = GetBases(Proof, query, bases, doneFlag);
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

    // the characteristic should be a predicate
    // NOTE: (or formula with one free variable, TODO)
    // that captures its mode of presentation
    // 
    // returns true if the object represented already
    // is found within the model, false otherwise.
    // 
    // either way, a percept with
    // the given characteristic is asserted.
    public Expression ConstructPercept(Expression characteristic, Vector3 location) {
        Debug.Assert(characteristic.Type.Equals(PREDICATE));

        Expression param = null;

        // @Note this is linear search. Not great. Change data structure later.
        foreach (var nameAndLocation in Locations) {
            if (location == nameAndLocation.Value) {
                param = nameAndLocation.Key;
            }
        }

        if (param == null) {
            param = new Expression(new Parameter(SemanticType.INDIVIDUAL, GetNextParameterID()));
            Locations.Add(param, new Vector3(location.x, location.y, location.z));
        }

        // @NOTE once tense is implemented, this shouldn't be an assertion, and
        // should just be added directly to the belief base.
        //StartCoroutine(Assert(new Expression(PERCEIVE, SELF, new Expression(characteristic, param))));

        Expression percept =
            new Expression(PERCEIVE, SELF, new Expression(characteristic, param));
        
        Add(percept, Timestamp);

        return param;
    }

    // TODO: finish this

    // removes all parameters from the expression e
    // and replaces them with demonstrative expressions, determiners, etc.
    public IEnumerator ReplaceParameters(Expression e, Container<Expression> result) {
        // for now, we assume every
        // parameter has type e
        if (e.Head is Parameter) {
            Debug.Assert(e.Head.Type.Equals(INDIVIDUAL));

            var fc = new Expression(PERCEIVE, SELF, new Expression(FET, e));

            var fcBases = new HashSet<Basis>();
            var done = new Container<bool>(false);

            StartCoroutine(GetBases(Proof, fc, fcBases, done));

            while (!done.Item) {
                yield return null;
            }

            Expression bestPredicate = null;
            foreach (var fcBasis in fcBases) {
                var predicate = fcBasis.Value[FET.Head as Variable];
                if (bestPredicate == null || predicate.Depth < bestPredicate.Depth) {
                    bestPredicate = predicate;
                }
            }

            // if we fail, there's always 'this'
            if (bestPredicate == null) {
                result.Item = THIS;
                yield break;
            }

            result.Item = new Expression(SELECTOR, bestPredicate);
            yield break;
        }

        var newArgs = new Expression[e.NumArgs];

        for (int i = 0; i < e.NumArgs; i++) {
            if (e.GetArg(i) is Empty) {
                continue;
            }

            Container<Expression> res = new Container<Expression>(null);

            StartCoroutine(ReplaceParameters(e.GetArgAsExpression(i), res));

            while (res.Item == null) {
                yield return null;
            }

            newArgs[i] = res.Item;
        }

        result.Item = new Expression(new Expression(e.Head), newArgs);
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
    public IEnumerator Assert(Expression assertion) {
        // We already believe assertion A.
        // We accept it, but don't change our belief state.
        var answer = new Container<bool>(false);
        var queryDone = new Container<bool>(false);
        var query = Query(assertion, answer, queryDone);
        StartCoroutine(query);

        while (!queryDone.Item) {
            yield return null;
        }

        if (answer.Item) {
            StopCoroutine(query);
            yield break;
        }

        var notAssertionBases = new HashSet<Basis>();
        var done = new Container<bool>(false);
        var disproof = GetBases(Proof, new Expression(NOT, assertion), notAssertionBases, done);
        StartCoroutine(disproof);

        var weakestPremises = new HashSet<Expression>();

        while (!done.Item || notAssertionBases.Count > 0) {
            // if we have a proof of ~A come in,
            // we want to evaluate the premises of the proof.
            if (notAssertionBases.Count > 0) {
                foreach (var notAssertionBasis in notAssertionBases) {
                    Expression weakestPremise = VERUM;
                    float weakestPlausibility = float.PositiveInfinity;
                    foreach (var notAssertionPremise in notAssertionBasis.Key) {
                        float premisePlausibility;
                        
                        // here, we estimate the plausibility of the premises.
                        // We'll want this probably to be its own judgement
                        // which can be proven, but details for that aren't
                        // clear yet.
                        if (notAssertionPremise.Head.Equals(PERCEIVE.Head)) {
                            premisePlausibility = 2;
                        } else if (notAssertionPremise.Head.Equals(BELIEVE.Head)) {
                            premisePlausibility = 1;
                        } else {
                            premisePlausibility = 3;
                        }

                        if (premisePlausibility < weakestPlausibility) {
                            weakestPremise = notAssertionPremise;
                            weakestPlausibility = premisePlausibility;
                        } else if (premisePlausibility == weakestPlausibility) {
                            // unclear what we want in this case.
                            // should it be a coin flip, or should it be
                            // a function of premise order? Right now,
                            // I'll always remove the rightmost premise.
                            weakestPremise = notAssertionPremise;
                            weakestPlausibility = premisePlausibility;
                        }
                    }

                    float assertionPlausibility;
                    if (assertion.Head.Equals(PERCEIVE.Head)) {
                        assertionPlausibility = 2.5f;
                    } else if (assertion.Head.Equals(BELIEVE.Head)) {
                        assertionPlausibility = 1.5f;
                    } else {
                        assertionPlausibility = 3.5f;
                    }

                    if (assertionPlausibility > weakestPlausibility) {
                        weakestPremises.Add(weakestPremise);
                    } else {
                        // in this case, the assertion is
                        // weaker than the weakest premise.
                        // 
                        // in this case, we reject the assertion.
                        StopCoroutine(disproof);
                        yield break;
                    }
                }
                // here, we clear the bases so
                // we don't repeat work next time.
                notAssertionBases.Clear();
            }
            yield return null;
        }

        // A is more plausible than
        // at least one premise in each proof.
        foreach (var weakPremise in weakestPremises) {
            Remove(weakPremise, Timestamp);
        }

        // now, we add A.
        Add(assertion, Timestamp);
        yield break;
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
        var preferencesInBeliefBase = BeliefBase[TRUTH_FUNCTION_2][BETTER.Head];
        var preferables = new HashSet<Expression>();

        // @Note: we assume preferences are timestamped 0
        // and are eternal. This should change, eventually.
        foreach (var preferenceAndTimes in preferencesInBeliefBase) {
            if (preferenceAndTimes.Value.Contains(0) || preferenceAndTimes.Value.Contains(1)) {
                preferables.Add((Expression) preferenceAndTimes.Key.GetArg(0));
                preferables.Add((Expression) preferenceAndTimes.Key.GetArg(1));
            }
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

        var bestPlan = new List<Expression>();
        while (ranking.Count != 0) {
            Expression nextBestGoal = ranking.Pop();
            var goalBases = new HashSet<Basis>();
            var doneFlag = new Container<bool>(false);
            StartCoroutine(GetBases(Plan, nextBestGoal, goalBases, doneFlag));

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

                alreadyTrue = true;
                
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
