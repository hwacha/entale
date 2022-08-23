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
        public SortedSet<Expression> LemmaPool;

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
                LemmaPool = new SortedSet<Expression>(omegaPool);
            } else {
                Basis = basis;
                Rules = rules;
                LemmaPool = omegaPool;
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
            foreach (Expression e in LemmaPool) {
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
        public readonly bool HasYoungerSibling;
        public readonly bool IsAssumption;
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
            ProofNode olderSibling = null,
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
            HasYoungerSibling = hasYoungerSibling;
            IsAssumption = isAssumption;

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

            Debug.Log("================================================");
            Debug.Log("proving " + conclusion + " at depth=" + maxDepth);

            bases.Clear();

            reachedDepth = 0;

            // we set up our stack for DFS
            // with the intended
            var root = new ProofNode(conclusion, KS, 0, null, 0);
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

                Debug.Log("searching " + current);
 
                bool allExhaustive = current.YoungerSiblingBases.Count == 0;
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
                            PushNode(new ProofNode(subclause, current.KnowledgeState,
                                nextDepth, current, i));
                        }

                        // truly - contraposed
                        if (currentLemma.PrejacentHeadedBy(NOT, TRULY)) {
                            var subclause = currentLemma.GetArgAsExpression(0).GetArgAsExpression(0);
                            PushNode(new ProofNode(new Expression(NOT, subclause), current.KnowledgeState,
                                nextDepth, current, i));
                        }

                        // star + 
                        // M |/- A => M |- *A
                        if (currentLemma.HeadedBy(STAR)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i,
                                isAssumption: true));
                        }

                        // M |- A => M |- ~*A
                        if (currentLemma.PrejacentHeadedBy(NOT, STAR)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0).GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i));
                        }

                        // nonidentity assumption
                        if (currentLemma.PrejacentHeadedBy(NOT, IDENTITY)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                current.KnowledgeState,
                                nextDepth, current, i,
                                isAssumption: true));
                        }

                        // double negation introduction
                        if (currentLemma.PrejacentHeadedBy(NOT, NOT)) {
                            PushNode(new ProofNode(
                                currentLemma.GetArgAsExpression(0).GetArgAsExpression(0),
                                current.KnowledgeState, nextDepth,
                                current, i));
                        }

                        // contraposed very +
                        // M |- ~A => M |- ~+A
                        if (currentLemma.PrejacentHeadedBy(NOT, VERY)) {
                            PushNode(new ProofNode(
                                new Expression(NOT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)),
                                current.KnowledgeState, nextDepth,
                                current, i));
                        }

                        // factive -
                        if (currentLemma.HeadedBy(DF)) {
                            var factive = new Expression(
                                currentLemma.GetArgAsExpression(0),
                                currentLemma.GetArgAsExpression(1),
                                currentLemma.GetArgAsExpression(2));
                            PushNode(new ProofNode(factive, current.KnowledgeState, nextDepth, current, i));
                        }
                        if (currentLemma.HeadedBy(IF) &&
                            currentLemma.GetArgAsExpression(1).HeadedBy(DF) &&
                            currentLemma.GetArgAsExpression(0).Equals(
                                currentLemma.GetArgAsExpression(1).GetArgAsExpression(1))) {
                            var factive = new Expression(
                                currentLemma.GetArgAsExpression(1).GetArgAsExpression(0),
                                currentLemma.GetArgAsExpression(0),
                                currentLemma.GetArgAsExpression(1).GetArgAsExpression(2));
                            PushNode(new ProofNode(factive, current.KnowledgeState, nextDepth, current, i));
                        }
                        // contraposed
                        // @Note I assume every ITR is
                        // factive by default
                        if (currentLemma.HeadedBy(NOT) &&
                            currentLemma.GetArgAsExpression(0).Head.Type.Equals(INDIVIDUAL_TRUTH_RELATION)) {
                            var factive = currentLemma.GetArgAsExpression(0);
                            var head = new Expression(factive.Head);
                            var prop = factive.GetArgAsExpression(0);
                            var subject = factive.GetArgAsExpression(1);
                            var df = new Expression(DF, head, prop, subject);
                            var notDf = new Expression(NOT, df);
                            var notPIfDf = new Expression(NOT, new Expression(IF, prop, notDf));

                            PushNode(new ProofNode(notDf, current.KnowledgeState, nextDepth, current, i));
                            PushNode(new ProofNode(notPIfDf, current.KnowledgeState, nextDepth, current, i));
                        }

                        // factive +
                        if (currentLemma.Head.Type.Equals(INDIVIDUAL_TRUTH_RELATION)) {
                            var df =
                                new Expression(DF,
                                    new Expression(currentLemma.Head),
                                    currentLemma.GetArgAsExpression(0),
                                    currentLemma.GetArgAsExpression(1));
                            var pIfDf = new Expression(IF, currentLemma.GetArgAsExpression(0), df);

                            var pIfDfNode =
                                new ProofNode(pIfDf,
                                    current.KnowledgeState,
                                    nextDepth, current, i,
                                    hasYoungerSibling: true);
                            var dfNode = new ProofNode(df, current.KnowledgeState, nextDepth, current, i, pIfDfNode);

                            PushNode(dfNode);
                            PushNode(pIfDfNode);
                        }

                        // M |- A => M |- past(A)
                        if (currentLemma.HeadedBy(PAST)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            PushNode(new ProofNode(
                                subclause, current.KnowledgeState, nextDepth,
                                current, i));
                        }

                        // M |- good(~A) => M |- ~good(A)
                        if (currentLemma.PrejacentHeadedBy(NOT, GOOD)) {
                            var goodNotA = new Expression(GOOD,
                                new Expression(NOT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)));
                            PushNode(new ProofNode(
                                goodNotA, current.KnowledgeState, nextDepth,
                                current, i));
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
                                current, i));
                            PushNode(new ProofNode(
                                b, current.KnowledgeState, nextDepth,
                                current, i));
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

                            // var starANode = new ProofNode(
                            //     a.HeadedBy(NOT) ? a.GetArgAsExpression(0) : new Expression(NOT, a),
                            //     current.KnowledgeState, nextDepth,
                            //     current, i,
                            //     hasYoungerSibling: true,
                            //     isAssumption: true);
                            var bNodeToStarA = new ProofNode(
                                b, current.KnowledgeState, nextDepth,
                                current, i,
                                // olderSibling: starANode,
                                hasYoungerSibling: true);
                            var aNodeToStarA = new ProofNode(
                                a, current.KnowledgeState, nextDepth,
                                current, i, bNodeToStarA);

                            // var starBNode = new ProofNode(
                            //     b.HeadedBy(NOT) ? b.GetArgAsExpression(0) : new Expression(NOT, b),
                            //     current.KnowledgeState, nextDepth,
                            //     current, i,
                            //     hasYoungerSibling: true,
                            //     isAssumption: true);
                            var bNodeToStarB = new ProofNode(
                                b, current.KnowledgeState, nextDepth,
                                current, i,
                                // olderSibling: starBNode,
                                hasYoungerSibling: true);
                            var aNodeToStarB = new ProofNode(
                                a, current.KnowledgeState, nextDepth,
                                current, i, bNodeToStarB);

                            
                            PushNode(aNodeToStarA);
                            PushNode(bNodeToStarA);
                            // PushNode(starANode);

                            PushNode(aNodeToStarB);
                            PushNode(bNodeToStarB);
                            // PushNode(starBNode);
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

                            var gxNode = new ProofNode(gx, current.KnowledgeState, nextDepth, current, i,
                                hasYoungerSibling: true);
                            var fxNode = new ProofNode(fx, current.KnowledgeState, nextDepth, current, i, gxNode);

                            PushNode(fxNode);
                            PushNode(gxNode);
                        }

                        // conditional proof
                        // M, A |- B => M |- A -> B
                        // 
                        // TODO figure out logic for conditional
                        // with antecedent known to be false
                        if (currentLemma.HeadedBy(IF) || currentLemma.PrejacentHeadedBy(NOT, IF)) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            // @Note this is not a typo ---
                            // antecedent is the second argument of the conditional
                            var consequent = query.GetArgAsExpression(0);
                            var antecedent = query.GetArgAsExpression(1);

                            var newKnowledgeState = new KnowledgeState(current.KnowledgeState.Basis, current.KnowledgeState.Rules, current.KnowledgeState.LemmaPool);

                            AddToKnowledgeState(newKnowledgeState, antecedent);

                            var consequentNode = new ProofNode(consequent, newKnowledgeState, nextDepth, current, i,
                                isAssumption: currentLemma.HeadedBy(NOT));

                            PushNode(consequentNode);
                        }

                        if (currentLemma.PrejacentHeadedBy(NOT, IF)) {
                            var consequent = currentLemma.GetArgAsExpression(0).GetArgAsExpression(0);
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
                            var converseNode = new ProofNode(converse, current.KnowledgeState, nextDepth, current, i);
                            PushNode(converseNode);
                        }

                        // converse rules
                        if (currentLemma.HeadedBy(CONVERSE) ||
                            currentLemma.PrejacentHeadedBy(NOT, CONVERSE)) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var rel = new Expression(
                                query.GetArgAsExpression(0),
                                query.GetArgAsExpression(2),
                                query.GetArgAsExpression(1));
                            if (currentLemma.HeadedBy(NOT)) {
                                rel = new Expression(NOT, rel);
                            }
                            var relNode = new ProofNode(rel, current.KnowledgeState, nextDepth, current, i);
                            PushNode(relNode);
                        }

                        // M |- banana(x) => M |- fruit(x)
                        // M |- tomato(x) => M |- fruit(x)
                        if (currentLemma.HeadedBy(FRUIT)) {
                            var tomatoX = new Expression(TOMATO, currentLemma.GetArgAsExpression(0));
                            var bananaX = new Expression(BANANA, currentLemma.GetArgAsExpression(0));
                            PushNode(new ProofNode(tomatoX, current.KnowledgeState, nextDepth, current, i));
                            PushNode(new ProofNode(bananaX, current.KnowledgeState, nextDepth, current, i));
                        }
                        // contraposed
                        if (currentLemma.PrejacentHeadedBy(NOT, TOMATO, BANANA)) {
                            var fruitX = new Expression(NOT, new Expression(FRUIT, currentLemma.GetArgAsExpression(0).GetArgAsExpression(0)));
                            PushNode(new ProofNode(fruitX, current.KnowledgeState, nextDepth, current, i));
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
                                        PushNode(new ProofNode(premises[0], current.KnowledgeState, nextDepth, current, i));
                                    } else {
                                        ProofNode nextPremNode = null;
                                        var nodes = new Stack<ProofNode>();
                                        for (int j = premises.Count - 1; j >= 0; j--) {
                                            var curPremNode = new ProofNode(
                                                premises[j], current.KnowledgeState, nextDepth, current, i,
                                                nextPremNode, hasYoungerSibling: j > 0);
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
                        // 
                        // M, omega(F, P) |- Q, M |- omega(F, P) => M |- F(Q)
                        var xtt = GetUnusedVariable(TRUTH_FUNCTION, currentLemma.GetVariables());
                        var xt  = GetUnusedVariable(TRUTH_VALUE, currentLemma.GetVariables());
                        var xttxtFormula = new Expression(new Expression(xtt), new Expression(xt));
                        var xttxtMatches = xttxtFormula.GetMatches(currentLemma);
                        foreach (var xttxtMatch in xttxtMatches) {
                            // @Note this limits omega proof
                            // should be replaced with something smarter
                            // but no way to bound it right now
                            var omegaBottom = new Expression(OMEGA, xttxtMatch[xtt], new Expression(new Bottom(TRUTH_VALUE)));
                            var omegaTop    = new Expression(OMEGA, xttxtMatch[xtt], new Expression(new Top(TRUTH_VALUE)));

                            var omegaRange = current.KnowledgeState.LemmaPool.GetViewBetween(omegaBottom, omegaTop);
                            foreach (var omega in omegaRange) {
                                var xtIfOmega = new Expression(IF, xttxtMatch[xt], omega);
                                var omegaNode = new ProofNode(omega, current.KnowledgeState, nextDepth, current, i, hasYoungerSibling: true);
                                var xtIfOmegaNode = new ProofNode(xtIfOmega, current.KnowledgeState, nextDepth, current, i, omegaNode);

                                PushNode(xtIfOmegaNode);
                                PushNode(omegaNode);
                            }
                        }
                        // M |- F(P), M |- Q -> F(Q) => omega(F, P)
                        // ~F(P) => M |- ~omega(F, P)
                        // ~(Q -> F(Q)) |- ~omega(F, P)
                        if (currentLemma.HeadedBy(OMEGA) ||
                            currentLemma.PrejacentHeadedBy(NOT, OMEGA)) {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var fp = new Expression(query.GetArgAsExpression(0), query.GetArgAsExpression(1));
                            var fTestIfTest = new Expression(IF, new Expression(query.GetArgAsExpression(0), TEST), TEST);

                            ProofNode fpNode;
                            ProofNode fTestIfTestNode;
                            if (currentLemma.HeadedBy(NOT)) {
                                fpNode = new ProofNode(new Expression(NOT, fp), current.KnowledgeState, nextDepth, current, i);
                                fTestIfTestNode = new ProofNode(new Expression(NOT, fTestIfTest), current.KnowledgeState, nextDepth, current, i);
                            } else {
                                fTestIfTestNode = new ProofNode(fTestIfTest, current.KnowledgeState, nextDepth, current, i, hasYoungerSibling: true);
                                fpNode = new ProofNode(fp, current.KnowledgeState, nextDepth, current, i, fTestIfTestNode);    
                            }                            

                            PushNode(fpNode);
                            PushNode(fTestIfTestNode);

                            // M |-  omega(F, P) => M |-  omega(F, F(P))
                            // M |- ~omega(F, P) => M |- ~omega(F, F(P))
                            if (query.GetArgAsExpression(1).HeadedBy(query.GetArgAsExpression(0))) {
                                var newOmega = new Expression(OMEGA,
                                    query.GetArgAsExpression(0), query.GetArgAsExpression(1).GetArgAsExpression(0));

                                if (currentLemma.HeadedBy(NOT)) {
                                    newOmega = new Expression(NOT, newOmega);
                                }

                                var newOmegaNode = new ProofNode(newOmega, current.KnowledgeState, nextDepth, current, i);
                                PushNode(newOmegaNode);
                            }
                        }
                        
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
                                    nextDepth, current, i,
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

                            PushNode(new ProofNode(df, current.KnowledgeState, nextDepth, current, i));
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
                                nextDepth, current, i,
                                hasYoungerSibling: true);

                            var atNode = new ProofNode(at, current.KnowledgeState,
                                nextDepth, current, i, dfNode);

                            PushNode(atNode);
                            PushNode(dfNode);
                        }
                        // M |- make(at(self, X), self) => M |- at(self, X)
                        // M |- make(informed(P, x), self) => M |- informed(P, x)
                        if ((currentLemma.HeadedBy(AT) && currentLemma.GetArgAsExpression(0).Equals(SELF)) ||
                            currentLemma.HeadedBy(INFORMED)) {
                            var make = new Expression(MAKE, currentLemma, SELF);
                            PushNode(new ProofNode(make, current.KnowledgeState, nextDepth, current, i));
                        }

                        // end abilities
                        
                        var currentVariables = currentLemma.GetVariables();
                        var f1 = GetUnusedVariable(PREDICATE, currentVariables);
                        var x1 = GetUnusedVariable(INDIVIDUAL, currentVariables);

                        // generalized geach introduction
                        // (at least for geaches with truth values
                        // at the end of them)
                        // 
                        // @Note if geached functions with other types
                        // as output are necessary, then we'll need to
                        // recursively descend through an expression
                        // and transform them all
                        // 
                        // R(X1, ..., X2) |- 𝔾_x(R, X->x1, ..., X->x2, x)
                        // P |- G_x(P, x)
                        if ((currentLemma.Head is Name) && (currentLemma.Head as Name).ID == "𝔾" ||
                            currentLemma.HeadedBy(NOT) && (currentLemma.GetArgAsExpression(0).Head is Name)
                            && (currentLemma.GetArgAsExpression(0).Head as Name).ID == "𝔾") {
                            var query = currentLemma.HeadedBy(NOT) ? currentLemma.GetArgAsExpression(0) : currentLemma;
                            var head = query.GetArgAsExpression(0);
                            var lift = query.GetArgAsExpression(query.NumArgs - 1);
                            Argument[] appliedArgs = new Expression[query.NumArgs - 2];
                            for (int j = 0; j < appliedArgs.Length; j++) {
                                appliedArgs[j] = new Expression(query.GetArgAsExpression(j + 1), lift);
                            }
                            var ungeached = new Expression(head, appliedArgs);
                            if (currentLemma.HeadedBy(NOT)) {
                                ungeached = new Expression(NOT, ungeached);
                            }
                            PushNode(new ProofNode(ungeached, current.KnowledgeState, nextDepth, current, i));
                        }

                        // here we reverse the order of new proof nodes.
                        if (newStack.Count > 0) {
                            newStack.Peek().IsLastChild = true;
                            do {
                                stack.Push(newStack.Pop());
                            } while (newStack.Count > 0);
                        }
                    }

                    allExhaustive = allExhaustive && exhaustive;

                    // we're not going to pass down the child bases this time,
                    // because we don't have anything to give.
                    if (searchBases.IsEmpty() &&
                        !exhaustive &&
                        current.Depth != maxDepth) {
                        continue;
                    }

                    // Debug.Log(currentLemma + " is exhaustive? " + exhaustive);

                    current.ChildBases.Add(searchBases);
                    sends.Add(new KeyValuePair<ProofBases, bool>(searchBases, exhaustive));
                }

                int meetBasisIndex = 0;
                // TODO fix this condition so that an empty bases gets sent only when
                // it constitutes the last possible search for an assumption.
                if (sends.Count == 0 && current.IsLastChild && (current.Depth == maxDepth || allExhaustive)) {
                    // Debug.Log(current);
                }
                if (sends.Count == 0 &&
                    (!current.IsAssumption || current.YoungerSiblingBases.Count > 0) &&
                    current.IsLastChild &&
                    (current.Depth == maxDepth || allExhaustive)) {
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
                        if (merge.Lemma.HeadedBy(IF) && !merge.KnowledgeState.LemmaPool.Contains(merge.Lemma)) {
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
                            // Debug.Log("current: " + current);
                            // Debug.Log("merge: " +  merge);
                            // Debug.Log(current.Lemma + " send bases are empty? " + sendBases.IsEmpty());
                            // Debug.Log(sendBases);
                            // Debug.Log(current.Lemma + " child bases are empty? " + merge.ChildBases.IsEmpty());
                            // Debug.Log(merge.ChildBases);
                            // Debug.Log("exhaustive? " + exhaustive);
                            // Debug.Log("depth is max? " + (current.Depth == maxDepth));
                            // Debug.Log("last child? " + merge.IsLastChild);
                            // no refutation
                            if (sendBases.IsEmpty() &&
                                merge.ChildBases.IsEmpty() &&
                                ((exhaustive ||
                                 current.Depth == maxDepth) &&
                                 current.IsLastChild)) {
                                // we can safely assume the content of
                                // this assumption node
                                var assumptionBasis = new ProofBasis();
                                if (merge.Parent != null && merge.Parent.Lemma.PrejacentHeadedBy(NOT, IF)) {
                                    assumptionBasis.AddPremise(merge.Parent.Lemma);
                                } else {
                                    assumptionBasis.AddPremise(new Expression(STAR, mergeLemma));
                                }

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

    private void AddToLemmaPool(KnowledgeState knowledgeState, Expression lemma) {
        knowledgeState.LemmaPool.Add(lemma);
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

        if (knowledge.HeadedBy(TRULY, VERY)) {
            var subclause = knowledge.GetArgAsExpression(0);
            AddToKnowledgeState(knowledgeState, subclause, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule(knowledge.Head + "-",
                e => e.Equals(subclause),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.HeadedBy(KNOW, MAKE, SEE, INFORMED)) {
            var pIfDf =
                new Expression(IF,
                    knowledge.GetArgAsExpression(0),
                    new Expression(DF,
                        new Expression(knowledge.Head),
                        knowledge.GetArgAsExpression(0),
                        knowledge.GetArgAsExpression(1)));

            AddToKnowledgeState(knowledgeState, pIfDf, false, signature);
            AddRule(knowledgeState, pIfDf, new InferenceRule("factive-",
                e => e.Equals(pIfDf),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.PrejacentHeadedBy(NOT, TRULY)) {
            var justNot = new Expression(NOT, knowledge.GetArgAsExpression(0).GetArgAsExpression(0));
            AddToKnowledgeState(knowledgeState, justNot, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("truly- contraposed",
                e => e.Equals(justNot),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.PrejacentHeadedBy(NOT, NOT) ||
            knowledge.PrejacentHeadedBy(NOT, STAR)) {
            var subSubclause = knowledge.GetArgAsExpression(0).GetArgAsExpression(0);
            AddToKnowledgeState(knowledgeState, subSubclause, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("double negation elimination",
                e => e.Equals(subSubclause),
                e => new List<Expression>{knowledge}));
        }

        if (knowledge.HeadedBy(OMEGA)) {
            var functor = new Expression(knowledge.GetArgAsExpression(0), knowledge.GetArgAsExpression(1));
            AddToKnowledgeState(knowledgeState, functor, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("omega(F, P) |- F(P)",
                e => e.Equals(functor),
                e => new List<Expression>{knowledge}));
            AddToLemmaPool(knowledgeState, knowledge);
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
                e => a.Matches(e),
                e => new List<Expression>{
                        knowledge.Substitute(a.GetMatches(e).First())
                    }));
            AddRule(knowledgeState, signature, new InferenceRule(rightRuleName,
                e => b.Matches(e),
                e => new List<Expression>{
                        knowledge.Substitute(b.GetMatches(e).First())
                    }));
        }

        // disjunctive syllogism and conjunctive syllogism
        // A v B, ~A |- B; A v B, ~B |- A
        // ~(A v B), A |- ~B; ~(A v B), B |- ~A
        if (knowledge.HeadedBy(OR) ||
            knowledge.PrejacentHeadedBy(NOT, AND)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var adjunctA = query.GetArgAsExpression(0);
            var adjunctB = query.GetArgAsExpression(1);
            var notAdjunctA = new Expression(NOT, adjunctA);
            var notAdjunctB = new Expression(NOT, adjunctB);

            if (knowledge.HeadedBy(NOT)) {
                AddToKnowledgeState(knowledgeState, notAdjunctA, false, signature);
                AddToKnowledgeState(knowledgeState, notAdjunctB, false, signature);

                AddRule(knowledgeState, signature, new InferenceRule("conjunctive syllogism left",
                    e => notAdjunctA.Matches(e),
                    e => new List<Expression>{knowledge, adjunctB}));
                AddRule(knowledgeState, signature, new InferenceRule("conjunctive syllogism right",
                    e => notAdjunctB.Matches(e),
                    e => new List<Expression>{knowledge, adjunctA}));
            } else {
                AddToKnowledgeState(knowledgeState, adjunctA, false, signature);
                AddToKnowledgeState(knowledgeState, adjunctB, false, signature);

                AddRule(knowledgeState, signature, new InferenceRule("disjunctive syllogism left",
                    e => adjunctA.Matches(e),
                    e => new List<Expression>{knowledge, notAdjunctB}));
                AddRule(knowledgeState, signature, new InferenceRule("disjunctive syllogism right",
                    e => adjunctB.Matches(e),
                    e => new List<Expression>{knowledge, notAdjunctA}));
            }
        }

        if (knowledge.HeadedBy(IF)) {
            var consequent = knowledge.GetArgAsExpression(0);
            var antecedent = knowledge.GetArgAsExpression(1);
            var notConsequent = new Expression(NOT, consequent);
            var notAntecedent = new Expression(NOT, antecedent);

            AddToKnowledgeState(knowledgeState, consequent, false, signature);
            AddToKnowledgeState(knowledgeState, notAntecedent, false, signature);

            AddRule(knowledgeState, signature, new InferenceRule("Modus Ponens",
                e => e.Equals(consequent),
                e => new List<Expression>{knowledge, antecedent}));

            AddRule(knowledgeState, signature, new InferenceRule("Modus Tollens",
                e => e.Equals(notAntecedent),
                e => new List<Expression>{knowledge, notConsequent}));

            AddToLemmaPool(knowledgeState, knowledge);
        }

        // some -
        // some(F, G) |- F(c), some(F, G) |- G(c)
        // ~all(F, G) |- F(c), ~all(F, G) |- ~G(c)
        if (knowledge.HeadedBy(SOME) ||
            knowledge.PrejacentHeadedBy(NOT, ALL)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;

            var c = new Expression(new Parameter(INDIVIDUAL, GetNextParameterID()));

            var fc = new Expression(query.GetArgAsExpression(0), c);
            var gc = new Expression(query.GetArgAsExpression(1), c);

            if (knowledge.HeadedBy(NOT)) {
                gc = new Expression(NOT, gc);
            }

            AddToKnowledgeState(knowledgeState, fc, false, signature);
            AddToKnowledgeState(knowledgeState, gc, false, signature);

            AddRule(knowledgeState, signature, new InferenceRule("some -",
                e => e.Matches(fc),
                e => new List<Expression>{knowledge.Substitute(e.GetMatches(fc).First())}));
            AddRule(knowledgeState, signature, new InferenceRule("some -",
                e => e.Matches(gc),
                e => new List<Expression>{knowledge.Substitute(e.GetMatches(gc).First())}));
        }

        // ~some(F, G),  G(x) |- ~F(x)
        //   all(F, G), ~G(x) |- ~F(x)
        if (knowledge.HeadedBy(ALL, GEN) ||
            knowledge.PrejacentHeadedBy(NOT, SOME)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var f  = query.GetArgAsExpression(0);
            var g  = query.GetArgAsExpression(1);
            var x  = new Expression(GetUnusedVariable(INDIVIDUAL, g.GetVariables()));
            var gx = new Expression(g, x);
            var notFx = new Expression(NOT, new Expression(f, x));
            var ruleName = "∀-";

            if (knowledge.HeadedBy(NOT)) {
                gx = new Expression(NOT, gx);
                ruleName = "~∃-";
            }

            if (knowledge.HeadedBy(GEN)) {
                ruleName = "gen-";
            }

            AddRule(knowledgeState, signature, new InferenceRule(ruleName,
                e => gx.Matches(e),
                e => {
                    var gxMatches = gx.GetMatches(e);

                    foreach (var gxMatch in gxMatches) {
                        var c = gxMatch[x.Head as Variable];
                        var fc = new Expression(f, c);
                        var premises = new List<Expression>{knowledge, fc};

                        if (knowledge.HeadedBy(GEN)) {
                            premises.Add(new Expression(STAR, new Expression(NOT,
                                    new Expression(INS, knowledge.GetArgAsExpression(0), knowledge.GetArgAsExpression(1)))));
                        }

                        return premises;
                    }
                    return null;
                }));

            AddRule(knowledgeState, signature, new InferenceRule(ruleName,
                e => notFx.Matches(e),
                e => {
                    var notFxMatches = notFx.GetMatches(e);

                    foreach (var notFxMatch in notFxMatches) {
                        var c = notFxMatch[x.Head as Variable];
                        var notGc = new Expression(NOT, new Expression(g, c));
                        if (knowledge.HeadedBy(NOT)) {
                            notGc = notGc.GetArgAsExpression(0);
                        }
                        var premises = new List<Expression>{knowledge, notGc};

                        if (knowledge.HeadedBy(GEN)) {
                            premises.Add(new Expression(STAR, new Expression(NOT,
                                    new Expression(INS, knowledge.GetArgAsExpression(0), knowledge.GetArgAsExpression(1)))));
                        }
                        return premises;
                    }
                    return null;
                }));

            AddToKnowledgeState(knowledgeState, gx, false, signature);
            AddToKnowledgeState(knowledgeState, notFx, false, signature);
        }

        if (knowledge.HeadedBy(Expression.CONVERSE) ||
            knowledge.PrejacentHeadedBy(NOT, CONVERSE)) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var rel = new Expression(
                query.GetArgAsExpression(0),
                query.GetArgAsExpression(2),
                query.GetArgAsExpression(1));

            if (knowledge.HeadedBy(NOT)) {
                rel = new Expression(NOT, rel);
            }

            AddToKnowledgeState(knowledgeState, rel, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("converse-",
                e => e.Equals(rel),
                e => new List<Expression>{knowledge}));
        }

        // general geach elimination
        // 𝔾_x(R, X->x1, ..., X->x2, x) |- R(X1, ..., X2)
        if (((knowledge.Head is Name) && (knowledge.Head as Name).ID == "𝔾") ||
            (knowledge.HeadedBy(NOT) &&
                (knowledge.GetArgAsExpression(0).Head is Name) &&
                (knowledge.GetArgAsExpression(0).Head as Name).ID == "𝔾")) {
            var query = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : knowledge;
            var head = query.GetArgAsExpression(0);
            var lift = query.GetArgAsExpression(query.NumArgs - 1);
            Argument[] appliedArgs = new Expression[query.NumArgs - 2];
            for (int j = 0; j < appliedArgs.Length; j++) {
                appliedArgs[j] = new Expression(query.GetArgAsExpression(j + 1), lift);
            }
            var ungeached = new Expression(head, appliedArgs);
            if (knowledge.HeadedBy(NOT)) {
                ungeached = new Expression(NOT, ungeached);
            }

            AddToKnowledgeState(knowledgeState, ungeached, false, signature);
            AddRule(knowledgeState, signature, new InferenceRule("𝔾-",
                e => ungeached.Matches(e),
                e => new List<Expression>{knowledge.Substitute(ungeached.GetMatches(e).First())}));

        }

        // contraposed past introduction
        // ~past(A) |- ~A
        if (knowledge.PrejacentHeadedBy(NOT, PAST)) {
            var notPresent = new Expression(NOT, knowledge.GetArgAsExpression(0).GetArgAsExpression(0));
            AddToKnowledgeState(knowledgeState, notPresent, false, signature);

            AddRule(knowledgeState, signature, new InferenceRule("~past(A) |- ~A",
                e => e.Equals(notPresent),
                e => new List<Expression>{knowledge}));
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
