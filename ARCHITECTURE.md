# Text Display Template — Architecture

## 1. Stack
- Unity 6, URP 17.4.0, uGUI 2.0 (TextMeshPro is built in: asmdef ref `Unity.TextMeshPro`), Input System 1.19 (gameplay), legacy `Input` for debug only.
- `th.increstive.unity.common`: we reuse `FadeUtils.Fade`, `CoroutineUtils.Start`, `CanvasGroup.StartFade`. We do not use `InOutAnimationUtils`, because it captures the position once in `Awake`, which breaks pooled items that move. We cannot reuse `HelpBoxAttribute` either: it lives in an Editor-only asmdef and is a full-width decorator, so runtime scripts cannot use it in a build.
- Project changes needed: set Active Input Handling to **Both** (`activeInputHandler: 1 → 2`, then restart the editor), and import TMP Essential Resources.

## 2. Data model
- `enum TextAnimationStyle { FloatUp, FadeIn, SlideInLeft, SlideInRight }`
- `enum TextOverflowPolicy { Queue, Drop }`, `enum TextLaneAlignment { Start, Center, End }`, `enum TextLaneOrder { TopDown, Random }` (all in `TextDisplayEnums.cs`).
- `TextDisplayConfig : ScriptableObject`. Every field is `[SerializeField] private` + `[HelpButton("…")]`, with a PascalCase getter. Clamping happens in `OnValidate`, because `[Range]`/`[Min]` would replace the help drawer.
  - Animation: `animationStyle`, `inSeconds`, `staySeconds`, `outSeconds`, `inCurve`, `outCurve` (AnimationCurve), `floatDistance` (px), `slideSpeed` (px/s), `laneItemSpacing` (px), `fadeScaleFrom`.
  - Text: `fontSize`, `maxTextWidth`.
  - Scatter (FloatUp/FadeIn): `spacing` (px gap between items), `edgePadding`, `maxPlacementTries`.
  - Lanes (Slide*): `laneCount`, `laneSpacing`, `laneAlignment`, `laneOrder`.
  - Capacity: `maxActiveItems`, `overflowPolicy`, `maxQueueLength`.
- `struct TextPlacementSlot { Vector2 restPosition; Rect reservedRect; int laneIndex; }` (`laneIndex` is -1 in scatter mode).

## 3. Boundaries
- **`TextDisplayManager`** (on the TextDisplay root; refs: `config`, `itemPrefab`, `areaTransform`). Public API: `Show(string text)`, `Clear()`. It owns the pool (`List<TextItemController>`: reuse an inactive item, else `Instantiate(itemPrefab, areaTransform)` up to `maxActiveItems`), the pending `Queue<string>`, and an `ITextPlacement` chosen in `Awake` from the style (Slide* → Lane, otherwise Scatter). Show flow: get a free item → `item.Prepare(config, text)` returns the measured size → `placement.TryReserve(size, out slot)` → `item.Play(this, slot)`. If there is no free item or no free space, the request is queued (capacity `maxQueueLength`, oldest dropped) or dropped, depending on `overflowPolicy`. When an item finishes, it calls `NotifyItemFinished(item)`, which releases the slot, deactivates the item, and dequeues one pending text. Changing the style at runtime requires `Clear()`.
- **`TextItemController`** (on the prefab; refs: `label` TMP_Text, `canvasGroup`, `rectTransform`). `Prepare`: activate at alpha 0, set the text and fontSize, `ForceMeshUpdate`, set `sizeDelta = GetPreferredValues(maxTextWidth)`. `Play(manager, slot)` starts `lifetimeCoroutine` → `LifetimeRoutine`: in, then stay, then out, using `FadeUtils.Fade`, with the pose from `TextAnimationPose`. It never touches placement.
- **`TextAnimationPose`** (static, pure): `Evaluate(config, phase, step, areaRect, slot, out Vector2 offset, out float alpha, out float scale)`. This is where all style math lives.
- **`ITextPlacement`**: `bool TryReserve(Vector2 size, out TextPlacementSlot slot)`, `void Release(TextPlacementSlot slot)`, `void Clear()`. Plain C# classes, constructed with `(config, areaTransform)`.
  - `ScatterPlacement`: a port of ref1 `SpawnZone.TryGetSpawnPosition` (random tries, `Rect.Overlaps`). It checks against the **reserved** rects, not live animated positions. The reserved rect = size × max(1, fadeScaleFrom) + spacing, and in FloatUp mode it is also extended by `floatDistance` vertically.
  - `LanePlacement`: the area is split into `laneCount` rows (`laneSpacing` gap) with one item per lane, picked by `laneOrder`. The rest position x depends on `laneAlignment`. The slide starts `slideDistance` away on the entry side. An item taller than a lane is scaled down to fit.
