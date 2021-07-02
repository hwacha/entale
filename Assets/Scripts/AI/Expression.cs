using System;
using System.Text;
using System.Collections.Generic;
using static SemanticType;

// a wrapper for simple symbols.
public abstract class Atom : IComparable<Atom> {
    // @Note we might want this to be an integer or enum
    // in the future, and then provide a table
    // from integers to strings.
    public SemanticType Type { get; private set; }

    public Atom(SemanticType type) {
        Type = type;
    }

    public int CompareTo(Atom that) {
        var typeComparison = this.Type.CompareTo(that.Type);
        if (typeComparison < 0) {
            return -1;
        }
        if (typeComparison > 0) {
            return 1;
        }

        var thisValue = 0;
        if (this is Bottom) {
            thisValue = 0;
        } else if (this is Variable) {
            thisValue = 1;
        } else if (this is Parameter) {
            thisValue = 2;
        } else if (this is Name) {
            thisValue = 3;
        } else if (this is Top) {
            thisValue = 4;
        }

        var thatValue = 0;
        
        if (that is Bottom) {
            thatValue = 0;
        } else if (that is Variable) {
            thatValue = 1;
        } else if (that is Parameter) {
            thatValue = 2;
        } else if (that is Name) {
            thatValue = 3;
        } else if (that is Top) {
            thatValue = 4;
        }

        var comparison = thisValue - thatValue;

        if (comparison < 0) {
            return -1;
        }
        if (comparison > 0) {
            return 1;
        }

        if (this is Variable) {
            Variable v1 = this as Variable;
            Variable v2 = that as Variable;
            return v1.ID.CompareTo(v2.ID);
        } else if (this is Parameter) {
            Parameter p1 = this as Parameter;
            Parameter p2 = that as Parameter;
            return p1.ID.CompareTo(p2.ID);
        } else if (this is Name) {
            Name n1 = this as Name;
            Name n2 = that as Name;
            return String.Compare(n1.ID, n2.ID);
        }

        return 0;
    }
}

// an atom that serves as an upper and
// lower bound for a comparison within
// a given type.
public abstract class Bound : Atom {
    public Bound(SemanticType type) : base(type) {}
}

// A constant. Cannot be reassigned.
public abstract class Constant : Atom {
    public Constant(SemanticType type) : base(type) {}
}

// A variable. Can be replaced by
// different values in a substitution.
public class Variable : Atom {
    public readonly int ID;
    public Variable(SemanticType type, int id) : base(type) {
        ID = id;
    }

    public override bool Equals(Object o) {
        if (!(o is Variable)) {
            return false;
        }
        Variable that = o as Variable;
        
        return ID == that.ID && Type == that.Type;
    }

    public override int GetHashCode() {
        return 23 * Type.GetHashCode() * (int) ID;
    }

    public override string ToString() {
        return "{" + ID + "}";
    }
}

// A symbol that can be publicly
// expressed by this NPC.
public class Name : Constant {
    public readonly string ID;
    public Name(SemanticType type, string id) : base(type) {
        ID = id;
    }

    public override bool Equals(Object o) {
        if (!(o is Name)) {
            return false;
        }

        Name that = o as Name;

        return Type.Equals(that.Type) && ID.Equals(that.ID);
    }

    public override int GetHashCode() {
        return 29 * Type.GetHashCode() * ID.GetHashCode();
    }

    public override string ToString() {
        return ID;
    }
}

// A parameter. A private symbol the
// mental state can privately assign.
public class Parameter : Constant {
    public readonly int ID;
    public Parameter(SemanticType type, int id) : base(type) {
        ID = id;
    }

    public override bool Equals(Object o) {
        if (!(o is Parameter)) {
            return false;
        }

        Parameter that = o as Parameter;

        return ID == that.ID && Type.Equals(that.Type);
    }

    public override int GetHashCode() {
        return 31 * Type.GetHashCode() * (int) ID;
    }

    public override string ToString() {
        return "$" + ID;
    }

    
}

public class Bottom : Bound {
    public Bottom(SemanticType type) : base(type) {}

    public override bool Equals(Object o) {
        return o is Bottom;
    }

    public override string ToString() {
        return "\u22A5";
    }
}

public class Top : Bound {
    public Top(SemanticType type) : base(type) {}

    public override bool Equals(Object o) {
        return o is Top;
    }

    public override string ToString() {
        return "\u22A4";
    }
}

// a wrapper for what can occur in the
// argument position in an expression.
public abstract class Argument {
    public SemanticType Type { get; protected set; }
    public int Depth { get; protected set; }
}

// A typed empty argument slot.
// Is the '_' in something like 'helps(_, bob)'
public class Empty : Argument {
    public Empty(SemanticType type) {
        Type = type;
        Depth = 1;
    }

    public override bool Equals(Object o) {
        if (!(o is Empty)) {
            return false;
        }

        Empty that = (Empty) o;

        return this.Type.Equals(that.Type);
    }

    public override String ToString() {
        return "_";
    }

