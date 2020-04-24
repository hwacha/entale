# entale
An open-world game with autonomous agents that the player can freely talk to using a robust language system.

## Controls

- **Move**: `wasd`/`arrowkeys`
- **Look**: `mouse`
- **Word Menu**
  - **Open**: `M`
  - **Select**: `hover` and `click`
- **Highlight**
  - **Mode**: `space`
  - **Select**: `point` and `click`

## Developement

### Source control

We're using git and [git-lfs](https://git-lfs.github.com/) for source control.
Make sure to have git-lfs installed to pull large files such as models.

#### Pushing changes
Since we still haven't figured out how to merge branches with unity projects, if a branch has a merge conflict with master we're completely overwriting master.
To replace master with your branch you can run:
```
git checkout your_branch
git merge -s ours master
git checkout master
git merge your_branch
```
See related [stackoverflow post](https://stackoverflow.com/questions/2862590/how-to-replace-master-branch-in-git-entirely-from-another-branch)

#### Pulling changes
After pulling make sure:
- You have all lfs files by running `git lfs fetch && git lfs checkout`
- All the asset linking is working properly by right clicking on the Assets folder in the Project tab and choosing "Reimport All".

## Features

### Movement

#### Functionality 
First person movement.

**Controls**:
- `wasd`/`arrowkeys` to move.
- `mouse` to look around.

### Notes
- There is no gravity or collision correction.

#### Code
- [PlayerMovement.cs](Assets/Scripts/PlayerMovement.cs)
- [MouseLook.cs](Assets/Scripts/MouseLook.cs)

### Word Menu

#### Functionality
A radial menu used to select words to then create expressions with.
It's a menu of depth two where the first layer filters by semantic type and the second layer has word nodes

**Controls**
- `M` to open/close menu
- `hover` and `click` to select an item

#### Notes
- Currently hardcoded to be a two layer menu that reads from a map from semantic type to words ([Lexicon](Assets/Scripts/UI/RadialMenu.cs))
- Icons for semantic types and words are fetched in [Icons.cs](Assets/Scripts/UI/Icons.cs)

#### Code
- [RadialMenu.cs](Assets/Scripts/UI/RadialMenu.cs)
- [RadialMenuItem.cs](Assets/Scripts/UI/RadialMenuItem.cs)
- [Icons.cs](Assets/Scripts/UI/Icons.cs)

#### Images

![radial_menu](https://user-images.githubusercontent.com/3184499/78930261-ce582e00-7a71-11ea-87c0-310c22ddf984.gif)

### Highlighting

#### Functionality
Certain objects in the world are highlightable (`highlightable` layer) to indicate `this` or `that` in the language.
Considering you might want to refer to a couple of things at once, there are different registers that you can store `this`/`that` references in. They will be highlighted in different colors.

**Controls**:
- `space` to toggle through highlight registers.
  - There is a box on the top right which will indicate which register you're in by showing the color. Note that one of these registers is neutral (the box will be white).
- `point` and `click` to highlight an object.
  - The pointer will turn the color of the register if the object is highlightable.

#### Notes 
- This is currently just an aesthetic change. There isn't a property set on the game object that indicates that it's highlighted.
- Only one object can be highlighted for each register and an object can only be highlighted by one register.

#### Code
- [Highlighter.cs](Assets/Scripts/Highlighter.cs)
- An object needs to be in the `highlightable` layer and have the shader [Highlightable.shader](https://github.com/hwacha/entale/blob/master/Assets/Shader/Highlightable.shader) to be highlightable.

#### Images

![highlight demo](https://user-images.githubusercontent.com/3184499/77706284-516c8500-6f98-11ea-913b-aa67de165cd1.gif)

### Resources

- Graphics
  - [roystan: Toon Shader](https://roystan.net/articles/toon-shader.html)