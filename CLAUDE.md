# Unity Code Style

These rules define how code is written in this project. Follow them exactly.

## Project Context

- **Engine:** Unity 6.
- **Render pipeline:** Universal Render Pipeline (URP) `17.4.0`. Author materials and shaders for URP; do not use Built-in pipeline shaders.
- **Input:** the new Input System package is present (`InputSystem_Actions.inputactions`). Gameplay input goes through it. Legacy `Input.GetKey` is reserved for disposable debug tooling only, and requires *Active Input Handling* set to "Both".
- **Shared module:** `inc_common_unity` provides reusable utilities and coroutine helpers. Call into it instead of reimplementing equivalents.

## Core Principles

- Write clean, maintainable, technically accurate code.
- Follow DRY: iterate, modularize, and reuse logic instead of duplicating it.
- Encapsulate reusable logic into shared scripts across the application.
- Keep files small. Split long scripts into multiple focused scripts.
- Write self-explanatory code. Do not add comments or Unity tooltips — naming should make intent clear.
- Do not introduce namespaces unless strictly necessary.
- Reuse utilities from the `inc_common_unity` module instead of writing new equivalents.

## Project Structure

- Organize assets by type at the root of `Assets/`: `Scripts/`, `Prefabs/`, `VFX/`, `ScriptableObjects/`, `Scenes/`, `Settings/`.
- Within `Scripts/`, group related scripts by feature (e.g. `Scripts/Scenes/`, `Scripts/Balls/`).
- Define an assembly definition (`.asmdef`) per module to enforce boundaries and speed up compilation. Reference `inc_common_unity` through its assembly definition rather than reaching across raw folders.

## Scene & Rendering

- Use a single scene. Compose it with multiple root objects, or a single UI canvas whose sub-scenes are toggled via `CanvasGroup`.
- Prefer a UI canvas over world objects. Only set up world (2D/3D) objects when their functionality is required (physics, lighting, collision, etc.).

## Instantiation & Object Management

- Never instantiate a `GameObject` or component directly in code. Instantiate only user-provided prefabs.
- Build required child objects and components into the prefab. Do not assemble them in code.
- Never instantiate an object that already exists in the scene. Reference the existing object instead.
- Cache every `GameObject` and component reference. Never search for them repeatedly (e.g. per frame).
- Manage instantiated objects from their instantiator (manager):
  - Track live instances in a list on the manager. Remove them from the list when destroyed.
  - Configure an instance by exposing a config method on its controller and calling it from the manager. Do not reach into the instance's internals.
  - Pass the manager reference into the instance via that config method, so the instance can call back into the manager on events (e.g. destroy).
- Destroy every instantiated object and free memory-intensive resources (textures, `UnityWebRequest`, etc.) to avoid leaks.
- Pool prefab instances that are created and destroyed frequently. Activate/deactivate pooled instances from the manager instead of repeated `Instantiate`/`Destroy`, which causes GC spikes on mobile. The pool is the manager's instance list — reuse inactive entries before instantiating new ones.

## ScriptableObjects

- Hold tunable config and shared data in `ScriptableObject` assets, not hard-coded in scripts or duplicated across scene objects.
- Reference a config `ScriptableObject` from the Inspector, keeping data decoupled from the components that consume it.
- Use `ScriptableObject` assets to share state between scripts instead of static fields or singletons where practical.

## Events & Configuration

- Prefer Unity Inspector configuration over in-script configuration.
- Expose Inspector fields with `[SerializeField] private`. Use `public` only when another script must access the field directly; otherwise expose read access through a PascalCase property.
- Wire Unity events in the Inspector. Do not add listeners in code (no `+=` subscriptions).
- Expose a public method only when it is a high-level entry point for another caller or a Unity event.
  - High-level public methods must stay short and delegate to internal methods. Never expose internal code.
- For tweening and easing, expose an `AnimationCurve` in the Inspector.

## Naming Conventions

