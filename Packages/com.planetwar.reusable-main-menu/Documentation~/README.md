# Reusable Main Menu

`MainMenu.prefab` is a presentation-only UGUI menu based on PlanetWar's three menu layers: background, main content and bottom navigation. Its complete hierarchy is serialized in the prefab and is visible/editable in the Unity Hierarchy before Play mode. Drag it beneath a Canvas with an EventSystem; use **Scale With Screen Size**, reference resolution **720 × 1280**, and Match Width Or Height **0**.

## Integration

```csharp
menu.ActionInvoked += action => { /* host project decides what to do */ };
menu.SetTheme(myTheme);
```

`MainMenuView` never creates UI, loads scenes, plays sound, reads saves or calls game services. It only exposes semantic button actions. Use `MainMenuTheme` assets stored in the consuming project's `Assets/` directory as the extension point for host-specific presentation data.

## Ranks

Version 0.2.0 includes a static `UIRankList` child of `MainMenu.prefab`. Its twenty `RankRow_01`–`RankRow_20` children are all pre-authored and can be edited directly in the Hierarchy. `RanksView` only assigns data to those serialized rows; it never instantiates UI. The bottom **Ranks** tab opens the page and **Home** restores the main page. `RankEntry` data, country sprites, and the current-player row are editable from the `RanksView` Inspector.

The package uses DOTween for the original bottom-tab slide and bounce animation. Install DOTween in the consuming project before importing this version of the package.

Every packaged button also writes a `[MainMenu Debug] Button pressed: ...` entry to the Unity Console. This is intentionally diagnostic-only and does not trigger navigation or gameplay.

## Art and updates

The packaged PlanetWar PNGs are fallback art, loaded from `Runtime/Resources/PlanetWarMainMenu`. Theme sprite fields take precedence. Keep host-specific themes and prefab variants outside the package; this makes replacing the package folder in another project safe.
