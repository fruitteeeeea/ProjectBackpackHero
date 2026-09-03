# 平衡 Profile 切换说明

## 用途

项目提供两套不共享运行时状态的数值：

- **Proposed**：正式默认 Profile，使用首轮商业化调优后的飞机与装备数值。
- **Legacy**：冻结的调整前基线，用于对照录制、回归测试和主观体验比较。

Legacy 不会写入玩家存档，也不会在非开发构建中启用。

## 在编辑器中对照

1. 进入 Play Mode，并停留在准备阶段。
2. 打开“调试中心 → 平衡调整”。
3. 在“平衡测试方案”选择 `数值 Profile`，再点击“应用到运行时”。
4. 使用相同 Deck、布局、养成等级和随机航线设置重复对局。

战斗阶段禁止切换 Profile；请回到准备阶段或点击“重启对局”后再应用，确保一局内不会混合两套数值。

## 数据来源

- `Tools/Luban/tables/ItemConfig.csv` 与 `FighterConfig.csv`：Proposed 正式表。
- `Tools/Luban/tables/BalanceProfiles/Legacy.csv`：Legacy 冻结差异表。
- `Tools/Luban/tables/BalanceProfiles/Proposed.csv`：Proposed 差异审阅表。
- `Assets/Scripts/Config/BalanceProfile.cs`：两套 Profile 的运行时覆盖与装备效果参数。

装备伤害系数仅影响装备攻击；飞机默认攻击和飞机死亡爆炸仍使用其自身定义的伤害结算。
