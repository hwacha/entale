using System;
using System.Text;
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
}

// A typed empty argument slot.
// Is the '_' in something like 'helps(_, bob)'
public class Empty : Argument {
    public Empty(SemanticType type) {
        Type = type;
    }

    public override String ToString() {
        return "_";
    }

    public override int GetHashCode() {
        return Type.GetHashCode();
    }
}

public class Expression : Argument {
    public Atom Head { get; protected set; }
    protected Argument[] Args;

    // @Note we might make these constructors their own
    // classes if it's easier to think about making
    // Words or Phrases. But I wanted to consolidate a bit.

    // This constructor is a 'word' constructor.
    // It makes a constant or a variable by itself.
    public Expression(Atom head) {
        Type = head.Type;
        Head = head;

        // we check if the head expression has an atomic type.
        // if it does, then we just initialize an empty array
        // since this expression doesn't take any arguments.
        if (Head.Type is AtomicType) {
            Args = new Argument[0];
            return;
        }

        // we're going to populate the argument array with
        // empty slots that are typed according to the
        // semantic type of the functional head expression.
        FunctionalType fType = head.Type as FunctionalType;
        int fNumArgs = fType.GetNumArgs();
        Args = new Argument[fNumArgs];
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
                    Type = Type.Remove(argumentToPlace.Type);
                }

                inputIndex++;
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
    }

    public Argument GetArg(int i) {
        return Args[i];
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();

        s.Append(Head);
        
        // if an expression doesn't have any arguments,
        // don't draw any parentheses
        if (Args.Length == 0) {
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
        Expression that = o as Expression;

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

    // Individual variables
    public static readonly Expression XE = new Expression(new Variable(INDIVIDUAL, "x"));
    public static readonly Expression YE = new Expression(new Variable(INDIVIDUAL, "y"));
    public static readonly Expression ZE = new Expression(new Variable(INDIVIDUAL, "z"));

    // Truth Value constants
    public static readonly Expression VERUM  = new Expression(new Constant(TRUTH_VALUE, "verum"));
    public static readonly Expression FALSUM = new Expression(new Constant(TRUTH_VALUE, "falsum"));

    // Truth Value variables
    public static readonly Expression ST = new Expression(new Variable(TRUTH_VALUE, "S"));
    public static readonly Expression TT = new Expression(new Variable(TRUTH_VALUE, "T"));

    // Predicate constants
    public static readonly Expression RED  = new Expression(new Constant(PREDICATE, "red"));
    public static readonly Expression BLUE = new Expression(new Constant(PREDICATE, "blue"));

    // Predicate variables
    public static readonly Expression FET = new Expression(new Variable(PREDICATE, "F"));
    // @Note I may want a constant called get eventually
    public static readonly Expression GET = new Expression(new Variable(PREDICATE, "G"));

    // 2-place relation constants
    public static readonly Expression IDENTITY = new Expression(new Constant(RELATION_2, "="));
    public static readonly Expression AT       = new Expression(new Constant(RELATION_2, "at"));

    // 2-place relation variables
    public static readonly Expression REET = new Expression(new Variable(RELATION_2, "R"));

    // 1-place truth functions
    public static readonly Expression NOT = new Expression(new Constant(TRUTH_FUNCTION, "not"));

    // 2-place truth functions
    public static readonly Expression AND = new Expression(new Constant(TRUTH_FUNCTION_2, "and"));
    public static readonly Expression OR  = new Expression(new Constant(TRUTH_FUNCTION_2, "or"));

    // truth-conformity relations
    // "will" is interpreted as an instruction for the actuator in LOT
    // and is interpreted as a promise when expression in public language
    public static readonly Expression WILL  = new Expression(new Constant(TRUTH_CONFORMITY_FUNCTION, "will"));
    public static readonly Expression WOULD = new Expression(new Constant(TRUTH_CONFORMITY_FUNCTION, "would"));

    // individual-truth relations
    public static readonly Expression BELIEVE = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "believe"));
    public static readonly Expression ABLE    = new Expression(new Constant(INDIVIDUAL_TRUTH_RELATION, "able"));
}