    public override int GetHashCode() {
        return Type.GetHashCode();
    }
}

public class Expression : Argument, IComparable<Expression> {
    public Atom Head { get; protected set; }
    protected Argument[] Args;
    public int NumArgs { get; protected set; }

    // @Note we might make these constructors their own
    // classes if it's easier to think about making
    // Words or Phrases. But I wanted to consolidate a bit.

    // This constructor is a 'word' constructor.
    // It makes a constant or a variable by itself.
    public Expression(Atom head) {
        Type = head.Type;
        Head = head;
        Depth = 1;

        // we check if the head expression has an atomic type.
        // if it does, then we just initialize an empty array
        // since this expression doesn't take any arguments.
        if (Head.Type is AtomicType) {
            Args = new Argument[0];
            NumArgs = 0;
            return;
        }

        // we're going to populate the argument array with
        // empty slots that are typed according to the
        // semantic type of the functional head expression.
        FunctionalType fType = head.Type as FunctionalType;
        int fNumArgs = fType.GetNumArgs();
        Args = new Argument[fNumArgs];
        NumArgs = fNumArgs;
        for (int i = 0; i < fNumArgs; i++) {
            Args[i] = new Empty(fType.GetInput(i));
        }
    }

    // This constructor is a 'phrase' constructor.
    // It combines two or more expressions
    // make a compound expression. This handles
    // partial applications.
    public Expression(Expression f, params Argument[] args) {
        Head = f.Head;
        NumArgs = f.NumArgs;

        // we want a deep copy of the array so we can treat
        // expressions as immutable
        Args = new Argument[f.Args.Length];
        for (int i = 0; i < f.Args.Length; i++) {
            Args[i] = f.Args[i];
        }

        // we're going to place the provided arguments into
        // the available empty slots.
        Type = f.Type;
        int inputIndex = 0;
        int inputTypeIndex = 0;
        for (int i = 0; i < Args.Length; i++) {
            if (inputIndex >= args.Length) {
                break;
            }
            if (Args[i] is Empty) {
                Argument argumentToPlace = args[inputIndex];

                // if the types don't match up, exit with an error
                if (!Args[i].Type.Equals(argumentToPlace.Type)) {
                    throw new ArgumentException("Expression phrase constructor: " +
                        "type mismatch with " + argumentToPlace);
                }

                // reduce the type if it's not an empty slot
                if (argumentToPlace is Expression) {
                    // otherwise, replace the slot with the inputted expression
                    // @Note this also might not to be copied
                    // to avoid weird modification bugs.
                    Args[i] = argumentToPlace;
                    Type = Type.RemoveAt(inputTypeIndex);
                    inputTypeIndex--;
                }

                inputIndex++;
                inputTypeIndex++;
            }
        }

        // if there were more arguments than slots to fill, exit with an error.
        while (inputIndex < args.Length) {
            if (args[inputIndex] is Expression) {
                throw new ArgumentException("Expression phrase constructor: " +
                    "arity mismatch. Too many arguments were applied.");
            }
            inputIndex++;
        }

        int maxDepth = 0;
        for (int i = 0; i < Args.Length; i++) {
            int argDepth = Args[i].Depth;
            if (argDepth > maxDepth) {
                maxDepth = argDepth;
            }
        }
        Depth = maxDepth + 1;
    }

    public Expression RemoveAt(int index) {
        if (index > Args.Length) {
            throw new ArgumentException("RemoveAt(): index must be within range of arguments");
        }
        if (Args[index] is Empty) {
            return this;
        }

        Argument[] newArgs = new Argument[Args.Length];
        for (int i = 0; i < Args.Length; i++) {
            if (i == index) {
                newArgs[i] = new Empty(Args[i].Type);
            } else {
                newArgs[i] = Args[i];
            }
        }

        return new Expression(new Expression(Head), newArgs);
    }

    public Argument GetArg(int i) {
        return Args[i];
    }

    public Expression GetArgAsExpression(int i) {
        return (Expression) Args[i];
    }

    public bool HeadedBy(params Expression[] exprs) {
        for (int i = 0; i < exprs.Length; i++) {
            if (Head.Equals(exprs[i].Head)) {
                return true;
            }
        }
        return false;
    }