- **Decisions (user-confirmed):** Slide styles are a ticker: the item enters from the chosen side (Left or Right), travels across the full area to the opposite edge, and fades out there (`slideSpeed` px/s replaces `staySeconds`; fade-out starts at the far edge). Several items may share a lane: `LanePlacement` tracks per-lane `nextFreeTime` so the gap behind the previous item is at least `laneItemSpacing` px (cannot overlap; same speed for all). Text is Thai + English: a Thai-capable TMP dynamic font asset (Noto Sans Thai / Sarabun, SIL OFL) is the default font; Thai has no spaces, so wrapping relies on `maxTextWidth` and TMP line breaking. Text source is API only: `TextApiController` polls a configurable URL (`apiUrl`, `pollSeconds`), dedupes by id, and calls `TextDisplayManager.Show`. The input field idea is dropped.
- **`HelpButtonAttribute(string text)`** (runtime asmdef) and **`HelpButtonDrawer`** (Editor asmdef, IMGUI). The drawer draws the field minus 20 px, then a trailing "?" button that toggles a `HelpBox` underneath. The open state is stored in `SessionState` (key = target id + propertyPath). The label also gets a hover tooltip. Height = `GetPropertyHeight(includeChildren)` + help height. **This attribute goes on every serialized field in our scripts.**
- **`TextDisplayDebugKeyboard`** (a separate, disableable script, legacy Input): Space → `Show(random sample)`, C → `Clear()`.

## 4. Folders / asmdefs
```
Assets/Scripts/Inspector/            HelpButton.asmdef: HelpButtonAttribute.cs
Assets/Scripts/Inspector/Editor/     HelpButton.Editor.asmdef (Editor only, refs HelpButton): HelpButtonDrawer.cs
Assets/Scripts/Text/                 TextDisplay.asmdef (refs Increstive.CommonUnity, Unity.TextMeshPro, HelpButton):
                                     TextDisplayManager, TextItemController, TextDisplayConfig, TextDisplayEnums,
                                     TextPlacementSlot, TextAnimationPose, TextDisplayDebugKeyboard
Assets/Scripts/Text/Placement/       ITextPlacement, ScatterPlacement, LanePlacement
Assets/Tests/EditMode/               TextDisplay.Tests.asmdef: ScatterPlacementTests, LanePlacementTests
Assets/Prefabs/Text/TextItem.prefab
Assets/ScriptableObjects/Text/       TextDisplay_FloatUp / _FadeIn / _SlideLeft / _SlideRight .asset
Assets/Scenes/Main.unity             Camera, EventSystem, Canvas(Overlay, 1920x1080) > TextDisplay(CanvasGroup, Manager) > TextArea; Debug
```

## 5. Build order
The first vertical slice is tasks 1–11: a FadeIn/FloatUp scatter preset playable with Space. Then 12 (lanes → slide styles), 13–15.

## 6. Tasks
After every script task, run: `refresh_unity` → wait for `editor_state.isCompiling == false` → `read_console(types=["error"])`. The task is done only when this shows 0 errors. All code follows CLAUDE.md (MARK sections, two-line SerializeField, no comments or namespaces).

