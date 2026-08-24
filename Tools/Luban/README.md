# Project Luban configuration pipeline

`tables/` holds the authored CSV configuration sources. This project pins the official
Luban release **v4.11.0** (`SHA-256 899703281bcca3e22d78c44045fb08ac05a72abb29428650a149eca612779892`).

Install or repair the local toolchain:

```powershell
pwsh -File Tools/Luban/install.ps1
```

Generate the project configuration payloads and C# Runtime tables:

```powershell
pwsh -File Tools/Luban/generate.ps1
```

This generates tracked C# tables in `Assets/Scripts/Generated/Luban/` and native JSON in
`Assets/Resources/Configs/Luban/`. `GameConfigService` loads these generated tables; Unity
SO assets remain the resource bridge and retain all Unity object references.

The command requires the official Luban Unity Runtime package declared in `Packages/manifest.json`.
Use the compatibility payload export only when comparing the old migration path:

```powershell
pwsh -File Tools/Luban/generate.ps1 -Compatibility
```

The main command regenerates Luban's internal schema and temporary native-input CSVs from `tables/`.
Those generated intermediates are intentionally ignored; only the authored tables, build script,
and native JSON outputs are kept in version control.
