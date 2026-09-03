# Balance Profile snapshots

`ItemConfig.csv` and `FighterConfig.csv` are the release-default **Proposed** tables.
`Legacy.csv` is the immutable before-adjustment overlay. Runtime application is implemented in
`Assets/Scripts/Config/BalanceProfile.cs`; it is deliberately an overlay so both profiles use the
same generated Luban schema and object references.

Equipment effect values are recorded in the same source file because these values are currently
owned by effect Prefabs rather than a Luban schema. They are applied to instantiated equipment
attacks only, never to aircraft default attacks or death effects. Fighter range overrides are
implemented alongside the other runtime profile overrides in `BalanceProfile.cs`; the CSV remains
a compact review snapshot of the original equipment and fighter damage fields.
