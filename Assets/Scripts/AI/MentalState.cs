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

    protected int ParameterID;
    public int Timestamp = 0; // public for testing purposes

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
    int MaxDepth = 0;

    void Update() {
        Timestamp++;
    }

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public void Initialize(Expression[] initialKnowledge) {
        ParameterID = 0;
        Timestamp = 1;
        Locations = new Dictionary<Expression, Vector3>();

        if (KnowledgeBase != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        KnowledgeBase = new SortedSet<Expression>();
        var timeParameter = new Expression(new Parameter(TIME, Timestamp));

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
    public int GetNextParameterID() {
        var param = ParameterID;
        ParameterID++;
        return param;
    }

    // helper funtion for StreamBases().
    // checks two geached, complex evidentials against one another.
    // 
    // if, for example, we have
    // knows(knows(p, a), b) being checked against knows(knows(knows(p, a), b), c),
    // we want this to return true (along with any other subsequence of a, b, c).
    // 
    private IEnumerator EvidentialContains() {
        // TODO 6/23
        yield break;
    }

    private Expression Tensify(Expression e) {
        if (e.Head.Type.Equals(PREDICATE) ||
            e.Head.Type.Equals(RELATION_2)) {
            return new Expression(WHEN, e, new Expression(new Parameter(TIME, Timestamp)));
        }
        if (e.Head.Equals(NOT.Head)) {
            return new Expression(NOT, Tensify(e.GetArgAsExpression(0)));
        }
        if (e.Head.Equals(PAST.Head)) {
            return new Expression(PAST, Tensify(e.GetArgAsExpression(0)));
        }
        if (e.Head.Equals(FUTURE.Head)) {
            return new Expression(FUTURE, Tensify(e.GetArgAsExpression(0)));
        }
        if (e.Head.Equals(KNOW.Head)) {
            return new Expression(WHEN,
                new Expression(KNOW,
                    Tensify(e.GetArgAsExpression(0)),
                    e.GetArgAsExpression(1)),
                new Expression(new Parameter(TIME, Timestamp)));
        }
        if (e.Head.Equals(SEE.Head)) {
                new Expression(SEE,
                    Tensify(e.GetArgAsExpression(0)),
                    e.GetArgAsExpression(1));
        }
        return e;
    }

    private static Expression GetContent(Expression e) {
        if (e.Head.Equals(KNOW.Head) ||
            e.Head.Equals(SEE.Head) ||
            e.Head.Equals(WHEN.Head)) {
            return GetContent(e.GetArgAsExpression(0));
        }

        if (e.Head.Equals(NOT.Head)) {
            return new Expression(NOT, GetContent(e.GetArgAsExpression(0)));
        }
        return e;
    }

    private static Expression GetContentTimeBounds(Expression e, HashSet<Variable> variables) {
        if (e.Head.Equals(KNOW.Head) ||
            e.Head.Equals(SEE.Head)) {
            return new Expression(new Expression(e.Head),
                GetContentTimeBounds(e.GetArgAsExpression(0), variables),
                e.GetArgAsExpression(1));
        }

        if (e.Head.Equals(NOT.Head)) {
            return new Expression(NOT, GetContentTimeBounds(e.GetArgAsExpression(0), variables));
        }

        if (e.Head.Equals(WHEN.Head)) {
            if (e.GetArgAsExpression(0).Head.Equals(KNOW.Head) ||
                e.GetArgAsExpression(0).Head.Equals(SEE.Head)) {
                return new Expression(WHEN, GetContentTimeBounds(e.GetArgAsExpression(0), variables), e.GetArgAsExpression(1));
            } else {
                return new Expression(WHEN, e.GetArgAsExpression(0), new Expression(GetUnusedVariable(TIME, variables)));
            }
        }

        return e;
    }

    // this should just be temporary,
    // as the tensed query seems untenable.
    private enum Tense {
        Present,
        Past,
        Future
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
        public readonly Tense Tense;
        public readonly bool Parity;
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
            bool isAssumption = false,
            Tense tense = Tense.Present,
            bool parity = true) {
            Lemma = lemma;
            Depth = depth;
            Parent = parent;
            MeetBasisIndex = meetBasisIndex;
            OlderSibling = olderSibling;
            Supplement = supplement;
            IsAssumption = isAssumption;
            Tense = tense;
            Parity = parity;

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
            var root = new ProofNode(Tensify(conclusion), 0, null, 0);
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

                    // the bases we get from directly
                    // querying the knowledge base.
                    var searchBases = new ProofBases();

                    // inertial tensed query
                    // (TAKE 2)
                    if (currentLemma.Head.Equals(WHEN.Head)) {
                        var boundedCurrentLemma = GetContentTimeBounds(currentLemma, currentLemma.GetVariables());
                        var variables = boundedCurrentLemma.GetVariables();
                        var bottomSubstitution = new Substitution();
                        var topSubstitution = new Substitution();
                        foreach (Variable v in variables) {
                            bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
                            topSubstitution.Add(v, new Expression(new Top(v.Type)));
                        }
                        var bottom = boundedCurrentLemma.Substitute(bottomSubstitution);
                        var top = boundedCurrentLemma.Substitute(topSubstitution);

                        var zero = new Expression(WHEN,
                            bottom.GetArgAsExpression(0),
                            new Expression(new Bottom(TIME)));

                        var tHorizon = new Expression(WHEN,
                            top.GetArgAsExpression(0),
                            new Expression(new Top(TIME)));

                        SortedSet<Expression> timespan;
                        IEnumerable<Expression> iter;

                        if (current.Tense == Tense.Present || current.Tense == Tense.Past) {
                            Debug.Log(zero + " | " + top);
                            timespan = KnowledgeBase.GetViewBetween(zero, top);
                            iter = timespan.Reverse();
                        } else {
                            timespan = KnowledgeBase.GetViewBetween(bottom, tHorizon);
                            iter = timespan;
                        }

                        bool admissible = true;
                        Expression currentContent = null;

                        foreach (var sample in iter) {
                            // @Note we assume as an invariant that
                            // each sample we encounter is tensed.
                            var sampleContent = GetContent(sample);
                            bool sampleParity = true;

                            // sentences will be of the form not(when(A, t))
                            if (sampleContent.Head.Equals(NOT.Head)) {
                                sampleContent = sampleContent.GetArgAsExpression(0);
                                sampleParity = false;
                            }

                            if (currentContent == null ||
                               !currentContent.Equals(sampleContent)) {
                                currentContent = sampleContent;
                                admissible = true;
                            }

                            if (!admissible) {
                                continue;
                            }

                            // @Note this may be very wrong.
                            // And even if it isn't, it isn't
                            // good to count on it being right.
                            // 
                            // Update: it is, in fact, very wrong.
                            // Need to reimplement EvidentialContains()
                            int lemmaDepthMinusNot = currentLemma.Depth;
                            if (currentLemma.Head.Equals(NOT.Head)) {
                                --lemmaDepthMinusNot;
                            }

                            int sampleDepthMinusNot = sample.Depth;
                            if (sample.Head.Equals(NOT.Head)) {
                                --sampleDepthMinusNot;
                            }

                            if (lemmaDepthMinusNot <= sampleDepthMinusNot) {
                                // we have a match!
                                if (current.Parity == sampleParity) {
                                    var basis = new ProofBasis();
                                    basis.AddPremise(sample);
                                    searchBases.Add(basis);
                                    // this means the sample we're looking at
                                    // contradicts the query.
                                } else if (current.Tense == Tense.Present) {
                                    admissible = false;
                                }
                            }
                        }
                    }
                    // (END TAKE 2)

                    // TODO change CompareTo() re: top/bottom so that
                    // expressions which would unify with F(x) are
                    // included within the bounds of bot(bot) and top(top)
                    // This will involve check partial type application
                    //
                    // BUT leave this until there's a geniune use case
                    // in inference, since the way it occurs now is
                    // potentially more efficient.

                    // these are our base cases.
                    if (currentLemma.GetVariables().Count == 0) {

                        // I can say anything.
                        if (currentLemma.Head.Equals(ABLE.Head) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).Head.Equals(SAY.Head) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(1).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // I can go anywhere.
                        if (currentLemma.Head.Equals(ABLE.Head) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).Head.Equals(AT.Head) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(0).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // if a and b are within 5 meters of each other,
                        // then at(a, b).
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

                    // TODO add expansion rules to turn predicates into
                    // evidential predicates.

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
                            newStack.Push(new ProofNode(
                                subclause,
                                nextDepth,
                                current,
                                i,
                                tense: current.Tense,
                                parity: current.Parity));
                            exhaustive = false;
                        }

                        // double negation +
                        if (currentLemma.Head.Equals(NOT.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            if (subclause.Head.Equals(NOT.Head)) {
                                var subsubclause = subclause.GetArgAsExpression(0);
                                newStack.Push(new ProofNode(subsubclause,
                                    nextDepth, current, i,
                                    tense: current.Tense,
                                    parity: current.Parity));
                                exhaustive = false;
                            } else {
                                newStack.Push(new ProofNode(
                                    subclause, nextDepth, current, i,
                                    tense: current.Tense,
                                    parity: !current.Parity));
                                exhaustive = false;
                            }

                            // nonidentity assumption
                            if (subclause.Head.Equals(IDENTITY.Head)) {
                                newStack.Push(new ProofNode(new Expression(NOT, currentLemma),
                                    nextDepth, current, i,
                                    isAssumption: true,
                                    tense: current.Tense,
                                    parity: current.Parity));
                                exhaustive = false;
                            }
                        }

                        // or +
                        if (currentLemma.Head.Equals(OR.Head)) {
                            var disjunctA = currentLemma.GetArgAsExpression(0);
                            var disjunctB = currentLemma.GetArgAsExpression(1);
                            newStack.Push(new ProofNode(disjunctA, nextDepth, current, i,
                                tense: current.Tense, parity: current.Parity));
                            newStack.Push(new ProofNode(disjunctB, nextDepth, current, i,
                                tense: current.Tense, parity: current.Parity));
                            exhaustive = false;
                        }

                        // and +
                        if (currentLemma.Head.Equals(AND.Head)) {
                            var conjunctA = currentLemma.GetArgAsExpression(0);
                            var conjunctB = currentLemma.GetArgAsExpression(1);

                            var bNode = new ProofNode(conjunctB, nextDepth, current, i,
                                hasYoungerSibling: true,
                                tense: current.Tense, parity: current.Parity);
                            var aNode = new ProofNode(conjunctA, nextDepth, current, i, bNode,
                                tense: current.Tense, parity: current.Parity);

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

                            var gxNode = new ProofNode(gx, nextDepth, current, i,
                                hasYoungerSibling: true,
                                tense: current.Tense, parity: current.Parity);
                            var fxNode = new ProofNode(fx, nextDepth, current, i, gxNode,
                                tense: current.Tense, parity: current.Parity);

                            newStack.Push(fxNode);
                            newStack.Push(gxNode);
                            exhaustive = false;
                        }

                        // past +
                        if (currentLemma.Head.Equals(PAST.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i,
                                tense: Tense.Past, parity: current.Parity));
                            exhaustive = false;
                        }

                        // future +
                        if (currentLemma.Head.Equals(FUTURE.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i,
                                tense: Tense.Future, parity: current.Parity));
                            exhaustive = false;
                        }

                        if (currentLemma.Depth <= this.MaxDepth) {
                            // plan -
                            if (pt == Plan) {
                                var able = new Expression(ABLE, currentLemma, SELF);
                                var will = new Expression(WILL, currentLemma);

                                var ableNode = new ProofNode(able, nextDepth, current, i,
                                    supplement: will,
                                    tense: current.Tense,
                                    parity: current.Parity);

                                newStack.Push(ableNode);
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

        var timeParam = new Expression(new Parameter(TIME, Timestamp));

        var percept = new Expression(SEE,
            new Expression(WHEN, new Expression(characteristic, param), timeParam),
            SELF);

        // we need to queue this up so that it doesn't cause a
        // concurrent modification problem.
        
        KnowledgeBase.Add(percept);

        return param;
    }

    // @Note: this should be private ultimately.
    // public for testing purposes.
    public IEnumerator AddToKnowledgeBase(Expression knowledge) {
        var assertionTime = Timestamp;
        Expression cur = knowledge;
        bool parity = true;
        Expression evidential = null;

        // we turn this statement into an evidentialized form.
        // 
        // TODO: make this smarter, so it doesn't mess up
        // sentences like not(knows(p, x)) or
        // knows(~~p, x) and stuff of the sort.
        while (true) {
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            if (cur.Head.Equals(NOT.Head)) {
                cur = cur.GetArgAsExpression(0);
                parity = !parity;
            } else if (parity && cur.Head.Equals(KNOW.Head)) {             
                var wrapper = new Expression(KNOW_TENSED,
                        new Empty(TRUTH_VALUE),
                        cur.GetArgAsExpression(1),
                        new Expression(new Parameter(TIME, assertionTime)));
                cur = cur.GetArgAsExpression(0);

                if (evidential == null) {
                    evidential = wrapper;
                } else {
                    evidential = new Expression(GEACH_T_TRUTH_FUNCTION, evidential, wrapper);
                }
            } else if (parity && cur.Head.Equals(SEE.Head)) {
                var wrapper = new Expression(SEE,
                    new Empty(TRUTH_VALUE),
                    cur.GetArgAsExpression(1));
                cur = cur.GetArgAsExpression(0);

                if (evidential == null) {
                    evidential = wrapper;
                } else {
                    evidential = new Expression(GEACH_T_TRUTH_FUNCTION, evidential, wrapper);
                }
            } else {
                break;
            }
        }

        // if there's no evidential given,
        // then the default is know(S, self)
        if (evidential == null) {
            evidential = new Expression(KNOW_TENSED,
                new Empty(TRUTH_VALUE),
                SELF,
                new Expression(new Parameter(TIME, assertionTime)));
        }

        Expression evidentializedExpression = new Expression(EVIDENTIALIZER,
            cur,
            new Expression(new Parameter(TIME, assertionTime)),
            (parity ? TRULY : NOT),
            evidential);

        // For testing purposes
        Debug.Log(evidentializedExpression);
        // end

        KnowledgeBase.Add(evidentializedExpression);

        yield break;
    }

    // a direct assertion.
    // @TODO add an inference rule to cover knowledge from
    // assertion. Now is a simple fix.
    public IEnumerator ReceiveAssertion(Expression content, Expression speaker) {
        StartCoroutine(AddToKnowledgeBase(new Expression(KNOW, content, speaker)));
        // TODO check to see if this is inconsistent
        // with the current knowledge base
        yield break;
    }

    public IEnumerator ReceiveRequest(Expression content, Expression speaker) {
        // the proposition we add here, we want to be the equivalent to
        // knowledge in certain ways. So, for example, knows(p, S) -> p
        // in the same way that X(p, S) -> good(p).
        // 
        // Right now, we literally have this as S knows that p is good,
        // but this feels somehow not aesthetically pleasing to me. I'll
        // try it out for now.
        StartCoroutine(AddToKnowledgeBase(new Expression(KNOW, new Expression(GOOD, content), speaker)));
        yield break;
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

