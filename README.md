# entale
An open-world game with autonomous agents the player can freely talk to using a robust language system.

https://user-images.githubusercontent.com/6621013/126084500-c607889f-d8ab-4b7e-839c-0fa852b7c572.mov

## How To Use

### Controls
Use ``WASD`` to **move** and the mouse to look around. To bring up the **workspace** press ``TAB``. To bring up the **word** menu press ``TAB`` again. To add a word, hover over an option to select a type with a mouse ``Click``, and then hover over an option to select a word. To exit either menu press ``Escape``.

When in the **workspace** use WASD or the ``Arrow Keys`` to focus on an expression. To **select** an expression, press ``Enter``. When an expression is selected, you can **use** it by pressing ``Q`` or you can combine it with another expression by pressing ``Enter`` on an open argument slot.

If you've pressed ``Q`` on an expression, the workspace should disappear and you'll have the expression in front of you. You can **say** this expression to an NPC by looking at an NPC and pressing ``Q``.

### How to Play
Use the workspace to combine expressions and form sentences to say to NPCs. These sentences all mean something different, and will have a different effect on the NPC. You can make requests, ask questions, or assert facts about the game world. The NPC will respond accordingly.

At this stage of development, most focus has been put on the AI system behind the scenes, so there isn't much content to try out, and most of it is opaque to the player. In the future, the language will be tutorialized so the player learns how to use it over the course of play. Also, there will be more gameplay and NPC behavior available to contextualize the use of the language.

## Features

### Language
The language of Entale is a constructed language with a unique tree-structure appearance and color-coded grammatical type system. The player uses this language to communicate to NPCs, who use it to communicate with each other. The language is also used internally in the agent's reasoning and planning.

One of the central gameplay challenges is for the player to learn this language. No translation manual is provided - the player must figure it out through careful observation and experimentation.

### AI
Agents respresent knowledge of the game world, reason to answer questions and make plans, and communicate with each other using a language system. AI logical reasoning has come the farthest. Yet to be implemented are features like utility/value determination, inductive reasoning, theory of mind, pragmatic reasoning, spatial reasoning and navigation, efficient visual processing, time, event, and action-based reasoning.

Another central component of gameplay is the player's interacting with these agents socially. The player must work to understand, persuade, make trades with, cooperate with, etc., AI agents to move forward.

### Highly dynamic open-world
The AI agents will be affected by the player and, in general, the game-events around them. They will remember them, and change how they act accordingly. This will make the single-player experience be more significant and intimate.
