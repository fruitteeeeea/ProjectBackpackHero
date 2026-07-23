# 生命值与血条

本目录保存当前阶段的通用生命值与血条显示组件。当前只负责生命值增减和 UI 显示，不包含子弹、碰撞体、受击判定或死亡逻辑。

## 组件

- `Health`：保存最大生命值和当前生命值，并把数值限制在 `0 ~ MaxHealth`。
- `HealthBar`：监听 `Health.HealthChanged`，将生命值比例写入 `Image.fillAmount`。
- `HealthDebug`：开发阶段的临时测试工具，可从组件右上角菜单执行扣血、恢复、归零和重置。

## 场景绑定

将 `Health` 挂在玩家/敌人的背包或飞机对象上，将 `HealthBar` 挂在对应血条的 UI 根节点上。

在 `HealthBar` Inspector 中：

- `Target Health`：绑定对应目标上的 `Health`。
- `Fill Image`：绑定当前血条自己的 `FillArea/Fill`。

`Fill` 的 Image 配置：

- Image Type：`Filled`
- Fill Method：`Horizontal`
- Fill Origin：`Left`
- Fill Amount：`1`

## 当前视觉规范

| 类型 | 宽度 | 默认颜色 |
|---|---:|---|
| 玩家背包 | 320 | 绿色 `#52C96B` |
| 玩家飞机 | 180 | 绿色 `#52C96B` |
| 敌人背包 | 320 | 红色 `#E25353` |
| 敌人飞机 | 180 | 红色 `#E25353` |

血条高度暂定为 `24`。玩家血条位于画面下方，敌人血条位于画面上方。背包初始生命值和最大生命值均为 `150`；飞机生命值可在 Inspector 中单独配置。

## 调试

进入 Play Mode，在目标的 `HealthDebug` 组件菜单中选择：

- `Test/Decrease Health`
- `Test/Increase Health`
- `Test/Set Health To Zero`
- `Test/Reset Health`

确认对应血条独立变化，生命值不会低于 `0`，也不会超过最大生命值。正式接入战斗伤害后可以移除 `HealthDebug`。

## 后续范围

后续再实现飞机和背包的受击碰撞体、子弹伤害，以及飞机血条仅在受到攻击时显示等功能。
