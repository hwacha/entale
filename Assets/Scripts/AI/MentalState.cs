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
    public static readonly long TIME_BUDGET = 8;

    public FrameTimer FrameTimer;

    protected int ParameterID;

    public static int MAX_CALLS = 10_000;
    protected int numCalls = 0;

    public class KnowledgeState {
        public SortedSet<Expression> Basis;
        public SortedList<Expression, List<InferenceRule>> Rules;

        public KnowledgeState(SortedSet<Expression> basis, SortedList<Expression, List<InferenceRule>> rules, bool copy = true) {
            if (copy) {
                Basis = new SortedSet<Expression>(basis);
                Rules = new SortedList<Expression, List<InferenceRule>>();
                foreach (var keyAndRules in rules) {
                    Rules.Add(keyAndRules.Key, new List<InferenceRule>());
                    foreach (var rule in keyAndRules.Value) {
                        Rules[keyAndRules.Key].Add(rule);
                    }
                }
            } else {
                Basis = basis;
                Rules = rules;
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
        KS = new KnowledgeState(new SortedSet<Expression>(), new SortedList<Expression, List<InferenceRule>>(), false);

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

    private static ProofBasis TrimmedSubstitution(Expression e, ProofBasis basis) {
        var variables = e.GetVariables();
        var trimmedSubstitution = new Substitution();
        foreach (var assignment in basis.Substitution) {
            if (variables.Contains(assignment.Key)) {
                trimmedSubstitution.Add(assignment.Key, assignment.Value);
            }
        }
        return new ProofBasis(basis.Premises.ToList(), trimmedSubstitution);
    }

    private ProofBases GetProofs(
        Expression lemma,
        HashSet<Expression> triedExpressions,
        KnowledgeState knowledgeState,
        ProofType pt,
        Expression require,
        bool isRequireGate = false,
        Expression supposition = null,
        bool isFailure = false) {
        numCalls++;
        Debug.Assert(lemma.Type.Equals(TRUTH_VALUE));

        var proofs = new ProofBases();

        if (triedExpressions.Contains(lemma) || numCalls > MAX_CALLS) {
            return proofs;
        }
        var nextTriedExpressions = new HashSet<Expression>(triedExpressions);
        nextTriedExpressions.Add(lemma);

        if (lemma.GetVariables().Count == 0) {
            // closed sentence
            if (knowledgeState.Basis.Contains(lemma)) {
                var basis = new ProofBasis();
                basis.AddPremise(lemma);
                proofs.Add(basis);
            }
        } else {
            // formula
            var (bottom, top) = lemma.GetBounds();
            var satisfiers = knowledgeState.Basis.GetViewBetween(bottom, top);

            foreach (var satisfier in satisfiers) {
                var unifiers = lemma.Unify(satisfier);
                foreach (var unifier in unifiers) {
                    var basis = new ProofBasis(new List<Expression>{satisfier.Substitute(unifier)}, unifier);
                    proofs.Add(basis);
                }
            }
        }

        // hard-coded rules
        
        // star as failure
        if (lemma.HeadedBy(STAR)) {
            // @Note we short-circuit and
            // don't check for explicitly stored stars.
            // Might need to change
            return GetProofs(lemma.GetArgAsExpression(0),
                nextTriedExpressions,
                knowledgeState, pt, require, isFailure: true, isRequireGate: isRequireGate, supposition: supposition);
        }

        // de se performative resolution
        // will(P) |- df(make, P, self)
        if (pt == Plan && lemma.HeadedBy(DF) &&
            lemma.GetArgAsExpression(0).HeadedBy(MAKE) &&
            lemma.GetArgAsExpression(2).Equals(SELF)) {
            var basis = new ProofBasis();
            basis.AddPremise(new Expression(WILL, lemma.GetArgAsExpression(1)));
            proofs.Add(basis);
        }

        // location
        if (lemma.HeadedBy(AT)) {
            var a = lemma.GetArgAsExpression(0);
            var b = lemma.GetArgAsExpression(1);

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
                    basis.AddPremise(lemma);
                    proofs.Add(basis);
                }
            }
        }

        ProofBases GetProofsFromRule(InferenceRule rule) {
            var (premises, ruleRequire, ruleSupposition) = rule.Apply(lemma);

            var prevBases = new ProofBases();
            if (premises == null) {
                return prevBases;
            }

            var ks = knowledgeState;
            if (ruleSupposition != null) {
                ks = new KnowledgeState(knowledgeState.Basis, knowledgeState.Rules);
                AddToKnowledgeState(ks, ruleSupposition);
            }
            
            prevBases.Add(new ProofBasis());
            foreach (var premise in premises) {
                var premiseBases = new ProofBases();
                foreach (var prevBasis in prevBases) {
                    var subbedPremise = premise.Substitute(prevBasis.Substitution);
                    var preMeetBases = GetProofs(
                        subbedPremise,
                        nextTriedExpressions,
                        ks,
                        pt,
                        ruleRequire == null ? require : ruleRequire,
                        isRequireGate: ruleRequire != null,
                        supposition: ruleSupposition);
                    var productBases = new ProofBases();
                    foreach (var preMeetBasis in preMeetBases) {
                        productBases.Add(new ProofBasis(prevBasis, preMeetBasis));
                    }
                    premiseBases = ProofBases.Join(premiseBases, productBases);
                }
                prevBases = premiseBases;
            }

            var returnBases = new ProofBases();
            foreach (var prevBasis in prevBases) {
                returnBases.Add(TrimmedSubstitution(lemma, prevBasis));
            }

            return returnBases;
        }

        foreach (var rule in InferenceRule.RULES.Item1) {
            proofs = ProofBases.Join(proofs, GetProofsFromRule(rule));
        }

        foreach (var rules in knowledgeState.Rules.Values) {
            foreach (var rule in rules) {
                proofs = ProofBases.Join(proofs, GetProofsFromRule(rule));
            }
        }

        // factive +
        // M::[df(F, p, x)] |- p => M |- F(p, x)
        if (lemma.Head.Type.Equals(INDIVIDUAL_TRUTH_RELATION)) {
            var df = new Expression(DF,
                new Expression(lemma.Head),
                lemma.GetArgAsExpression(0),
                lemma.GetArgAsExpression(1));
            var p = lemma.GetArgAsExpression(0);

            proofs = ProofBases.Join(proofs,
                GetProofs(p, nextTriedExpressions, knowledgeState, pt, df, isRequireGate: true));
        }

        // de se knowledge transparency
        // M |-  P => M |-  know(P, self)
        // M |/- P => M |- ~know(P, self)
        if (knowledgeState == KS &&
            (lemma.HeadedBy(KNOW) || lemma.PrejacentHeadedBy(NOT, KNOW))) {
            var query = lemma.HeadedBy(NOT) ? lemma.GetArgAsExpression(0) : lemma;

            if (query.GetArgAsExpression(1).Equals(SELF)) {
                proofs = ProofBases.Join(proofs, GetProofs(
                    query.GetArgAsExpression(0),
                    nextTriedExpressions,
                    knowledgeState,
                    pt,
                    require,
                    isFailure: lemma.HeadedBy(NOT)));
            }
        }

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
        if ((lemma.Head is Name) && (lemma.Head as Name).ID == "𝔾" ||
            lemma.HeadedBy(NOT) && (lemma.GetArgAsExpression(0).Head is Name)
            && (lemma.GetArgAsExpression(0).Head as Name).ID == "𝔾") {
            var query = lemma.HeadedBy(NOT) ? lemma.GetArgAsExpression(0) : lemma;
            var head = query.GetArgAsExpression(0);
            var lift = query.GetArgAsExpression(query.NumArgs - 1);
            Argument[] appliedArgs = new Expression[query.NumArgs - 2];
            for (int j = 0; j < appliedArgs.Length; j++) {
                appliedArgs[j] = new Expression(query.GetArgAsExpression(j + 1), lift);
            }
            var ungeached = new Expression(head, appliedArgs);
            if (lemma.HeadedBy(NOT)) {
                ungeached = new Expression(NOT, ungeached);
            }
            proofs = ProofBases.Join(proofs, GetProofs(ungeached, nextTriedExpressions, knowledgeState, pt, require));
        }

        // omega + (inductive proof)
        if (lemma.HeadedBy(OMEGA) || lemma.PrejacentHeadedBy(NOT, OMEGA)) {
            bool neg = lemma.HeadedBy(NOT);
            var query = neg ? lemma.GetArgAsExpression(0) : lemma;

            ProofBases baseProofs = null;

            if (!neg) {
                var fp = new Expression(query.GetArgAsExpression(0), query.GetArgAsExpression(1));
                baseProofs = GetProofs(fp, nextTriedExpressions, knowledgeState, pt, require);    
            }

            var fTest  = new Expression(query.GetArgAsExpression(0), TEST);
            var ffTest = new Expression(query.GetArgAsExpression(0), fTest);

            var ks = new KnowledgeState(knowledgeState.Basis, knowledgeState.Rules);
            AddToKnowledgeState(ks, fTest);

            var inductiveProofs = GetProofs(ffTest, nextTriedExpressions, ks, pt, fTest,
                isRequireGate: true, supposition: fTest, isFailure: neg);

            var nextProofs = neg ? inductiveProofs : ProofBases.Meet(baseProofs, inductiveProofs);

            proofs = ProofBases.Join(proofs, nextProofs);
        }

        if (lemma.Equals(require)) {
            var reqProofs = new ProofBases();
            foreach (var basis in proofs) {
                var reqPremises = new List<Expression>(basis.Premises);
                reqPremises.Add(new Expression(REQUIRE, require));
                reqProofs.Add(new ProofBasis(reqPremises, basis.Substitution));
            }
            proofs = reqProofs;
        }

        if (isRequireGate) {
            var filteredProofs = new ProofBases();
            var req = new Expression(REQUIRE, require);
            foreach (var basis in proofs) {
                var filteredPremises = new List<Expression>(basis.Premises);
                if (filteredPremises.Remove(req)) {
                    filteredProofs.Add(new ProofBasis(filteredPremises, basis.Substitution));    
                }
                
            }
            proofs = filteredProofs;
        }

        if (supposition != null) {
            var trimmedProofs = new ProofBases();
            foreach (var basis in proofs) {
                var trimmedPremises = new List<Expression>(basis.Premises);
                trimmedPremises.Remove(supposition);
                trimmedProofs.Add(new ProofBasis(trimmedPremises, basis.Substitution));
            }

            proofs = trimmedProofs;
        }

        if (isFailure) {
            if (proofs.IsEmpty()) {
                var basis = new ProofBasis();
                if (supposition != null) {
                    basis.AddPremise(new Expression(NOT, new Expression(IF, lemma, supposition)));
                } else if (isRequireGate && require != null) {
                    basis.AddPremise(new Expression(NOT, new Expression(THEREFORE, lemma, require)));
                } else {
                    basis.AddPremise(new Expression(STAR, lemma));        
                }
                
                proofs.Add(basis);
            } else {
                proofs = new ProofBases();
            }
        }

        return proofs;
    }

    public ProofBases GetProofs(Expression lemma, ProofType pt = Proof) {
        return GetProofs(lemma, new HashSet<Expression>(), KS, pt, null);
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

    public bool AddToKnowledgeState(KnowledgeState knowledgeState, Expression knowledge, bool firstCall = true, Expression trace = null) {
        if (firstCall) {
            numCalls = 0;
        }
        numCalls++;
        if (numCalls > MAX_CALLS) {
            return false;
        }
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

        foreach (var rule in InferenceRule.RULES.Item2) {
            var instantiatedRule = rule.Instantiate(knowledge);
            if (instantiatedRule != null) {
                AddRule(knowledgeState, signature, instantiatedRule);
                var negKnowledge = knowledge.HeadedBy(NOT) ? knowledge.GetArgAsExpression(0) : new Expression(NOT, knowledge);
                foreach (var conclusion in instantiatedRule.Conclusions) {
                    if (conclusion.Matches(negKnowledge)) {
                        continue;
                    }
                    if (conclusion.Head is not Variable) {
                        AddToKnowledgeState(knowledgeState, conclusion, false, signature);
                    }
                }
            }
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
            AddRule(knowledgeState, pIfDf,
                new InferenceRule(new List<Expression>{knowledge}, new List<Expression>{pIfDf}));
        }

        if (knowledge.HeadedBy(OMEGA)) {
            var st = new Expression(GetUnusedVariable(TRUTH_VALUE, knowledge.GetVariables()));
            AddRule(knowledgeState, signature,
                new InferenceRule(new List<Expression>{st},
                    new List<Expression>{new Expression(knowledge.GetArgAsExpression(0), st)}));
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
            AddRule(knowledgeState, signature,
                new InferenceRule(new List<Expression>{knowledge}, new List<Expression>{ungeached}));
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
    public void ReceiveAssertion(Expression content, Expression speaker) {
        AddToKnowledgeState(KS, new Expression(MAKE, new Expression(INFORMED, content, SELF), speaker));
    }

    public void ReceiveRequest(Expression content, Expression speaker) {
        // the proposition we add here, we want to be the equivalent to
        // knowledge in certain ways. So, for example, knows(p, S) -> p
        // in the same way that X(p, S) -> good(p).
        // 
        // Right now, we literally have this as S knows that p is good,
        // but this feels somehow not aesthetically pleasing to me. I'll
        // try it out for now.
        ReceiveAssertion(new Expression(OMEGA, VERY, new Expression(GOOD, content)), speaker);
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

    public List<Expression> EstimateValueFor(Expression goal, List<Expression> goods) {
        // first, we get all the goods we can prove from
        // our goal. These are in the running to approximate
        // the value of the goal.
        List<Expression> goodsFromGoal = new List<Expression>();
        foreach (var good in goods) {
            var goodFromGoalBases = GetProofs(new Expression(IF, good, goal));

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
                var infFromGoodBases = GetProofs(new Expression(IF, oldInfimum, good));

                if (!infFromGoodBases.IsEmpty()) {
                    continue;
                }

                // we add the old infimum back in, since it was
                // not supplanted this round.
                newInfimums.Add(oldInfimum);

                // next we see if the old beats the good at issue.
                // if it does, then we stop checking, as this
                // good will not be used in our estimation.
                var goodFromInfBases = GetProofs(new Expression(IF, good, oldInfimum));

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

        return oldInfimums;
    }

    public List<Expression> DecideCurrentPlan() {
        // we're going to get our domain of goods by trying to prove
        // good(p) and seeing what it assigns to p.
        var goodProofs = GetProofs(new Expression(GOOD, ST));

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

            var proofBases = GetProofs(good, Proof);

            if (!proofBases.IsEmpty()) {
                continue;
            }

            var planBases = GetProofs(good, Plan);

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
                        if (premise.HeadedBy(WILL)) {
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

                    var benefitEstimates = EstimateValueFor(Conjunctify(benefitConjunction), goods);
                    var costEstimates = EstimateValueFor(Conjunctify(costConjunction), goods);

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
        return bestPlan;
    }
}
