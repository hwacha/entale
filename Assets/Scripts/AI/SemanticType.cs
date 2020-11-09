using System;
using System.Text;
using System.Collections.Generic;

// the type of an expression.
// The type determines the kind of thing an expression refers to (semantics).
// 
// expressions of type 'e' refer to individuals like Bob or Mount Everest,
// 
// expressions of type 't' refer to truth values, 1 or 0,
// Donald Trump is president = 1, Bob Dole is president = 0
// 
// expressions of type 'e -> t' refer to predicates, which either
// hold or don't hold of individuals i.e. "runs", or "green"
// 
// they're a function that maps an individual to
// 1 if the predicate holds
//   (so runs(Bob) -> 1 because Bob does run) and
// 0 if the predicate doesn't hold
//   (so runs(Anthony) -> 0 because Anthony doesn't run)
//   
// the grammar of the language also determines what expressions can combine. 
// i.e. runs can only combine with expressions of type 'e' because
// they consume an individual as input.
// i.e. runs(Bob) is well-formed but runs(green) is not.
// 
// expressions with a functional type combine with expressions of their
// input types to yield an expression of the output type.
// expressions can also partially apply to some but not all of their inputs.
public abstract class SemanticType {
    // Removes this type from the input of the semantic type,
    // or if no matching input type exists,
    // return the type itself
    public virtual SemanticType RemoveAt(int index) {
        return this;
    }

    // if you can partially apply arguments to an expression with that type
    // and get an expression of this type, then return true.
    // 
    // e -> t is a parital application of e, e -> t
    // 
    public abstract bool IsPartialApplicationOf(SemanticType that);

    // references to the atomic types.
    public static readonly AtomicType INDIVIDUAL = new E();
    public static readonly AtomicType TRUTH_VALUE = new T();
    public static readonly AtomicType CONFORMITY_VALUE = new C();
    public static readonly AtomicType ASSERTION = new A();
    public static readonly AtomicType QUESTION = new Q();

    // references to common functional types.
    public static readonly FunctionalType PREDICATE =
        new FunctionalType(new SemanticType[]{INDIVIDUAL}, TRUTH_VALUE);
    public static readonly FunctionalType RELATION_2 =
        new FunctionalType(new SemanticType[]{INDIVIDUAL, INDIVIDUAL}, TRUTH_VALUE);

    public static readonly FunctionalType DETERMINER =
        new FunctionalType(new SemanticType[]{PREDICATE}, INDIVIDUAL);

    public static readonly FunctionalType QUANTIFIER_PHRASE =
        new FunctionalType(new SemanticType[]{PREDICATE}, TRUTH_VALUE);
    public static readonly FunctionalType QUANTIFIER =
        new FunctionalType(new SemanticType[]{PREDICATE, PREDICATE}, TRUTH_VALUE);
    public static readonly FunctionalType QUANTIFIER_PHRASE_COORDINATOR_2 =
        new FunctionalType(new SemanticType[]{
            RELATION_2, QUANTIFIER_PHRASE, QUANTIFIER_PHRASE}, TRUTH_VALUE);

    public static readonly FunctionalType TRUTH_FUNCTION =
        new FunctionalType(new SemanticType[]{TRUTH_VALUE}, TRUTH_VALUE);
    public static readonly FunctionalType TRUTH_FUNCTION_2 =
        new FunctionalType(new SemanticType[]{TRUTH_VALUE, TRUTH_VALUE}, TRUTH_VALUE);

    public static readonly FunctionalType TRUTH_QUESTION_FUNCTION =
        new FunctionalType(new SemanticType[]{TRUTH_VALUE}, QUESTION);
    public static readonly FunctionalType TRUTH_ASSERTION_FUNCTION =
        new FunctionalType(new SemanticType[]{TRUTH_VALUE}, ASSERTION);
    public static readonly FunctionalType TRUTH_CONFORMITY_FUNCTION =
        new FunctionalType(new SemanticType[]{TRUTH_VALUE}, CONFORMITY_VALUE);

    public static readonly FunctionalType INDIVIDUAL_TRUTH_RELATION =
        new FunctionalType(new SemanticType[]{INDIVIDUAL, TRUTH_VALUE}, TRUTH_VALUE);

    public static readonly FunctionalType RELATION_2_REDUCER =
        new FunctionalType(new SemanticType[]{RELATION_2, INDIVIDUAL}, TRUTH_VALUE);
    public static readonly FunctionalType RELATION_2_MODIFIER =
        new FunctionalType(new SemanticType[]{RELATION_2, INDIVIDUAL, INDIVIDUAL}, TRUTH_VALUE);

