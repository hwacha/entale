using System;
using System.Collections.Generic;
using UnityEngine;

// TODO in order to make the expression trie
// work in cases of expressions with multiple
// arguments, we need to have an Expression
// 'Forest' which constists of an array of
// these expression tries. Lookup would then
// happen jointly for each entry in an
// expression forist.
public class ExpressionTrie {
    private class HeadSymbolTrie {
        Atom Head;
        bool IsTerminal;
        ExpressionTrie NextExpressions;

        public HeadSymbolTrie(Atom head) {
            // TODO
            Head = head;
        }

        public void Add(Expression e) {
            for (int i = 0; i < e.NumArgs; i++) {
                var arg = e.GetArg(i);
                // TODO
            }
        }
    }

    private class HeadTypeTrie {
        SortedList<Atom, HeadSymbolTrie> HeadSymbolNodes;

        public HeadTypeTrie() {
            HeadSymbolNodes = new SortedList<Atom, HeadSymbolTrie>();
        }

        public void Add(Expression e) {
            var head = e.Head;
            if (!HeadSymbolNodes.ContainsKey(head)) { 
                HeadSymbolNodes.Add(head, new HeadSymbolTrie());
            }

            HeadSymbolNodes[head].Add(e);
        }
    }

    SemanticType Type;
    SortedList<SemanticType, HeadTypeTrie> HeadTypeNodes;
    
    // tree constructor
    public ExpressionTrie(SemanticType type) {
        Type = type;
        HeadTypeNodes = new SortedList<SemanticType, HeadTypeTrie>();
    }

    public void Add(Expression e) {
        Debug.Assert(Type.Equals(e.Type));

        var headType = e.Head.Type;
        if (!HeadTypeNodes.ContainsKey(headType)) {
            HeadTypeNodes.Add(headType, new HeadTypeTrie());
        }

        HeadTypeNodes[headType].Add(e);
    }
}
