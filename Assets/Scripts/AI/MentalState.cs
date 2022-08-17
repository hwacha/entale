using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    public class KnowledgeState {
        public SortedSet<Expression> Basis;
        public SortedList<Expression, List<InferenceRule>> Rules;
        public SortedSet<Expression> OmegaPool;

        public KnowledgeState(SortedSet<Expression> basis, SortedList<Expression, List<InferenceRule>> rules, SortedSet<Expression> omegaPool, bool copy = true) {
            if (copy) {
                Basis = new SortedSet<Expression>(basis);
                Rules = new SortedList<Expression, List<InferenceRule>>();
                foreach (var keyAndRules in rules) {
                    Rules.Add(keyAndRules.Key, new List<InferenceRule>());
                    foreach (var rule in keyAndRules.Value) {
                        Rules[keyAndRules.Key].Add(rule);
                    }
                }
                OmegaPool = new SortedSet<Expression>(omegaPool);
            } else {
                Basis = basis;
                Rules = rules;
                OmegaPool = omegaPool;
            }
        }

        public override string ToString() {
            var str = new StringBuilder();
            str.Append("Basis [\n");
            foreach (Expression e in Basis) {
                str.Append("\t" + e + "\n");
            }
            str.Append("]\nRules [\n");
            foreach (var keyAndRules in Rules) {
                str.Append("\t" + keyAndRules.Key + " -> {\n");
                foreach (var rule in keyAndRules.Value) {
                    str.Append("\t\t" + rule + "\n");
                }
                str.Append("\t}\n");
            }
            str.Append("]\nOmega Pool [\n");
            foreach (Expression e in OmegaPool) {
                str.Append("\t" + e + "\n");
            }
            str.Append("]");
            return str.ToString();
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
        Locations[SELF] = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
    }

    // @Note this doesn't check to see if
    // the initial belief set is inconsistent.
    // Assume, for now, as an invariant, that it is.
    public void Initialize(Expression[] initialKnowledge) {
        ParameterID = 0;
        Locations = new Dictionary<Expression, Vector3>();

        if (KS != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        KS = new KnowledgeState(new SortedSet<Expression>(), new SortedList<Expression, List<InferenceRule>>(), new SortedSet<Expression>(), false);

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
            if (a[a.Count - 1] >= 0) {
                return a;    
            } else {
                return b;
            }
            
        }
        if (b.Count > a.Count) {
            if (b[b.Count - 1] >= 0) {
                return b;
            } else {
                return a;
            }
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

    private class ProofNode {
        #region Parameters
        public readonly Expression Lemma;
        public readonly KnowledgeState KnowledgeState;
        public readonly Substitution Substitution;
        public readonly uint Depth;
        public readonly ProofNode Parent;
        public readonly int MeetBasisIndex;
        public readonly ProofNode OlderSibling;
        public readonly Expression Supplement;
        public readonly bool IsAssumption;
        public readonly bool Parity;
        public readonly Expression Omega;
        #endregion

        #region Variables
        public List<ProofBasis> YoungerSiblingBases;
        public ProofBases ChildBases;
        public bool IsLastChild;
        #endregion

        public ProofNode(Expression lemma,
            KnowledgeState knowledgeState,
            uint depth, ProofNode parent,
            int meetBasisIndex,
            Expression omega,
            ProofNode olderSibling = null,
            Expression supplement = null,
            bool hasYoungerSibling = false,
            bool isAssumption = false,
            Substitution substitution = null) {
            Lemma = lemma;
            KnowledgeState = knowledgeState;
            Substitution = substitution;
            Depth = depth;
            Parent = parent;
            MeetBasisIndex = meetBasisIndex;
            OlderSibling = olderSibling;
            IsAssumption = isAssumption;
            Omega = omega;

            YoungerSiblingBases = new List<ProofBasis>();
            if (!hasYoungerSibling) {
                YoungerSiblingBases.Add(new ProofBasis());
            }
            ChildBases = new ProofBases();
            IsLastChild = false;
        }

        public override string ToString() {
            return "Proof Node {" +
                "\n\tlemma: " + Lemma +
                "\n\tparent: " + (Parent == null ? "ROOT" : Parent.Lemma.ToString()) +
                "\n\tolderSibling: " + (OlderSibling == null ? "SINGLETON" : OlderSibling.Lemma.ToString()) +
                "\n\tdepth: " + Depth +
                "\n\tis Assumption: " + IsAssumption +
                "\n\tOmega: " + Omega +
                "\n}";
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

            // Debug.Log("================================================");
            // Debug.Log("proving " + conclusion + " at depth=" + maxDepth);

            bases.Clear();

            reachedDepth = 0;

            // we set up our stack for DFS
            // with the intended
            var root = new ProofNode(conclusion, KS, 0, null, 0, null);
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

                // Debug.Log("searching " + current);
 
                for (int i = 0; i < current.YoungerSiblingBases.Count; i++) {
                    if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                        yield return null;
                    }

                    var youngerSiblingBasis = current.YoungerSiblingBases[i];

                    var currentLemma = current.Lemma.Substitute(youngerSiblingBasis.Substitution);
                    
                    // Debug.Log("current lemma is " + currentLemma);
                    // Debug.Log("the current knowledge state is " + current.KnowledgeState);

                    // the bases we get from directly
                    // querying the knowledge base.
                    var searchBases = new ProofBases();
                    
                    var variables = currentLemma.GetVariables();

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
                    if (current.Omega == null) {
                        if (variables.Count > 0) {
                            var range = current.KnowledgeState.Basis.GetViewBetween(bottom, top);

                            foreach (var e in range) {
                                var matches = currentLemma.GetMatches(e);
                                // we have a match
                                foreach (var match in matches) {
                                    searchBases.Add(new ProofBasis(new List<Expression>{e}, match));
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
                            if (currentLemma.Equals(VERUM)) {
                                searchBases.Add(new ProofBasis());
                            }

                            // M |- ~falsum
                            if (currentLemma.Equals(new Expression(NOT, FALSUM))) {
                                searchBases.Add(new ProofBasis());
                            }

                            // M |- x = x
                            if (currentLemma.HeadedBy(IDENTITY) &&
                                currentLemma.GetArgAsExpression(0).Equals(currentLemma.GetArgAsExpression(1))) {
                                searchBases.Add(new ProofBasis());
                            }

                            // de se performative resolution
                            // will(P) |- df(make, P, self)
                            if (pt == Plan && currentLemma.HeadedBy(DF) &&
                                currentLemma.GetArgAsExpression(0).HeadedBy(MAKE) &&
                                currentLemma.GetArgAsExpression(2).Equals(SELF)) {
                                var basis = new ProofBasis();
                                basis.AddPremise(new Expression(WILL, currentLemma.GetArgAsExpression(1)));
                                searchBases.Add(basis);
                            }

                            // if a and b are within 5 meters
                            // of each other, then M |- at(a, b).
                            if (currentLemma.HeadedBy(AT)) {
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

                                    if (distance < 10) {
                                        var basis = new ProofBasis();
                                        basis.AddPremise(currentLemma);
                                        searchBases.Add(basis);
                                    }
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

                        void PushNode(ProofNode pushNode) {
                            var pushLemma = pushNode.Lemma;
                            var ancestor = pushNode.Parent;
                            while (ancestor != null) {
                                if (ancestor.Lemma.Equals(pushLemma)) {
                                    return;
                                }
                                // @note/TODO for formulas we may
                                // want to check if the
                                // lemmas match one another
                                ancestor = ancestor.Parent;
                            }
                            newStack.Push(pushNode);
                            exhaustive = false;
                        }

                        // truly +
                        if (currentLemma.HeadedBy(TRULY)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            PushNode(new ProofNode(
                                subclause, current.KnowledgeState,
                                nextDepth,
                                current,
                                i,
                                current.Omega));
                        }

                        // star + 
                        // M |/- A => M |- *A
                        if (currentLemma.HeadedBy(STAR)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i, current.Omega,
                                isAssumption: true));
                        }

                        // nonidentity assumption
                        if (currentLemma.PrejacentHeadedBy(NOT, IDENTITY)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i, current.Omega,
                                isAssumption: true));
                        }

                        if (currentLemma.PrejacentHeadedBy(NOT, NOT)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0).GetArgAsExpression(0),
                                current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // contraposed very +
                        // M |- ~A => M |- ~+A
                        if (currentLemma.PrejacentHeadedBy(NOT, VERY)) {
                            PushNode(new ProofNode(
                                new Expression(NOT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)),
                                current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // defactivizer +
                        if (currentLemma.HeadedBy(DF)) {
                            var factive = new Expression(
                                currentLemma.GetArgAsExpression(0),
                                currentLemma.GetArgAsExpression(1),
                                currentLemma.GetArgAsExpression(2));
                            PushNode(new ProofNode(
                                factive, current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // M |- A => M |- past(A)
                        if (currentLemma.HeadedBy(PAST)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            PushNode(new ProofNode(
                                subclause, current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // M |- good(~A) => M |- ~good(A)
                        if (currentLemma.PrejacentHeadedBy(NOT, GOOD)) {
                            var goodNotA = new Expression(GOOD,
                                new Expression(NOT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)));
                            PushNode(new ProofNode(
                                goodNotA, current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // or +, ~and +
                        if (currentLemma.HeadedBy(OR)  ||
                            currentLemma.PrejacentHeadedBy(NOT, AND)) {
                            var adjunction = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var a = adjunction.GetArgAsExpression(0);
                            var b = adjunction.GetArgAsExpression(1);

                            if (currentLemma.HeadedBy(NOT)) {
                                a = new Expression(NOT, a);
                                b = new Expression(NOT, b);
                            }

                            PushNode(new ProofNode(
                                a, current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                            PushNode(new ProofNode(
                                b, current.KnowledgeState, nextDepth,
                                current, i, current.Omega));
                        }

                        // and +, ~or +
                        if (currentLemma.HeadedBy(AND) ||
                            currentLemma.PrejacentHeadedBy(NOT, OR)) {
                            var adjunction = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var a = adjunction.GetArgAsExpression(0);
                            var b = adjunction.GetArgAsExpression(1);

                            if (currentLemma.HeadedBy(NOT)) {
                                a = new Expression(NOT, a);
                                b = new Expression(NOT, b);
                            }

                            var bNode = new ProofNode(
                                b, current.KnowledgeState, nextDepth,
                                current, i, current.Omega,
                                hasYoungerSibling: true);
                            var aNode = new ProofNode(
                                a, current.KnowledgeState, nextDepth,
                                current, i, current.Omega, bNode);

                            PushNode(aNode);
                            PushNode(bNode);
                        }

                        // some +, ~all +
                        if (currentLemma.HeadedBy(SOME) ||
                            currentLemma.PrejacentHeadedBy(NOT, ALL)) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var f = query.GetArgAsExpression(0);
                            var g = query.GetArgAsExpression(1);

                            var x = new Expression(GetUnusedVariable(INDIVIDUAL, query.GetVariables()));

                            var fx = new Expression(f, x);
                            var gx = new Expression(g, x);

                            if (currentLemma.HeadedBy(NOT)) {
                                gx = new Expression(NOT, gx);
                            }

                            var gxNode = new ProofNode(gx, current.KnowledgeState, nextDepth, current, i, current.Omega,
                                hasYoungerSibling: true);
                            var fxNode = new ProofNode(fx, current.KnowledgeState, nextDepth, current, i, current.Omega, gxNode);

                            PushNode(fxNode);
                            PushNode(gxNode);
                        }

                        // conditional proof
                        // M, A |- B => M |- A -> B
                        // 
                        // TODO figure out logic for conditional
                        // with antecedent known to be false
                        if (currentLemma.HeadedBy(IF)) {
                            // @Note this is not a typo ---
                            // antecedent is the second argument of the conditional
                            var consequent = currentLemma.GetArgAsExpression(0);
                            var antecedent = currentLemma.GetArgAsExpression(1);

                            var newKnowledgeState = new KnowledgeState(current.KnowledgeState.Basis, current.KnowledgeState.Rules, current.KnowledgeState.OmegaPool);

                            AddToKnowledgeState(newKnowledgeState, antecedent);

                            var consequentNode = new ProofNode(consequent, newKnowledgeState, nextDepth, current, i, current.Omega);

                            PushNode(consequentNode);
                        }

                        // symmetry of identity
                        // M |- a = b => M |- b = a
                        if (currentLemma.HeadedBy(IDENTITY) ||
                            currentLemma.PrejacentHeadedBy(NOT, IDENTITY)) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var converse = new Expression(IDENTITY, query.GetArgAsExpression(1), query.GetArgAsExpression(0));
                            if (currentLemma.HeadedBy(NOT)) {
                                converse = new Expression(NOT, converse);
                            }
                            var converseNode = new ProofNode(converse, current.KnowledgeState, nextDepth, current, i, current.Omega);
                            PushNode(converseNode);
                        }

                        // M |- banana(x) => M |- fruit(x)
                        // M |- tomato(x) => M |- fruit(x)
                        if (currentLemma.HeadedBy(FRUIT)) {
                            var tomatoX = new Expression(TOMATO, currentLemma.GetArgAsExpression(0));
                            var bananaX = new Expression(BANANA, currentLemma.GetArgAsExpression(0));
                            PushNode(new ProofNode(tomatoX, current.KnowledgeState, nextDepth, current, i, current.Omega));
                            PushNode(new ProofNode(bananaX, current.KnowledgeState, nextDepth, current, i, current.Omega));
                        }
                        // contraposed
                        if (currentLemma.PrejacentHeadedBy(NOT, TOMATO, BANANA)) {
                            var fruitX = new Expression(NOT, new Expression(FRUIT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)));
                            PushNode(new ProofNode(fruitX, current.KnowledgeState, nextDepth, current, i, current.Omega));
                        }

                        // PREMISE-EXPANSIVE RULES

                        // here, we check against rules that
                        // would otherwise be premise-expansive.
                        
                        foreach (var rules in current.KnowledgeState.Rules.Values) {
                            foreach (var rule in rules) {
                                var premises = rule.Apply(currentLemma);
                                if (premises != null) {
                                    if (premises.Count == 0) {
                                        searchBases.Add(new ProofBasis());
                                    } else if (premises.Count == 1) {
                                        PushNode(new ProofNode(premises[0], current.KnowledgeState, nextDepth, current, i, current.Omega));
                                    } else {
                                        ProofNode nextPremNode = null;
                                        var nodes = new Stack<ProofNode>();
                                        for (int j = premises.Count - 1; j >= 0; j--) {
                                            var curPremNode = new ProofNode(
                                                premises[j], current.KnowledgeState, nextDepth, current, i,
                                                current.Omega, nextPremNode, hasYoungerSibling: j > 0);
                                            nodes.Push(curPremNode);
                                            nextPremNode = curPremNode;
                                        }
                                        while (nodes.Count > 0 ) {
                                            PushNode(nodes.Pop());    
                                        }
                                    }
                                }
                            }
                        }

                        // END PREMISE-EXPANSIVE RULES
                        
                        // OMEGA MADNESS
                        // var tf = GetUnusedVariable(TRUTH_FUNCTION, currentLemma.GetVariables());
                        // var t  = GetUnusedVariable(TRUTH_VALUE, currentLemma.GetVariables());
                        // var truthFunctionFormula = new Expression(new Expression(tf), new Expression(t));

                        // var matches = truthFunctionFormula.GetMatches(currentLemma);
                        // // pattern match on sentences of the form F(Q)
                        // foreach (var match in matches) {
                        //     // omega(F, Q)
                        //     var omega = new Expression(OMEGA, match[tf], match[t]);
                        //     if (current.Omega == null) {
                        //         bool ignorePerfectMatch = false;
                        //         if (current.KnowledgeState.Basis.Contains(omega)) {
                        //             searchBases.Add(new ProofBasis(new List<Expression>{omega}, new Substitution()));
                        //             ignorePerfectMatch = true;
                        //         }

                        //         foreach (var imperfectOmega in current.KnowledgeState.OmegaPool) {
                        //             if (ignorePerfectMatch && imperfectOmega.Equals(omega)) {
                        //                 continue;
                        //             }

                        //             var omegaNode = new ProofNode(match[t], current.KnowledgeState, nextDepth, current, i, imperfectOmega);
                        //             PushNode(omegaNode);
                        //         }
                        //     } else if (current.Omega.GetArgAsExpression(0).Equals(match[tf])) {
                        //         if (current.Omega.Equals(omega)) {
                        //             if (current.KnowledgeState.Basis.Contains(current.Omega)) {
                        //                 searchBases.Add(new ProofBasis(new List<Expression>{omega}, new Substitution()));
                        //             }
                        //         } else {
                        //             var omegaNode = new ProofNode(match[t], current.KnowledgeState, nextDepth, current, i, current.Omega);
                        //             PushNode(omegaNode);
                        //         }
                        //     }
                        // }
                        // END OMEGA MADNESS
                        
                        // @Note: we only check the above rule
                        // if we're proving something unconditionally.
                        // 
                        // should this be the case? shouldn't we be able
                        // to say, e.g. that if ~A, we don't know A?
                        // 
                        // de se knowledge transparency
                        // M |- P => M  |- know(P, self)
                        // M |/- P => M |- ~know(P, self)
                        if (current.KnowledgeState == KS &&
                            (currentLemma.HeadedBy(KNOW) || currentLemma.PrejacentHeadedBy(NOT, KNOW))) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;

                            if (query.GetArgAsExpression(1).Equals(SELF)) {
                                PushNode(new ProofNode(
                                    query.GetArgAsExpression(0),
                                    current.KnowledgeState,
                                    nextDepth, current, i, current.Omega,
                                    isAssumption: currentLemma.HeadedBy(NOT)));
                            }
                        }

                        // we add a specific rule for de se abilities that are always provable
                        // I can go anywhere.
                        // M |- df(make, at(self, X), self) => M |- make(at(self, X), self)
                        if (currentLemma.HeadedBy(MAKE) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(AT) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(0).Equals(SELF) &&
                            Locations.ContainsKey(currentLemma.GetArgAsExpression(0).GetArgAsExpression(1))) {
                            var df = new Expression(DF, MAKE,
                                currentLemma.GetArgAsExpression(0),
                                currentLemma.GetArgAsExpression(1));

                            PushNode(new ProofNode(df, current.KnowledgeState, nextDepth, current, i, current.Omega));
                        }                        
                        // I can inform anyone as long as I'm close enough.
                        // 
                        // (what I say also needs to be true, but if I don't believe
                        // P then I can't actually _try_ to inform x of P, I can only
                        // try to make them think they're informed of P)
                        // 
                        // M |- at(self, x), M |- df(make, informed(P, x), self) =>
                        // M |- make(informed(P, x), self)
                        if (currentLemma.HeadedBy(MAKE) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(INFORMED) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF)) {
                            var addressee = currentLemma.GetArgAsExpression(0).GetArgAsExpression(1);
                            var at = new Expression(AT, SELF, addressee);
                            var df = new Expression(DF, MAKE, currentLemma.GetArgAsExpression(0), currentLemma.GetArgAsExpression(1));

                            var dfNode = new ProofNode(df, current.KnowledgeState,
                                nextDepth, current, i, current.Omega,
                                hasYoungerSibling: true);

                            var atNode = new ProofNode(at, current.KnowledgeState,
                                nextDepth, current, i, current.Omega, dfNode);

                            PushNode(atNode);
                            PushNode(dfNode);
                        }
                        // M |- make(at(self, X), self) => M |- at(self, X)
                        // M |- make(informed(P, x), self) => M |- informed(P, x)
                        if ((currentLemma.HeadedBy(AT) && currentLemma.GetArgAsExpression(0).Equals(SELF)) ||
                            currentLemma.HeadedBy(INFORMED)) {
                            var make = new Expression(MAKE, currentLemma, SELF);
                            PushNode(new ProofNode(make, current.KnowledgeState, nextDepth, current, i, current.Omega));
                        }

                        // end abilities
                        
                        var currentVariables = currentLemma.GetVariables();
                        var f1 = GetUnusedVariable(PREDICATE, currentVariables);
                        var x1 = GetUnusedVariable(INDIVIDUAL, currentVariables);

                        // geach: (t -> t), (e -> t), e -> t
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

                            // PushNode(new ProofNode(geachedTfx, current.KnowledgeState,
                            //     nextDepth, current, i, current.Omega));
                        }
                        // M |- T(F(x)) => M |- geach(T, F, x)
                        if (currentLemma.HeadedBy(GEACH_E_TRUTH_FUNCTION, GEACH_T_QUANTIFIER_PHRASE)) {
                            var ungeachedTfx = new Expression(currentLemma.GetArgAsExpression(0),
                                    new Expression(currentLemma.GetArgAsExpression(1),
                                        currentLemma.GetArgAsExpression(2)));

                            // PushNode(new ProofNode(ungeachedTfx, current.KnowledgeState,
                            //     nextDepth, current, i, current.Omega));
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

                            // PushNode(new ProofNode(
                            //     geachedTf1tf2t, current.KnowledgeState,
                            //     nextDepth, current, i, current.Omega));
                        }

                        // geach - : (e -> t) -> t, (t, e -> t), t -> t
                        // g(qp, i, t) => qp(i(t))
                        var qp1 = GetUnusedVariable(QUANTIFIER_PHRASE, currentVariables);
                        var i1  = GetUnusedVariable(INDIVIDUAL_TRUTH_RELATION, currentVariables);
                        var qpitFormula = new Expression(
                            new Expression(qp1),
                                new Expression(new Expression(i1), new Expression(t1)));

                        var qpitMatches = qpitFormula.GetMatches(currentLemma);

                        foreach (var qpitBinding in qpitMatches) {
                            var geachedQpit =
                                new Expression(GEACH_T_QUANTIFIER_PHRASE,
                                    qpitBinding[qp1],
                                    qpitBinding[i1],
                                    qpitBinding[t1]);

                            // PushNode(new ProofNode(geachedQpit, current.KnowledgeState,
                            //     nextDepth, current, i, current.Omega));
                        }

                        // here we reverse the order of new proof nodes.
                        if (newStack.Count > 0) {
                            newStack.Peek().IsLastChild = true;
                            do {
                                stack.Push(newStack.Pop());
                            } while (newStack.Count > 0);
                        }
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

                        // Debug.Log("merging " + merge);

                        // this is the basis which gave us this assignment -
                        // we want to meet with this one, and none of the others.
                        var meetBasis = meetBasisIndex == -1 ? new ProofBasis() : merge.YoungerSiblingBases[meetBasisIndex];

                        // trim each of the merged bases to
                        // discard unused variable assignments.
                        foreach (var sendBasis in sendBases) {
                            var trimmedSubstitution = new Substitution();
                            foreach (var assignment in sendBasis.Substitution) {
                                if (merge.Lemma.HasOccurenceOf(assignment.Key)) {
                                    trimmedSubstitution.Add(assignment.Key, assignment.Value);
                                }
                            }

                            if (merge.Substitution != null) {
                                foreach (var assignment in merge.Substitution) {
                                    trimmedSubstitution.Add(assignment.Key, assignment.Value);
                                }
                            }

                            sendBasis.Substitution = trimmedSubstitution;
                        }

                        // trim antecedents of conditionals.
                        // TODO make this more robust for if
                        // the merge lemma is a formula
                        if (merge.Lemma.HeadedBy(IF) && !merge.KnowledgeState.Basis.Contains(merge.Lemma)) {
                            var antecedent = merge.Lemma.GetArgAsExpression(1);
                            var trimmedBases = new ProofBases();

                            foreach (var sendBasis in sendBases) {
                                bool proofHasAntecedent = false;
                                var trimmedBasis = new ProofBasis(new List<Expression>(), new Substitution(sendBasis.Substitution));
                                foreach (var premise in sendBasis.Premises) {
                                    if (premise.Equals(antecedent)) {
                                        proofHasAntecedent = true;
                                    } else {
                                        trimmedBasis.AddPremise(premise);
                                    }
                                }
                                if (proofHasAntecedent) {
                                    trimmedBases.Add(trimmedBasis);
                                }
                            }
                            
                            if (!sendBases.IsEmpty()) {
                                sendBases = trimmedBases;
                                if (merge.Parent == null && merge.OlderSibling == null) {
                                    // Debug.Log(merge.ChildBases + " -> " + sendBases);
                                    merge.ChildBases.Clear();
                                    merge.ChildBases.Add(sendBases);
                                }
                            }
                        }

                        // this is the fully assigned formula,
                        // the proofs of which we're merging.
                        var mergeLemma = meetBasis == null ? merge.Lemma : merge.Lemma.Substitute(meetBasis.Substitution);

                        ProofBases productBases = new ProofBases();

                        if (merge.IsAssumption) {
                            // Debug.Log(merge);
                            // no refutation
                            if (sendBases.IsEmpty() &&
                                merge.ChildBases.IsEmpty() &&
                                (merge.YoungerSiblingBases.Count > 0) &&
                                (exhaustive ||
                                 current.Depth == maxDepth ||
                                 merge.IsLastChild)) {
                                // Debug.Log("exhaustive? " + exhaustive);
                                // Debug.Log("depth is max? " + (current.Depth == maxDepth));
                                // Debug.Log("last child? " + (current.Depth == maxDepth));
                                // we can safely assume the content of
                                // this assumption node
                                var assumptionBasis = new ProofBasis();
                                assumptionBasis.AddPremise(new Expression(STAR, mergeLemma));

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

                        // Debug.Log("the product bases are " + productBases);

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
                            // Debug.Log(merge.Parent.Lemma + " bases added " + productBases);
                            merge.Parent.ChildBases.Add(productBases);
                            sendBases = productBases;
                        }

                        // if (merge.Parent == null && merge.OlderSibling == null) {
                        //     Debug.Log(merge.Lemma + " bases added " + productBases);
                        //     merge.ChildBases.Add(productBases);
                        // }

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

    public void AddNamedPercept(Expression name, Vector3 location) {
        Debug.Assert(name.Type.Equals(INDIVIDUAL));
        Locations[name] = new Vector3(location.x, location.y, location.z);
        var percept = new Expression(SEE, new Expression(EXIST, name), SELF);
        AddToKnowledgeState(KS, percept);
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
                break;
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
    
    private void AddRule(KnowledgeState knowledgeState, Expression key, InferenceRule rule) {
        if (!knowledgeState.Rules.ContainsKey(key)) {
            knowledgeState.Rules.Add(key, new List<InferenceRule>());
        }
        knowledgeState.Rules[key].Add(rule);
    }

    private void AddToOmegaPool(KnowledgeState knowledgeState, Expression omega) {
        knowledgeState.OmegaPool.Add(omega);
    }

    public bool AddToKnowledgeState(KnowledgeState knowledgeState, Expression knowledge, bool firstCall = true, Expression trace = null) {
        Debug.Assert(knowledge.Type.Equals(TRUTH_VALUE));
        Debug.Assert(firstCall || trace != null);

        if (knowledgeState.Basis.Contains(knowledge)) {
            return false;
        }

        // Debug.Log("adding " + knowledge + " to knowledge state.");

        if (knowledge.Depth > MaxDepth) {
            MaxDepth = knowledge.Depth;
        }

        Expression signature = trace;

        if (signature == null) {
            signature = knowledge;
        }

        if (firstCall) {
            if (knowledge.HeadedBy(SEE, MAKE)) {
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
                // TODO figure out a better way to handle events and states
                // 
                // return AddToKnowledgeState(knowledgeState, pSinceSawP, true);
            }
            // we want to ensure a self-supporting premise isn't
            // removed if other links to it are removed.
            knowledgeState.Basis.Add(knowledge);
        }

        if (knowledge.HeadedBy(VERY, KNOW, MAKE, SEE, INFORMED)) {
            var subclause = knowledge.GetArgAsExpression(0);
            AddToKnowledgeState(knowledgeState, subclause, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("K-",
                e => e.Equals(subclause),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.HeadedBy(OMEGA)) {
            var functor = new Expression(knowledge.GetArgAsExpression(0), knowledge.GetArgAsExpression(1));
            AddToKnowledgeState(knowledgeState, functor, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("omega(F, P) |- F(P)",
                e => e.Equals(functor),
                e => new List<Expression>{knowledge}));
            AddToOmegaPool(knowledgeState, knowledge);
        }
        
        if (knowledge.HeadedBy(AND) ||
            knowledge.PrejacentHeadedBy(NOT, OR)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var a = query.GetArgAsExpression(0);
            var b = query.GetArgAsExpression(1);
            var leftRuleName = "A & B |- A";
            var rightRuleName = "A & B |- B";

            if (knowledge.HeadedBy(NOT)) {
                a = new Expression(NOT, a);
                b = new Expression(NOT, b);
                leftRuleName  = "~(A v B) |- ~A";
                rightRuleName = "~(A v B) |- ~B";
            }
            
            AddToKnowledgeState(knowledgeState, a, false, signature);
            AddToKnowledgeState(knowledgeState, b, false, signature);

            AddRule(knowledgeState, signature, new InferenceRule(leftRuleName,
                e => e.Equals(a),
                e => new List<Expression>{knowledge}));
            AddRule(knowledgeState, signature, new InferenceRule(rightRuleName,
                e => e.Equals(b),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.HeadedBy(IF)) {
            var consequent = knowledge.GetArgAsExpression(0);
            var antecedent = knowledge.GetArgAsExpression(1);
            AddRule(knowledgeState, signature, new InferenceRule("Modus Ponens",
                e => e.Equals(consequent),
                e => new List<Expression>{knowledge, antecedent}));

            AddToKnowledgeState(knowledgeState, consequent, false, signature);
        }

        if (knowledge.HeadedBy(ALL, GEN) ||
            knowledge.PrejacentHeadedBy(NOT, SOME)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var g  = query.GetArgAsExpression(1);
            var x  = new Expression(GetUnusedVariable(INDIVIDUAL, g.GetVariables()));
            var gx = new Expression(g, x);
            if (knowledge.HeadedBy(NOT)) {
                gx = new Expression(NOT, gx);
            }

            var ruleName = "∀-";
            if (knowledge.HeadedBy(GEN)) {
                ruleName = "Gen-";
            }
            if (knowledge.PrejacentHeadedBy(NOT, SOME)) {
                ruleName = "~∃-";
            }

            AddRule(knowledgeState, signature, new InferenceRule(ruleName,
                e => gx.GetMatches(e) != null,
                e => {
                    var gxMatches = gx.GetMatches(e);

                    foreach (var gxMatch in gxMatches) {
                        var c = gxMatch[x.Head as Variable];
                        var fc = new Expression(query.GetArgAsExpression(0), c);
                        var premises = new List<Expression>{knowledge, fc};

                        if (knowledge.HeadedBy(GEN)) {
                            premises.Add(new Expression(STAR, new Expression(NOT, new Expression(g, c))));
                        }
                        return premises;
                    }
                    return null;
                }));

            AddToKnowledgeState(knowledgeState, gx, false, signature);
        }

        if (knowledge.HeadedBy(GEACH_T_QUANTIFIER_PHRASE)) {
            var qp  = knowledge.GetArgAsExpression(0);
            var itr = knowledge.GetArgAsExpression(1);
            var t   = knowledge.GetArgAsExpression(2);

            var ungeached = new Expression(qp, new Expression(itr, t));

            // AddToKnowledgeState(knowledgeState, ungeached, false, signature);
        }

        if (knowledge.HeadedBy(SINCE)) {
            var topic = knowledge.GetArgAsExpression(0);
            var anchor = new Expression(PAST, knowledge.GetArgAsExpression(1));

            AddToKnowledgeState(knowledgeState, topic, false, signature);
            AddToKnowledgeState(knowledgeState, anchor, false, signature);

            AddRule(knowledgeState, signature, new InferenceRule("since(P, Q) |- P",
                e => e.Equals(topic),
                e => new List<Expression>{knowledge}));
            AddRule(knowledgeState, signature, new InferenceRule("since(P, Q) |- was(Q)",
                e => e.Equals(anchor),
                e => new List<Expression>{knowledge}));
        }

        return true;
    }

    public bool RemoveFromKnowledgeState(KnowledgeState knowledgeState, Expression knowledge) {
        if (knowledgeState.Basis.Remove(knowledge)) {
            knowledgeState.Rules.Remove(knowledge);
            return true;
        }
        return false;
    }

    // a direct assertion.
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

        AddToKnowledgeState(KS, new Expression(MAKE, new Expression(INFORMED, content, SELF), speaker));
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
        return AddToKnowledgeState(KS, new Expression(OMEGA, VERY, new Expression(GOOD, content)));
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

            StartCoroutine(StreamProofs(goodFromGoalBases, new Expression(IF, good, goal), goodFromGoalDone));

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

                StartCoroutine(StreamProofs(infFromGoodBases, new Expression(IF, oldInfimum, good), infFromGoodDone));

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

                StartCoroutine(StreamProofs(goodFromInfBases, new Expression(IF, good, oldInfimum), goodFromInfDone));

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

                    var costConjunction = new List<Expression>();
                    foreach (var conjunct in benefitConjunction) {
                        costConjunction.Add(new Expression(NOT, conjunct));
                    }

                    var benefitEstimates = new List<Expression>();
                    var benefitEstimationDone = new Container<bool>(false);

                    var costEstimates = new List<Expression>();
                    var costEstimationDone = new Container<bool>(false);

                    StartCoroutine(EstimateValueFor(Conjunctify(benefitConjunction), goods, benefitEstimates, benefitEstimationDone));
                    StartCoroutine(EstimateValueFor(Conjunctify(costConjunction), goods, costEstimates, costEstimationDone));

                    while (!benefitEstimationDone.Item || !costEstimationDone.Item) {
                        yield return null;
                    }

                    var positiveValueForThisPlan = new List<int>();
                    var negativeValueForThisPlan = new List<int>();

                    foreach (var benefitEstimate in benefitEstimates) {
                        positiveValueForThisPlan = Plus(positiveValueForThisPlan, evaluativeBase[benefitEstimate]);
                    }

                    foreach (var costEstimate in costEstimates) {
                        negativeValueForThisPlan = Plus(negativeValueForThisPlan, evaluativeBase[costEstimate]);
                    }
                    for (int i = 0; i < negativeValueForThisPlan.Count; i++) {
                        negativeValueForThisPlan[i] = -1 * negativeValueForThisPlan[i];
                    }

                    var netValueForThisPlan = Plus(positiveValueForThisPlan, negativeValueForThisPlan);

                    bestValueForThisGood = MaxValue(bestValueForThisGood, netValueForThisPlan);
                    if (bestValueForThisGood == netValueForThisPlan) {
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
;
