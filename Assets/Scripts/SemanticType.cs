using System.Collections.Generic;

public abstract class SemanticType {}

public abstract class AtomicType : SemanticType {}

// individuals
public class E : AtomicType {}
// truth values
public class T : AtomicType {}

// conformity values
public class C : AtomicType {}
// question (potentially redundant)
public class Q : AtomicType {}
// assertion
public class A : AtomicType {}

public class Arrow : SemanticType {
    private SemanticType[] input;
    private AtomicType output;

    public Arrow(SemanticType[] input, AtomicType output) {
        this.input  = input;
        this.output = output;
    }
}