    // returns true if x occurs in this expression
    public bool HasOccurenceOf(Variable x) {
        if (Head.Equals(x)) {
            return true;
        }

        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression && ((Expression) Args[i]).HasOccurenceOf(x)) {
                return true;
            }
        }

        return false;
    }

    public HashSet<Variable> GetVariables() {
        HashSet<Variable> variables = new HashSet<Variable>();
        if (Head is Variable) {
            variables.Add((Variable) Head);
        }

        for (int i = 0; i < Args.Length; i++) {
            Argument arg = Args[i];
            if (arg is Empty) {
                continue;
            }
            Expression argExpression = (Expression) arg;
            variables.UnionWith(argExpression.GetVariables());
        }

        return variables;
    }

    // replaces all occurances of the variables within s with the
    // associated expression in s.
    public Expression Substitute(Dictionary<Variable, Expression> s) {
        Argument[] substitutedArgs = new Argument[Args.Length];
        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression) {
                substitutedArgs[i] = ((Expression) Args[i]).Substitute(s);
            } else {
                substitutedArgs[i] = Args[i];
            }
        }

        Expression newHead = new Expression(Head);
        if (Head is Variable && s.ContainsKey((Variable) Head)) {
            newHead = s[(Variable) Head];
        }

        return new Expression(newHead, substitutedArgs);
    }

    public Expression ZeroTimeIndices() {
        if (Type.Equals(TIME)) {
            return new Expression(new Parameter(TIME, 0));
        }
        Argument[] replacedArgs = new Argument[Args.Length];
        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression) {
                replacedArgs[i] = (Args[i] as Expression).ZeroTimeIndices();
            } else {
                replacedArgs[i] = Args[i];
            }
        }

        return new Expression(new Expression(Head), replacedArgs);
    }

    private void GetSelfSubstitution(Dictionary<Variable, Expression> sub) {
        if (Head is Variable) {
            sub[(Variable) Head] = this;
        }

        foreach (var arg in Args) {
            if (arg is Expression) {
                ((Expression) arg).GetSelfSubstitution(sub);
            }
        }
    }

    public Dictionary<Variable, Expression> GetSelfSubstitution() {
        var sub = new Dictionary<Variable, Expression>();
        GetSelfSubstitution(sub);
        return sub;
    }

    // find the mgu (most general unifier) between this expression
    // and that expression. A unifier is a variable substitution that,
    // when applied to both expressions, leads the expressions to
    // be syntactically equal
    // 
    // @Note we want to change this to be pattern matching instead
    // of unification (or closer to pattern matching, in any case.)
    private HashSet<Dictionary<Variable, Expression>> GetMatches(Expression that,
        HashSet<Dictionary<Variable, Expression>> substitutions) {
        // if the types don't match or the substitutions are empty, we fail.
        if (substitutions.Count == 0 || !this.Type.Equals(that.Type)) {
            return new HashSet<Dictionary<Variable, Expression>>();
        }

        // Adds an assignment of x to e into a given set of substitutions.
        HashSet<Dictionary<Variable, Expression>> AddAssignment(Variable x, Expression e,
            HashSet<Dictionary<Variable, Expression>> sub) {
            // first, we check if x occurs in e.
            if (e.HasOccurenceOf(x) && !e.Equals(new Expression(x))) {
                return new HashSet<Dictionary<Variable, Expression>>();
            }

            // otherwise, we're good to try and assign x to e in the substitution.
            var newSubstitutions = new HashSet<Dictionary<Variable, Expression>>();

            foreach (Dictionary<Variable, Expression> substitution in sub) {
                // now, substitute the substitution in for e, and
                // also substitute the substituted e for all occurences of x
                // on the right hand side of the substitution.
                Expression eWithSubstitution = e.Substitute(substitution);
                var xBoundToE = new Dictionary<Variable, Expression>();
                xBoundToE[x] = eWithSubstitution;
                var newSubstitution = new Dictionary<Variable, Expression>();
                foreach (var assignment in substitution) {
                    newSubstitution[assignment.Key] = assignment.Value.Substitute(xBoundToE);
                }

                // filter out substitutions that are incompatible with this variable assignment.
                if (newSubstitution.ContainsKey(x) && !substitution[x].Equals(e)) {
                    continue;
                }

                // Finally, add the assignment of this to e to the new substitution
                newSubstitution[x] = eWithSubstitution;
                newSubstitutions.Add(newSubstitution);
            }

            return newSubstitutions;
        }

        // we have a singular variable. Do variable assignment here.
        if (this.Head is Variable && this.Head.Type.Equals(this.Type)) {
            return AddAssignment((Variable) this.Head, that, substitutions);
        }

        // @Note: this is commented out to make it so unification
        // only happens left to right (essentially pattern matching)
        // same case as above, but reversed.
        // if (that.Head is Variable && that.Head.Type.Equals(that.Type)) {
        //     return AddAssignment((Variable) that.Head, this, substitutions);
        // }

        // both head symbols are constants or parameters. We need to recur on arguments and
        // collect results of unifying them.
        if (this.Head is Constant && that.Head is Constant) {
            if (!this.Head.Equals(that.Head)) {
                return new HashSet<Dictionary<Variable, Expression>>();
            }

            var currentSubstitutions = substitutions;
            
            // we now know they're the same head symbol, and that the overall
            // expression has the same type, so we can safely say they
            // have the same number of arguments, although some may
            // be empty in different positions, in which case unification fails
            for (int i = 0; i < Args.Length; i++) {
                // @Note not strictly necessary. But may be more efficient.
                if (currentSubstitutions.Count == 0) {
                    break;
                }

                // pass over empty argument slots
                if (Args[i] is Empty && that.Args[i] is Empty) {
                    continue;
                }

                // if one is empty and the other is not, fail
                if (Args[i] is Empty || that.Args[i] is Empty) {
                    return new HashSet<Dictionary<Variable, Expression>>();
                }

                // otherwise, we have two expressions
                // @Note: should we be substituting the expressions when we
                // first call Unify() with a substitution?
                // It shouldn't be, because we're checking to see
                // if x is bound to anything 
                // Let see if it's necessary in testing.
                currentSubstitutions =
                    ((Expression) this.Args[i]).GetMatches((Expression) that.Args[i],
                        currentSubstitutions);
            }

            return currentSubstitutions;
        }
        
        // 
        // @Note this is a naive implementation with O(2^n) complexity.
        // Is there a way to improve this?
        // 
        // decomposes an expression with respect to another pattern to match
        // 
        // e.g. R(a, b) will decompose into R(a,_)(b) and R(_, b)(a) w/r/t F(x)
        // 
        Dictionary<Expression, Argument[]> DecomposeExpression(int patternIndex, int expressionIndex,
            SemanticType patternHeadType, Expression f, Argument[] args) {
            var decompositions = new Dictionary<Expression, Argument[]>();
            // if there aren't enough arguments to match the pattern,
            // then this arrangement is no good.
            if (f.Args.Length - expressionIndex < args.Length - patternIndex) {
                return decompositions;
            }

            // if there are no more argument patterns to fill,
            // then this decomposition is good to go.
            if (patternIndex == args.Length) {
                if (f.Type.Equals(patternHeadType)) {
                    decompositions.Add(f, args);
                }
                return decompositions;
            }

            // otherwise, select each of the arguments to move, if applicable,
            // into the decomposed slot, and recur.
            for (int i = expressionIndex; i < f.Args.Length; i++) {
                if (args[patternIndex].Type.Equals(f.Args[i].Type)) {
                    Argument[] nextArgs = new Argument[args.Length];
                    for (int j = 0; j < patternIndex; j++) {
                        nextArgs[j] = args[j];
                    }

                    nextArgs[patternIndex] = f.Args[i];

                    for (int j = patternIndex + 1; j < nextArgs.Length; j++) {
                        nextArgs[j] = args[j];
                    }

                    var partialDecompositions = DecomposeExpression(patternIndex + 1, i + 1,
                        patternHeadType, f.RemoveAt(i), nextArgs);

                    foreach (var partialDecomposition in partialDecompositions) {
                        decompositions[partialDecomposition.Key] = partialDecomposition.Value;
                    }
                }
            }

            return decompositions;
        }

        List<HashSet<Dictionary<Variable, Expression>>> patternMatch(Expression pattern, Expression match) {
            Argument[] initialArguments = new Argument[pattern.Args.Length];
            for (int i = 0; i < initialArguments.Length; i++) {
                initialArguments[i] = new Empty(pattern.Args[i].Type);
            }

            Dictionary<Expression, Argument[]> decompositionsOfMatch =
                DecomposeExpression(0, 0, pattern.Head.Type, match, initialArguments);


            var alternativeSubstitutions = new List<HashSet<Dictionary<Variable, Expression>>>();

            // similar logic to that found in the code with two constants, except with
            // the decomposed expressions instead of the expressions themselves.
            foreach (var decompositionOfMatch in decompositionsOfMatch) {
                var currentSubstitutions = (new Expression(pattern.Head)).GetMatches(decompositionOfMatch.Key, substitutions);
                for (int i = 0; i < pattern.Args.Length; i++) {
                    if (currentSubstitutions.Count == 0) {
                        break;
                    }

                    Argument matchArg = decompositionOfMatch.Value[i];

                    if (pattern.Args[i] is Empty && matchArg is Empty) {
                        continue;
                    }

                    if (pattern.Args[i] is Empty || matchArg is Empty) {
                        break;
                    }

                    currentSubstitutions = ((Expression) pattern.Args[i]).GetMatches((Expression) matchArg, currentSubstitutions);
                }

                if (currentSubstitutions.Count != 0) {
                    alternativeSubstitutions.Add(currentSubstitutions);    
                }
            }

            return alternativeSubstitutions;
        }

        var patternSubstitutions = new HashSet<Dictionary<Variable, Expression>>();

        // the only other option, one of the heads is a variable, but has arguments too.
        // we need to do a partial application decomposition of the
        // two expressions to try to fit the two patterns against one another.
        if (this.Head is Variable) {
            // we get all of the decompositions of that, and check them against this.
            var thisMatchThatSubstitutions = patternMatch(this, that);

            foreach (var thisMatchThatSubstitution in thisMatchThatSubstitutions) {
                patternSubstitutions.UnionWith(thisMatchThatSubstitution);
            }
        }

        // // @Note this is commented out because we want unification
        // // to only occur from the left to the right.
        
        // if (patternSubstitutions.Count == 0) {
        //     var thatMatchThisSubstitutions = patternMatch(that, this);

        //     foreach (var thatMatchThisSubstitution in thatMatchThisSubstitutions) {
        //         patternSubstitutions.UnionWith(thatMatchThisSubstitution);
        //     }            
        // }

        return patternSubstitutions;
    }

    public HashSet<Dictionary<Variable, Expression>>
        GetMatches(Expression that) {
        var initialSubstitution = new Dictionary<Variable, Expression>();
        var initialSubstitutions = new HashSet<Dictionary<Variable, Expression>>();
        initialSubstitutions.Add(initialSubstitution);
        return GetMatches(that, initialSubstitutions);
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();

        s.Append(Head);
        
        // if an expression doesn't have any arguments,
        // don't draw any parentheses
        if (Args.Length == 0) {
            return s.ToString();
        }

        bool hasOneExpression = false;
        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression) {
                hasOneExpression = true;
                break;
            }
        }

        if (!hasOneExpression) {
            return s.ToString();
        }

        s.Append("(");

        for (int i = 0; i < Args.Length; i++) {
            s.Append(Args[i]);
            s.Append(", ");
        }

        if (s.Length > 1) {
            s.Remove(s.Length - 2, 2);
        }

        s.Append(")");

        return s.ToString();
    }

    public override bool Equals(Object o) {
        if (!(o is Expression)) {
            return false;
        }
        Expression that = (Expression) o;

        if (!this.Head.Equals(that.Head)) {
            return false;
        }

        if (this.Args.Length != that.Args.Length) {
            return false;
        }

        for (int i = 0; i < Args.Length; i++) {
            if (!this.Args[i].Equals(that.Args[i])) {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() {
        int hash = 5381 * Head.GetHashCode();
        for (int i = 0; i < Args.Length; i++) {
            hash = 33 * hash + Args[i].GetHashCode();
        }
        return hash;
    }

    public int CompareTo(Expression that) {
        var typeComparison = this.Type.CompareTo(that.Type);
        if (typeComparison < 0) {
            return -1;
        }
        if (typeComparison > 0) {
            return 1;
        }

        // if we have a bound, then
        // we test this before we
        // test for it here.
        if (Type.Equals(Head.Type) && Head is Top &&
            that.Type.Equals(that.Head.Type) && that.Head is Top) {
            return 0;
        }
        if (Type.Equals(Head.Type) && Head is Top) {
            return 1;
        }
        if (that.Type.Equals(that.Head.Type) && that.Head is Top) {
            return -1;
        }

        if (Type.Equals(Head.Type) && Head is Bottom &&
            that.Type.Equals(that.Head.Type) && that.Head is Bottom) {
            return 0;
        }
        if (Type.Equals(Head.Type) && Head is Bottom) {
            return -1;
        }
        if (that.Type.Equals(that.Head.Type) && that.Head is Bottom) {
            return 1;
        }

        // BEGIN CUSTOM-ORDERING FOR EVIDENTIALS, etc.
        if (Type.Equals(TRUTH_VALUE)) {
            
            // we check the depth to tell which to recur on.
            // @Note This _shouldn't_ cause problems if dealing
            // with reduced expressions.
            int thisDepth = this.Depth;
            int thatDepth = that.Depth;
            
            // EVIDENTIALS
            bool thisIsFactive = this.HeadedBy(KNOW, SEE, MAKE);
            bool thatIsFactive = that.HeadedBy(KNOW, SEE, MAKE);

            if (thisIsFactive && thatIsFactive && thisDepth == thatDepth) {
                var thisContent = this.GetArgAsExpression(0);
                var thatContent = that.GetArgAsExpression(0);

                int contentComparison = thisContent.CompareTo(thatContent);

                if (contentComparison != 0) {
                    return contentComparison;
                }

                int factiveHeadComparison = this.Head.CompareTo(that.Head);

                if (factiveHeadComparison != 0) {
                    return factiveHeadComparison;
                }

                var thisSubject = this.GetArgAsExpression(1);
                var thatSubject = that.GetArgAsExpression(1);

                int subjectComparison = thisSubject.CompareTo(thatSubject);

                return subjectComparison;
            }
            if (thisIsFactive && !thatIsFactive ||
                thisIsFactive && thatIsFactive && thisDepth > thatDepth) {
                var content = this.GetArgAsExpression(0);
                int comparison = content.CompareTo(that);
                if (comparison == 0) {
                    return 1;
                }
                return comparison;
            }
            if (!thisIsFactive && thatIsFactive ||
                thisIsFactive && thatIsFactive && thisDepth < thatDepth) {
                var content = that.GetArgAsExpression(0);
                int comparison = this.CompareTo(content);
                if (comparison == 0) {
                    return -1;
                }
                return comparison;
            }

            // NEGATION
            bool thisNot = this.HeadedBy(NOT);
            bool thatNot = that.HeadedBy(NOT);

            if (thisNot && thatNot && thisDepth == thatDepth) {
                var thisSubclause = this.GetArgAsExpression(0);
                var thatSubclause = that.GetArgAsExpression(0);

                int comparison = thisSubclause.CompareTo(thatSubclause);
                return comparison;
            }

            if (thisNot && !thatNot ||
                thisNot && thatNot && thisDepth > thatDepth) {
                var subclause = this.GetArgAsExpression(0);
                int comparison = subclause.CompareTo(that);
                if (comparison == 0) {
                    return 1;
                }
                return comparison;
            }

            if (!thisNot && thatNot ||
                thisNot && thatNot && thisDepth < thatDepth) {
                var subclause = that.GetArgAsExpression(0);
                int comparison = this.CompareTo(subclause);
                if (comparison == 0) {
                    return -1;
                }
                return comparison;
            }

            // TIME
            // @Note this depends on the order of not/when
            // e.g. we get the right ordering for not(when(A, t))
            // but not for when(not(A), t)
            // 
            // We could check for more cases in the ordering,
            // introduce subtyping to place restriction on
            // which arguments the expressions accept,
            // or (as we currently do) maintain the working
            // order as an invariant.
            bool thisWhen = this.HeadedBy(WHEN, BEFORE, AFTER);
            bool thatWhen = that.HeadedBy(WHEN, BEFORE, AFTER);

            if (thisWhen && thatWhen && thisDepth == thatDepth) {
                var thisContent = this.GetArgAsExpression(0);
                var thatContent = that.GetArgAsExpression(0);

                int contentComparison = thisContent.CompareTo(thatContent);

                if (contentComparison != 0) {
                    return contentComparison;
                }

                var thisTime = this.GetArgAsExpression(1);
                var thatTime = that.GetArgAsExpression(1);

                int timeComparison = thisTime.CompareTo(thatTime);

                if (timeComparison == 0) {
                    if (this.HeadedBy(AFTER) && that.HeadedBy(WHEN, BEFORE) ||
                        this.HeadedBy(AFTER, WHEN) && that.HeadedBy(BEFORE)) {
                        return 1;
                    }
                    if (this.HeadedBy(BEFORE) && that.HeadedBy(WHEN, AFTER) ||
                        this.HeadedBy(BEFORE, WHEN) && that.HeadedBy(AFTER)) {
                        return -1;
                    }
                    return 0;
                }

                return timeComparison;
            }

            if (thisWhen && !thatWhen ||
                thisWhen && thatWhen && thisDepth > thatDepth) {
                var content = this.GetArgAsExpression(0);
                int comparison = content.CompareTo(that);
                if (comparison == 0) {
                    return 1;
                }
                return comparison;
            }
            if (!thisWhen && thatWhen ||
                thisWhen && thatWhen && thisDepth < thatDepth) {
                var content = that.GetArgAsExpression(0);
                int comparison = this.CompareTo(content);
                if (comparison == 0) {
                    return -1;
                }
                return comparison;
            }
        }
        // END

        var headTypeComparison = this.Head.Type.CompareTo(that.Head.Type);
        if (headTypeComparison < 0) {
            return -1;
        }
        if (headTypeComparison > 0) {
            return 1;
        }

        var headComparison = this.Head.CompareTo(that.Head);
        if (headComparison < 0) {
            return -1;
        }
        if (headComparison > 0) {
            return 1;
        }

        for (int i = 0; i < Args.Length; i++) {
            var thisArg = Args[i];
            var thatArg = that.Args[i];

            if (thisArg is Empty && thatArg is Empty) {
                return 0;
            }
            if (thisArg is Empty) {
                return -1;
            }
            if (thatArg is Empty) {
                return 1;
            }

            var thisExpression = thisArg as Expression;
            var thatExpression = thatArg as Expression;
            var subComparison = thisExpression.CompareTo(thatExpression);

            if (subComparison < 0) {
                return -1;
            }
            if (subComparison > 0) {
                return 1;
            }
        }

        return 0;
    }


    // Individual constants
    public static readonly Expression SELF        = new Expression(new Name(INDIVIDUAL, "self"));
    public static readonly Expression ALICE       = new Expression(new Name(INDIVIDUAL, "alice"));
    public static readonly Expression BOB         = new Expression(new Name(INDIVIDUAL, "bob"));
    public static readonly Expression CHARLIE     = new Expression(new Name(INDIVIDUAL, "charlie"));
    public static readonly Expression EVAN        = new Expression(new Name(INDIVIDUAL, "evan"));
    public static readonly Expression SOUP        = new Expression(new Name(INDIVIDUAL, "soup"));
    public static readonly Expression SWEETBERRY  = new Expression(new Name(INDIVIDUAL, "sweetberry"));
    public static readonly Expression SPICYBERRY  = new Expression(new Name(INDIVIDUAL, "spicyberry"));
    public static readonly Expression FOREST_KING = new Expression(new Name(INDIVIDUAL, "forest_king"));

    // Individual variables
    public static readonly Expression XE = new Expression(new Variable(INDIVIDUAL, 0));
    public static readonly Expression YE = new Expression(new Variable(INDIVIDUAL, 1));
    public static readonly Expression ZE = new Expression(new Variable(INDIVIDUAL, 2));

    // Truth Value constants
    public static readonly Expression VERUM   = new Expression(new Name(TRUTH_VALUE, "verum"));
    public static readonly Expression FALSUM  = new Expression(new Name(TRUTH_VALUE, "falsum"));
    public static readonly Expression NEUTRAL = new Expression(new Name(TRUTH_VALUE, "neutral"));

    // Truth Value variables
    public static readonly Expression ST = new Expression(new Variable(TRUTH_VALUE, 0));
    public static readonly Expression TT = new Expression(new Variable(TRUTH_VALUE, 1));
    public static readonly Expression PT = new Expression(new Variable(TRUTH_VALUE, 2));

    // Predicate constants
    public static readonly Expression RED    = new Expression(new Name(PREDICATE, "red"));
    public static readonly Expression BLUE   = new Expression(new Name(PREDICATE, "blue"));
    public static readonly Expression GREEN  = new Expression(new Name(PREDICATE, "green"));
    public static readonly Expression YELLOW = new Expression(new Name(PREDICATE, "yellow"));
    public static readonly Expression APPLE  = new Expression(new Name(PREDICATE, "apple"));
    public static readonly Expression SPICY  = new Expression(new Name(PREDICATE, "spicy"));
    public static readonly Expression SWEET  = new Expression(new Name(PREDICATE, "sweet"));
    public static readonly Expression TREE   = new Expression(new Name(PREDICATE, "tree"));
    public static readonly Expression TOMATO = new Expression(new Name(PREDICATE, "tomato"));
    public static readonly Expression BANANA = new Expression(new Name(PREDICATE, "banana"));

    // a predicate that applies to any individual
    public static readonly Expression VEROUS = new Expression(new Name(PREDICATE, "verous"));

    // Predicate variables
    public static readonly Expression FET = new Expression(new Variable(PREDICATE, 0));
    // @Note I may want a constant called get eventually
    public static readonly Expression GET = new Expression(new Variable(PREDICATE, 1));

    // 2-place relation constants
    public static readonly Expression IDENTITY = new Expression(new Name(RELATION_2, "="));
    public static readonly Expression AT       = new Expression(new Name(RELATION_2, "at"));
    public static readonly Expression ADDED_TO = new Expression(new Name(RELATION_2, "added_to"));

    // 2-place relation variables
    public static readonly Expression REET = new Expression(new Variable(RELATION_2, 0));

    // 1-place truth functions
    public static readonly Expression NOT    = new Expression(new Name(TRUTH_FUNCTION, "not"));
    public static readonly Expression TRULY  = new Expression(new Name(TRUTH_FUNCTION, "truly"));
    // the question of whether "A" is closed,
    // so you either believe A or ~A
    public static readonly Expression CLOSED = new Expression(new Name(TRUTH_FUNCTION, "closed"));
    public static readonly Expression GOOD   = new Expression(new Name(TRUTH_FUNCTION, "good"));
    // tense operators
    public static readonly Expression PAST    = new Expression(new Name(TRUTH_FUNCTION, "past"));
    public static readonly Expression PRESENT = new Expression(new Name(TRUTH_FUNCTION, "present"));
    public static readonly Expression FUTURE  = new Expression(new Name(TRUTH_FUNCTION, "future"));


    // higher-order variables
    public static readonly Expression FTF  = new Expression(new Variable(TRUTH_FUNCTION, 0));
    public static readonly Expression GTF  = new Expression(new Variable(TRUTH_FUNCTION, 1));
    public static readonly Expression FTTF = new Expression(new Variable(TRUTH_FUNCTION_2, 0));
    public static readonly Expression PQP  = new Expression(new Variable(QUANTIFIER_PHRASE, 0));
    public static readonly Expression QQP  = new Expression(new Variable(QUANTIFIER_PHRASE, 1));

    // 2-place truth functions
    public static readonly Expression AND        = new Expression(new Name(TRUTH_FUNCTION_2, "and"));
    public static readonly Expression OR         = new Expression(new Name(TRUTH_FUNCTION_2, "or"));
    public static readonly Expression IF         = new Expression(new Name(TRUTH_FUNCTION_2, "if"));
    public static readonly Expression BETTER     = new Expression(new Name(TRUTH_FUNCTION_2, "better"));
    public static readonly Expression AS_GOOD_AS = new Expression(new Name(TRUTH_FUNCTION_2, "~"));

    // truth-conformity relations
    // "will" is interpreted as an instruction for the actuator in LOT
    // and is interpreted as a promise when expression in public language
    public static readonly Expression WILL  = new Expression(new Name(TRUTH_CONFORMITY_FUNCTION, "will"));
    public static readonly Expression WOULD = new Expression(new Name(TRUTH_CONFORMITY_FUNCTION, "would"));

    // individual-truth relations
    public static readonly Expression SAY       = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "say"));
    public static readonly Expression KNOW      = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "know"));
    public static readonly Expression SEE       = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "see"));
    public static readonly Expression MAKE      = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "make"));
    public static readonly Expression BELIEVE   = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "believe"));
    public static readonly Expression ABLE      = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "able"));
    public static readonly Expression PERCEIVE  = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "perceive"));
    public static readonly Expression VERIDICAL = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "veridical"));
    public static readonly Expression TRIED     = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "tried"));
    public static readonly Expression PERCEPTUALLY_CLOSED =
        new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "perceptually_closed"));

    // tensed ITRs
    public static readonly Expression KNOW_TENSED = new Expression(new Name(TENSED_INDIVIDUAL_TRUTH_RELATION, "know"));
    
    // determiners
    public static readonly Expression SELECTOR = new Expression(new Name(DETERMINER, "selector"));

    // tensers
    public static readonly Expression WHEN   = new Expression(new Name(TENSER, "when"));
    public static readonly Expression BEFORE = new Expression(new Name(TENSER, "before"));
    public static readonly Expression AFTER  = new Expression(new Name(TENSER, "after"));

    // quantifiers
    public static readonly Expression SOME = new Expression(new Name(QUANTIFIER, "some"));
    public static readonly Expression ALL  = new Expression(new Name(QUANTIFIER, "all"));

    // sentential adverbs/quantifiers
    public static readonly Expression ALWAYS    = new Expression(new Name(PROPOSITIONAL_QUANTIFIER, "always"));
    public static readonly Expression SOMETIMES = new Expression(new Name(PROPOSITIONAL_QUANTIFIER, "sometimes"));
    public static readonly Expression NORMALLY  = new Expression(new Name(PROPOSITIONAL_QUANTIFIER, "normally"));
    public static readonly Expression NORMAL    = new Expression(new Name(PROPOSITIONAL_QUANTIFIER, "normal"));

    // weird function words
    public static readonly Expression ITSELF   = new Expression(new Name(RELATION_2_REDUCER, "itself"));
    // for permutations of arguments for higher-arity functions,
    // 'shift': ABC -> CAB and 'swap': ABC -> BAC should do the trick
    public static readonly Expression CONVERSE = new Expression(new Name(RELATION_2_MODIFIER, "converse"));
    // more function words: Geach
    public static readonly Expression GEACH_E_TRUTH_FUNCTION    = new Expression(new Name(
        SemanticType.Push(TRUTH_FUNCTION, SemanticType.Geach(INDIVIDUAL, TRUTH_FUNCTION)), "geach"));
    public static readonly Expression GEACH_T_TRUTH_FUNCTION    = new Expression(new Name(
        SemanticType.Push(TRUTH_FUNCTION, SemanticType.Geach(TRUTH_VALUE, TRUTH_FUNCTION)), "geach"));
    public static readonly Expression GEACH_E_TRUTH_FUNCTION_2  = new Expression(new Name(
        SemanticType.Push(TRUTH_FUNCTION_2, SemanticType.Geach(INDIVIDUAL, TRUTH_FUNCTION_2)), "geach"));
    public static readonly Expression GEACH_E_QUANTIFIER_PHRASE = new Expression(new Name(
        SemanticType.Push(QUANTIFIER_PHRASE, SemanticType.Geach(INDIVIDUAL, QUANTIFIER_PHRASE)), "geach"));

    // simplifies geach operations for sentences
    // with two quantifiers
    public static readonly Expression QUANTIFIER_PHRASE_COORDINATOR_2 =
        new Expression(new Name(SemanticType.QUANTIFIER_PHRASE_COORDINATOR_2, "rel2"));

    // question and assert functions
    public static readonly Expression ASK    = new Expression(new Name(TRUTH_QUESTION_FUNCTION, "ask"));
    public static readonly Expression ASSERT = new Expression(new Name(TRUTH_ASSERTION_FUNCTION, "assert"));
    public static readonly Expression DENY   = new Expression(new Name(TRUTH_ASSERTION_FUNCTION, "deny"));

    // assertion constants
    public static readonly Expression YES   = new Expression(new Name(ASSERTION, "assert"));
    public static readonly Expression NO    = new Expression(new Name(ASSERTION, "deny"));
    public static readonly Expression MAYBE = new Expression(new Name(ASSERTION, "maybe"));

    // conformity constants
    public static readonly Expression ACCEPT = new Expression(new Name(CONFORMITY_VALUE, "accept"));
    public static readonly Expression REFUSE = new Expression(new Name(CONFORMITY_VALUE, "refuse"));

    // heads for deictic expressions
    public static readonly Expression THIS = new Expression(new Name(INDIVIDUAL, "this"));

    // // sourced predicates
    // public static readonly Expression SOURCED_RED = new Expression(new Name(EVIDENTIAL_PREDICATE, "sred"));
    // public static readonly Expression SOURCED_GREEN = new Expression(new Name(EVIDENTIAL_PREDICATE, "sgreen"));
}
