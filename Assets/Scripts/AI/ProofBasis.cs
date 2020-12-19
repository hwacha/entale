using System;
using System.Text;
using System.Collections.Generic;

using Substitution = System.Collections.Generic.Dictionary<Variable, Expression>;

//
// a representation of the 'essential' parts of a proof for
// a sentence S according to a belief state. It doesn't
// represent the tree-structure of a proof. It just keeps
// track of the 'open premsises': what is taken for granted
// as an assumption or part of the belief base in order for
// this proof to succeed. Also includes 'resolutions' if
// the proof is a plan.
// 
// Also, the variable assignment for the proof of a formula
// is keep track of, so that it's passed on recursively if
// any other proofs depend on it. We want the same variable
// assignment accross the same proof.
// 
public class ProofBasis {
    // premises are a list to preserve the
    // order of resolutions: if one resolution
    // depends on a further resolution, the latter
    // should be executed first.
    // 
    // there may be repetition of premises, but
    // this won't spell trouble for the proof,
    // and may be necessary for resolutions.
    public readonly List<Expression> Premises;
    // a common variable assignment for
    // all formulas in this proof.
    public Substitution Substitution;

    public ProofBasis(List<Expression> premises,
        Substitution substitution) {
        Premises = premises;
        Substitution = substitution;
    }

    public ProofBasis() : this(new List<Expression>(),
        new Substitution()) {}

    // the product basis a x b.
    public ProofBasis(ProofBasis a, ProofBasis b) {
        var productPremises = new List<Expression>();

        productPremises.AddRange(a.Premises);
        foreach (var bPremise in b.Premises) {
            productPremises.Add(bPremise.Substitute(a.Substitution));
        }

        var productSubstitution = Compose(a.Substitution, b.Substitution);

        Premises = productPremises;
        Substitution = productSubstitution;
    }

    // simple
    public void AddPremise(Expression premise) {
        Premises.Add(premise);
    }

    // composes two substitutions together a * b,
    // according the rule that the A[a * b] = (A[a])[b] 
    public static Substitution Compose(Substitution a, Substitution b) {
        Substitution composition = new Substitution();

        foreach (KeyValuePair<Variable, Expression> bAssignment in b) {
            composition[bAssignment.Key] = bAssignment.Value;
        }

        // here, we override the previous value if there is one.
        foreach (KeyValuePair<Variable, Expression> aAssignment in a) {
            composition[aAssignment.Key] = aAssignment.Value;
        }

        return composition;
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();
        s.Append("<");
        if (Premises.Count > 0) {
            s.Append(Premises[0]);
            for (int i = 1; i < Premises.Count; i++) {
                s.Append(", ");
                s.Append(Premises[i]);
            }
        }

        s.Append("> with {");
        foreach (KeyValuePair<Variable, Expression> assignment in Substitution) {
            s.Append(assignment.Key);
            s.Append(" -> ");
            s.Append(assignment.Value);
            s.Append(", ");
        }
        s.Append("}");

        return s.ToString();
    }

    public override int GetHashCode() {
        int hash = 5381;
        foreach (var p in Premises) {
            hash = 33 * hash + p.GetHashCode();
        }

        foreach (var sub in Substitution) {
            hash = 29 * hash + sub.Key.GetHashCode();
            hash = 23 * hash + sub.Value.GetHashCode();
            hash %= 100000000;
        }
        return hash;
    }

    public override bool Equals(object o) {
        if (!(o is ProofBasis)) {
            return false;
        }

        ProofBasis that = o as ProofBasis;

        if (this.Premises.Count != that.Premises.Count) {
            return false;
        }

        for (int i = 0; i < Premises.Count; i++) {
            if (!this.Premises[i].Equals(that.Premises[i])) {
                return false;
            }
        }

        if (this.Substitution.Count != that.Substitution.Count) {
            return false;
        }

        foreach (var assignment in this.Substitution.Keys) {
            if (!that.Substitution.ContainsKey(assignment)) {
                return false;
            }

            if (!this.Substitution[assignment].Equals(that.Substitution[assignment])) {
                return false;
            }
        }

        return true;
    }
}

public class ProofBases {
    private HashSet<ProofBasis> ProofBasisCollection;

    public ProofBases() {
        ProofBasisCollection = new HashSet<ProofBasis>();
    }

    public void Clear() {
        ProofBasisCollection.Clear();
    }

    public bool IsEmpty() {
        return ProofBasisCollection.Count == 0;
    }

    public bool Contains(ProofBasis basis) {
        return ProofBasisCollection.Contains(basis);
    }

    public void Add(ProofBasis basis) {
        ProofBasisCollection.Add(basis);
    }

    public void Add(ProofBases bases) {
        ProofBasisCollection.UnionWith(bases.ProofBasisCollection);
    }

    // corresponds to the sum of two proofs:
    // either basis is sufficient to prove S
    public static ProofBases Join(ProofBases a, ProofBases b) {
        ProofBases ret = new ProofBases();
        ret.ProofBasisCollection.UnionWith(a.ProofBasisCollection);
        ret.ProofBasisCollection.UnionWith(b.ProofBasisCollection);
        return ret;
    }

    // corresponds to the product of two proofs:
    // both bases are necessary to prove S
    public static ProofBases Meet(ProofBases a, ProofBases b) {
        ProofBases ret = new ProofBases();

        foreach (var aBasis in a.ProofBasisCollection) {
            foreach (var bBasis in b.ProofBasisCollection) {
                ret.Add(new ProofBasis(aBasis, bBasis));
            }
        }
        return ret;
    }

    public IEnumerator<ProofBasis> GetEnumerator() {
        return ProofBasisCollection.GetEnumerator();
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();
        s.Append("{\n");
        foreach (var basis in ProofBasisCollection) {
            s.Append(basis);
            s.Append('\n');
        }
        s.Append('}');
        return s.ToString();
    }
}
