# Roadmap for Entale

## AI System

### Inconsistency resolution
[Paraconsistency in the Knowledge State](paraconsistency.md)

When an inconsistency occurs, and there is a rule to 'force down' a sentence, we should 'conserve' as much as doesn't contradict coknowledge rules.

e.g. if knows(red(bob), alice) is overriden because red(bob) is overriden in M, df(knows(red(bob), alice)) should still be proven in M. This will involve 'sideways' entailment paths that aren't blocked by marked ignorance.

If there are multiple ways to break the link of proof from M to A, there needs to a procedure to decide which in the line of proof to fallibilize.

e.g. if make(knows(P, bob), alice) is in our knowledge base and \*red(charlie), we have at least seven options in how to fallibilize:

make(knows(was(P), bob), alice)
  'alice let bob know it was P'
make(df(knows(P, bob)), alice)
  'alice makes bob believe P'
make(df(knows(was(P), bob)), alice)
  'alice makes bob believe it was P'
df(make(knows(P, bob), alice))
  'alice tried to let bob know P'
df(make(knows(was(P), bob), alice))
  'alice tried to let bob know it was P'
df(make(df(knows(P, bob)), alice))
  'alice tried to make bob believe P'
df(make(df(knows(was(P), bob)), alice))
  'alice tried to make bob believe it was P'

Is there a way to tell which one to do? Is more information needed?

### Valuation, decision making and planning
Problem: should only real knowledge be acted upon? i.e. K(P) and ~K(~P)?

- Implement the value estimation algorithm described in the wiki

### Memory
- Keep track of how many times a given sentence is used in proofs. If it's used a lot, keep it in hot memory. If it's used less often, store it away. Generally, the reasons that entail a salient sentence are 'forgotten', and the important proposition is simply recorded explicitly in hot memory. The 'forgotten' sentences can be retrieved if necessary, but more slowly.

### Efficient perception
- Change to a 'rasterizer' approach - give each NPC a camera, and then do some sort of callback to get the objects that are in view. Maybe this means having to use a custom buffer or a raycast in tandem. Each visible Unity object will either be a conceptualizable object or part of one. The Unity object will be broken up into children, some parts of which uniquely identify an object as being a certain kind of thing.

- NO COMPUTER VISION! We should be able to skip any sort of complicated classification business.

### Efficient spatial navigation and reasoning
- Some sort of internal map representation that interacts systematically with the language. That way we can get places intelligently.

- 'at' logic and actuator must work together to get the NPC in the right place to perform tasks, after getting to the general vicinity with higher-level instructions

### Inductive reasoning
- Already do default reasoning
- Bayesian reasoning?
- Statistical reasoning?
- Popper-style falsificationism could be a fun try for universal introduction as a default
- Inference to the best explanation

### Theory of Mind/Pragmatics
- Rules for knowledge, belief, desires, intentions, actions
- Once the framework for those is settled a lot of pragmatic interpretation should fall out of it

### Personality
- Don't want all NPCs to act the same. How to differentiate past initial beliefs?
- If inductive/ampliative reasoning is partly a matter of convention, then people can have different conventions
- Emotional dispositions, change the way fast processing happens
- Risk aversion/various decision strategies
- Different preferences and values
- Cultural variation
- Style/expression in language and dress

## UI
We need to extend the UI for making expressions in language. For one, the UI can't do everything it needs to, e.g. it can't hold an indefinite number of expressions. So features should be added. Especially crucial is making sentence construction as fast and seemless as possible. Getting the UI as fast and smooth as possible is essential to the game, the same way designers made sure Mario's running and jumping was as fun as possible in Super Mario 64!

### Workspace zoom, scroll, auto-expand
There should be any number of arbitrarily large expressions on the workspace. To accommodate this, the workspace should automatically expand to fit all expressions on the workspace. A *zoom* and *scroll* feature should be implemented to zoom in on subexpressions, zoom out, and move up, down, left, right.

### Speed/Ease of use

#### Improved cursor mobility and cursor feel
The cursor is janky now. Need to make it more responsive and tighter. Also select, deselect, etc. should be done more crisply. Also, make button layout more intuitive.

#### Argument completion
Right now, expressions are formed 'bottom-up' by placing already-formed subexpressions into the argument slots of functional expressions to form a larger expression. We can also add a 'top-down' placement through clever type-completion. The player can only

### Type System
Add a richer type system to deal with questions and other possible additions like type-restricting factive sentences and various modalities like 'good' that shouldn't iterate, etc.

The best option to work with the color system is a sum operator for types. This allows a set of types to form a sum supertype to which any of its constituent subtypes can enter into. The color of the sum type would be a voronoi diagram or other such mixture of the subtype colors.

### Clitics
Along these lines is to add clitics which don't adhere to the typical expression construction. So, for example, clitics mark the scope of quantifiers if they are made into determiners. The UI should allow the player to swap the clitics to rearrange scope.

### Particle Effects
Add a little bit of movement to the language cards. Also add some weight to the placement/removal of expressions within each other. Add extra effects to highlight which arguments will accept a given expression. And so on.

## Content/Narrative
TBD