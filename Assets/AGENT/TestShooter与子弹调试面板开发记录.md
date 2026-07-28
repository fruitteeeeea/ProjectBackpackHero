# TestShooter 与子弹调试面板开发记录

## 目录

```text
Assets/Scripts/Battle/TestShooter2D.cs
Assets/Scripts/Battle/Combat/ProjectileFireModeController2D.cs
Assets/Scripts/Battle/Combat/ProjectileFirePattern.cs
Assets/Scripts/Battle/Debug/TestShooterDebugRuntimeBridge.cs
Assets/Editor/BattleDebug/TestShooterDebugWindow.cs
Assets/Editor/BattleDebug/TestShooterDebugWindowLifecycle.cs
Assets/Settings/Battle/ProjectileFirePatterns/
Assets/Scenes/BattlePrototype.unity
```

## 使用方法

1. 打开 `BattlePrototype` 并进入 Play Mode。
2. `Test Shooter Debug` 窗口会在 `TestShooter2D` 注册后自动打开；也可以通过 `Tools/Battle/Test Shooter Debug` 手动打开。
3. 在 Game View 中移动鼠标可改变 TestShooter 朝向。
4. 单击鼠标左键会立即触发一次；按住左键会按照 `Hold Trigger Interval` 持续触发 TestShooter 当前挂载的全部发射模式。
5. TestShooter 的模式间隔固定为 `-1`，表示不会自动计时发射。
6. 调试窗口可以实时修改 Projectile Prefab、阵营、伤害、速度和寿命，也可以选择 Pattern 资源增删发射模式。修改只影响本次 Play Mode。

## 发射模式

`ProjectileFireModeController2D` 是飞机和 TestShooter 共用的发射模块。每个模式由一个 Pattern 数据和一个独立间隔组成：

- 间隔大于等于 0 时独立计时，冷却完成自动请求发射。
- 间隔为 `-1` 时只响应 `TriggerAll` 主动触发。
- 一个实体可以同时挂载多个模式，各模式冷却互不影响。

当前提供两个 Pattern 数据：

```text
ForwardSingle.asset     朝当前正前方发射1颗
Spread30Three.asset     在30度扇形中均匀发射3颗
```

现有飞机初始化时只装配 `ForwardSingle`，间隔读取该飞机自身 `FighterDefinition.AttackInterval`，因此保留 Normal、Charge、Shield 原有的射速差异。后续可以通过 `AddMode`、`RemoveModeAt`、`SetModePattern` 和 `SetModeInterval` 扩展飞机的发射模式。

## 子弹 Prefab 接入约定

- 根对象必须带有 `Projectile2D`、`ProjectileTrajectoryController2D` 和 `HitBox2D`。
- 子弹图像的默认前方为本地 Y 轴正方向。
- 轨迹控制器引用 `Straight`、`Sine` 或 `Bezier` Profile；没有有效 Profile 时会警告并回退直线。
- 若需要调试寿命，Prefab 应带有 `LifetimeAndScreenBounds2D`；没有该组件时子弹仍能发射，但面板中的寿命不会生效。
- 不同弹道和特殊命中行为可由后续子弹 Prefab 上的独立运行时组件实现，不需要改变 TestShooter 的瞄准与生成职责。

## 默认配置

`BattlePrototype/TestShooter` 默认发射现有 `Assets/Prefabs/Battle/Projectile.prefab`，阵营为 Player，伤害为 1，速度为 8，寿命为 5 秒，并挂载一个间隔为 `-1` 的 `ForwardSingle` 模式。

TestShooter 默认按住触发间隔为 `0.2` 秒。该间隔属于输入层，不会改变发射模式自身的 `-1` 手动间隔。