    public static readonly FunctionalType GEACH_TRUTH_FUNCTION =
        new FunctionalType(new SemanticType[]{TRUTH_FUNCTION, PREDICATE, INDIVIDUAL}, TRUTH_VALUE);
    public static readonly FunctionalType GEACH_TRUTH_FUNCTION_2 =
        new FunctionalType(new SemanticType[]{TRUTH_FUNCTION_2, PREDICATE, PREDICATE, INDIVIDUAL}, TRUTH_VALUE);

    public static readonly FunctionalType GEACH_QUANTIFIER_PHRASE =
        new FunctionalType(new SemanticType[]{QUANTIFIER_PHRASE, RELATION_2, INDIVIDUAL}, TRUTH_VALUE);

    public static readonly FunctionalType PROPOSITIONAL_QUANTIFIER =
        new FunctionalType(new SemanticType[]{TRUTH_FUNCTION, TRUTH_FUNCTION}, TRUTH_VALUE);
}

public abstract class AtomicType : SemanticType {
    public override bool IsPartialApplicationOf(SemanticType that) {
        if (that is AtomicType) {
            return this.Equals(that);
        }

        return this.Equals(((FunctionalType) that).Output);
    }
}

public class FunctionalType : SemanticType {
    private SemanticType[] Input;
    public AtomicType Output {get; private set;}

    public FunctionalType(SemanticType[] input, AtomicType output) {
        for (int i = 0; i < input.Length; i++) {
            if (input[i] == null) {
                throw new ArgumentException("SemanticType constructor: inputs must not be null");
            }
        }
        Input  = input;
        Output = output;
    }

    public int GetNumArgs() {
        return Input.Length;
    }

    public SemanticType GetInput(int index) {
        return Input[index];
    }

    public override SemanticType RemoveAt(int index) {
        if (index < 0 || index >= Input.Length) {
            throw new ArgumentException(this + ".RemoveAt: index " + index + " out of bounds.");
        }
        // if the functional type has only one argument,
        // it reduces to an atomic type.
        if (Input.Length == 1) {
            return Output;
        }

        // otherwise we remove one instance of the input type
        // from the input types.
        SemanticType[] newInput = new SemanticType[Input.Length - 1];

        for (int i = 0; i < Input.Length; i++) {
            if (i == index) {
                while (i < Input.Length - 1) {
                    newInput[i] = Input[i + 1];
                    i++;
                }
                return new FunctionalType(newInput, Output);
            }
            newInput[i] = Input[i];
        }

        // if there were none to remove, simply return the original type.
        return this;
    }

    public override bool IsPartialApplicationOf(SemanticType that) {
        if (that is AtomicType) {
            return false;
        }

        var thatFunctional = (FunctionalType) that;

        int indexOfThisFirstInputInThat = -1;
        for (int i = 0; i < thatFunctional.Input.Length; i++) {
            if (thatFunctional.Input[i].Equals(this.Input[0])) {
                indexOfThisFirstInputInThat = i;
                break;
            }
        }

        if (indexOfThisFirstInputInThat == -1) {
            return false;
        }
        
        return this.RemoveAt(0).IsPartialApplicationOf(thatFunctional.RemoveAt(indexOfThisFirstInputInThat));
    }

    public override bool Equals(Object o) {
        if (o.GetType() != typeof(FunctionalType)) {
            return false;
        }

        FunctionalType that = (FunctionalType) o;

        if (Input.Length != that.GetNumArgs()) {
            return false;
        }
        
        for (int i = 0; i < Input.Length; i++) {
            if (Input[i] != that.GetInput(i)) {
                return false;
            }
        }

        return Output.Equals(that.Output);
    }

    public override int GetHashCode() {
        int hash = 5381 * Output.GetHashCode();
        for (int i = 0; i < Input.Length; i++) {
            hash = 33 * hash + (Input[i] == null ? i : Input[i].GetHashCode());
        }
        return hash;
    }

    public override string ToString() {
        StringBuilder s = new StringBuilder();
        s.Append("(");

        for (int i = 0; i < Input.Length; i++) {
            s.Append(Input[i].ToString());
            s.Append(", ");
        }

        if (s.Length > 1) {
            s.Remove(s.Length - 2, 2);
        }

        s.Append(" -> " + Output.ToString() + ")");

        return s.ToString();
    }
}

// individuals
public class E : AtomicType {
    public override string ToString() {
        return "e";
    }
}

// truth values
public class T : AtomicType {
    public override string ToString() {
        return "t";
    }
}

// conformity values
public class C : AtomicType {
    public override string ToString() {
        return "c";
    }
}

// question (potentially redundant)
public class Q : AtomicType {
    public override string ToString() {
        return "q";
    }
}

// assertion
public class A : AtomicType {
    public override string ToString() {
        return "a";
    }
}
