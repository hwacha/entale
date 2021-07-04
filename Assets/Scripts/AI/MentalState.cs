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
        Timestamp = 0;
        Locations = new Dictionary<Expression, Vector3>();

        if (KnowledgeBase != null) {
            throw new Exception("Initialize: mental state already initialized.");
        }
        KnowledgeBase = new SortedSet<Expression>();

        for (int i = 0; i < initialKnowledge.Length; i++) {
            AddToKnowledgeBase(initialKnowledge[i]);
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
    // checks two geached, complex factives against one another.
    // 
    // if, for example, we have
    // knows(knows(p, a), b) being checked against knows(knows(knows(p, a), b), c),
    // we want this to return true (along with any other subsequence of a, b, c).
    // 
    public IEnumerator FactiveContains(
        Expression factive,
        Expression content,
        HashSet<Substitution> matches,
        Container<bool> answer,
        Container<bool> parityAligned,
        Container<bool> done) {
        // TODO 6/23
        
        Expression currentFactive = factive;
        Expression currentContent = content;

        int currentFactiveTimestamp = Timestamp;
        Tense currentFactiveTense = Tense.Present;
        Tense currentContentTense = Tense.Present;

        bool encounteredNegativeInFactive = false;

        int factiveParity = 0;
        bool factiveParityLock = false;

        bool contentParity = true;
        bool contentParityLock = false;

        while (true) {
            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                yield return null;
            }

            bool factiveNot = currentFactive.HeadedBy(NOT);
            bool contentNot = currentContent.HeadedBy(NOT);

            // we encounter a negative on both sides.
            if (factiveNot && contentNot) {
                encounteredNegativeInFactive = true;

                if (!factiveParityLock) {
                    factiveParity = 3;
                }

                if (!contentParityLock) {
                    contentParity = false;
                }
                contentParityLock = true;

                currentFactive = currentFactive.GetArgAsExpression(0);
                currentContent = currentContent.GetArgAsExpression(0);
                continue;
            }

            // we encounter a negative in the factive.
            if (factiveNot) {
                // here, we still want to fail if we encounter a mismatch.
                if (encounteredNegativeInFactive) {
                    answer.Item = false;
                    break;
                // we can peel of the negative if we were still
                // in a positive factive up to this point.
                } else {
                    // TODO: soup up the logic
                    // to handle double+ negation.
                    encounteredNegativeInFactive = true;

                    if (!factiveParityLock) {
                        factiveParity = 3;
                    }

                    currentFactive = currentFactive.GetArgAsExpression(0);    
                    continue;
                }              
            }

            // we encounter a negative in the content.
            if (contentNot) {
                // if we've encountered a negative in
                // the factive, then we want to fail here.
                if (encounteredNegativeInFactive) {
                    answer.Item = false;
                    break;
                // otherwise, we carry on as normal.
                } else {
                    if (!contentParityLock) {
                        contentParity = false;
                    }
                    contentParityLock = true;

                    currentContent = currentContent.GetArgAsExpression(0);
                    continue;
                }
            }

            // we just want to skip over tense.
            // 
            // TODO: we should screen for tense-appriopriateness
            // somewhere here, because we want the timestamp
            // checks to be tense-appropriate for the content.
            // 
            // Currently, this will give the wrong results for
            // tensed KNOW claims.
            if (currentFactive.HeadedBy(WHEN, BEFORE, AFTER)) {
                if (currentFactive.HeadedBy(WHEN)) {
                    currentFactiveTense = Tense.Present;
                } else if (currentFactive.HeadedBy(BEFORE)) {
                    currentFactiveTense = Tense.Past;
                } else if (currentFactive.HeadedBy(AFTER)) {
                    currentFactiveTense = Tense.Future;
                }
                // @Note this assumes the timestamp will be a parameter.
                currentFactiveTimestamp = (currentFactive.GetArgAsExpression(1).Head as Parameter).ID;
                if (factiveParity > 0 && !factiveParityLock) {
                    factiveParity--;
                }
                currentFactive = currentFactive.GetArgAsExpression(0);
                continue;
            }
            if (currentContent.HeadedBy(PAST)) {
                // @Note this assumes the timestamp will be a parameter.
                currentContentTense = Tense.Past;
                currentContent = currentContent.GetArgAsExpression(0);
                continue;
            }
            if (currentContent.HeadedBy(PRESENT)) {
                // @Note this assumes the timestamp will be a parameter.
                currentContentTense = Tense.Present;
                currentContent = currentContent.GetArgAsExpression(0);
                continue;
            }
            if (currentContent.HeadedBy(FUTURE)) {
                // @Note this assumes the timestamp will be a parameter.
                currentContentTense = Tense.Future;
                currentContent = currentContent.GetArgAsExpression(0);
                continue;
            }

            // when we match against the factives, we ensure
            // that the tense is properly aligned as well.
            bool tenseAligned = true;
            if (currentContentTense == Tense.Future) {
                if (currentFactiveTense == Tense.Present) {
                    tenseAligned = currentFactiveTimestamp > Timestamp;    
                } else if (currentFactiveTense == Tense.Future) {
                    tenseAligned = currentFactiveTimestamp >= Timestamp;
                }
            } else {
                if (currentFactiveTense == Tense.Present) {
                    tenseAligned = currentFactiveTimestamp <= Timestamp;
                } else if (currentFactiveTense == Tense.Past) {
                    tenseAligned = currentFactiveTimestamp < Timestamp;
                }
            }

            // we have a 'very' on the left side.
            if (currentFactive.HeadedBy(VERY)) {
                if (currentContent.HeadedBy(VERY) && tenseAligned) {
                    currentFactive = currentFactive.GetArgAsExpression(0);
                    currentContent = currentContent.GetArgAsExpression(0);
                    continue;
                } else {
                    currentFactive = currentFactive.GetArgAsExpression(0);
                    continue;
                }
            }

            // we have an factive on the left side.
            if (currentFactive.HeadedBy(KNOW, SEE, MAKE)) {
                // the right side is a matching factive.
                // recur on both sides.
                if (currentFactive.HeadedBy(currentContent) &&
                    // currentFactive.GetArgAsExpression(1).Equals(currentContent.GetArgAsExpression(1)) &&
                    currentContent.GetArgAsExpression(1).GetMatches(currentFactive.GetArgAsExpression(1)).Count > 0 &&
                    tenseAligned) {
                    factiveParityLock = true;
                    contentParityLock = true;

                    currentFactive = currentFactive.GetArgAsExpression(0);
                    currentContent = currentContent.GetArgAsExpression(0);
                    continue;
                // negative factive w/ mismatch.
                // this should return false, because
                // we need the rest of the expression to
                // match once we encounter a negative
                // on the factive side.
                // 
                // @Note this is true for an 'entails P'
                // reading of 'knows P'. If we change to a
                // 'presupposes P' account of 'knows P',
                // this will have to change.
                } else if (encounteredNegativeInFactive) {
                    answer.Item = false;
                    break;
                // on positive mismatch, recur only on the left side.
                } else {
                    if (factiveParity > 0 && !factiveParityLock) {
                        factiveParity--;
                    }
                    currentFactive = currentFactive.GetArgAsExpression(0);
                    continue;
                }
            }

            

            // we've reached a 'dead end' - nothing to peel off -
            // so we see if the rest of the expression matches up,
            // and that our timestamps are aligned according to the given tense.
            matches.UnionWith(currentContent.GetMatches(currentFactive));
            answer.Item = matches.Count > 0 && tenseAligned;
            parityAligned.Item = (factiveParity == 0) == contentParity;
            break;
        }

        done.Item = true;
        yield break;
    }

    private static Expression Reduce(Expression e) {
        // @Note this is only to prevent problems.
        // This should be souped-up later to
        // account for Geaches, etc.
        if (!e.Type.Equals(TRUTH_VALUE)) {
            return e;
        }
        if (e.HeadedBy(TRULY)) {
            return Reduce(e);
        }
        if (e.HeadedBy(NOT)) {
            var subclause = e.GetArgAsExpression(0);
            if (subclause.HeadedBy(NOT)) {
                return Reduce(subclause.GetArgAsExpression(0));
            }
        }

        var reducedArgs = new Argument[e.NumArgs];
        for (int i = 0; i < e.NumArgs; i++) {
            if (e.GetArg(i) is Empty) {
                reducedArgs[i] = e.GetArg(i);
            } else {
                reducedArgs[i] = Reduce(e.GetArgAsExpression(i));
            }
        }

        return new Expression(new Expression(e.Head), reducedArgs);
    }

    private static Expression Tensify(Expression e, bool lockTense = false) {
        if (!e.Type.Equals(TRUTH_VALUE)) {
            return e;
        }

        bool newLockTense = false;
        bool dontTenseTopLevel = false;

        if (e.HeadedBy(PAST, PRESENT, FUTURE, WHEN, NOT,
            GEACH_E_TRUTH_FUNCTION, GEACH_T_TRUTH_FUNCTION)) {
            newLockTense = true;
            dontTenseTopLevel = true;
        }

        // if (e.HeadedBy(VERY)) {
        //     newLockTense = true;
        // }

        if (e.HeadedBy(ABLE, GOOD)) {
            return e;
        }

        if (e.HeadedBy(AND, OR, IF, SOME, ALL, VERY)) {
            dontTenseTopLevel = true;
        }

        Argument[] tensedArgs = new Argument[e.NumArgs];
        for (int i = 0; i < e.NumArgs; i++) {
            var arg = e.GetArg(i);
            if (arg is Empty) {
                tensedArgs[i] = arg;
            } else {
                tensedArgs[i] = Tensify(arg as Expression, newLockTense);
            }
        }
        var content = new Expression(new Expression(e.Head), tensedArgs);

        // @Note we may want to implement 'sticky tense' in which
        // we would keep the tense the same for subclauses until
        // stated otherwise. This may be most intuitive.
        return lockTense || dontTenseTopLevel ? content : new Expression(PRESENT, content);
    }

    private static Expression Timeify(Expression e) {
        if (e.HeadedBy(PAST, PRESENT, FUTURE)) {
            var timeifiedArg = Timeify(e.GetArgAsExpression(0));

            var head = WHEN;
            if (e.HeadedBy(PAST)) {
                head = BEFORE;
            }
            if (e.HeadedBy(FUTURE)) {
                head = AFTER;
            }
            return new Expression(head, timeifiedArg,
                new Expression(GetUnusedVariable(TIME, timeifiedArg.GetVariables())));
        }

        Argument[] timeifiedArgs = new Argument[e.NumArgs];
        for (int i = 0; i < e.NumArgs; i++) {
            var arg = e.GetArg(i);
            if (arg is Empty) {
                timeifiedArgs[i] = arg;
            } else {
                timeifiedArgs[i] = Timeify(arg as Expression);
            }
        }
        return new Expression(new Expression(e.Head), timeifiedArgs);
    }

    private Expression AddCurrentTimestamp(Expression e) {
        if (e.HeadedBy(PAST, PRESENT, FUTURE)) {
            var timestampedArg = AddCurrentTimestamp(e.GetArgAsExpression(0));

            var head = WHEN;
            if (e.HeadedBy(PAST)) {
                head = BEFORE;
            }
            if (e.HeadedBy(FUTURE)) {
                head = AFTER;
            }
            return new Expression(head,
                timestampedArg,
                new Expression(new Parameter(TIME, Timestamp)));
        }

        Argument[] timestampedArgs = new Argument[e.NumArgs];
        for (int i = 0; i < e.NumArgs; i++) {
            var arg = e.GetArg(i);
            if (arg is Empty) {
                timestampedArgs[i] = arg;
            } else {
                timestampedArgs[i] = AddCurrentTimestamp(arg as Expression);
            }
        }
        return new Expression(new Expression(e.Head), timestampedArgs);
    }

    private static Expression GetContent(Expression e) {
        var cur = e;
        while (cur.HeadedBy(KNOW, SEE, MAKE, WHEN, NOT)) {
            cur = cur.GetArgAsExpression(0);
        }
        return cur;
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
                
                var sends = new List<KeyValuePair<ProofBases, bool>>();
 
                for (int i = 0; i < current.YoungerSiblingBases.Count; i++) {
                    if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                        yield return null;
                    }

                    var youngerSiblingBasis = current.YoungerSiblingBases[i];

                    var currentLemma = current.Lemma.Substitute(youngerSiblingBasis.Substitution);

                    // currentLemma = Tensify(currentLemma);

                    // the bases we get from directly
                    // querying the knowledge base.
                    var searchBases = new ProofBases();

                    // inertial tensed query
                    if (currentLemma.HeadedBy(PAST, PRESENT, FUTURE)) {
                        var boundedCurrentLemma = Timeify(currentLemma);
                        var variables = boundedCurrentLemma.GetVariables();
                        var bottomSubstitution = new Substitution();
                        var topSubstitution = new Substitution();
                        foreach (Variable v in variables) {
                            bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
                            topSubstitution.Add(v, new Expression(new Top(v.Type)));
                        }
                        var bottom = boundedCurrentLemma.Substitute(bottomSubstitution);
                        var top = boundedCurrentLemma.Substitute(topSubstitution);

                        var beginning = new Expression(WHEN,
                            bottom.GetArgAsExpression(0),
                            new Expression(new Bottom(TIME)));

                        var end = new Expression(WHEN,
                            top.GetArgAsExpression(0),
                            new Expression(new Top(TIME)));

                        var pastPresent = new Expression(WHEN,
                            top.GetArgAsExpression(0),
                            new Expression(new Parameter(TIME, Timestamp)));

                        var futurePresent = new Expression(WHEN,
                            bottom.GetArgAsExpression(0),
                            new Expression(new Parameter(TIME, Timestamp)));

                        SortedSet<Expression> timespan;
                        IEnumerable<Expression> iter;

                        Debug.Log(beginning + " | " + pastPresent);

                        if (currentLemma.HeadedBy(PAST, PRESENT)) {
                            timespan = KnowledgeBase.GetViewBetween(beginning, pastPresent);
                            iter = timespan.Reverse();
                        } else {
                            timespan = KnowledgeBase.GetViewBetween(futurePresent, end);
                            iter = timespan;
                        }

                        bool admissible = true;
                        Expression currentContent = null;
                        foreach (var sample in iter) {
                            if (FrameTimer.FrameDuration >= TIME_BUDGET) {
                                yield return null;
                            }

                            Debug.Log(sample);

                            // @Note we assume as an invariant that
                            // each sample we encounter is tensed.
                            
                            // here, we check if we can reset the
                            // admissibility of a sample because
                            // its content is different
                            // e.g. when matching against a formula
                            var sampleContent = GetContent(sample);
                            if (currentContent == null ||
                                !currentContent.Equals(sampleContent)) {
                                admissible = true;
                                currentContent = sampleContent;
                            }

                            if (!admissible) {
                                continue;
                            }
                            
                            // here, we see if the factive
                            // lines up with our expectations.
                            var matches = new HashSet<Substitution>();
                            var factiveContains = new Container<bool>(false);
                            var parityAligned = new Container<bool>(true);
                            var fcDone = new Container<bool>(false);

                            StartCoroutine(FactiveContains(
                                sample,
                                currentLemma,
                                matches,
                                factiveContains,
                                parityAligned,
                                fcDone));

                            while (!fcDone.Item) {
                                yield return null;
                            }

                            if (factiveContains.Item) {
                                // we have a match!
                                if (parityAligned.Item) {
                                    foreach (var match in matches) {
                                        searchBases.Add(new ProofBasis(new List<Expression>{sample}, match));
                                    }
                                // this means the sample we're looking at
                                // contradicts the query.
                                } else if (currentLemma.HeadedBy(PRESENT)) {
                                    admissible = false;
                                }
                            }
                        }
                    } else {
                        // if the expression isn't tensed, simply search for it.
                        Expression bottom = null;
                        Expression top = null;

                        var variables = currentLemma.GetVariables();

                        if (variables.Count > 0) {
                            var bottomSubstitution = new Substitution();
                            var topSubstitution = new Substitution();
                            foreach (Variable v in variables) {
                                bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
                                topSubstitution.Add(v, new Expression(new Top(v.Type)));
                            }

                            bottom = currentLemma.Substitute(bottomSubstitution);
                            top    = currentLemma.Substitute(topSubstitution);
                        } else {
                            bottom = currentLemma;
                            top = new Expression(WHEN, currentLemma, new Expression(new Top(TIME)));
                        }

                        var range = KnowledgeBase.GetViewBetween(bottom, top);

                        foreach (var e in range) {
                            var matches = new HashSet<Substitution>();
                            var factiveContains = new Container<bool>(false);
                            var parityAligned = new Container<bool>(true);
                            var fcDone = new Container<bool>(false);
                            StartCoroutine(FactiveContains(
                                e,
                                currentLemma,
                                matches,
                                factiveContains,
                                parityAligned,
                                fcDone));

                            while (!fcDone.Item) {
                                yield return null;
                            }

                            if (factiveContains.Item && parityAligned.Item) {
                                foreach (var match in matches) {
                                    searchBases.Add(new ProofBasis(new List<Expression>{e}, match));
                                }
                            }
                        }
                    }

                    // if any of the following rules apply to sentence
                    // forms that also take tense,
                    // we shave off the tense here.
                    if (currentLemma.HeadedBy(PAST, PRESENT, FUTURE)) {
                        currentLemma = currentLemma.GetArgAsExpression(0);
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
                        if (currentLemma.HeadedBy(ABLE) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(SAY) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(1).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // I can go anywhere.
                        if (currentLemma.HeadedBy(ABLE) &&
                            currentLemma.GetArgAsExpression(1).Equals(SELF) &&
                            currentLemma.GetArgAsExpression(0).HeadedBy(AT) &&
                            currentLemma.GetArgAsExpression(0).GetArgAsExpression(0).Equals(SELF)) {
                            var basis = new ProofBasis();
                            basis.AddPremise(currentLemma);
                            searchBases.Add(basis);
                        }

                        // if a and b are within 5 meters of each other,
                        // then at(a, b).
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
                                subclause,
                                nextDepth,
                                current,
                                i,
                                parity: current.Parity));
                            exhaustive = false;
                        }

                        // negation: toggle parity
                        if (currentLemma.HeadedBy(NOT)) {
                            var subclause = currentLemma.GetArgAsExpression(0);
                            // nonidentity assumption
                            if (subclause.HeadedBy(IDENTITY)) {
                                newStack.Push(new ProofNode(subclause,
                                    nextDepth, current, i,
                                    isAssumption: true,
                                    parity: current.Parity));
                                exhaustive = false;
                            } else {
                                newStack.Push(new ProofNode(
                                    subclause, nextDepth, current, i,
                                    parity: !current.Parity));
                                exhaustive = false;
                            }
                        }

                        // or +, ~and +
                        if (currentLemma.HeadedBy(OR)  &&  current.Parity ||
                            currentLemma.HeadedBy(AND) && !current.Parity) {
                            var a = currentLemma.GetArgAsExpression(0);
                            var b = currentLemma.GetArgAsExpression(1);
                            newStack.Push(new ProofNode(a, nextDepth, current, i));
                            newStack.Push(new ProofNode(b, nextDepth, current, i));
                            exhaustive = false;
                        }

                        // and +, ~or +
                        if (currentLemma.HeadedBy(AND) &&  current.Parity ||
                            currentLemma.HeadedBy(OR)  && !current.Parity) {
                            var a = currentLemma.GetArgAsExpression(0);
                            var b = currentLemma.GetArgAsExpression(1);

                            var bNode = new ProofNode(b, nextDepth, current, i,
                                hasYoungerSibling: true);
                            var aNode = new ProofNode(a, nextDepth, current, i, bNode);

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

                            var gxNode = new ProofNode(gx, nextDepth, current, i,
                                hasYoungerSibling: true);
                            var fxNode = new ProofNode(fx, nextDepth, current, i, gxNode);

                            newStack.Push(fxNode);
                            newStack.Push(gxNode);
                            exhaustive = false;
                        }

                        // M |- P => M |- know(P, self)
                        // M |/- P => not(know(P, self))
                        if (currentLemma.HeadedBy(KNOW) && currentLemma.GetArgAsExpression(1).Equals(SELF)) {
                            newStack.Push(new ProofNode(
                                currentLemma.GetArgAsExpression(0),
                                nextDepth, current, i,
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
                            
                            var f1xNode = new ProofNode(f1xFormula,
                                    nextDepth, current, i,
                                    hasYoungerSibling: true,
                                    parity: current.Parity);

                            var allNode = new ProofNode(allF1F2,
                                nextDepth, current, i, f1xNode, parity: current.Parity);

                            newStack.Push(allNode);
                            newStack.Push(f1xNode);

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

                            newStack.Push(new ProofNode(
                                geachedTfx,
                                nextDepth, current, i,
                                parity: current.Parity));

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

                            newStack.Push(new ProofNode(
                                geachedTf1tf2t,
                                nextDepth, current, i,
                                parity: current.Parity));

                            exhaustive = false;
                        }

                        if (currentLemma.Depth <= this.MaxDepth) {
                            // plan -
                            if (pt == Plan && !currentLemma.HeadedBy(ABLE)) {
                                var able = new Expression(ABLE, currentLemma, SELF);
                                var will = new Expression(WILL, currentLemma);

                                var ableNode = new ProofNode(able, nextDepth, current, i,
                                    supplement: will,
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
                                assumptionBasis.AddPremise(
                                    new Expression(NOT, new Expression(KNOW, mergeLemma, SELF)));

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

        // we need to queue this up so that it doesn't cause a
        // concurrent modification problem.
        
        AddToKnowledgeBase(percept);

        return param;
    }

    // @Note: this should be private ultimately.
    // public for testing purposes.
    public bool AddToKnowledgeBase(Expression knowledge) {
        Debug.Assert(knowledge.Type.Equals(TRUTH_VALUE));
        var modifiedFormKnowledge = AddCurrentTimestamp(Tensify(Reduce(knowledge)));

        if (KnowledgeBase.Contains(modifiedFormKnowledge)) {
            return false;
        }

        KnowledgeBase.Add(modifiedFormKnowledge);
        if (modifiedFormKnowledge.Depth > MaxDepth) {
            MaxDepth = modifiedFormKnowledge.Depth;
        }
        return true;
    }

    // a direct assertion.
    // @TODO add an inference rule to cover knowledge from
    // assertion. Now is a simple fix.
    public bool ReceiveAssertion(Expression content, Expression speaker) {
        return AddToKnowledgeBase(new Expression(KNOW, content, speaker));
        // TODO check to see if this is inconsistent
        // with the current knowledge base
    }

    public bool ReceiveRequest(Expression content, Expression speaker) {
        // the proposition we add here, we want to be the equivalent to
        // knowledge in certain ways. So, for example, knows(p, S) -> p
        // in the same way that X(p, S) -> good(p).
        // 
        // Right now, we literally have this as S knows that p is good,
        // but this feels somehow not aesthetically pleasing to me. I'll
        // try it out for now.
        return AddToKnowledgeBase(new Expression(KNOW, new Expression(GOOD, content), speaker));
    }

    public static Expression Conjunctify(List<Expression> set) {
        Expression conjunction = null;
        for (int i = set.Count - 1; i >= 0; i--) {
            var conjunct = set[i];
            if (conjunction == null) {
                conjunction = conjunct;
            } else {
                conjunction = new Expression(AND, conjunct, conjunction);    
            }
        }
        return conjunction;
    }

    // @Note accessibility for testing purposes
    public IEnumerator FindMostSpecificConjunction(List<Expression> conjunction,  List<List<Expression>> result, Container<bool> done) {
        var currentGeneration =
            new List<KeyValuePair<int, List<Expression>>>{
                new KeyValuePair<int, List<Expression>>(0, conjunction)};

        while (currentGeneration.Count > 0) {
            var nextGeneration = new List<KeyValuePair<int, List<Expression>>>();
            foreach (var generation in currentGeneration) {
                int lastRemovedIndex = generation.Key;
                var subconjunction   = generation.Value;

                // we have to generate all combinations of negations
                // of each conjunct in the subconjunction.
                var currentNegationList = new List<List<Expression>>{subconjunction};
                var nextNegationList = new List<List<Expression>>();
                for (int i = 0; i < subconjunction.Count; i++) {
                    foreach (var cur in currentNegationList) {
                        var subconjunct = cur[i];

                        Expression negation;
                        if (subconjunct.HeadedBy(NOT)) {
                            negation = subconjunct.GetArgAsExpression(0);
                        } else {
                            negation = new Expression(NOT, subconjunct);
                        }
                        var neg = new List<Expression>(cur);
                        neg[i] = negation;

                        nextNegationList.Add(cur);
                        nextNegationList.Add(neg);
                    }
                    currentNegationList = nextNegationList;
                    nextNegationList = new List<List<Expression>>();
                }

                bool lockNextGen = false;
                foreach (var subconjunctionNegation in currentNegationList) {
                    bool isSubset = false;
                    foreach (var res in result) {
                        if (!subconjunctionNegation.Except(res).Any()) {
                            isSubset = true;
                            break;
                        }
                    }

                    if (isSubset) {
                        // we have a more specific conjunction in the result
                        // already, so we want to stop searching for this
                        // negation set altogether.
                        break;
                    } else {
                        var subconjunctionNegationBases = new ProofBases();
                        var subconjunctionNegationDone = new Container<bool>(false);                    

                        StartCoroutine(StreamProofs(
                            subconjunctionNegationBases,
                            new Expression(GOOD, Conjunctify(subconjunctionNegation)),
                            subconjunctionNegationDone));

                        while (!subconjunctionNegationDone.Item) {
                            yield return null;
                        }

                        if (!subconjunctionNegationBases.IsEmpty()) {
                            result.Add(subconjunctionNegation);

                            // remove from the next generation any potential
                            // subconjunctions of this conjunction, if they
                            // were already added.
                            for (int i = 0; i < lastRemovedIndex; i++) {
                                var removeOne = new List<Expression>(subconjunction);
                                removeOne.RemoveAt(i);
                                
                                KeyValuePair<int, List<Expression>> toRemoveFromNextGeneration =
                                    default(KeyValuePair<int, List<Expression>>);

                                foreach (var info in nextGeneration) {
                                    if (info.Value.SequenceEqual(removeOne)) {
                                        toRemoveFromNextGeneration = info;
                                        break;    
                                    }
                                }

                                if (!toRemoveFromNextGeneration.Equals(default(KeyValuePair<int, List<Expression>>))) {
                                    nextGeneration.Remove(toRemoveFromNextGeneration);
                                }
                            }
                            // these alternatives are all mutually incompatible,
                            // so if we found it, we break out of this loop.
                            break;
                        } else if (!lockNextGen) {
                            for (int i = lastRemovedIndex; i < subconjunction.Count; i++) {
                                var removeOne = new List<Expression>(subconjunction);
                                removeOne.RemoveAt(i);
                                if (removeOne.Count > 0) {
                                    nextGeneration.Add(new KeyValuePair<int, List<Expression>>(i, removeOne));    
                                }
                            }
                            lockNextGen = true;
                        }
                    }
                }
            }
            currentGeneration = nextGeneration;
        }

        done.Item = true;
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

        int bestTotalValue = 0;
        List<Expression> bestPlan = new List<Expression>{new Expression(WILL, NEUTRAL)};

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
                foreach (var basis in planBases) {
                    List<Expression> benefitConjunction = new List<Expression>();
                    List<Expression> resolutions = new List<Expression>();

                    // @Note the assumption here is that, because the goal
                    // is good, bringing it about should automatically
                    // confer some value. This isn't true, however, if
                    // the plan contains a resolution which _entails_
                    // the goal but is itself disvalued.
                    // 
                    // Currently, that confers an overall value of 0,
                    // as the positive and negative are both counted.
                    // However, it should count as -1.
                    // 
                    // TODO figure out how to check for this case
                    int localBestValue = 1;
                    
                    foreach (var premise in basis.Premises) {
                        if (premise.Type.Equals(CONFORMITY_VALUE)) {
                            resolutions.Add(premise);

                            var collateral = premise.GetArgAsExpression(0);

                            var benefit = collateral;
                            var makeBenefit = new Expression(MAKE, collateral, SELF);

                            benefitConjunction.Add(benefit);
                            benefitConjunction.Add(makeBenefit);
                        }
                    }

                    var benefitResult = new List<List<Expression>>();
                    var benefitDone = new Container<bool>(false);
                    StartCoroutine(FindMostSpecificConjunction(benefitConjunction, benefitResult, benefitDone));

                    while (!benefitDone.Item) {
                        yield return null;
                    }

                    
                    // sum up the value of the most specific subconjunctions for this plan.
                    int benefitValue = 0;
                    foreach (var conjunction in benefitResult) {
                        var conjunctIndex = 0;
                        bool isNegation = false;

                        // here, we check if the subconjunction we found
                        // contradicts the resolution.
                        foreach (var benefitConjunct in benefitConjunction) {
                            var benefitContent = benefitConjunct;
                            bool benefitParity = true;
                            if (benefitConjunct.HeadedBy(NOT)) {
                                benefitContent = benefitConjunct.GetArgAsExpression(0);
                                benefitParity = false;
                            }

                            var conjunct = conjunction[conjunctIndex];
                            var conjunctContent = conjunct;
                            bool conjunctParity = true;
                            if (conjunct.HeadedBy(NOT)) {
                                conjunctContent = conjunct.GetArgAsExpression(0);
                                conjunctParity = false;
                            }

                            if (benefitContent.Equals(conjunctContent)) {
                                if (benefitParity == conjunctParity) {
                                    ++conjunctIndex;
                                    if (conjunctIndex >= conjunction.Count) {
                                        break;
                                    }
                                    continue;
                                } else {
                                    isNegation = true;
                                    break;
                                }
                            }
                        }
                        // @Note this should ultimately be a different
                        // aggregation w/ preferences/utilities.
                        if (isNegation) {
                            --benefitValue;
                        } else {
                            ++benefitValue;
                        }
                    }

                    localBestValue += benefitValue;

                    if (localBestValue > bestTotalValue) {
                        bestPlan = resolutions;
                        bestTotalValue = localBestValue;
                    }
                }
            }
        }
        plan.AddRange(bestPlan);
        done.Item = true;
        yield break;
    }
}


