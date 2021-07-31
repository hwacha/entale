Paraconsistency in the Knowledge State
  For a mental state M, The knowledge base KB = {Ki} such that each sentence Ki, and every sentence entailed by KB constitute all the propositions known in M.

  If a sentence S is not contained within or is not entailed by KB, it is assumed not to be known in M. If a sentence's negation \~S is also not known in M, then neither S nor \~S is known in M. That is, the question of whether or not S is left open. In this way, the knowledge state is K-paracomplete.

  When neither S nor \~S is known in M, then neither are generally used in further reasoning. A way to 'close' the question of whether S is to include a rule such as:

  M |/- \~S => M |- S

  That is, if \~S is not known in M, then S is known in M. This closes the question of S in M in the absence of sufficient, positive evidence for S in M.

  When a sentence S is asserted in M, it is first checked whether it's already known in M. If it is, then no change is made to KB. If it isn't, then it's added into KB. S, and any sentence entailed by S, is now known in M. If there were rules that 'closed' the question of S by assuming \~S, these rules no longer apply. For example, suppose we had a rule like the following:

  M |/- rain => M |- \~rain

  In M, it is known it isn't raining until it is positively known that it _is_.

  When asserting a sentence S in M, it shouldn't only be checked whether S is already, but rather whether \~S is known. If S is known in M and S is added to KB, M is K-inconsistent, in that both S and \~S are known.

  However, we can make our system K-paraconsistent by analogy to how we made it K-paracomplete. Just as our system can handle knowing neither A nor \~A without violating the law of excluded middle, our system can know both A and not \~A without violating the law of non-contradiction. When both A and \~A are known, the question of whether A is _doubly_ closed. We only reason from A or \~A when the question of whether A is _singly_ closed. So, we can know A and know \~A but fail to know (A & \~A). This means, when we want to reason from S we must not only know S, but not know \~S. Vice versa for \~S.

  Thus the knowledge base is not only K-paracomplete but also K-paraconsistent. Just like we want rules to force closure of a question whether P when the question is otherwise open, we want rules to force opening of the question of P when it's doubly-closed. That is, we have rules of the following form.

  M |- S => M |/- \~S

  This is the converse of the 'closure' rule schema stated above. Rules that 'open' doubly-closed questions generally don't have to do with the content of S but rather their evidential source. But to simplify the discussion let's ignore that for now.

  M |- sunny => M |/- \~sunny

  This rule states that, if it's known to be sunny, then it's not known not to be sunny. This rule would be vacuously true on a K-consistent reading of knowledge, but here we don't take it for granted.

  In the context of the program, 'closure' rules apply as assumption nodes. If a recursive node is pushed onto the call stack as an assumption node, it passes on 'no proof' if a proof is found, and a proof containing 'I don't know S' if no proof of 'S' is found. So, the 'closure' rule is naturally overridden when S is known in M.

  If we treat 'opening' rules analogously, we want them to override normal proof search. In other words, even if S is entailed by KB, we want S not to be known if it's on the end of an active 'opening' rule. Let's suppose we're trying to prove '\~sunny'. First, we try to prove 'sunny'. If we succeed, then we stop right there, and fail to prove '\~sunny'.

  Suppose KB contains see(sunny) and hear(\~sunny). Should M fail to prove hear(\~sunny)? Yes - we have to work 'forwards' recursively to check for *overriding* ignorance of '\~sunny' to prove ignorance of 'hear(\~sunny)'. (Normal ignorance of '\~sunny' is to be expected.)

  This handles cases of rebutting defeaters.

  Another approach is to modify the sentences in KB when an inconsistency is found and can be forced open.

  K(A), A |- B, K(B)

  \~K(\~A), A |- B, \~K(\~B)

# Scratchpad

A & \*A <- THIS possibility is excluded


M, C |- A, M, C |- B => C -> A & B


A, B, \*\~A    A, B, \*\~B
---------,   ---------
  A & B        A & B

CK(P) & CK(Q) -> CK(P & Q) v (CK(\~P) & CK(\~Q))

CK(A v B) & (CK(\~A) v CK(\~B)) -> CK(A) v CK(B)
  

A v B, \~A, \*A   A v B, \~B, \*B
-------------,  -------------
     B                A

A v B
-----
A,  B

      \*A
-------------
\*(A v B), \*\~B

    A
---------
A & B, \~B

normally(A), \*\~A
----------------
       A

unless_not(A), \~A
-----------------
       \*A

Let BCR = black clouds -> rain

M = {
  normally(\~rain)
  unless_not(wildfire -> \~BCR)
  normally(wildfire -> \~BCR)
  unless_not(BCR)
  normally(BCR)
}


M U= black clouds

rain?

normally(BCR), \*\~BCR
--------------------
         BCR,            black clouds
         ----------------------------
                   rain


M U= wildfire

rain?
                    wildfire -> \~BCR, wildfire
                    --------------------------
unless_not(BCR),                \~BCR
------------------------------------
                \*BCR,                 black clouds
                ----------------------------------
                           (\*)rain

unmarked ignorance < marked knowledge < marked ignorance
