# 飞机战斗伤害飘字（FEEL + TMP）开发记录

## 目标

为战斗中的玩家与敌方飞机添加伤害飘字。每次有效受击显示攻击结算传入的原始伤害值；文字从受击点弹出，带少量横向随机偏移，先上升、再下坠，并在生命周期后段渐隐回收。

玩家飞机使用冷色文字，敌方飞机使用暖色文字；两者均使用 TMP 世界空间文字与深色描边。

## 伤害到飘字的数据流

```text
HurtBox2D.ReceiveHit
  -> Health.DecreaseHealth
  -> Health.Damaged(原始伤害值)
  -> Fighter2D.HandleDamaged
  -> FighterFeedbacks.PlayHit
  -> FighterDamageFloatingText.Play
  -> MMFloatingTextSpawnEvent
  -> MMFloatingTextSpawner 对象池
  -> MMFloatingTextMeshPro TMP 飘字
```

`Health.Damaged` 只会在实际扣除生命值时触发。因此无效伤害、已死亡目标和友军攻击不会生成飘字；击杀一击仍会显示标称伤害值。

## 主要资源

| 类型 | 路径 | 用途 |
| --- | --- | --- |
| 配置资产 | `Assets/Settings/Battle/FloatingDamageTextSettings.asset` | 集中调整字体、颜色、对象池、位移、缩放和渐隐参数。 |
| 玩家飘字预制体 | `Assets/Prefabs/Battle/FloatingText/PlayerDamageFloatingText.prefab` | 冷色 TMP 飘字。 |
| 敌方飘字预制体 | `Assets/Prefabs/Battle/FloatingText/EnemyDamageFloatingText.prefab` | 暖色 TMP 飘字。 |
| 伤害事件 | `Assets/Scripts/Health/Health.cs` | 新增 `Damaged(float)` 公开事件。 |
| 飞机反馈桥接 | `Assets/Scripts/Battle/Fighter/Fighter2D.cs`、`FighterFeedbacks.cs` | 将伤害事件交给 FEEL 与飘字发射器。 |
| FEEL 发射器 | `Assets/Scripts/Battle/Fighter/FighterDamageFloatingText.cs` | 按阵营发送到通道 41（玩家）或 42（敌方）。 |
| 场景后备 | `Assets/Scripts/Battle/Fighter/FloatingTextSpawnerBootstrap.cs` | 未手动放置生成器的场景（例如 SampleScene）首次生成飞机时自动创建两套 FEEL 对象池。 |
| TMP 样式 | `Assets/Scripts/Battle/Fighter/FloatingTextTmpStyle.cs` | 应用配置资产中的字体与描边，并逐帧应用透明度曲线。 |

`BattlePrototype` 已放置两个场景级 `MMFloatingTextSpawner`。`SampleScene` 依赖 `FloatingTextSpawnerBootstrap` 自动创建等价的运行时生成器。

## 调参说明

选中 `FloatingDamageTextSettings.asset`，所有调整均在 Inspector 完成：

- **Pool**：`Pool Size` 是预热实例数量；`Lifetime` 是飘字存活区间。
- **Spawn Randomness**：控制出生位置与横向终点的随机范围。
- **Parabolic Motion**：`Vertical Peak Height` 控制最高高度；`Vertical Motion` 曲线控制上弹后下坠的轨迹。
- **Scale**：`Scale At Start`、`Scale At Peak` 与 `Scale Motion` 控制弹出感。
- **Fade**：`Opacity Motion` 控制透明度。当前曲线在约 55% 生命周期后开始下降，结束时为 0。
- **TMP Style**：可修改字体尺寸、描边宽度、排序顺序，以及玩家/敌方的文字与描边颜色。

配置资产在运行时生成对象池时读取。修改参数后应停止并重新运行场景，以销毁旧对象池并按新设置创建实例。

## 排查清单

1. **Hierarchy 有生成器但没有文字**：检查 Console 是否出现 `MMFloatingTextSpawner.Spawn` 的空引用；运行时生成器必须在赋值预制体后激活，当前 Bootstrap 已处理该顺序。
2. **文字过小或被遮挡**：提高配置资产的 `Font Size` 与 `Sorting Order`；当前默认排序顺序为 200。
3. **渐隐不明显**：编辑 `Fade > Opacity Motion`，让曲线更早下降；TMP 样式组件也会每帧将该曲线的值写入文字 alpha。
4. **SampleScene 没有生成器**：确认生成的 Fighter 来自 `Assets/Prefabs/Battle/Fighter.prefab`，其上挂有 `FloatingTextSpawnerBootstrap` 并绑定了配置资产。
