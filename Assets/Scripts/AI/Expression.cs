using System;
using System.Text;
using System.Collections.Generic;
using static SemanticType;

// a wrapper for simple symbols.
public abstract class Atom {
    // @Note we might want this to be an integer or enum
    // in the future, and then provide a table
    // from integers to strings.
    public SemanticType Type { get; private set; }
    public String ID { get; private set; }

    public Atom(SemanticType type, String id) {
        Type = type;
        ID = id;
    }

    public override String ToString() {
        return ID;
    }

    public override bool Equals(Object o) {
        if (!(o is Atom)) {
            return false;
        }

        Atom that = o as Atom;

        return this.Type.Equals(that.Type) && this.ID.Equals(that.ID);
    }
 
    public override int GetHashCode() {
        return Type.GetHashCode() * ID.GetHashCode();
    }
}

// A constant. Just a regular symbol.
public class Constant : Atom {
    public Constant(SemanticType type, String id) : base(type, id) {}
}

// A variable. Can be replaced by different values in a substitution.
public class Variable : Atom {
    public Variable(SemanticType type, String id) : base(type, id) {}
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

// @Note this is a placeholder, to be replaced
// by a more clever 'subjective' representation.
// But right now, we're only concerned with
// making 'this' words, and we have them
// directly and objectly refer to their component
// objects.
public class Deictic : Expression {
    public UnityEngine.GameObject Referent { get; protected set; }

    public Deictic(Atom head, UnityEngine.GameObject referent) : base(head) {
        Referent = referent;
    }

    public override Expression Substitute(Dictionary<Variable, Expression> substitution) {
        Expression substitutedExpression = base.Substitute(substitution);

        return new Deictic(substitutedExpression.Head, Referent);
    }

    public override bool Equals(Object o) {
        if (!(o is Deictic)) {
            return false;
        }

        if (!base.Equals(o)) {
            return false;
        }

        return Referent == (((Deictic) o).Referent);
    }

    public override String ToString() {
       return base.ToString() + " ~> " + Referent.ToString();
    }

    public override int GetHashCode() {
        return base.GetHashCode() * Referent.GetHashCode();
    }
}

public class Expression : Argument {
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
    public virtual Expression Substitute(Dictionary<Variable, Expression> s) {
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

        // same case as above, but reversed.
        if (that.Head is Variable && that.Head.Type.Equals(that.Type)) {
            return AddAssignment((Variable) that.Head, this, substitutions);
        }

        // both head symbols are constants. We need to recur on arguments and
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
        // 
        // if (patternSubstitutions.Count == 0) {
        //     var thatMatchThisSubstitutions = patternMatch(that, this);

        //     foreach (var thatMatchThisSubstitution in thatMatchThisSubstitutions) {
        //         patternSubstitutions.UnionWith(thatMatchThisSubstitution);
        //     }            
        // }

        return patternSubstitutions;
    }

