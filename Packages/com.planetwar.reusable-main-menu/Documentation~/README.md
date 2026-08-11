# Reusable Main Menu

`MainMenu.prefab` is a presentation-only UGUI menu based on PlanetWar's three menu layers: background, main content and bottom navigation. Its complete hierarchy is serialized in the prefab and is visible/editable in the Unity Hierarchy before Play mode. Drag it beneath a Canvas with an EventSystem; use **Scale With Screen Size**, reference resolution **720 × 1280**, and Match Width Or Height **0**.

## Integration

```csharp
menu.ActionInvoked += action => { /* host project decides what to do */ };
menu.SetTheme(myTheme);
```

`MainMenuView` never creates UI, loads scenes, plays sound, reads saves or calls game services. It only exposes semantic button actions. Use `MainMenuTheme` assets stored in the consuming project's `Assets/` directory as the extension point for host-specific presentation data.

Every packaged button also writes a `[MainMenu Debug] Button pressed: ...` entry to the Unity Console. This is intentionally diagnostic-only and does not trigger navigation or gameplay.

## Art and updates

The packaged PlanetWar PNGs are fallback art, loaded from `Runtime/Resources/PlanetWarMainMenu`. Theme sprite fields take precedence. Keep host-specific themes and prefab variants outside the package; this makes replacing the package folder in another project safe.
