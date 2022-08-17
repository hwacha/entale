using System;
using System.Collections.Generic;
using System.Text;

using static Expression;

public class InferenceRule
{
    // let's try to implement some simple inference rules
    // without using variable binding
    string Name;
    Predicate<Expression> Condition;
    Func<Expression, List<Expression>> Selector;

    public InferenceRule(string name, Predicate<Expression> condition, Func<Expression, List<Expression>> selector) {
        Name = name;
        Condition = condition;
        Selector = selector;
    }

    public List<Expression> Apply(Expression e) {
        if (Condition(e)) {
            return Selector(e);
        }
        return null;
    }

    public override string ToString() {
        return Name;
    }
}