    public HashSet<Dictionary<Variable, Expression>>
        Unify(Expression that) {
        var initialSubstitution = new Dictionary<Variable, Expression>();
        var initialSubstitutions = new HashSet<Dictionary<Variable, Expression>>();
        initialSubstitutions.Add(initialSubstitution);
        return Unify(that, initialSubstitutions);
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();

        s.Append(Head);
        
        // if an expression doesn't have any arguments,
        // don't draw any parentheses
        if (Args.Length == 0) {
            return s.ToString();
        }

        for (int i = 0; i < Args.Length; i++) {
            if (Args[i] is Expression) {
                break;
            }
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

    // Individual constants
    public static readonly Expression SELF    = new Expression(new Constant(INDIVIDUAL, "self"));
    public static readonly Expression ALICE   = new Expression(new Constant(INDIVIDUAL, "alice"));
    public static readonly Expression BOB     = new Expression(new Constant(INDIVIDUAL, "bob"));
    public static readonly Expression CHARLIE = new Expression(new Constant(INDIVIDUAL, "charlie"));
    public static readonly Expression SOUP = new Expression(new Constant(INDIVIDUAL, "soup"));
    public static readonly Expression SWEETBERRY = new Expression(new Constant(INDIVIDUAL, "sweetberry"));
    public static readonly Expression SPICYBERRY = new Expression(new Constant(INDIVIDUAL, "spicyberry"));
    public static readonly Expression FOREST_KING = new Expression(new Constant(INDIVIDUAL, "forest_king"));

    // Individual variables
    public static readonly Expression XE = new Expression(new Variable(INDIVIDUAL, "x"));
    public static readonly Expression YE = new Expression(new Variable(INDIVIDUAL, "y"));
    public static readonly Expression ZE = new Expression(new Variable(INDIVIDUAL, "z"));

    // Truth Value constants
    public static readonly Expression VERUM  = new Expression(new Constant(TRUTH_VALUE, "verum"));
    public static readonly Expression FALSUM = new Expression(new Constant(TRUTH_VALUE, "falsum"));
    public static readonly Expression NEUTRAL = new Expression(new Constant(TRUTH_VALUE, "neutral"));

    // Truth Value variables
    public static readonly Expression ST = new Expression(new Variable(TRUTH_VALUE, "S"));
    public static readonly Expression TT = new Expression(new Variable(TRUTH_VALUE, "T"));
    public static readonly Expression PT = new Expression(new Variable(TRUTH_VALUE, "P"));

    // Predicate constants
    public static readonly Expression RED  = new Expression(new Constant(PREDICATE, "red"));
    public static readonly Expression BLUE = new Expression(new Constant(PREDICATE, "blue"));
    public static readonly Expression GREEN = new Expression(new Constant(PREDICATE, "green"));
    public static readonly Expression YELLOW = new Expression(new Constant(PREDICATE, "yellow"));
    public static readonly Expression APPLE = new Expression(new Constant(PREDICATE, "apple"));
    public static readonly Expression SPICY = new Expression(new Constant(PREDICATE, "spicy"));
    public static readonly Expression SWEET = new Expression(new Constant(PREDICATE, "sweet"));

    // a predicate that applies to any individual
    public static readonly Expression VEROUS = new Expression(new Constant(PREDICATE, "verous"));

    // Predicate variables
    public static readonly Expression FET = new Expression(new Variable(PREDICATE, "F"));
    // @Note I may want a constant called get eventually
    public static readonly Expression GET = new Expression(new Variable(PREDICATE, "G"));

    // 2-place relation constants
    public static readonly Expression IDENTITY = new Expression(new Constant(RELATION_2, "="));
    public static readonly Expression AT       = new Expression(new Constant(RELATION_2, "at"));
    public static readonly Expression ADDED_TO = new Expression(new Constant(RELATION_2, "added_to"));

    // 2-place relation variables
    public static readonly Expression REET = new Expression(new Variable(RELATION_2, "R"));

    // 1-place truth functions
    public static readonly Expression NOT = new Expression(new Constant(TRUTH_FUNCTION, "not"));
    public static readonly Expression TRULY = new Expression(new Constant(TRUTH_FUNCTION, "truly"));

    public static readonly Expression FTF = new Expression(new Variable(TRUTH_FUNCTION, "FTF"));
    public static readonly Expression GTF = new Expression(new Variable(TRUTH_FUNCTION, "GTF"));

    // 2-place truth functions
    public static readonly Expression AND = new Expression(new Constant(TRUTH_FUNCTION_2, "and"));
    public static readonly Expression OR  = new Expression(new Constant(TRUTH_FUNCTION_2, "or"));
    public static readonly Expression IF  = new Expression(new Constant(TRUTH_FUNCTION_2, "if"));
    public static readonly Expression BETTER = new Expression(new Constant(TRUTH_FUNCTION_2, "better"));
    public static readonly Expression AS_GOOD_AS = new Expression(new Constant(TRUTH_FUNCTION_2, "~"));

    // truth-conformity relations
    // "will" is interpreted as an instruction for the actuator in LOT
    // and is interpreted as a promise when expression in public language
    public static readonly Expression WILL  = new Expression(new Constant(TRUTH_CONFORMITY_FUNCTION, "will"));
    public static readonly Expression WOULD = new Expression(new Constant(TRUTH_CONFORMITY_FUNCTION, "would"));

    // individual-truth relations
    public static readonly Expression BELIEVE = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "believe"));
    public static readonly Expression ABLE    = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "able"));
    public static readonly Expression PERCEIVE = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "perceive"));
    public static readonly Expression VERIDICAL = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "veridical"));
    public static readonly Expression TRIED = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "tried"));

    // quantifiers
    public static readonly Expression SOME = new Expression(new Constant(QUANTIFIER, "some"));
    public static readonly Expression ALL  = new Expression(new Constant(QUANTIFIER, "all"));

    // sentential adverbs/quantifiers
    public static readonly Expression ALWAYS = new Expression(new Constant(PROPOSITIONAL_QUANTIFIER, "always"));
    public static readonly Expression SOMETIMES = new Expression(new Constant(PROPOSITIONAL_QUANTIFIER, "sometimes"));
    public static readonly Expression NORMALLY = new Expression(new Constant(PROPOSITIONAL_QUANTIFIER, "normally"));
    public static readonly Expression NORMAL = new Expression(new Constant(PROPOSITIONAL_QUANTIFIER, "normal"));

    // weird function words
    public static readonly Expression ITSELF = new Expression(new Constant(RELATION_2_REDUCER, "itself"));

    // question and assert functions
    public static readonly Expression ASK = new Expression(new Constant(TRUTH_QUESTION_FUNCTION, "ask"));
    public static readonly Expression ASSERT = new Expression(new Constant(TRUTH_ASSERTION_FUNCTION, "assert"));

    // assertion constants
    public static readonly Expression YES = new Expression(new Constant(ASSERTION, "yes"));
    public static readonly Expression NO  = new Expression(new Constant(ASSERTION, "no"));
    public static readonly Expression OK  = new Expression(new Constant(ASSERTION, "ok"));

    // heads for deictic expressions
    public static readonly Constant THAT = new Constant(INDIVIDUAL, "that");
}