Scripts:
- `{Name}Manager` — a script that manages many other scripts (e.g. `SceneManager`, `BallsManager`).
- `{Name}Controller` — a script that manages its own single GameObject (e.g. `BallController`).
- Group related scripts in the same folder (e.g. `Scenes/SceneManager`, `Scenes/SceneController`).

Identifiers:
- Classes: **PascalCase** (e.g. `SceneManager`).
- Fields and methods: **camelCase**, no underscores (e.g. `firstName`, `submitData()`).
- Getters/setters: **PascalCase** (e.g. `CurrentItemIndex`). Expose data through a PascalCase property backed by a camelCase field (e.g. `public int CurrentSceneIndex => currentSceneIndex;`).
- Booleans: prefix with `is` (e.g. `isSuccess`, `isError`).
- Typed GameObjects: suffix with the type (e.g. `backgroundImage`, `bodyRenderer`, `mainCanvasGroup`).
- Time values: suffix with the unit (e.g. `fadeSeconds`, `cooldownSeconds`).
- Index values: suffix with `Index` (e.g. `currentActiveIndex`, `nextActiveIndex`).
- Stateful values: suffix with state — `Target`, `Curr`, `Prev`, `Next` (e.g. `alphaTarget`, `alphaCurr`, `positionXTarget`, `indexPrev`).

## File Structure

- Order sections as shown below and separate each with one blank line.
- Skip any section that is not needed.
- Use `// MARK:` comments to separate sections (do not add explanatory comments).
- Expose Inspector fields with `[SerializeField] private` on two lines, separated by a blank line.

```csharp
// Imports from C# library
using UnityEngine;

// Imports from external package
using AVPro;

// Imports from local package
using Script1;

public class SceneManager
{
    // MARK: Inspector

    [SerializeField]
    private CanvasGroup sceneItems;

    [SerializeField]
    private float fadeSeconds;

    // MARK: Public
    public int startingSceneIndex = 0;

    // MARK: Private
    private int currentSceneIndex = 0;

    // MARK: Getters/Setters
    public int CurrentSceneIndex => currentSceneIndex;

    // MARK: Lifecycle
    private void Start() { ... }

    private void Update() { ... }

    // MARK: Public
    public void ChangeScene(int index) { ... }

    // MARK: Private
    private void FadeScene(int index, float alphaTarget) { ... }
}

public struct Scene
{
    ...
}
```

## Performance

- Cache every reference. Never call `GetComponent`, `Find`, or `Camera.main` per frame (`Camera.main` runs a tagged search on every access).
- Do not allocate in `Update`, `FixedUpdate`, or per-frame coroutine steps. Avoid `new` and LINQ in hot paths; cache `WaitForEndOfFrame`/`WaitForSeconds` instances.
- Use `CompareTag("x")` instead of `gameObject.tag == "x"` to avoid string allocation.
- Reuse collections instead of reallocating them each frame.

## Coroutines

- Reuse coroutine utilities from `inc_common_unity` instead of writing new equivalents.
- Use coroutines for frame-based work tied to the Unity lifecycle. Use `async`/`await` (with cancellation) for I/O such as web requests. Never use `async void` except for Unity event handlers.
- Name coroutine methods with the `Routine` suffix (e.g. `LoadSceneRoutine`).
- Name coroutine reference fields with the `Coroutine` suffix (e.g. `loadSceneCoroutine`).
- Track every started coroutine in a private field:
  - Before starting one, check whether it is already running.
  - If it is, stop it before starting a new one.
- Prefer `for` over `while` in coroutines.
- Prefer `yield return WaitForEndOfFrame();` over `yield return new WaitForSeconds(0f);`.
- Prefer coroutine methods with no arguments and no return value.

## Debug Tooling

- When adding debug keyboard input, call an existing public method to simulate the real event, rather than duplicating logic.
- Put the input checks in a `DebugKeyboard` method called from `Update`, using the legacy input system (`Input.GetKey`).
- Keep debug logic in a small, separate script that can be disabled from the Inspector.

## Visual Effect Graph

- Use VFX Graph for visual effects.
- When sending multiple events to a VFX instance, send one event per frame — via a coroutine or a queue drained in `Update` — instead of all at once.
