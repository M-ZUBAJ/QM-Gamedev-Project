# QM Gamedev Project

Unity 2023.1.13f1, 3D.

# Script Style (.cs)

Every script starts with this `/* */` comment at the very top, before the `using` lines.

```
/*
First Date: 4/10/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the Directional Light in the scene.
Unity Editor: Set Skybox Material = Default-Skybox and Sun Source = this light
              (Window > Rendering > Lighting > Environment).
              Testing: drag "Time Of Day" in the Inspector while playing.
Purpose: Day and night. Turns the sun around the world.
Requirement: DaylightCycle.cs x-> GameRules.cs, Light (Unity)
*/
```

| Field | What to write |
|---|---|
| First Date | The day the script was first made (D/M/YYYY). Never change it. Scripts brought over from an older project keep their original First Date. |
| Date | The day the script was last changed (D/M/YYYY). Update it every edit. |
| Author | Tony |
| Use by | Which GameObject to drag the script onto. |
| Unity Editor | What to do in the Unity Editor to make it work: windows / menus to set up, tags, layers, and how to test it while playing. Don't repeat the `[Tooltip]` text of each field; tooltips already explain those. |
| Purpose | What the script does, in plain words. One script = one job. |
| Requirement | What this script needs. See [Requirement symbols](#requirement-symbols). |

# Rules for Scripts

1. **One script does one specific thing.**
2. **Drag and drop:** put the script on a GameObject and it works. It finds what it needs by itself (`GetComponent`, `[RequireComponent]`).
3. **Make scripts as easy as possible for the developer to use.** When two scripts need to talk, pick the way that needs the fewest steps in the Unity Editor and is easiest to understand when reading the code:
   - **Direct link:** one script uses the other directly. It finds it by itself (`GetComponent`, `FindFirstObjectByType`) or has a field to drag it into. Write the link in `Requirement:`.
   - **UnityEvent:** the script has a `UnityEvent` field. In the Inspector, drag the other object in and pick the method to call. Good when one script should be able to start many different things (sounds, effects, UI) that it doesn't need to know about.
   - **Bridge script:** a small script whose only job is to connect two scripts. Name it `...Bridge` (e.g. `DaylightRuleBridge`). Use it only when it is really easier than the other two.
4. **Core scripts can be used directly from anywhere.** They are the shared pieces every system may need. Current core scripts:
   - `GameRules`: on/off world rules
   - `MenuState`: is any menu open?
   - `ClickClaim`: which mode owns the mouse buttons right now
   - `InteractionGate`: may the player interact here?
   - `IHand`: is the player's hand empty?

## UnityEvent example

```csharp
using UnityEngine;
using UnityEngine.Events;

public class DaylightCycle : MonoBehaviour
{
    [Tooltip("Called once when night starts. Drag objects in and pick what they should do.")]
    [SerializeField] UnityEvent onNightStart;

    void StartNight()
    {
        onNightStart.Invoke();   // calls whatever was set up in the Inspector
    }
}
```

In the Inspector, click **+** under *On Night Start*, drag in e.g. the MonsterSpawner object and pick `MonsterSpawner > StartSpawning`.
`DaylightCycle` never mentions `MonsterSpawner` in code, so each still works on its own.

# Requirement Symbols

You write only **what your script needs**. The Script Map tool writes **what needs it** for you (see [Script Map](#script-map)).

The arrow points at the script that is NEEDED.

| Symbol | Meaning | Who writes it |
|---|---|---|
| `A.cs x-> B.cs` | A needs B. B does not need A. | You |
| `A.cs <-> B.cs` | A and B need each other (write it in both files, as a warning). | You |
| `A.cs o-> B.cs` | A works without B, but does more when B is there. | You |
| `B.cs <-x A.cs` | A needs B. | The tool. Don't write it by hand. |
| `B.cs <-o A.cs` | A uses B if it's there. | The tool. Don't write it by hand. |
| `None` | Needs nothing, and nothing needs it. | The tool (when nothing else is left) |

- Unity's built-in components (Light, Rigidbody, Collider, ...) get `(Unity)` after the name, e.g. `Light (Unity)`. These are the ones you'd put in `[RequireComponent]`.
- "Need" = won't compile or won't do its job without the other script.
- Only write **direct** links. If A needs B and B needs C, `A.cs` only lists B.
- Links made with a UnityEvent are set up in the Inspector, not in code, so they are not a Requirement.

## Examples

```
Needs nothing:
  Requirement: None

Needs one script:
  Requirement: A.cs x-> B.cs

Needs many scripts:
  Requirement: W.cs x-> X.cs, Y.cs, Z.cs

Needs a Unity component on the same GameObject:
  Requirement: DaylightCycle.cs x-> Light (Unity)

Optional (a clock that shows the time only if DaylightCycle exists):
  Requirement: Clock.cs o-> DaylightCycle.cs

Need each other (same line in both files):
  Requirement: A.cs <-> B.cs

Mixed (one line for each kind):
  Requirement: A.cs x-> B.cs, C.cs, Rigidbody (Unity)
               A.cs <-> E.cs
               A.cs o-> F.cs

Bridge script:
  Requirement: DaylightRuleBridge.cs x-> GameRules.cs, DaylightCycle.cs

After the tool runs, GameRules.cs shows who needs it:
  Requirement: GameRules.cs <-x DaylightCycle.cs, DaylightRuleBridge.cs
```

# Script Map

`Assets/Script/Editor/ScriptMapTool.cs` keeps every `Requirement:` up to date. In Unity:

- **Tools > Script Map > Update Now** runs it once.
- **Tools > Script Map > Auto Update** (on by default) runs it by itself every time Unity finishes compiling scripts.

Each run, it:

1. Reads the `x->`, `<->` and `o->` lines you wrote.
2. Searches the code to see which scripts really use which (comments don't count).
3. Rewrites the `<-x` / `<-o` lines in every header. If nothing is left, it writes `None`.
4. Writes [ScriptMap.md](ScriptMap.md): every script, what it needs and what needs it.
5. Warns (in the Console and in ScriptMap.md) when a header doesn't match the code, e.g. a script uses `GameRules` but doesn't list `GameRules.cs`.

It never changes `Date:`. That is only for changes people make.

# Folders

Scripts are grouped by system in `Assets/Script/`:

| Folder | What goes there |
|---|---|
| `Core/` | Core scripts that any script may use directly (see rule 4). |
| `World/` | The world itself: time of day, weather, ... |
| `Bridge/` | Bridge scripts. |
| `UI/` | Menus and HUD (inventory window, tabs...). Setup guides are in `Docs/`. |
| `Editor/` | Tools for the Unity Editor (like Script Map). Not part of the game. |

Add a new folder when a new system starts (e.g. `Building/`, `Combat/`, `Camera/`).
When moving a script, always move its `.meta` file with it (or move it inside Unity), or Unity loses the link to it in scenes.
