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
        return "{" + ID + "#" + Type + "}";
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

    public bool PrejacentHeadedBy(Expression f, params Expression[] exprs) {
        if (!(f.Type is FunctionalType) /* || (f.Type as FunctionalType).GetNumArgs() != 1 */) {
            throw new ArgumentException("f(x) must be a one-place function");
        }
        if (!this.HeadedBy(f)) {
            return false;
        }
        var prejacent = this.GetArgAsExpression(0);
        return prejacent.HeadedBy(exprs);
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

    // find the mgu (most general unifier) between this expression
    // and that expression. A unifier is a variable substitution that,
    // when applied to both expressions, leads the expressions to
    // be syntactically equal
    // 
    // @Note we want to change this to be pattern matching instead
    // of unification (or closer to pattern matching, in any case.)
    private HashSet<Dictionary<Variable, Expression>> Unify(Expression that,
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
                    ((Expression) this.Args[i]).Unify((Expression) that.Args[i],
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
                var currentSubstitutions = (new Expression(pattern.Head)).Unify(decompositionOfMatch.Key, substitutions);
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

                    currentSubstitutions = ((Expression) pattern.Args[i]).Unify((Expression) matchArg, currentSubstitutions);
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

        // @Note this is commented out because we want unification
        // to only occur from the left to the right.
        
        if (patternSubstitutions.Count == 0) {
            var thatMatchThisSubstitutions = patternMatch(that, this);

            foreach (var thatMatchThisSubstitution in thatMatchThisSubstitutions) {
                patternSubstitutions.UnionWith(thatMatchThisSubstitution);
            }            
        }

        return patternSubstitutions;
    }

    public HashSet<Dictionary<Variable, Expression>> Unify(Expression that) {
        var initialSubstitution = new Dictionary<Variable, Expression>();
        var initialSubstitutions = new HashSet<Dictionary<Variable, Expression>>();
        initialSubstitutions.Add(initialSubstitution);
        return Unify(that, initialSubstitutions);
    }

    public bool Matches(Expression that) {
        return Unify(that).Count > 0;
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();

        s.Append(Head);
        
        // if an expression doesn't have any arguments,
        // don't draw any parentheses
        if (Args.Length == 0) {
            return s.ToString();
        }

        int lastNonEmptyIndex = -1;
        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression) {
                lastNonEmptyIndex = i;
            }
        }

        if (lastNonEmptyIndex == -1) {
            return s.ToString();
        }

        bool skipParens =
            Args.Length == 1 &&
            Head is Constant &&
            !Char.IsLetter((Head as Name).ID[0]);

        if (!skipParens) {
            s.Append("(");
        }

        for (int i = 0; i <= lastNonEmptyIndex; i++) {
            s.Append(Args[i]);
            s.Append(", ");
        }

        if (s.Length > 1) {
            s.Remove(s.Length - 2, 2);
        }

        if (!skipParens) {
            s.Append(")");
        }

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

    public static bool operator <(Expression a, Expression b) {
        return a.CompareTo(b) < 0;
    }

    public static bool operator >(Expression a, Expression b) {
        return a.CompareTo(b) > 0;
    }

    public (Expression, Expression) GetBounds() {
        var variables = this.GetVariables();

        Expression bottom = null;
        Expression top = null;

        var bottomSubstitution = new Dictionary<Variable, Expression>();
        var topSubstitution    = new Dictionary<Variable, Expression>();
        foreach (Variable v in variables) {
            bottomSubstitution.Add(v, new Expression(new Bottom(v.Type)));
            topSubstitution.Add(v, new Expression(new Top(v.Type)));
        }

        bottom = this.Substitute(bottomSubstitution);
        top    = this.Substitute(topSubstitution);
        return (bottom, top);
    }

    // Individual constants
    // function words
    public static readonly Expression THIS = new Expression(new Name(INDIVIDUAL, "this"));

    // determiners
    public static readonly Expression SELECTOR = new Expression(new Name(DETERMINER, "selector"));

    // names
    public static readonly Expression SELF    = new Expression(new Name(INDIVIDUAL, "self"));
    public static readonly Expression ALICE   = new Expression(new Name(INDIVIDUAL, "alice"));
    public static readonly Expression BOB     = new Expression(new Name(INDIVIDUAL, "bob"));
    public static readonly Expression CHARLIE = new Expression(new Name(INDIVIDUAL, "charlie"));
    public static readonly Expression DANI    = new Expression(new Name(INDIVIDUAL, "dani"));
    public static readonly Expression EVAN    = new Expression(new Name(INDIVIDUAL, "evan"));

    

    // Individual variables
    public static readonly Expression XE = new Expression(new Variable(INDIVIDUAL, 0));
    public static readonly Expression YE = new Expression(new Variable(INDIVIDUAL, 1));
    public static readonly Expression ZE = new Expression(new Variable(INDIVIDUAL, 2));

    // Truth Value constants
    public static readonly Expression VERUM  = new Expression(new Name(TRUTH_VALUE, "⊤"));
    public static readonly Expression FALSUM = new Expression(new Name(TRUTH_VALUE, "⊥"));
    public static readonly Expression NEUTRAL = new Expression(new Name(TRUTH_VALUE, "⦵"));
    public static readonly Expression TEST   = new Expression(new Parameter(TRUTH_VALUE, -1));

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
    public static readonly Expression FRUIT  = new Expression(new Name(PREDICATE, "fruit"));
    public static readonly Expression PEPPER = new Expression(new Name(PREDICATE, "pepper"));
    public static readonly Expression ROUND  = new Expression(new Name(PREDICATE, "round"));
    public static readonly Expression EXIST  = new Expression(new Name(PREDICATE, "exist"));
    public static readonly Expression PERSON = new Expression(new Name(PREDICATE, "person"));

    // Predicate variables
    public static readonly Expression FET = new Expression(new Variable(PREDICATE, 0));
    // @Note I may want a constant called get eventually
    public static readonly Expression GET = new Expression(new Variable(PREDICATE, 1));

    // 2-place relation constants
    public static readonly Expression IDENTITY = new Expression(new Name(RELATION_2, "="));
    public static readonly Expression AT       = new Expression(new Name(RELATION_2, "at"));
    public static readonly Expression YIELDS   = new Expression(new Name(RELATION_2, "yields"));

    // 2-place relation variables
    public static readonly Expression REET = new Expression(new Variable(RELATION_2, 0));

    // 1-place truth functions
    public static readonly Expression STAR   = new Expression(new Name(TRUTH_FUNCTION, "*"));
    public static readonly Expression NOT    = new Expression(new Name(TRUTH_FUNCTION, "~"));
    public static readonly Expression TRULY  = new Expression(new Name(TRUTH_FUNCTION, "t"));
    // the question of whether "A" is closed,
    // so you either believe A or ~A
    public static readonly Expression CLOSED = new Expression(new Name(TRUTH_FUNCTION, "closed"));
    public static readonly Expression GOOD   = new Expression(new Name(TRUTH_FUNCTION, "good"));
    // tense operators
    public static readonly Expression PAST    = new Expression(new Name(TRUTH_FUNCTION, "past"));
    public static readonly Expression PRESENT = new Expression(new Name(TRUTH_FUNCTION, "present"));
    public static readonly Expression FUTURE  = new Expression(new Name(TRUTH_FUNCTION, "future"));
    // very
    public static readonly Expression VERY  = new Expression(new Name(TRUTH_FUNCTION, "+"));
    // limit-ordinal: used to mark lexical priority
    public static readonly Expression OMEGA = new Expression(new Name(TRUTH_FUNCTOR, "Ω"));

    // higher-order variables
    public static readonly Expression FTF  = new Expression(new Variable(TRUTH_FUNCTION, 0));
    public static readonly Expression GTF  = new Expression(new Variable(TRUTH_FUNCTION, 1));
    public static readonly Expression FTTF = new Expression(new Variable(TRUTH_FUNCTION_2, 0));
    public static readonly Expression PQP  = new Expression(new Variable(QUANTIFIER_PHRASE, 0));
    public static readonly Expression QQP  = new Expression(new Variable(QUANTIFIER_PHRASE, 1));

    // 2-place truth functions
    
    // truth functions
    public static readonly Expression AND = new Expression(new Name(TRUTH_FUNCTION_2, "and"));
    public static readonly Expression OR  = new Expression(new Name(TRUTH_FUNCTION_2, "or"));
    public static readonly Expression IF  = new Expression(new Name(TRUTH_FUNCTION_2, "if"));
    public static readonly Expression THEREFORE = new Expression(new Name(TRUTH_FUNCTION_2, "therefore"));

    // tense operators
    public static readonly Expression SINCE  = new Expression(new Name(TRUTH_FUNCTION_2, "since"));
    public static readonly Expression UNTIL  = new Expression(new Name(TRUTH_FUNCTION_2, "until"));
    // public static readonly Expression BEFORE = new Expression(new Name(TRUTH_FUNCTION_2, "before"));
    // public static readonly Expression AFTER  = new Expression(new Name(TRUTH_FUNCTION_2, "after"));
    
    // four-place truth function
    public static readonly Expression BETTER_BY_MORE = new Expression(new Name(TRUTH_FUNCTION_4, "better_by_more"));

    // truth-conformity relations
    // "will" is interpreted as an instruction for the actuator in LOT
    // and is interpreted as a promise when expression in public language
    public static readonly Expression WILL  = new Expression(new Name(TRUTH_CONFORMITY_FUNCTION, "will"));
    public static readonly Expression WOULD = new Expression(new Name(TRUTH_CONFORMITY_FUNCTION, "would"));
    public static readonly Expression REQUIRE = new Expression(new Name(TRUTH_CONFORMITY_FUNCTION, "require"));

    // FACTIVES
    // evidentials
    public static readonly Expression KNOW      = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "know"));
    public static readonly Expression SEE       = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "see"));
    public static readonly Expression RECALL    = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "recall"));
    public static readonly Expression INFORMED  = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "informed"));

    // agentives
    public static readonly Expression MAKE = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION, "make"));

    public static readonly Expression ITET = new Expression(new Variable(INDIVIDUAL_TRUTH_RELATION, 0));

    // defactivizer: turns a sentence R(P, x) that entails P
    // into one that doesn't entail P
    // df(R, P, x) is also entailed by R(P, x)
    public static readonly Expression DF = new Expression(new Name(INDIVIDUAL_TRUTH_RELATION_FUNCTOR, "df"));

    // quantifiers
    public static readonly Expression SOME = new Expression(new Name(QUANTIFIER, "∃"));
    public static readonly Expression ALL  = new Expression(new Name(QUANTIFIER, "∀"));
    public static readonly Expression GEN  = new Expression(new Name(QUANTIFIER, "gen"));
    public static readonly Expression INS  = new Expression(new Name(QUANTIFIER, "ins"));

    // weird function words
    public static readonly Expression ITSELF   = new Expression(new Name(RELATION_2_REDUCER, "itself"));
    // for permutations of arguments for higher-arity functions,
    // 'shift': ABC -> CAB and 'swap': ABC -> BAC should do the trick
    public static readonly Expression CONVERSE = new Expression(new Name(RELATION_2_MODIFIER, "converse"));
    // more function words: Geach
    public static Expression Geach(SemanticType functorType, SemanticType baseType) {
        return new Expression(new Name(SemanticType.Push(baseType, SemanticType.Geach(functorType, baseType)), "𝔾"));
    }

    // compose
    public static readonly Expression COMPOSE_TRUTH_FUNCTION = new Expression(new Name(
        SemanticType.Compose(TRUTH_VALUE, TRUTH_VALUE, TRUTH_VALUE), "compose"));

    // simplifies geach operations for sentences
    // with two quantifiers
    public static readonly Expression QUANTIFIER_PHRASE_COORDINATOR_2 =
        new Expression(new Name(SemanticType.QUANTIFIER_PHRASE_COORDINATOR_2, "rel2"));

    // question and assert functions
    public static readonly Expression ASK    = new Expression(new Name(TRUTH_QUESTION_FUNCTION, "ask"));
    public static readonly Expression ASSERT = new Expression(new Name(TRUTH_ASSERTION_FUNCTION, "assert"));
    public static readonly Expression DENY   = new Expression(new Name(TRUTH_ASSERTION_FUNCTION, "deny"));

    // assertion constants
    public static readonly Expression YES    = new Expression(new Name(ASSERTION, "assert"));
    public static readonly Expression NO     = new Expression(new Name(ASSERTION, "deny"));
    public static readonly Expression POSIGN = new Expression(new Name(ASSERTION, "posign"));
    public static readonly Expression NEGIGN = new Expression(new Name(ASSERTION, "negign"));

    // conformity constants
    public static readonly Expression ACCEPT = new Expression(new Name(CONFORMITY_VALUE, "accept"));
    public static readonly Expression REFUSE = new Expression(new Name(CONFORMITY_VALUE, "refuse"));
}
