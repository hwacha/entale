# entale
An open-world game with autonomous agents that the player can freely talk to using a robust language system.

## Features

### Movement

#### Functionality 
First person movement.

**Controls**:
- *wasd*/*arrowkeys* to move.
- *mouse* to look around.

### Notes
- There is no gravity or collision correction.

#### Code
- [PlayerMovement.cs](Assets/Scripts/PlayerMovement.cs)
- [MouseLook.cs](Assets/Scripts/MouseLook.cs)

### Highlighting

#### Functionality
Certain objects in the world are highlightable (`highlightable` layer) to indicate `this` or `that` in the language.
Considering you might want to refer to a couple of things at once, there are different registers that you can store `this`/`that` references in. They will be highlighted in different colors.

**Controls**:
- *space* to toggle through highlight registers.
  - There is a box on the top right which will indicate which register you're in by showing the color. Note that one of these registers is neutral (the box will be white).
- *point and click* to highlight an object.
  - The pointer will turn the color of the register if the object is highlightable.

#### Notes 
- This is currently just an aesthetic change. There isn't a property set on the game object that indicates that it's highlighted.
- Only one object can be highlighted for each register and an object can only be highlighted by one register.

#### Code
- [Highlighter.cs](Assets/Scripts/Highlighter.cs)
- An object needs to be in the `highlightable` layer and have the shader [Highlightable.shader](https://github.com/hwacha/entale/blob/master/Assets/Shader/Highlightable.shader) to be highlightable.

#### Images

![highlight demo](https://user-images.githubusercontent.com/3184499/77706284-516c8500-6f98-11ea-913b-aa67de165cd1.gif)
