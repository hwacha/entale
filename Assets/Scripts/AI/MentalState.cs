using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using static SemanticType;
using static Expression;
using static ProofType;
using static InferenceRule;

using UnityEngine;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;

// The prover and the planner each use the same inference mechanism,
// so this enum specifies some parameters to it.
public enum ProofType {
    Proof,
    Plan
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
    // the time, in milliseconds, that the mental
    // state is allowed to run search in one frame
    protected static long TIME_BUDGET = 8;

    public FrameTimer FrameTimer;

    protected uint ParameterID;

    private SortedSet<Expression> KnowledgeBase;

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
    // protected uint Timestamp = 0;
    int MaxDepth = 0;

    void Update() {
        //Timestamp++;
    }

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public void Initialize(Expression[] initialKnowledge) {
        ParameterID = 0;
        Locations = new Dictionary<Expression, Vector3>();

        if (KnowledgeBase != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        KnowledgeBase = new SortedSet<Expression>();

        for (int i = 0; i < initialKnowledge.Length; i++) {
            if (!initialKnowledge[i].Type.Equals(TRUTH_VALUE)) {
                throw new ArgumentException("MentalState(): expected sentences for base.");
            }
            KnowledgeBase.Add(initialKnowledge[i]);
            if (initialKnowledge[i].Depth > MaxDepth) {
                MaxDepth = initialKnowledge[i].Depth;
            }
        }
    }

    // gets a variable that's unused in the goal
    private static Variable GetUnusedVariable(SemanticType t, HashSet<Variable> usedVariables) {
        Variable x = new Variable(t, 0);
        while (usedVariables.Contains(x)) {
            x = new Variable(t, x.ID + 1);
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
        #region Parameters
        public readonly Expression Lemma;
        public readonly uint Depth;
        public readonly ProofNode Parent;
        public readonly int MeetBasisIndex;
        public readonly ProofNode OlderSibling;
        public readonly Expression Supplement;
        public readonly bool IsAssumption;
        #endregion

        #region Variables
        public List<ProofBasis> YoungerSiblingBases;
        public ProofBases ChildBases;
        public bool IsLastChild;
        #endregion

        public ProofNode(Expression lemma, uint depth, ProofNode parent,
            int meetBasisIndex,
            ProofNode olderSibling = null,
            Expression supplement = null,
            bool hasYoungerSibling = false,
            bool isAssumption = false) {
            Lemma = lemma;
            Depth = depth;
            Parent = parent;
            MeetBasisIndex = meetBasisIndex;
            OlderSibling = olderSibling;
            Supplement = supplement;
            IsAssumption = isAssumption;

            YoungerSiblingBases = new List<ProofBasis>();
            if (!hasYoungerSibling) {
                YoungerSiblingBases.Add(new ProofBasis());
            }
            ChildBases = new ProofBases();
            IsLastChild = false;
        }
    }

    // TODO:
    // - prevent cycles by tracking completed proofs of given lemmas
    // - conditionals (maybe a different approach than having a list of
    //   extra suppositions at each proof node?)
    // - consistent ordering of inferential complexity
    //   (this was working in BFS but seems messed up by the stack)
    public IEnumerator StreamProofs(ProofBases bases, Expression conclusion,
        Container<bool> done, ProofType pt = Proof) {
        // we can only prove sentences.
        Debug.Assert(conclusion.Type.Equals(TRUTH_VALUE));
 
        // we're going to do a depth-first search
        // with successively higher bounds on the
        // depth allowed.
        uint maxDepth = 0;
        // we use this to gauge if we've made an
        // exhaustive search at this level.
        // If the depth we reach is less than the
        // maximum, then no further inferences
        // were attempted.
        uint reachedDepth = 0;
        while (reachedDepth + 1 >= maxDepth) {
            // at the beginning of any iterated step,
            // we check if we've gone past
            // our allotted time budget.
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            // Debug.Log("Starting search at d=" + maxDepth);

            bases.Clear();

            reachedDepth = 0;

            // we set up our stack for DFS
            // with the intended
            var root = new ProofNode(conclusion, 0, null, 0);
            root.ChildBases = bases;
            root.IsLastChild = true;
            var stack = new Stack<ProofNode>();
            stack.Push(root);

            // we go through the stack.
            while (stack.Count != 0) {
                if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                    yield return null;
                }

                var current = stack.Pop();

                if (current.Depth > reachedDepth) {
                    reachedDepth = current.Depth;
                }

                // Debug.Log("visiting " + current.Lemma);
                
                var sends = new List<KeyValuePair<ProofBases, bool>>();
 
                for (int i = 0; i < current.YoungerSiblingBases.Count; i++) {
                    if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                        yield return null;
                    }

                    var youngerSiblingBasis = current.YoungerSiblingBases[i];

                    var currentLemma = current.Lemma.Substitute(youngerSiblingBasis.Substitution);

                    // Debug.Log("substituted to " + currentLemma);

                    var searchBases = new ProofBases();

                    // these are our base cases.
                    if (currentLemma.GetVariables().Count == 0) {
                        if (KnowledgeBase.Contains(currentLemma)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        if (currentLemma.Head.Equals(ABLE.Head) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).Head.Equals(SAY.Head) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(1).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        if (currentLemma.Head.Equals(ABLE.Head) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).Head.Equals(AT.Head) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(0).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        if (currentLemma.Head.Equals(AT.Head)) {
                            var a = currentLemma.GetArgAsExpression(0);
                            var b = currentLemma.GetArgAsExpression(1);

                            if (Locations.ContainsKey(a) &&
                                Locations.ContainsKey(b)) {
                                var aLocation = Locations[a];
                                var bLocation = Locations[b];

                                var dx = aLocation.x - bLocation.x;
                                var dy = aLocation.y - bLocation.y;
                                var dz = aLocation.z - bLocation.z;

                                var distance = dx * dx + dy * dy + dz * dz;

                                if (distance < 5) {
                                    var basis = new ProofBasis();
                                    basis.AddPremise(currentLemma);
                                    searchBases.Add(basis);
                                }
                            }
                        }
                    } else {
                        // Satisfiers():
                        // first, we get the domain to search through.
                        // this is going to correspond to all sentences
                        // that are structural candidates for matching
                        // the formula, given the structure of the formula's
                        // variables and semantic types.
                        var variables = currentLemma.GetVariables();
                        var bottomSubstitution = new Substitution();
                        var topSubstitution = new Substitution();
                        foreach (Variable v in variables) {
                            bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
                            topSubstitution.Add(v, new Expression(new Top(v.Type)));
                        }

                        var bottom = currentLemma.Substitute(bottomSubstitution);
                        var top = currentLemma.Substitute(topSubstitution);
                        // TODO change CompareTo() re: top/bottom so that
                        // expressions which would unify with F(x) are
                        // included within the bounds of bot(bot) and top(top)
                        // This will involve check partial type application
                        //
                        // BUT leave this until there's a geniune use case
                        // in inference, since the way it occurs now is
                        // potentially more efficient.
                        SortedSet<Expression> domain = KnowledgeBase.GetViewBetween(bottom, top);

                        // then, we iterate through the domain and pattern match (unify)
                        // the formula against the sentences in the belief base.
                        // any sentences that match get added, along with the
                        // unifying substitution, to the basis set.
                        foreach (Expression belief in domain) {
                            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                                yield return null;
                            }
                            // Debug.Log("domain includes " + belief);
                            HashSet<Substitution> unifiers = currentLemma.GetMatches(belief);
                            foreach (Substitution unifier in unifiers) {
                                searchBases.Add(new ProofBasis(new List<Expression>(){belief},
                                    unifier));
                            }
                        }
                    }

                    //
                    // Slight @Bug: because we pop onto the stack,
                    // the order of proofs for otherwise equivalent
                    // can be reversed depending on depth.
                    // This violates the natural ordering of reasons
                    // that an NPC will provide to justify their
                    // answer. We want those reasons to be maximally
                    // simple (the least amount of inferential remove)
                    // and consistent (the same answer will yield the
                    // same reason, if that reason applies in both cases)
                    // 
                    // How can we ensure the nodes are pushed in the
                    // correct order? I already tried to reverse the
                    // order of the new nodes.
                    //

                    bool exhaustive = false;

                    // we only check against inference rules if
                    // our search bound hasn't been reached.
                    if (current.Depth < maxDepth) {
                        uint nextDepth = current.Depth + 1;
                        exhaustive = true;
                        // inferences here
                        
                        var newStack = new Stack<ProofNode>();

                        // truly +
                        if (currentLemma.Head.Equals(TRULY.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i));
                            exhaustive = false;
                        }

                        // double negation +
                        if (currentLemma.Head.Equals(NOT.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            if (subclause.Head.Equals(NOT.Head)) {
                                var subsubclause = subclause.GetArgAsExpression(0);
                                newStack.Push(new ProofNode(subsubclause, nextDepth, current, i));
                                exhaustive = false;
                            }

                            // nonidentity assumption
                            if (subclause.Head.Equals(IDENTITY.Head)) {
                                newStack.Push(new ProofNode(new Expression(NOT, currentLemma), nextDepth, current, i, isAssumption: true));
                                exhaustive = false;
                            }
                        }

                        // or +
                        if (currentLemma.Head.Equals(OR.Head)) {
                            var disjunctA = currentLemma.GetArgAsExpression(0);
                            var disjunctB = currentLemma.GetArgAsExpression(1);
                            newStack.Push(new ProofNode(disjunctA, nextDepth, current, i));
                            newStack.Push(new ProofNode(disjunctB, nextDepth, current, i));
                            exhaustive = false;
                        }

                        // and +
                        if (currentLemma.Head.Equals(AND.Head)) {
                            var conjunctA = currentLemma.GetArgAsExpression(0);
                            var conjunctB = currentLemma.GetArgAsExpression(1);

                            var bNode = new ProofNode(conjunctB, nextDepth, current, i, hasYoungerSibling: true);
                            var aNode = new ProofNode(conjunctA, nextDepth, current, i, bNode);

                            newStack.Push(aNode);
                            newStack.Push(bNode);
                            exhaustive = false;
                        }

                        // some +
                        if (currentLemma.Head.Equals(SOME.Head)) {
                            var f = currentLemma.GetArgAsExpression(0);
                            var g = currentLemma.GetArgAsExpression(1);

                            var x = new Expression(GetUnusedVariable(INDIVIDUAL, currentLemma.GetVariables()));

                            var fx = new Expression(f, x);
                            var gx = new Expression(g, x);

                            var gxNode = new ProofNode(gx, nextDepth, current, i, hasYoungerSibling: true);
                            var fxNode = new ProofNode(fx, nextDepth, current, i, gxNode);

                            newStack.Push(fxNode);
                            newStack.Push(gxNode);
                            exhaustive = false;
                        }

                        if (currentLemma.Depth <= this.MaxDepth) {
                            // plan +
                            if (pt == Plan) {
                                var able = new Expression(ABLE, currentLemma, SELF);
                                var will = new Expression(WILL, currentLemma);

                                var ableNode = new ProofNode(able, nextDepth, current, i, supplement: will);

                                newStack.Push(ableNode);
                                exhaustive = false;
                            }

                            // see -
                            if (!currentLemma.Head.Equals(SEE.Head)) {
                                var see = new Expression(SEE, currentLemma);
                                var seeNode = new ProofNode(see, nextDepth, current, i);
                                newStack.Push(seeNode);
                                exhaustive = false;
                            }

                            // knows -
                            if (!currentLemma.Head.Equals(KNOW.Head)) {
                                var know = new Expression(KNOW, currentLemma,
                                    new Expression(GetUnusedVariable(INDIVIDUAL, currentLemma.GetVariables())));

                                var knowNode = new ProofNode(know, nextDepth, current, i);
                                newStack.Push(knowNode);
                                exhaustive = false;
                            }
                        }

                        // here we reverse the order of new proof nodes.
                        if (newStack.Count > 0) {
                            newStack.Peek().IsLastChild = true;
                            do {
                                stack.Push(newStack.Pop());
                            } while (newStack.Count > 0);
                        }
                    } else {
                        exhaustive = false;
                    }

                    // we're not going to pass down the child bases this time,
                    // because we don't have anything to give.
                    if (searchBases.IsEmpty() &&
                        !exhaustive &&
                        current.Depth != maxDepth) {
                        continue;
                    }

                    current.ChildBases.Add(searchBases);
                    sends.Add(new KeyValuePair<ProofBases, bool>(searchBases, exhaustive));
                }

                int meetBasisIndex = 0;
                // TODO fix this condition so that an empty bases gets sent only when
                // it constitutes the last possible search for an assumption.
                if (sends.Count == 0 && current.IsLastChild) {
                    sends.Add(new KeyValuePair<ProofBases, bool>(new ProofBases(), true));
                    meetBasisIndex = -1;
                }

                for (int i = 0; i < sends.Count; i++) {
                    ProofNode merge = current;
                    ProofBases sendBases = sends[i].Key;
                    bool exhaustive = sends[i].Value;

                    // pass on the bases and merge them all the way to
                    // its ancestral node.
                    while (merge != null) {
                        if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                            yield return null;
                        }

                        // this is the basis which gave us this assignment -
                        // we want to meet with this one, and none of the others.
                        var meetBasis = meetBasisIndex == -1 ? null : merge.YoungerSiblingBases[meetBasisIndex];

                        // Debug.Log("Merge " + merge.Lemma + " sending " + sendBases);

                        // trim each of the merged bases to
                        // discard unused variable assignments.
                        foreach (var sendBasis in sendBases) {
                            var trimmedSubstitution = new Substitution();
                            foreach (var assignment in sendBasis.Substitution) {
                                if (merge.Lemma.HasOccurenceOf(assignment.Key)) {
                                    trimmedSubstitution.Add(assignment.Key, assignment.Value);
                                }
                            }
                            sendBasis.Substitution = trimmedSubstitution;
                        }

                        // this is the fully assigned formula,
                        // the proofs of which we're merging.
                        var mergeLemma = meetBasis == null ? merge.Lemma : merge.Lemma.Substitute(meetBasis.Substitution);
                        // Debug.Log("Merging: " + mergeLemma + " sending: " + sendBases);

                        ProofBases productBases = new ProofBases();

                        if (merge.IsAssumption) {
                            
                            // no refutation
                            if (sendBases.IsEmpty() &&
                                merge.ChildBases.IsEmpty() &&
                                meetBasis != null &&
                                (exhaustive || current.Depth == maxDepth ||
                                 mergeLemma.Depth >= this.MaxDepth ||
                                 merge.IsLastChild)) {
                                // Debug.Log("Assumption sending " + mergeLemma);
                                // we can safely assume the content of
                                // this assumption node
                                var assumptionBasis = new ProofBasis();
                                assumptionBasis.AddPremise(mergeLemma.GetArgAsExpression(0));

                                var productBasis = new ProofBasis(meetBasis, assumptionBasis);
                                productBases.Add(productBasis);
                            } else {
                                // Debug.Log("Assumption not Sending " + mergeLemma);
                            }
                            // otherwise, if there's a refutation,
                            // or if it's too early too tell,
                            // don't send anything
                        } else {
                            var joinBases = sendBases;
                            if (!joinBases.IsEmpty() && meetBasis != null) {

                                // here, we merge the bases from siblings and
                                // children. sibling bases ^ child bases

                                // if we have a supplemental premise,
                                // we add it here.
                                if (merge.Supplement != null) {
                                    joinBases = new ProofBases();
                                    joinBases.Add(sendBases);
                                    foreach (var joinBasis in joinBases) {
                                        joinBasis.AddPremise(merge.Supplement.Substitute(joinBasis.Substitution));
                                    }
                                }

                                // we form the product of our meet basis
                                // and child bases.
                                foreach (var joinBasis in joinBases) {
                                    var productBasis = new ProofBasis(meetBasis, joinBasis);
                                    productBases.Add(productBasis);
                                }
                            }
                        }

                        // if (productBases.IsEmpty() &&
                        //     !exhaustive &&
                        //     current.Depth != maxDepth) {
                        //     break;
                        // }

                        // we pass on our new bases to the older sibling
                        // if we have one, to the parent otherwise.
                        if (merge.OlderSibling != null) {
                            foreach (var productBasis in productBases) {
                                merge.OlderSibling.YoungerSiblingBases.Add(productBasis);
                            }
                            break;
                        }

                        if (merge.Parent != null) {
                            merge.Parent.ChildBases.Add(productBases);
                            sendBases = productBases;
                        }

                        meetBasisIndex = merge.MeetBasisIndex;

                        merge = merge.Parent;
                    }

                    meetBasisIndex = i + 1;
                }
            }
            // increment the upper bound and go again.
            maxDepth++;
        }
        done.Item = true;
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