| # | Task | Files | I/O | Verify | Rung | MCP |
|---|------|-------|-----|--------|------|-----|
| 1 | Prep | ProjectSettings.asset, TMP import | input handler = Both, TMP Essentials | `Assets/TextMesh Pro` exists; console clean | haiku/low | yes (`execute_menu_item`) |
| 2 | HelpButton attr + drawer + 2 asmdefs | Inspector/** | `[HelpButton("txt")]` → field + "?" toggle | compile; checked visually in task 10 | sonnet/medium | no |
| 3 | Enums, Slot, Config SO + TextDisplay.asmdef | Text/*Config, *Enums, *Slot, asmdef | `[CreateAssetMenu]` config | compile | sonnet/low | no |
| 4 | TextAnimationPose | Text/TextAnimationPose.cs | pure static per §3 | compile (+ task 14) | sonnet/low | no |
| 5 | ITextPlacement + ScatterPlacement | Placement/* | per §3 | compile | sonnet/low | no |
| 6 | TextItemController | Text/TextItemController.cs | Prepare/Play/ForceHide | compile | sonnet/medium | no |
| 7 | TextDisplayManager | Text/TextDisplayManager.cs | Show/Clear, pool, queue | compile | sonnet/medium | no |
| 8a | TextApiController + response DTO (needs API URL/JSON shape from user) | Text/Api/* | poll → Show(text) | compile; play against real/mock endpoint | sonnet/medium | no |
| 8 | DebugKeyboard | Text/TextDisplayDebugKeyboard.cs | Space/C | compile | haiku/low | no |
| 9 | TextItem prefab | Prefabs/Text/TextItem.prefab | center anchor/pivot, CanvasGroup, TMP label, refs wired | `manage_prefabs` inspect refs non-null | sonnet/low | yes |
| 10 | 4 config assets | ScriptableObjects/Text/* | one per style, sane defaults | assets load; "?" visible in Inspector (screenshot) | haiku/low | yes (`manage_scriptable_object`) |
| 11 | Main scene wiring | Scenes/Main.unity, Build Settings | hierarchy per §4 | play mode + Space ×20: no overlap (screenshot), console clean | sonnet/low | yes |
| 12 | LanePlacement + manager switch | Placement/LanePlacement.cs | per §3 | compile; play with SlideLeft/Right | sonnet/low | partly |
| 13 | Gizmos (area, lanes, reserved rects) | TextDisplayManager | OnDrawGizmos | scene-view screenshot | haiku/low | yes |
| 14 | EditMode tests: placements | Tests/EditMode/** | no overlap over N reserves; lanes full → false; release frees | `run_tests` green | sonnet/low | yes |
| 15 | Acceptance across 4 presets + overflow | none | — | play each preset, burst 50 Shows, queue/drop behave | sonnet/medium → opus/high only on failure | yes |

## Open questions
1. **Slide modes:** we assume each item slides in, stays at a rest position, then fades out, with one item per lane. Do you want a continuous ticker instead (text keeps moving and exits on the other side, with several items per lane)?
2. **Language/font:** will the text be Thai? If so, we need a Thai font file you provide (with its license) plus a TMP dynamic font asset. Thai has no spaces between words, so TMP word wrap will only break at spaces or at `maxTextWidth`.
3. **Text source:** is the input only `Show(string)` plus the debug keys? Or should the template also include a typing UI (TMP_InputField → Show) or an API/network feed? We assume the default overflow is Queue.

## Addendum: API pattern (from ref repos, user asked to follow them)
- `TextApiController` (Text/Api/): `[SerializeField] apiUrl, pollSeconds, textManager`. Uses `UnityWebRequest.Get(apiUrl)` in a coroutine (ref2 `SpawnPopup` pattern), parses with `JsonUtility.FromJson<TextApiResponse>`; response shape `{ "items": [ { "_id": "...", "text": "..." } ] }` (DTO `TextApiResponse`/`TextApiItem`, `[Serializable]`, public fields so JsonUtility works). The field names are an assumption, adjust the DTO when the real endpoint is known.
- Bootstrap: first successful fetch only seeds `seenIds` (HashSet) so old items are not replayed; later polls call `textManager.Show(item.text)` once per unseen `_id`, one item per frame via the coroutine. Skip empty `_id`/`text`. A failed request logs a warning and retries on the next poll. A running poll coroutine is tracked in `pollCoroutine` and stopped before restart.
