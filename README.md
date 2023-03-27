# entale
An open-world game with autonomous agents the player can freely talk to using a robust language system.

https://user-images.githubusercontent.com/6621013/126084500-c607889f-d8ab-4b7e-839c-0fa852b7c572.mov

## How To Use

### Controls
Controls are currently best on a Playstation controller. Use the `Left Analog Stick` to move and `Right Analog Stick` to look around. Press `X` to jump.

Press `Square` to pull up the language workspace. There, press `Square` to bring up a radial menu to select a word to place onto the workspace. While in the radial menu, use the `Left Analog Stick` to highlight a word or word type, and press `X` to select it. In the workspace, move the cursor with the directional pad, and press `X` to select a word. When a word is selected, a new cursor will show argument slots where the selected expression may be placed. Press `X` to combine the expressions accordingly. While in the workspace or word menu, you may press `Circle` to return to the previous menu or to the game.

You may also press `Triangle` when an expression is selected to equip the expression. With an expression equipped, press `Triangle` while looking at an NPC to say the equipped expression to them. You may also press `Square` to place the equipped expression back in the workspace.  Pressing `Circle` will unequip an equipped word without placing it back in the workspace.

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