        Expression percept = new Expression(SEE, new Expression(characteristic, param));
        
        // we need to queue this up so that it doesn't cause a
        // concurrent modification problem.
        
        KnowledgeBase.Add(percept);

        return param;
    }

    // a direct assertion.
    // @TODO add an inference rule to cover knowledge from
    // assertion. Now is a simple fix.
    public IEnumerator ReceiveAssertion(Expression content, Expression speaker) {
        KnowledgeBase.Add(new Expression(KNOW, content, speaker));
        // TODO check to see if this is inconsistent
        // with the current knowledge base
        yield return null;
    }

    public IEnumerator ReceiveRequest(Expression content, Expression speaker) {
        // the proposition we add here, we want to be the equivalent to
        // knowledge in certain ways. So, for example, knows(p, S) -> p
        // in the same way that X(p, S) -> good(p).
        // 
        // Right now, we literally have this as S knows that p is good,
        // but this feels somehow not aesthetically pleasing to me. I'll
        // try it out for now.
        KnowledgeBase.Add(new Expression(KNOW, new Expression(GOOD, content), speaker));

        yield return null;
    }

    public IEnumerator DecideCurrentPlan(List<Expression> plan, Container<bool> done) {
        var goodProofs = new ProofBases();
        var goodDone = new Container<bool>(false);
        // we're going to get our domain of goods by trying to prove
        // good(p) and seeing what it assigns to p.
        StartCoroutine(StreamProofs(goodProofs, new Expression(GOOD, ST), goodDone));
        while (!goodDone.Item) {
            yield return null;
        }

        List<Expression> evaluativeBase = new List<Expression>();
        foreach (var goodProof in goodProofs) {
            var assignment = goodProof.Substitution[ST.Head as Variable];
            evaluativeBase.Add(assignment);
        }

        foreach (var good in evaluativeBase) {
            var proofBases = new ProofBases();
            var proofDone = new Container<bool>(false);
            StartCoroutine(StreamProofs(proofBases, good, proofDone, Proof));
            while (!proofDone.Item) {
                yield return null;
            }
            if (!proofBases.IsEmpty()) {
                continue;
            }

            var planBases = new ProofBases();
            var planDone = new Container<bool>(false);
            StartCoroutine(StreamProofs(planBases, good, planDone, Plan));
            while (!planDone.Item) {
                yield return null;
            }

            if (!planBases.IsEmpty()) {
                List<Expression> bestPlan = null;
                foreach (var basis in planBases) {
                    var resolutions = new List<Expression>();
                    foreach (var premise in basis.Premises) {
                        if (premise.Type.Equals(CONFORMITY_VALUE)) {
                            resolutions.Add(premise);
                        }
                    }
                    if (bestPlan == null || bestPlan.Count > resolutions.Count) {
                        bestPlan = resolutions;
                    }
                }
                plan.AddRange(bestPlan);
                done.Item = true;
                yield break;
            }
        }

        plan.Add(new Expression(WILL, NEUTRAL));

        done.Item = true;
        yield break;
    }
}

