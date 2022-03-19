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

    public class KnowledgeState {
        public SortedSet<Expression> Basis;
        public Dictionary<Expression, HashSet<Expression>> Links;

        public KnowledgeState(SortedSet<Expression> basis, Dictionary<Expression, HashSet<Expression>> links, bool copy = true) {
            if (copy) {
                Basis = new SortedSet<Expression>(basis);
                Links = new Dictionary<Expression, HashSet<Expression>>();
                foreach (var keyAndValue in Links) {
                    Links.Add(keyAndValue.Key, new HashSet<Expression>(keyAndValue.Value));
                }
            } else {
                Basis = basis;
                Links = links;
            }
        }
    }

    private KnowledgeState KS;

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
        Timestamp = 0;
        Locations = new Dictionary<Expression, Vector3>();

        if (KS != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        KS = new KnowledgeState(new SortedSet<Expression>(), new Dictionary<Expression, HashSet<Expression>>(), false);

        for (int i = 0; i < initialKnowledge.Length; i++) {
            AddToKnowledgeState(KS, initialKnowledge[i]);
        }

    }

    public void ClearPresentPercepts() {
        var iAmSeeing = new Expression(SEE, new Empty(TRUTH_VALUE), SELF);
        var bot = new Expression(iAmSeeing, new Expression(new Bottom(TRUTH_VALUE)));
        var top = new Expression(iAmSeeing, new Expression(new Top(TRUTH_VALUE)));
        
        var range = KS.Basis.GetViewBetween(bot, top);

        foreach (var percept in range) {
            RemoveFromKnowledgeState(KS, percept);
            AddToKnowledgeState(KS, new Expression(PAST, percept));
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


    // this method reduces an expression to
    // an equivalent, but more compact or canonical form.
    // 
    // sentences inserted into the mental state should
    // take their reduced form.
    public Expression Reduce(Expression e, bool parity = true) {
        // we reduce identical names to the least
        // (this approach assumes all identities
        // are directly provable, which isn't a
        // safe assumption)
        if (e.Type.Equals(INDIVIDUAL)) {
            var idLowerBound = new Expression(IDENTITY, e, new Expression(new Bottom(INDIVIDUAL)));
            var idUpperBound = new Expression(IDENTITY, e, new Expression(new Top(INDIVIDUAL)));
            // we assume identity stores the
            // lesser argument to the left
            var lesserIdentities = KS.Basis.GetViewBetween(idLowerBound, idUpperBound);

            Expression leastIdentical = e;
            foreach (var lesserIdentity in lesserIdentities) {
                var lesserIdentical = lesserIdentity.GetArgAsExpression(1);
                var fullyreducedLesserIdentical = Reduce(lesserIdentical, parity);
                if (fullyreducedLesserIdentical < leastIdentical) {
                    leastIdentical = fullyreducedLesserIdentical;
                }
            }
            return leastIdentical;
        } else if (e.Type.Equals(TRUTH_VALUE)) {
            if (e.HeadedBy(TRULY)) {
                return Reduce(e.GetArgAsExpression(0), parity);
            }

            if (e.HeadedBy(NOT)) {
                return Reduce(e.GetArgAsExpression(0), !parity);
            }

            // TODO: reduce logical expressions
            // to a canonical form
            // 
            // will also involve arranging the sentences
            // and reassociating them
            // 
            // Ideas: use Quine-McCluskey algorithm to
            // get reduced form.
            // 
            // Also can represent a truth-table as
            // a pair: [A, B, C], 11011000
            // of relevant sentences and the
            // truth function they're inputs of
            if (e.HeadedBy(AND)) {

            }

            if (e.HeadedBy(OR)) {

            }

            var reducedArgs = new Argument[e.NumArgs];
            for (int i = 0; i < e.NumArgs; i++) {
                if (e.GetArg(i) is Empty) {
                    reducedArgs[i] = e.GetArg(i);
                } else {
                    reducedArgs[i] = Reduce(e.GetArgAsExpression(i), true);
                }
            }
            var reducedArgsExpression = new Expression(new Expression(e.Head), reducedArgs);
            return parity ? reducedArgsExpression : new Expression(NOT, e);
        } else {
            return e;
        }
    }

    public static List<int> ConvertToValue(Expression e) {
        List<int> value = new List<int>{0};
        var cur = e;

        // @Note we assume the numeric e is coming in
        // canonical form, where the modifiers are ordered
        // from least to greatest.
        
        while (cur.HeadedBy(VERY)) {
            value[0]++;
            cur = cur.GetArgAsExpression(0);
        }

        while (cur.HeadedBy(OMEGA)) {
            int place = 0;
            Expression modifier = cur;
            while (modifier.HeadedBy(OMEGA)) {
                place++;
                if (value.Count <= place) {
                    value.Add(0);
                }
                modifier = modifier.GetArgAsExpression(0);
            }
            value[place]++;
            cur = cur.GetArgAsExpression(1);
        }

        if (!cur.HeadedBy(GOOD)) {
            return null;
        }

        // we should increment the value by 1
        // if there are no omega (limit) modifiers
        if (value.Count == 1) {
            value[0]++;
        }

        return value;
    }

    public static List<int> MaxValue(List<int> a, List<int> b) {
        if (a == null) {
            return b;
        }
        if (b == null) {
            return a;
        }

        if (a.Count > b.Count) {
            return a;
        }
        if (b.Count > a.Count) {
            return b;
        }

        for (int i = a.Count - 1; i >= 0; i--) {
            if (a[i] > b[i]) {
                return a;
            }
            if (b[i] > a[i]) {
                return b;
            }
        }

        return a;
    }

    // this should just be temporary,
    // as the tensed query seems untenable.
    public enum Tense {
        Present,
        Past,
        Future
    }

    private class ProofNode {
        #region Parameters
        public readonly Expression Lemma;
        public readonly KnowledgeState KnowledgeState;
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

        public ProofNode(Expression lemma,
            KnowledgeState knowledgeState,
            uint depth, ProofNode parent,
            int meetBasisIndex, bool parity,
            ProofNode olderSibling = null,
            Expression supplement = null,
            bool hasYoungerSibling = false,
            bool isAssumption = false,
            Tense tense = Tense.Present) {
            Lemma = lemma;
            KnowledgeState = knowledgeState;
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

            bases.Clear();

            reachedDepth = 0;

            // we set up our stack for DFS
            // with the intended
            var root = new ProofNode(conclusion, KS, 0, null, 0, true);
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
                
                var sends = new List<KeyValuePair<ProofBases, bool>>();
 
                for (int i = 0; i < current.YoungerSiblingBases.Count; i++) {
                    if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                        yield return null;
                    }

                    var youngerSiblingBasis = current.YoungerSiblingBases[i];

                    var currentLemma = current.Lemma.Substitute(youngerSiblingBasis.Substitution);

                    // Debug.Log(currentLemma);

                    // the bases we get from directly
                    // querying the knowledge base.
                    var searchBases = new ProofBases();
                    
                    var variables = currentLemma.GetVariables();

                    //
                    // if there are variables, then get a view of the
                    // expression in question and check each.
                    // 
                    // TODO change CompareTo() re: top/bottom so that
                    // expressions which would unify with F(x) are
                    // included within the bounds of bot(bot) and top(top)
                    // This will involve check partial type application
                    //
                    // BUT leave this until there's a geniune use case
                    // in inference, since the way it occurs now is
                    // potentially more efficient.
                    // 
                    if (variables.Count > 0) {
                        Expression bottom = null;
                        Expression top = null;

                        var bottomSubstitution = new Substitution();
                        var topSubstitution = new Substitution();
                        foreach (Variable v in variables) {
                            bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
                            topSubstitution.Add(v, new Expression(new Top(v.Type)));
                        }

                        bottom = currentLemma.Substitute(bottomSubstitution);
                        top    = currentLemma.Substitute(topSubstitution);

                        var range = current.KnowledgeState.Basis.GetViewBetween(bottom, top);

                        foreach (var e in range) {
                            bool sampleParity = !e.HeadedBy(NOT);

                            var matches = currentLemma.GetMatches(e);
                            // we have a match
                            if (sampleParity == current.Parity) {
                                foreach (var match in matches) {
                                    searchBases.Add(new ProofBasis(new List<Expression>{e}, match));
                                }
                            }
                        }
                    // if there are no variables
                    // in the current expression, then simply
                    // see if the knowledge base contains the expression.
                    } else if (current.KnowledgeState.Basis.Contains(currentLemma)) {
                        searchBases.Add(new ProofBasis(new List<Expression>{currentLemma}, new Substitution()));
                    // these are some base cases that we run programatically.
                    } else {
                        // M |- verum
                        if (currentLemma.Equals(VERUM) && current.Parity) {
                            var basis = new ProofBasis();
                            searchBases.Add(basis);
                        }

                        // M |- ~falsum
                        if (currentLemma.Equals(FALSUM) && !current.Parity) {
                            var basis = new ProofBasis();
                            searchBases.Add(basis);
                        }

                        // I can say anything.
                        if (current.Parity && currentLemma.HeadedBy(ABLE) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(INFORM) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(2).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // I can go anywhere.
                        if (current.Parity && currentLemma.HeadedBy(ABLE) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(AT) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(0).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // if a and b are within 5 meters
                        // of each other, then M |- at(a, b).
                        if (current.Parity && currentLemma.HeadedBy(AT)) {
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

                    // we only check against inference rules if
                    // our search bound hasn't been reached.
                    if (current.Depth < maxDepth) {
                        uint nextDepth = current.Depth + 1;
                        exhaustive = true;
                        // inferences here
                        
                        var newStack = new Stack<ProofNode>();

                        // truly +
                        if (currentLemma.HeadedBy(TRULY)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(
                                subclause, current.KnowledgeState,
                                nextDepth,
                                current,
                                i,
                                current.Parity));
                            exhaustive = false;
                        }

                        // negation: toggle parity
                        if (currentLemma.HeadedBy(NOT)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            // nonidentity assumption
                            if (subclause.HeadedBy(IDENTITY)) {
                                newStack.Push(new ProofNode(subclause, current.KnowledgeState,
                                    nextDepth, current, i, current.Parity,
                                    isAssumption: true));
                                exhaustive = false;
                            } else {
                                newStack.Push(new ProofNode(
                                    subclause, current.KnowledgeState, nextDepth, current, i, !current.Parity));
                                exhaustive = false;
                            }
                        }

                        // contraposed very +
                        if (currentLemma.HeadedBy(VERY) && !current.Parity) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, current.KnowledgeState, nextDepth, current, i, current.Parity));
                            exhaustive = false;
                        }

                        // M |- A => M |- past(A)
                        if (currentLemma.HeadedBy(PAST) && current.Parity) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, current.KnowledgeState, nextDepth, current, i, current.Parity));
                            exhaustive = false;
                        }

                        // or +, ~and +
                        if (currentLemma.HeadedBy(OR)  &&  current.Parity ||
                            currentLemma.HeadedBy(AND) && !current.Parity) {
                            var a = currentLemma.GetArgAsExpression(0);
                            var b = currentLemma.GetArgAsExpression(1);
                            newStack.Push(new ProofNode(a,  current.KnowledgeState,nextDepth, current, i, current.Parity));
                            newStack.Push(new ProofNode(b,  current.KnowledgeState,nextDepth, current, i, current.Parity));
                            exhaustive = false;
                        }

                        // and +, ~or +
                        if (currentLemma.HeadedBy(AND) &&  current.Parity ||
                            currentLemma.HeadedBy(OR)  && !current.Parity) {
                            var a = currentLemma.GetArgAsExpression(0);
                            var b = currentLemma.GetArgAsExpression(1);

                            var bNode = new ProofNode(b,  current.KnowledgeState,nextDepth, current, i, current.Parity,
                                hasYoungerSibling: true);
                            var aNode = new ProofNode(a,  current.KnowledgeState,nextDepth, current, i, current.Parity, bNode);

                            newStack.Push(aNode);
                            newStack.Push(bNode);
                            exhaustive = false;
                        }

                        // some +
                        if (currentLemma.HeadedBy(SOME) && current.Parity) {
                            var f = currentLemma.GetArgAsExpression(0);
                            var g = currentLemma.GetArgAsExpression(1);

                            var x = new Expression(GetUnusedVariable(INDIVIDUAL, currentLemma.GetVariables()));

                            var fx = new Expression(f, x);
                            var gx = new Expression(g, x);

                            var gxNode = new ProofNode(gx, current.KnowledgeState, nextDepth, current, i, current.Parity,
                                hasYoungerSibling: true);
                            var fxNode = new ProofNode(fx, current.KnowledgeState, nextDepth, current, i, current.Parity, gxNode);

                            newStack.Push(fxNode);
                            newStack.Push(gxNode);
                            exhaustive = false;
                        }

                        // conditional proof
                        // 
                        // NOTE: this is getting the right result but
                        // the basis returned has the antecedent of the
                        // conditional, not the conditional itself.
                        // 
                        // M, A |- B => M |- A -> B
                        if (currentLemma.HeadedBy(IF) && current.Parity) {
                            // @Note this is not a typo ---
                            // antecedent is the second argument of the conditional
                            var consequent = currentLemma.GetArgAsExpression(0);
                            var antecedent = currentLemma.GetArgAsExpression(1);

                            var newKnowledgeState = new KnowledgeState(current.KnowledgeState.Basis, current.KnowledgeState.Links);
                            AddToKnowledgeState(newKnowledgeState, antecedent);

                            var consequentNode = new ProofNode(consequent, newKnowledgeState, nextDepth, current, i, current.Parity);

                            newStack.Push(consequentNode);
                            exhaustive = false;
                        }

                        // PREMISE-EXPANSIVE RULES

                        // here, we check against rules that
                        // would otherwise be premise-expansive.
                        // 
                        // TODO integrate with variables.
                        // (How?)
                        HashSet<Expression> backwardLinks = null;

                        if (current.Parity) {
                            if (current.KnowledgeState.Links.ContainsKey(currentLemma)) {
                                backwardLinks = current.KnowledgeState.Links[currentLemma];
                            }
                        } else if (current.KnowledgeState.Links.ContainsKey(new Expression(NOT, currentLemma))) {
                            backwardLinks = current.KnowledgeState.Links[new Expression(NOT, currentLemma)];
                        }

                        if (backwardLinks != null) {

                            foreach (var backwardLink in backwardLinks) {
                                if (backwardLink.Equals(currentLemma)) {
                                    continue;
                                }

                                // M |- factive(P) => M |- P
                                // factive - (1)
                                if (backwardLink.HeadedBy(KNOW, SEE, MAKE, VERY, AND, SINCE)) {
                                    var factiveNode = new ProofNode(backwardLink, current.KnowledgeState, nextDepth, current, i, true);
                                    newStack.Push(factiveNode);
                                    exhaustive = false;
                                }
                                // Modus Ponens
                                // M |- B if A, M |- A => M |- B
                                if (backwardLink.HeadedBy(IF)) {
                                    var antecedent = backwardLink.GetArgAsExpression(1);
                                    var antecedentNode = new ProofNode(antecedent, current.KnowledgeState, nextDepth, current, i, true,
                                        hasYoungerSibling: true);
                                    var conditionalNode = new ProofNode(backwardLink, current.KnowledgeState, nextDepth, current, i, true,
                                        antecedentNode);

                                    newStack.Push(conditionalNode);
                                    newStack.Push(antecedentNode);
                                    exhaustive = false;
                                }

                                // M |- able(P, x), M::will(P) => M |- P
                                if (backwardLink.HeadedBy(ABLE) &&
                                    backwardLink.GetArgAsExpression(1).Equals(SELF) &&
                                    pt == ProofType.Plan) {
                                    var will = new Expression(WILL, backwardLink.GetArgAsExpression(0));
                                    var ableNode = new ProofNode(backwardLink, current.KnowledgeState, nextDepth, current, i, true,
                                        supplement: will);

                                    newStack.Push(ableNode);
                                    exhaustive = false;
                                }
                            }                            
                        }

                        //
                        // assume, for now, that the expressions
                        // are ordered in terms of
                        // omegas applied first, verys applied last
                        // 
                        // I'll have to do a reduction so that
                        // the equivalent expressions are captured
                        // 
                        // i.e. omega(very(P)) -||- omega(P)
                        // 
                        if (current.Parity) {
                            // here, we want omega(P) to entail
                            // very(...very(P)) for any number of very's
                            var verylessContent = currentLemma;
                            while (verylessContent.HeadedBy(VERY)) {
                                verylessContent = verylessContent.GetArgAsExpression(0);
                            }

                            if (current.KnowledgeState.Links.ContainsKey(verylessContent)) {
                                foreach (var backwardLink in current.KnowledgeState.Links[verylessContent]) {
                                    if (backwardLink.HeadedBy(OMEGA)) {
                                        var omegaNode = new ProofNode(backwardLink, current.KnowledgeState, nextDepth, current, i, true);
                                        newStack.Push(omegaNode);
                                        exhaustive = false;
                                    }
                                }
                            }

                            var omegalessContent = verylessContent;
                            var power = new Expression(OMEGA, VERY);
                            while (omegalessContent.HeadedBy(OMEGA)) {
                                var powerMinusOne = power.GetArgAsExpression(0);
                                while (omegalessContent.HeadedBy(OMEGA) &&
                                       omegalessContent.GetArgAsExpression(0).Equals(powerMinusOne)) {
                                    omegalessContent = omegalessContent.GetArgAsExpression(1);
                                }

                                var powerPlusOne = new Expression(OMEGA, power);

                                if (current.KnowledgeState.Links.ContainsKey(omegalessContent)) {
                                    if (current.KnowledgeState.Links.ContainsKey(omegalessContent)) {
                                        foreach (var backwardLink in current.KnowledgeState.Links[omegalessContent]) {
                                            var powerCounter = power;
                                            var linkPowerCounter = backwardLink;
                                            bool linkSupercedes = false;
                                            while (linkPowerCounter.HeadedBy(OMEGA)) {
                                                if (!powerCounter.HeadedBy(OMEGA)) {
                                                    linkSupercedes = true;
                                                    break;
                                                }
                                                powerCounter = powerCounter.GetArgAsExpression(0);
                                                linkPowerCounter = linkPowerCounter.GetArgAsExpression(0);
                                            }
                                            // TODO 7/19
                                            if (linkSupercedes) {
                                                var powerPlusOneNode = new ProofNode(backwardLink, current.KnowledgeState, nextDepth, current, i, true);
                                                newStack.Push(powerPlusOneNode);
                                                exhaustive = false;
                                            }
                                        }
                                    }
                                }

                                power = powerPlusOne;
                            }
                        }

                        // END PREMISE-EXPANSIVE RULES

                        // M |- P => M |- know(P, self)
                        // M |/- P => not(know(P, self))
                        if (currentLemma.HeadedBy(KNOW) && currentLemma.GetArgAsExpression(1).Equals(SELF)) {
                            newStack.Push(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i, true,
                                isAssumption: !current.Parity));
                            exhaustive = false;
                        }

                        // all -
                        // M |- all(F, G), M |- F(x) => G(x)
                        var currentVariables = currentLemma.GetVariables();
                        var x1 = GetUnusedVariable(INDIVIDUAL, currentVariables);
                        var f1 = GetUnusedVariable(PREDICATE, currentVariables);

                        var augmentedVariables = new HashSet<Variable>{f1};
                        augmentedVariables.UnionWith(currentVariables);

                        var f2 = GetUnusedVariable(PREDICATE, augmentedVariables);

                        var f2xFormula = new Expression(new Expression(f2), new Expression(x1));
                        var f2xMatches = f2xFormula.GetMatches(currentLemma);

                        foreach (var f2xBinding in f2xMatches) {
                            var allF1F2 = new Expression(ALL, new Expression(f1), f2xBinding[f2]);
                            
                            var f1xFormula = new Expression(new Expression(f1), f2xBinding[x1]);
                            
                            var f1xNode = new ProofNode(f1xFormula, current.KnowledgeState,
                                    nextDepth, current, i, current.Parity,
                                    hasYoungerSibling: true);

                            var allNode = new ProofNode(allF1F2, current.KnowledgeState,
                                nextDepth, current, i, current.Parity, f1xNode);

                            // newStack.Push(allNode);
                            // newStack.Push(f1xNode);
                            exhaustive = false;
                        }

                        // geach - : (t -> t), (e -> t), e -> t
                        // M |- geach(T, F, x) => M |- T(F(x))
                        var tf1 = GetUnusedVariable(TRUTH_FUNCTION, currentVariables);
                        var tfxFormula = new Expression(
                            new Expression(tf1),
                                new Expression(new Expression(f1), new Expression(x1)));

                        var tfxMatches = tfxFormula.GetMatches(currentLemma);

                        foreach (var tfxBinding in tfxMatches) {
                            var geachedTfx =
                                new Expression(GEACH_E_TRUTH_FUNCTION,
                                    tfxBinding[tf1],
                                    tfxBinding[f1],
                                    tfxBinding[x1]);

                            // newStack.Push(new ProofNode(
                            //   geachedTfx, current.KnowledgeState, nextDepth, current, i, current.Parity));

                            exhaustive = false;
                        }

                        // geach - : (t -> t), (t -> t), t -> t
                        // M |- geach(T1, T2, S) => M |- T1(T2(S))
                        var geachTTFAugmentedVariables = new HashSet<Variable>{tf1};
                        geachTTFAugmentedVariables.UnionWith(currentVariables);
                        var tf2 = GetUnusedVariable(TRUTH_FUNCTION, geachTTFAugmentedVariables);
                        var t1 = GetUnusedVariable(TRUTH_VALUE, currentVariables);
                        var tf1tf2tFormula = new Expression(
                            new Expression(tf1),
                                new Expression(
                                    new Expression(tf2),
                                    new Expression(t1)));

                        var tf1tf2tMatches = tf1tf2tFormula.GetMatches(currentLemma);

                        foreach (var tf1tf2tBinding in tf1tf2tMatches) {                           
                            var geachedTf1tf2t =
                                new Expression(GEACH_T_TRUTH_FUNCTION,
                                    tf1tf2tBinding[tf1],
                                    tf1tf2tBinding[tf2],
                                    tf1tf2tBinding[t1]);

                            // newStack.Push(new ProofNode(
                            //     geachedTf1tf2t, current.KnowledgeState,
                            //     nextDepth, current, i, current.Parity));

                            exhaustive = false;
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

                        ProofBases productBases = new ProofBases();

                        if (merge.IsAssumption) {
                            // no refutation
                            if (sendBases.IsEmpty() &&
                                merge.ChildBases.IsEmpty() &&
                                meetBasis != null &&
                                (exhaustive || current.Depth == maxDepth ||
                                 mergeLemma.Depth >= this.MaxDepth ||
                                 merge.IsLastChild)) {
                                // we can safely assume the content of
                                // this assumption node
                                var assumptionBasis = new ProofBasis();
                                assumptionBasis.AddPremise(
                                    new Expression(NOT, new Expression(KNOW, mergeLemma, SELF)));

                                var productBasis = new ProofBasis(meetBasis, assumptionBasis);
                                productBases.Add(productBasis);
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

                                // we form the product of our
                                // meet basis and child bases.
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
    // (or formula with one free variable, TODO)
    // that captures its mode of presentation
    // 
    // a percept with the given characteristic is asserted.
    // 
    // Returns the new parameter used.
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

        var percept = new Expression(SEE, new Expression(characteristic, param), SELF);
        
        //
        // @Note this might not be the right behavior.
        // We may want to keep the 'was'
        // 
        // However, if 'was' is inclusive of the present,
        // then we should remove it to keep maximal specificity.
        // 
        // At least until time/events are figured out better.
        // 
        RemoveFromKnowledgeState(KS, new Expression(PAST, percept));
        
        AddToKnowledgeState(KS, percept);

        return param;
    }

    private void AddLink(KnowledgeState knowledgeState, Expression premise, Expression conclusion) {
        if (knowledgeState.Links.ContainsKey(conclusion)) {
            knowledgeState.Links[conclusion].Add(premise);
        } else {
            knowledgeState.Links.Add(conclusion, new HashSet<Expression>{premise});
        }
    }

    public bool AddToKnowledgeState(KnowledgeState knowledgeState, Expression knowledge, bool firstCall = true) {
        Debug.Assert(knowledge.Type.Equals(TRUTH_VALUE));

        if (knowledgeState.Basis.Contains(knowledge)) {
            return false;
        }

        if (knowledge.Depth > MaxDepth) {
            MaxDepth = knowledge.Depth;
        }

        if (knowledge.HeadedBy(SEE, INFORM)) {
            var p = knowledge.GetArgAsExpression(0);
            var pSinceSawP = new Expression(SINCE, p, knowledge);

            // @Note: since(A, B) as a logical operator is typically
            // made to be a conditional on was(B) to conclude A.
            // 
            // We're making it factive. It's ampliatively assumed
            // we add a factive event into the knowledge base.
            // 
            // M <- see(P, x)
            // M <- since(P, see(P, x)) which entails both
            // P and was(see(P, x))
            // 
            if (firstCall) {
                return AddToKnowledgeState(knowledgeState, pSinceSawP, true);
            } else {
                return false;
            }
        } else {
            // @Note we add each derived sentence to
            // the base now, but remove this if we
            // figure out to query the links by formula
            knowledgeState.Basis.Add(knowledge);
        }

        if (firstCall) {
            // we want to ensure a self-supporting premise isn't
            // removed if other links to it are removed.
            AddLink(knowledgeState, knowledge, knowledge);
        }

        if (knowledge.HeadedBy(VERY, KNOW, MAKE)) {
            var subclause = knowledge.GetArgAsExpression(0);
            AddToKnowledgeState(knowledgeState, subclause, false);
            AddLink(knowledgeState, knowledge, subclause);
        }

        if (knowledge.HeadedBy(OMEGA)) {
            var subclause = knowledge.GetArgAsExpression(1);
            AddToKnowledgeState(knowledgeState, subclause, false);
            AddLink(knowledgeState, knowledge, subclause);
        }
        
        if (knowledge.HeadedBy(AND)) {
            var a = knowledge.GetArgAsExpression(0);
            var b = knowledge.GetArgAsExpression(1);
            
            AddToKnowledgeState(knowledgeState, a, false);
            AddToKnowledgeState(knowledgeState, b, false);

            AddLink(knowledgeState, knowledge, a);
            AddLink(knowledgeState, knowledge, b);
        }

        if (knowledge.HeadedBy(NOT)) {
            var subclause = knowledge.GetArgAsExpression(0);
            if (subclause.HeadedBy(OR)) {
                var notA = new Expression(NOT, subclause.GetArgAsExpression(0));
                var notB = new Expression(NOT, subclause.GetArgAsExpression(1));

                AddToKnowledgeState(knowledgeState, notA, false);
                AddToKnowledgeState(knowledgeState, notB, false);

                AddLink(knowledgeState, knowledge, notA);
                AddLink(knowledgeState, knowledge, notB);
            }
        }

        if (knowledge.HeadedBy(IF, ABLE)) {
            var consequent = knowledge.GetArgAsExpression(0);
            AddLink(knowledgeState, knowledge, consequent);
        }

        if (knowledge.HeadedBy(SINCE)) {
            var topic = knowledge.GetArgAsExpression(0);
            var anchor = new Expression(PAST, knowledge.GetArgAsExpression(1));

            AddToKnowledgeState(knowledgeState, topic, false);
            AddToKnowledgeState(knowledgeState, anchor, false);

            AddLink(knowledgeState, knowledge, topic);
            AddLink(knowledgeState, knowledge, anchor);
        }

        return true;
    }

    // we remove the chain of backward links
    // associated with the expression 'knowledge'
    protected void RemoveLinks(KnowledgeState knowledgeState, Expression knowledge) {
        var links = knowledgeState.Links;

        // if there is an expression that contains knowledge,
        // remove knowledge as a link.
        var toRemove = new List<Expression>();
        foreach (var link in links) {
            if (link.Value.Contains(knowledge)) {
                link.Value.Remove(knowledge);
            }
            // if there are no more links to this value, remove it.
            if (link.Value.Count == 0) {
                toRemove.Add(link.Key);
            }
        }
        foreach (var remove in toRemove) {
            links.Remove(remove);
            // if this is an intermediate link, then remove
            // all of what it links as well.
            
            RemoveFromKnowledgeState(knowledgeState, remove);
            // @Note remove above and uncomment below
            // once we can query links by formula

            // if (!KS.Basis.Contains(remove)) {
            //     RemoveLinks(remove, forward);
            // }
        }
    }

    public bool RemoveFromKnowledgeState(KnowledgeState knowledgeState, Expression knowledge) {
        if (knowledgeState.Basis.Remove(knowledge)) {
            RemoveLinks(knowledgeState, knowledge);
            return true;
        }
        return false;
    }

    // a direct assertion.
    // @TODO add an inference rule to cover knowledge from
    // assertion. Now is a simple fix.
    public IEnumerator ReceiveAssertion(Expression content, Expression speaker) {
        // check if the content would make our mental state inconsistent
        var notContentBases = new ProofBases();
        var notContentDone  = new Container<bool>(false);
        StartCoroutine(StreamProofs(notContentBases, new Expression(NOT, content), notContentDone));

        while (!notContentDone.Item) {
            yield return null;
        }

        if (!notContentBases.IsEmpty()) {
            Debug.Log("Found inconsistency for " + content + "! Aborting.");
            yield break;
        }

        AddToKnowledgeState(KS, new Expression(INFORM, content, SELF, speaker));
        yield break;
    }

    public bool ReceiveRequest(Expression content, Expression speaker) {
        // the proposition we add here, we want to be the equivalent to
        // knowledge in certain ways. So, for example, knows(p, S) -> p
        // in the same way that X(p, S) -> good(p).
        // 
        // Right now, we literally have this as S knows that p is good,
        // but this feels somehow not aesthetically pleasing to me. I'll
        // try it out for now.
        return AddToKnowledgeState(KS, new Expression(KNOW, new Expression(GOOD, content), speaker));
    }

    public static Expression Conjunctify(List<Expression> set) {
        if (set.Count == 0) {
            return null;
        }

        if (set.Count == 1) {
            return set[0];
        }

        int low  = 0;
        int high = set.Count;

        int mid  = high / 2;

        var left = set.GetRange(low, mid);
        
        var right = set.GetRange(mid, high - mid);

        var leftConjunct  = Conjunctify(left);
        var rightConjunct = Conjunctify(right);

        if (leftConjunct == null) {
            return rightConjunct;
        }
        if (rightConjunct == null) {
            return leftConjunct;
        }

        return new Expression(AND, leftConjunct, rightConjunct);
    }

    public static List<int> Plus(List<int> a, List<int> b) {
        if (a == null) {
            return b;
        }
        if (b == null) {
            return a;
        }
        var sum = new List<int>();
        int maxCount = a.Count > b.Count ? a.Count : b.Count;
        for (int i = 0; i < maxCount; i++) {
            sum.Add(0);
            if (i < a.Count) {
                sum[i] += a[i];
            }
            if (i < b.Count) {
                sum[i] += b[i];
            }
        }

        // remove leading zeros
        for (int i = sum.Count - 1; i >= 0; i--) {
            if (sum[i] != 0) {
                break;
            }
            sum.RemoveAt(i);
        }
        return sum;
    }

    public IEnumerator EstimateValueFor(
        Expression goal, List<Expression> goods,
        List<Expression> estimates, Container<bool> done) {
        // first, we get all the goods we can prove from
        // our goal. These are in the running to approximate
        // the value of the goal.
        List<Expression> goodsFromGoal = new List<Expression>();
        foreach (var good in goods) {
            var goodFromGoalBases = new ProofBases();
            var goodFromGoalDone  = new Container<bool>(false);

            StartCoroutine(StreamProofs(goodFromGoalBases, new Expression(IF, goal, good), goodFromGoalDone));

            while (!goodFromGoalDone.Item) {
                yield return null;
            }

            if (!goodFromGoalBases.IsEmpty()) {
                goodsFromGoal.Add(good);
            }
        }

        // now that we have the goods that are in the running,
        // we compare them to each other. If A and B are goods
        // and A |- B, then we throw away A. If neither entail
        // the other, we keep both.
        List<Expression> oldInfimums = new List<Expression>();
        List<Expression> newInfimums = new List<Expression>();
        foreach (var good in goodsFromGoal) {
            bool isInfimumSoFar = true;
            foreach (var oldInfimum in oldInfimums) {
                if (!isInfimumSoFar) {
                    newInfimums.Add(oldInfimum);
                    continue;
                }
                // @note here, we assume that they are not equivalent.
                // that should be handled at the insertion of a
                // GOOD sentence since equivalent propositions
                // can't be assigned different values.

                // first, we check if our new good
                // beats any of the old prospective infimums.
                // If it does, then we remove the old infimum
                // from the set of infimums.
                var infFromGoodBases = new ProofBases();
                var infFromGoodDone  = new Container<bool>(false);

                StartCoroutine(StreamProofs(infFromGoodBases, new Expression(IF, good, oldInfimum), infFromGoodDone));

                while (!infFromGoodDone.Item) {
                    yield return null;
                }

                if (!infFromGoodBases.IsEmpty()) {
                    continue;
                }

                // we add the old infimum back in, since it was
                // not supplanted this round.
                newInfimums.Add(oldInfimum);

                // next we see if the old beats the good at issue.
                // if it does, then we stop checking, as this
                // good will not be used in our estimation.
                var goodFromInfBases = new ProofBases();
                var goodFromInfDone  = new Container<bool>(false);

                StartCoroutine(StreamProofs(goodFromInfBases, new Expression(IF, oldInfimum, good), goodFromInfDone));

                while (!goodFromInfDone.Item) {
                    yield return null;
                }

                if (!goodFromInfBases.IsEmpty()) {
                    isInfimumSoFar = false;
                }
            }

            if (isInfimumSoFar) {
                newInfimums.Add(good);
            }

            oldInfimums = newInfimums;
            newInfimums = new List<Expression>();
        }

        estimates.AddRange(oldInfimums);
        done.Item = true;
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

        var evaluativeBase = new Dictionary<Expression, List<int>>();
        var goods = new List<Expression>();
        foreach (var goodProof in goodProofs) {
            var assignment = goodProof.Substitution[ST.Head as Variable];
            goods.Add(assignment);
            if (evaluativeBase.ContainsKey(assignment)) {
                evaluativeBase[assignment] = MaxValue(goodProof.MaxValue, evaluativeBase[assignment]);
            } else {
                evaluativeBase.Add(assignment, goodProof.MaxValue);
            }
        }

        List<int> bestTotalValue = new List<int>{0};
        List<Expression> bestPlan = new List<Expression>{new Expression(WILL, NEUTRAL)};

        foreach (var goodAndValueOfGood in evaluativeBase) {
            var good = goodAndValueOfGood.Key;
            var valueOfGood = goodAndValueOfGood.Value;

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

            // we have a feasible plan. So, we take
            // the joint value of making all the
            // resolutions come true.
            if (!planBases.IsEmpty()) {
                var bestValueForThisGood = new List<int>();
                var bestPlanForThisGood = new List<Expression>();
                foreach (var basis in planBases) {
                    var benefitConjunction = new List<Expression>();
                    var resolutions = new List<Expression>();
                    
                    foreach (var premise in basis.Premises) {
                        if (premise.Type.Equals(CONFORMITY_VALUE)) {
                            resolutions.Add(premise);

                            var collateral = premise.GetArgAsExpression(0);
                            var makeBenefit = new Expression(MAKE, collateral, SELF);

                            benefitConjunction.Add(makeBenefit);
                        }
                    }

                    var estimates = new List<Expression>();
                    var estimationDone = new Container<bool>(false);

                    StartCoroutine(EstimateValueFor(Conjunctify(benefitConjunction), goods, estimates, estimationDone));

                    while (!estimationDone.Item) {
                        yield return null;
                    }

                    var valueForThisPlan = new List<int>();

                    foreach (var estimate in estimates) {
                        valueForThisPlan = Plus(valueForThisPlan, evaluativeBase[estimate]);
                    }

                    bestValueForThisGood = MaxValue(bestValueForThisGood, valueForThisPlan);
                    if (bestValueForThisGood == valueForThisPlan) {
                        bestPlanForThisGood = resolutions;
                    }
                }

                bestTotalValue = MaxValue(bestTotalValue, bestValueForThisGood);
                if (bestTotalValue == bestValueForThisGood) {
                    bestPlan = bestPlanForThisGood;
                }
            }
        }
        plan.AddRange(bestPlan);
        done.Item = true;
        yield break;
    }
}
