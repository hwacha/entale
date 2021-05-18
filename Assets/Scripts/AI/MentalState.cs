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

    // changes all time indices to zero
    public static Expression ZeroTimeIndices(Expression e) {
        var newHead = e.Head;
        if (e.Head.Type.Equals(TIME)) {
            newHead = new Parameter(TIME, 0);
        }
        var newArgs = new Argument[e.NumArgs];
        for (int i = 0; i < e.NumArgs; i++) {
            var arg = e.GetArg(i);
            if (arg is Empty) {
                newArgs[i] = arg;
            } else {
                newArgs[i] = ZeroTimeIndices(arg as Expression);
            }
        }
        return new Expression(new Expression(newHead), newArgs);
    }

    // helper funtion for StreamBases().
    // checks two geached, complex evidentials against one another.
    // 
    // if, for example, we have
    // knows(knows(p, a), b) being checked against knows(knows(knows(p, a), b), c),
    // we want this to return true (along with any other subsequence of a, b, c).
    private IEnumerator EvidentialContains(
        Expression query,
        Expression knowledge,
        Container<bool> match,
        Container<bool> done) {

        // Debug.Log("query: " + query + ", knowledge: " + knowledge);

        var currentQuery = query;
        var currentKnowledge = knowledge;

        while (true) {
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            // Debug.Log("query: " + currentQuery + ", knowledge: " + currentKnowledge);

            // this means the knowledge evidential is the last
            // level before the content.
            // 
            // In this case, the only way for the query to match
            // is to equal the knowledge.
            if (!currentKnowledge.Head.Equals(GEACH_T_TRUTH_FUNCTION.Head)) {
                match.Item = currentQuery.GetMatches(currentKnowledge).Count > 0;
                done.Item = true;
                yield break;
            }

            // the knowledge clause at the top level of the knowledge-base evidential.
            var topLevelKnowledge = currentKnowledge.GetArgAsExpression(0);

            // does the query evidential have more than one clause left?
            bool isQueryDeep = currentQuery.Head.Equals(GEACH_T_TRUTH_FUNCTION.Head);

            // we check either the whole sentence,
            // if this is the last clause,
            // or just the top-level clause.
            var topLevelQuery = isQueryDeep ? currentQuery.GetArgAsExpression(0) : currentQuery;

            bool topLevelMatch = topLevelQuery.GetMatches(topLevelKnowledge).Count > 0;

            // another base case:
            // no more query levels, so if we
            // matched, we're good. Keep going
            // if we didn't match.
            if (!isQueryDeep && topLevelMatch) {
                match.Item = true;
                done.Item = true;
                yield break;
            }

            // we peel off a query level if we matched.
            if (topLevelMatch) {
                currentQuery = currentQuery.GetArgAsExpression(1);    
            }

            // we peel of a knowledge level, regardless.
            currentKnowledge = currentKnowledge.GetArgAsExpression(1);
        }

        // we shouldn't ever get here,
        // but just exit out if we do.
        done.Item = true;
        yield break;
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
            Tense tense = Tense.Present) {
            Lemma = lemma;
            Depth = depth;
            Parent = parent;
            MeetBasisIndex = meetBasisIndex;
            OlderSibling = olderSibling;
            Supplement = supplement;
            IsAssumption = isAssumption;
            Tense = tense;

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

                    // the bases we get from directly
                    // querying the knowledge base.
                    var searchBases = new ProofBases();

                    // inertial tensed query
                    // @Note: in this iteration, direct, tensed queries
                    // can only take place on an evidentialized sentence.
                    // All others go for regular containment.
                    if (currentLemma.Head.Type.Equals(EVIDENTIAL_FUNCTION)) {
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

                        

                        var valence = bottom.GetArgAsExpression(2);
                        var negation = valence.Equals(TRULY) ? NOT : TRULY;
                        
                        var zero = new Expression(
                                new Expression(bottom.Head),
                                bottom.GetArgAsExpression(0),
                                new Expression(new Parameter(TIME, 0)),
                                bottom.GetArgAsExpression(2),
                                bottom.GetArgAsExpression(3));

                        var tHorizon = new Expression(
                            new Expression(top.Head),
                            top.GetArgAsExpression(0),
                            new Expression(new Top(TIME)),
                            top.GetArgAsExpression(2),
                            top.GetArgAsExpression(3));

                        SortedSet<Expression> timespan;
                        IEnumerable<Expression> iter;

                        if (current.Tense == Tense.Present || current.Tense == Tense.Past) {
                            timespan = KnowledgeBase.GetViewBetween(zero, top);
                            iter = timespan.Reverse();
                        } else {
                            // future tense
                            timespan = KnowledgeBase.GetViewBetween(bottom, tHorizon);
                            iter = timespan;
                        }

                        Expression currentContent = null;
                        var admissible = true;

                        // we go through the timespan of samples in reverse
                        // order. All positive samples more recent than
                        // the most recent negative sample
                        // entail the target sentence (in present tense).
                        // 
                        // For past tense, any positive sample does.
                        foreach (var sample in iter) {
                            var sampleContent = sample.GetArgAsExpression(0);

                            if (currentContent == null ||
                               !currentContent.Equals(sampleContent)) {
                                currentContent = sampleContent;
                                admissible = true;
                            }

                            if (!admissible) {
                                continue;
                            }

                            var detensedCurrentLemma = new Expression(
                                new Expression(currentLemma.Head),
                                currentLemma.GetArgAsExpression(0),
                                new Expression(GetUnusedVariable(TIME, currentLemma.GetVariables())),
                                currentLemma.GetArgAsExpression(2),
                                currentLemma.GetArgAsExpression(3));

                            // pattern match the evidential parameter.
                            // 
                            // TODO figure out how match against the geached evidential
                            // parameters. Not working yet.
                            var evidentialContainsMatch = new Container<bool>(false);
                            var evidentialContainsDone = new Container<bool>(false);

                            StartCoroutine(EvidentialContains(
                                query: detensedCurrentLemma.GetArgAsExpression(3),
                                knowledge: sample.GetArgAsExpression(3),
                                match: evidentialContainsMatch,
                                done: evidentialContainsDone));

                            while (!evidentialContainsDone.Item) {
                                yield return null;
                            }

                            // if we do match the pattern, then the content
                            // and mode of evidence both match with contraries.
                            // 
                            // None of the evidence before this point is admissible.
                            // 
                            // @Note is this right for when it comes
                            // to evidence from other subjects?
                            // 
                            if (sample.GetArgAsExpression(2).Equals(negation) &&
                                evidentialContainsMatch.Item) {
                                if (current.Tense == Tense.Present) {
                                    admissible = false;
                                }
                                continue;
                            }

                            // Here, the evidential doesn't match, so we skip over it.
                            // This is for when we want to know not just P but whether
                            // a given subject X knows P.
                            // 
                            // Earlier evidence is still admissible
                            // if we pass over it, though.
                            if (!evidentialContainsMatch.Item) {
                                continue;
                            }

                            // TODO figure out a way to gather the variables
                            // for formulas, but not in a way that makes it
                            // so that knows(p, x) can fix the source.
                            // (unification doesn't work on its own since
                            // the timestamps won't be identitical)
                            var basis = new ProofBasis();
                            basis.AddPremise(sample);
                            searchBases.Add(basis);
                        }
                    }

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
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i, tense: current.Tense));
                            exhaustive = false;
                        }
                        
                        // we transform a simple expression with
                        // negation, tense, and knowledge as adjuncts into one where
                        // they are explicit parameters.
                        //
                        // This allows all knowledge sources to be adjacent in
                        // the expression ordering, sentences and their negations
                        // to be adjacent, and for tensed expressions to be in
                        // chronological order
                        // 
                        // TODO: make a general evidentialize() method that takes
                        // a simple sentence and gives its evidentialized form.
                        if (currentLemma.Head.Type.Equals(EVIDENTIALIZER.Head.Type)) {
                            var content = currentLemma.GetArgAsExpression(0);

                            // here, we're flipping the parity of the evidential
                            // and trimming off 'not's from the content sentence
                            if (content.Head.Equals(NOT.Head)) {
                                var parity = currentLemma.GetArgAsExpression(2);
                                var negation = parity.Equals(TRULY) ? NOT : TRULY;

                                var negatedEvidential = new Expression(EVIDENTIALIZER,
                                    content.GetArgAsExpression(0),
                                    currentLemma.GetArgAsExpression(1),
                                    negation,
                                    currentLemma.GetArgAsExpression(3));

                                newStack.Push(new ProofNode(negatedEvidential, nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }

                            // Here, we trim off a factive evidential/factive from the content,
                            // geach the current evidential, and apply the trimmed evidential
                            // to the geached evidential
                            // 
                            // @note instead of checking each word that happens to be factive,
                            // should we instead have a distinct type for them?
                            // 
                            // @note @note we could do this with the order of arguments between
                            // subject and content. For the factives, the content could come first,
                            // and for the non-factives, the subject could come first.
                            if (content.Head.Equals(SEE.Head) && currentLemma.GetArgAsExpression(2).Equals(TRULY)) {
                                // note that the parity of the evidential must be
                                // positive to trim the factive evidential off.
                                var subContent = content.GetArgAsExpression(0);

                                var geachedEvidential =
                                    new Expression(GEACH_T_TRUTH_FUNCTION,
                                            currentLemma.GetArgAsExpression(3));

                                newStack.Push(new ProofNode(
                                    new Expression(EVIDENTIALIZER, subContent,
                                    currentLemma.GetArgAsExpression(1),
                                    currentLemma.GetArgAsExpression(2),
                                    new Expression(geachedEvidential,
                                        new Expression(SEE,
                                            new Empty(TRUTH_VALUE),
                                            content.GetArgAsExpression(1)))),
                                nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }

                            if (content.Head.Equals(KNOW.Head) && currentLemma.GetArgAsExpression(2).Equals(TRULY)) {
                                var subContent = content.GetArgAsExpression(0);

                                var geachedEvidential =
                                    new Expression(GEACH_T_TRUTH_FUNCTION,
                                            currentLemma.GetArgAsExpression(3));

                                newStack.Push(new ProofNode(
                                    new Expression(EVIDENTIALIZER, subContent,
                                        currentLemma.GetArgAsExpression(1),
                                        currentLemma.GetArgAsExpression(2),
                                        new Expression(geachedEvidential,
                                            new Expression(KNOW_TENSED,
                                                new Empty(TRUTH_VALUE),
                                                content.GetArgAsExpression(1),
                                                new Expression(GetUnusedVariable(TIME,
                                                    currentLemma.GetVariables()))))),
                                nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }
                        } else {
                            // here we're checking if there's a factive
                            // evidential, e.g. knows or sees
                            if (currentLemma.Head.Equals(SEE.Head)) {
                                newStack.Push(new ProofNode(
                                    new Expression(EVIDENTIALIZER,
                                        new Expression(currentLemma.GetArgAsExpression(0)),
                                        new Expression(new Parameter(TIME, Timestamp)),
                                        TRULY,
                                        new Expression(SEE, new Empty(TRUTH_VALUE),
                                            currentLemma.GetArgAsExpression(1))),
                                    nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            } else if (currentLemma.Head.Equals(KNOW.Head)) {
                                var evidentializedExpression =
                                    new Expression(EVIDENTIALIZER,
                                            new Expression(currentLemma.GetArgAsExpression(0)),
                                            new Expression(new Parameter(TIME, Timestamp)),
                                            TRULY,
                                            new Expression(KNOW_TENSED, new Empty(TRUTH_VALUE),
                                                currentLemma.GetArgAsExpression(1),
                                                // TODO: change this to be a timestamp as well,
                                                // and change evidential search to do a whole-expression
                                                // replacement of the timestamp.
                                                new Expression(GetUnusedVariable(TIME, currentLemma.GetVariables()))));

                                newStack.Push(new ProofNode(evidentializedExpression, nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            } else {
                                // if not, we just put the whole sentence into the
                                // evidentializer.
                                newStack.Push(new ProofNode(
                                    new Expression(EVIDENTIALIZER,
                                        new Expression(currentLemma),
                                        new Expression(new Parameter(TIME, Timestamp)),
                                        TRULY,
                                        new Expression(new Variable(TRUTH_FUNCTION, 0))),
                                    nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }
                        }

                        // double negation +
                        if (currentLemma.Head.Equals(NOT.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            if (subclause.Head.Equals(NOT.Head)) {
                                var subsubclause = subclause.GetArgAsExpression(0);
                                newStack.Push(new ProofNode(subsubclause, nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }

                            // nonidentity assumption
                            if (subclause.Head.Equals(IDENTITY.Head)) {
                                newStack.Push(new ProofNode(new Expression(NOT, currentLemma),
                                    nextDepth, current, i, isAssumption: true, tense: current.Tense));
                                exhaustive = false;
                            }
                            
                            // NOT: adjunct -> parameter
                            if (!subclause.Head.Type.Equals(EVIDENTIALIZER.Head.Type)) {
                                newStack.Push(new ProofNode(
                                    new Expression(EVIDENTIALIZER,
                                        new Expression(subclause),
                                        new Expression(new Parameter(TIME, Timestamp)),
                                        NOT,
                                        new Expression(new Variable(TRUTH_FUNCTION, 0))),
                                    nextDepth, current, i, tense: current.Tense));
                                exhaustive = false;
                            }
                        }

                        // or +
                        if (currentLemma.Head.Equals(OR.Head)) {
                            var disjunctA = currentLemma.GetArgAsExpression(0);
                            var disjunctB = currentLemma.GetArgAsExpression(1);
                            newStack.Push(new ProofNode(disjunctA, nextDepth, current, i, tense: current.Tense));
                            newStack.Push(new ProofNode(disjunctB, nextDepth, current, i, tense: current.Tense));
                            exhaustive = false;
                        }

                        // and +
                        if (currentLemma.Head.Equals(AND.Head)) {
                            var conjunctA = currentLemma.GetArgAsExpression(0);
                            var conjunctB = currentLemma.GetArgAsExpression(1);

                            var bNode = new ProofNode(conjunctB, nextDepth, current, i, hasYoungerSibling: true, tense: current.Tense);
                            var aNode = new ProofNode(conjunctA, nextDepth, current, i, bNode, tense: current.Tense);

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

                            var gxNode = new ProofNode(gx, nextDepth, current, i, hasYoungerSibling: true, tense: current.Tense);
                            var fxNode = new ProofNode(fx, nextDepth, current, i, gxNode, tense: current.Tense);

                            newStack.Push(fxNode);
                            newStack.Push(gxNode);
                            exhaustive = false;
                        }

                        // past +
                        if (currentLemma.Head.Equals(PAST.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i, tense: Tense.Past));
                        }

                        // future +
                        if (currentLemma.Head.Equals(FUTURE.Head)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            newStack.Push(new ProofNode(subclause, nextDepth, current, i, tense: Tense.Future));
                        }

                        if (currentLemma.Depth <= this.MaxDepth) {
                            // plan -
                            if (pt == Plan) {
                                var able = new Expression(ABLE, currentLemma, SELF);
                                var will = new Expression(WILL, currentLemma);

                                var ableNode = new ProofNode(able, nextDepth, current, i, supplement: will);

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

    private IEnumerator AddToKnowledgeBase(Expression knowledge) {
        var assertionTime = Timestamp;
        // TODO: add a reduction step which reduces
        // and evidentializes the sentence to be added
        KnowledgeBase.Add(knowledge);

        Expression cur = knowledge;
        bool parity = true;
        Expression evidential = null;

        // we turn this statement into an evidentialized form.
        // 
        // TODO: make this smarter, so it doesn't mess up
        // sentences like not(knows, p, x) or
        // knows(~~p, x) and stuff of the sort.
        while (true) {
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            if (cur.Head.Equals(NOT.Head)) {
                cur = cur.GetArgAsExpression(0);
                parity = !parity;
            } else if (cur.Head.Equals(KNOW.Head)) {
                cur = cur.GetArgAsExpression(0);
                var wrapper = new Expression(KNOW_TENSED,
                        new Empty(TRUTH_VALUE),
                        cur.GetArgAsExpression(1),
                        new Expression(new Parameter(TIME, assertionTime)));

                if (evidential == null) {
                    evidential = wrapper;
                } else {
                    evidential = new Expression(GEACH_T_TRUTH_FUNCTION, evidential, wrapper);
                }
            } else if (cur.Head.Equals(SEE.Head)) {
                cur = cur.GetArgAsExpression(0);
                var wrapper = new Expression(SEE,
                    new Empty(TRUTH_VALUE),
                    cur.GetArgAsExpression(1));

                if (evidential == null) {
                    evidential = wrapper;
                } else {
                    evidential = new Expression(GEACH_T_TRUTH_FUNCTION, evidential, wrapper);
                }
            } else {
                break;
            }
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

