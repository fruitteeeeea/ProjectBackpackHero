# Packaged art

The original PlanetWar main-menu PNGs are retained under
`Runtime/Resources/PlanetWarMainMenu` so the runtime can present a working
default without Addressables. This folder exists as the art inventory entry
point: project-specific replacement art belongs in a consuming project's
`MainMenuTheme` asset, not in this package.
