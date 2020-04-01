using System;
using System.Text;
using System.Collections.Generic;

public abstract class SemanticType {
    public static readonly AtomicType INDIVIDUAL = new E();
    public static readonly AtomicType TRUTH_VALUE = new T();
    public static readonly AtomicType CONFORMITY_VALUE = new C();
    public static readonly AtomicType ASSERTION = new A();
    public static readonly AtomicType QUESTION = new Q();
    public static readonly FunctionalType PREDICATE =
        new FunctionalType(new SemanticType[]{INDIVIDUAL}, TRUTH_VALUE);
    public static readonly FunctionalType RELATION_2 =
        new FunctionalType(new SemanticType[]{INDIVIDUAL, INDIVIDUAL}, TRUTH_VALUE);
}

public abstract class AtomicType : SemanticType {}

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

public class FunctionalType : SemanticType {
    private SemanticType[] input;
    public AtomicType output {get; private set;}

    public FunctionalType(SemanticType[] input, AtomicType output) {
        for (int i = 0; i < input.Length; i++) {
            if (input[i] == null) {
                throw new ArgumentException("SemanticType constructor: inputs must not be null");
            }
        }
        this.input  = input;
        this.output = output;
    }

    public int GetNumArgs() {
        return input.Length;
    }

    public SemanticType GetInput(int index) {
        return input[index];
    }

    public override bool Equals(Object o) {
        if (o.GetType() != typeof(FunctionalType)) {
            return false;
        }

        FunctionalType that = (FunctionalType) o;

        if (input.Length != that.GetNumArgs()) {
            return false;
        }
        
        for (int i = 0; i < input.Length; i++) {
            if (input[i] != that.GetInput(i)) {
                return false;
            }
        }

        return output.Equals(that.output);
    }

    public override int GetHashCode() {
        int hash = 5381 * output.GetHashCode();
        for (int i = 0; i < input.Length; i++) {
            hash = 33 * hash + (input[i] == null ? i : input[i].GetHashCode());
        }
        return hash;
    }

    public override string ToString() {
        StringBuilder s = new StringBuilder();
        s.Append("(");

        for (int i = 0; i < input.Length; i++) {
            s.Append(input[i].ToString());
            s.Append(", ");
        }

        if (s.Length > 1) {
            s.Remove(s.Length - 2, 2);
        }

        s.Append(" -> " + output.ToString() + ")");

        return s.ToString();
    }
}
