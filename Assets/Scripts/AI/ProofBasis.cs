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
    // 
    // @Note 8/22/22 repititions removed tentatively
    public readonly HashSet<Expression> Premises;
    // a common variable assignment for
    // all formulas in this proof.
    public Substitution Substitution;

    public List<int> MaxValue {get; protected set;}

    public ProofBasis(List<Expression> premises, Substitution substitution) {
        Premises = new HashSet<Expression>(premises);
        Substitution = substitution;

        MaxValue = null;
        foreach (var premise in premises) {
            MaxValue = MentalState.MaxValue(MaxValue, MentalState.ConvertToValue(premise));
        }
    }

    public ProofBasis() : this(new List<Expression>(),
        new Substitution()) {}

    // the product basis a x b.
    public ProofBasis(ProofBasis a, ProofBasis b) {
        var productPremises = new HashSet<Expression>();

        productPremises.UnionWith(a.Premises);
        foreach (var bPremise in b.Premises) {
            productPremises.Add(bPremise.Substitute(a.Substitution));
        }

        var productSubstitution = Compose(a.Substitution, b.Substitution);

        Premises = productPremises;
        Substitution = productSubstitution;
        MaxValue = MentalState.MaxValue(a.MaxValue, b.MaxValue);
    }

    // simple
    public void AddPremise(Expression premise) {
        Premises.Add(premise);
        MaxValue = MentalState.MaxValue(MaxValue, MentalState.ConvertToValue(premise));
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
        foreach (var premise in Premises) {
            s.Append(premise);
            s.Append(", ");
        }
        if (Premises.Count > 0) {
            s.Remove(s.Length - 2, 2);
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

        if (this.Premises.Count != that.Premises.Count ||
            !this.Premises.SetEquals(that.Premises) ||
            this.Substitution.Count != that.Substitution.Count) {
            return false;
        }

        foreach (var thisSub in this.Substitution) {
            if (!that.Substitution.ContainsKey(thisSub.Key)) {
                return false;
            }

            if (!that.Substitution[thisSub.Key].Equals(thisSub.Value)) {
                return false;
            }
        }

        return true;
    }
}

public class ProofBases {
    private HashSet<ProofBasis> ProofBasisCollection;
    public List<int> MaxValue {get; protected set;}

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
        MaxValue = MentalState.MaxValue(MaxValue, basis.MaxValue);
    }

    public void Add(ProofBases bases) {
        ProofBasisCollection.UnionWith(bases.ProofBasisCollection);
        MaxValue = MentalState.MaxValue(MaxValue, bases.MaxValue);
    }

    // corresponds to the sum of two proofs:
    // either basis is sufficient to prove S
    public static ProofBases Join(ProofBases a, ProofBases b) {
        ProofBases ret = new ProofBases();
        ret.ProofBasisCollection.UnionWith(a.ProofBasisCollection);
        ret.ProofBasisCollection.UnionWith(b.ProofBasisCollection);
        ret.MaxValue = MentalState.MaxValue(a.MaxValue, b.MaxValue);
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
        // @Note it's not exactly right to maximize the value
        // and pass it this way. Instead, we only want to
        // pass down the max value we find if it is some
        // augmented version (with omegas and verys) of the
        // content we want to ultimately prove.
        // 
        // To do this right, I think we'd have to return
        // full proof trees and inspect it after we've got it.
        ret.MaxValue = MentalState.MaxValue(a.MaxValue, b.MaxValue);
        return ret;
    }

    public IEnumerator<ProofBasis> GetEnumerator() {
        return ProofBasisCollection.GetEnumerator();
    }

    public override String ToString() {
        StringBuilder s = new StringBuilder();
        s.Append("{\n");
        foreach (var basis in ProofBasisCollection) {
            s.Append("\t" + basis);
            s.Append('\n');
        }
        s.Append('}');
        return s.ToString();
    }
}
