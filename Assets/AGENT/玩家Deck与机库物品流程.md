# 玩家 Deck、机库与物品配置流程

本文是当前玩家物品系统的现行规范。旧的“场景 Item Catalog + 每个物品单独 UI Prefab”流程已移除，不应继续使用。

## 1. 数据来源与初始状态

- 所有可用物品登记在 `Assets/Resources/PlayerItemCatalog.asset`。
- `PlayerItemSystem` 读取此 Catalog，保存玩家货币、物品状态与 Deck 到 `PlayerPrefs`（键：`PlayerItemModel`）。
- Catalog 内每一个飞机与装备物品均为**已解锁且最大等级**；当前原型不存在锁定物品，也没有逐级养成流程。
- 这不是仅限新存档的默认值：每次载入都会把 Catalog 内已有存档状态规范化为已解锁、最大等级。货币和碎片数量会保留。
- 存档不再维护版本分支；只保存当前结构。历史 JSON 即使包含已废弃的 `Version` 字段，也会在读取时被安全忽略。
- `ItemData` 中遗留的解锁条件、升级成本和碎片成本字段仅为兼容旧数据/界面；它们不影响当前原型的可用状态。

## 2. Deck 规则

Deck 固定有五个槽位：

| 槽位 | 允许类型 |
| --- | --- |
| 0–2 | `Aircraft`（飞机） |
| 3–4 | `Equipment`（装备） |

- 同一物品不能重复配装。
- 新存档首次创建 Deck 时，按 `PlayerItemCatalog` 的顺序自动填入前三个飞机和前两个装备。
- 已有存档的空槽不会自动被新物品填充；玩家从 Collections 中选择物品后，选择兼容槽位完成替换。
- Deck 不会把物品直接放进战斗背包；它只决定玩家商店可刷新的候选池。

## 3. 商店与战斗

- 玩家商店的随机来源仅为当前 Deck 中的有效物品。
- 不在 Deck 的玩家物品不会出现在玩家商店。
- 商店中购买的物品仍必须由玩家放入背包，才会参与冷却、飞机生成、装备相邻加成和合成规则。
- 玩家与敌人的 `PlayerBackpackSystem.itemCatalog` / `EnemyBackpackSystem.itemCatalog` 已移除。
- 背包 UI 壳由形状选择器按 `ItemData.ShapeOffsets` 选择；不再为每一个具体物品登记 UI Prefab。当前支持 1×1、1×2、2×1 与四个 L 形方向。

## 4. Hangar / Collections 交互

- Deck 始终显示 5 个固定槽位；Collections 显示全部未配装物品。
- 点击 Collections 物品打开详情；点击详情中的 Equip 后进入选择目标槽位模式。
- 只有同类型 Deck 槽位会以 `-5° ↔ +5°`、0.5 秒线性循环高亮并可替换。
- Deck 卡点击只打开详情；不提供直接卸下。替换成功后旧物品回到 Collections。
- 关闭详情、切换页面或取消选择时，必须停止所有槽位高亮动画。

## 5. Hangar 卡片视觉

卡片底板使用原始卡片 `Image` 节点上的 `ImageLoader`，与目标项目 `ItemCard.Refresh()` 的 `bgLoader.Select(...)` 机制一致：

| 物品类型 | `ImageLoader` 索引 | 素材 |
| --- | --- | --- |
| 飞机 | 0 | `Packages/com.planetwar.reusable-main-menu/Art/Hangar/Card/bg_jiku_shang_1.png`（蓝色） |
| 装备 | 1 | `Packages/com.planetwar.reusable-main-menu/Art/Hangar/Card/bg_jiku_shang_2.png`（紫色） |

- 不要依赖 `HangarView` 外部 Sprite 引用设置底板；Unity 重序列化可能清空该引用。
- Deck、Collections 和详情预览都必须通过 `HangarCardItem.ApplyVisual()` 应用相同的底板逻辑。
- `guide` 在目标项目中仅用于首关前的法术新手引导。本项目没有对应任务系统，因此 Deck、Collections、详情预览以及 Prefab 构建流程均应关闭 `guide`，避免遮挡装备图标和底板。

## 6. 新增飞机或装备物品

1. 在 `Assets/Data/Backpack/Items/` 新建 `ItemData`。
2. 配置唯一 `Item Id`、显示名称、`Icon`、类型、形状与战斗数据。
   - 飞机：`Item Type = Aircraft`，配置冷却、形状和 `Fighter Definition`。
   - 装备：`Item Type = Equipment`，配置形状和 `Equipment Effects`。
3. 将 `ItemData` 加入 `Assets/Resources/PlayerItemCatalog.asset` 的 `items` 列表。
4. 为多格物品确认 `ShapeOffsets` 属于当前支持的形状之一；否则需要扩展 `ItemViewPrefabSelector` 与背包 Prefab 模板。
5. 重进 Play Mode 或重新加载 `PlayerItemSystem`。
6. 验证物品出现在 Collections；将它配装到兼容 Deck 槽位后，验证玩家商店会刷新该物品。

仅创建 `ItemData` 而不加入 `PlayerItemCatalog`，物品不会出现在 Hangar、无法配装，也不会进入玩家商店。

## 7. 验收清单

- [ ] 飞机仅能配装到 0–2 槽，装备仅能配装到 3–4 槽。
- [ ] Deck 内不存在重复物品。
- [ ] Collections 不显示已配装物品。
- [ ] 玩家商店只刷新 Deck 物品。
- [ ] 飞机卡显示蓝色底板，装备卡显示紫色底板。
- [ ] 所有物品显示其 `ItemData.Icon`；没有卡片被 `guide` 遮挡。
- [ ] 新增及已有 Catalog 物品均自动恢复为已解锁、最大等级，并可从 Collections 配装。
