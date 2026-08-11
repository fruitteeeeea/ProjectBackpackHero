# Unity UI 跨项目迁移规范

本规范用于把指定 Unity UI 制作成可复用的**本地嵌入式 UPM 包**，并稳定导入其他 Unity 6 项目。它适用于主菜单、HUD、弹窗、底栏等 UGUI/TMP 界面。

目标是迁移视觉、布局和交互契约，而不是迁移原项目的业务系统。

## 1. 交付边界

每次迁移应明确以下范围：

- 要迁移的 UI 根节点、子面板、按钮、贴图、字体、材质和动画状态；项目业务依赖，例如场景切换、存档、Addressables
- 要保留的设计分辨率、Canvas Scaler、RectTransform、锚点、排序；
- 要移除的旧、音频、DOTween、Spine、数值系统和全局 UI 管理器；
- 宿主项目以后需要接入的语义按钮动作；
- 背景是否由宿主场景提供。

第一阶段默认只交付展示和交互事件：按钮不执行实际游戏功能。

## 2. 推荐包结构

在目标项目 `Packages/` 下建立本地嵌入式包，例如：

```text
Packages/
  com.company.reusable-main-menu/
    package.json
    Runtime/
    Prefabs/
    Art/
    Themes/
    Samples~/MainMenuDemo/
    Tests/
    Documentation~/
```

`package.json` 仅声明真实需要的依赖。UGUI 界面通常需要：

```json
"dependencies": {
  "com.unity.ugui": "<Unity 对应版本>",
  "com.unity.textmeshpro": "<Unity 对应版本>",
  "com.unity.inputsystem": "<Unity 对应版本>"
}
```

不要为了让旧 prefab 编译通过而引入原项目完整业务框架。若旧序列化引用无法避免，可用最小兼容组件保留必要字段，但组件不得访问旧项目服务。

## 3. 静态 UI 是硬性要求

UI 的 GameObject 层级必须保存为 prefab 或 scene 的**静态序列化节点**：

- 在 Hierarchy 中直接可见；
- 可在 Inspector 中直接修改贴图、文字、颜色、锚点和大小；
- UI 资源引用在编辑器中可追踪；
- Demo 场景必须包含静态 Canvas、EventSystem 和 UI prefab 实例。

运行时脚本不得 `Instantiate`、`Resources.Load` 或根据代码创建 UI 布局。它只负责：

- 订阅已有 `Button.onClick`；
- 派发语义事件或输出调试日志；
- 在必要时刷新静态展示文本。

## 4. 布局与资源迁移

优先复制源 prefab 的序列化层级，而不是按截图重搭。逐项保持：

- `RectTransform` 的 Anchor Min/Max、Pivot、Anchored Position、Size Delta、Scale；
- Canvas 的 Render Mode、Sorting、Canvas Scaler、参考分辨率和匹配策略；
- Image 的 Sprite、Material、Image Type、9-slice Border、Preserve Aspect；
- TextMeshPro 的字体、字号、行距、对齐、描边和材质；
- sibling 顺序、Canvas 排序和遮罩层级。

本项目主菜单的基准为竖屏 `720 × 1280`。迁移后至少应在该分辨率下与源界面逐层比对，再检查常用宽高比。

### 资源原则

- 复制贴图时连同 `.meta` 与导入配置处理，保证 Sprite 子资源、9-slice 和材质引用正确；
- 可替换资源集中到 `ScriptableObject Theme` 或宿主项目的 prefab override；
- 不直接修改包内默认美术来适配某个项目；项目特化内容放在宿主 `Assets/` 中。

## 5. Unity 6 Input System

Unity 6 项目若使用新 Input System，Demo 场景不得保留旧输入模块：

- EventSystem 使用 `InputSystemUIInputModule`；
- 移除 `StandaloneInputModule`；
- Package Manager 中确保安装 `com.unity.inputsystem`；
- Project Settings 的 Active Input Handling 使用新 Input System（或项目明确选择的兼容模式）。

否则点击 UI 时可能出现：

```text
You are trying to read Input using the UnityEngine.Input class...
```

## 6. 安全区域（刘海屏）

不要把整个菜单根节点缩进安全区；这会压缩背景、主面板或底栏，并造成大块空白。

正确做法：

1. 全屏背景保持铺满屏幕；
2. 按原设计决定哪些 HUD 内容需要避开刘海；
3. 只对这些内容的容器（例如 `Top` HUD）施加最小必要的 `Screen.safeArea` 偏移；
4. 使用 `anchoredPosition` 或专用安全区容器，不要在每次尺寸变化时改写整棵 UI 的 `offsetMin/offsetMax`；
5. 在 Device Simulator、真机刘海屏、非刘海屏分别检查。

避免在 `OnRectTransformDimensionsChange` 中反复重建或改写布局，否则可能导致 Device Simulator / Editor 无限刷新。

## 7. 按钮交互契约

第一阶段的按钮不接场景切换、弹窗、音效或存档逻辑。所有静态按钮应能安全点击并输出可检索的日志，例如：

```text
[MainMenu Debug] Button pressed: Canvas/MainMenu/UIMain/Start
```

后续推荐以稳定语义枚举和事件接入：

```csharp
public event Action<MainMenuAction> ActionInvoked;
```

宿主项目订阅事件并实现自己的业务。包内不得引用宿主的场景名、单例或服务定位器。

## 8. 背景与宿主场景整合

可复用 UI prefab 的背景应默认保持透明或可通过 Theme / prefab override 关闭，使宿主项目可使用自己的场景背景（例如星空、3D 场景、粒子或视频）。

手动整合 SampleScene 风格背景的安全流程：

1. 在主菜单场景中保留或创建 `Main Camera`；
2. 从背景来源场景复制完整的背景根节点及其子节点和资源引用；
3. 不复制与背景无关的战斗、背包或调试桥接组件；
4. 在 `MainMenu > UIMainBase > Image` 将原全屏背景 `Image` 的 Color Alpha 设为 `0`（仅创建场景或 prefab instance override，不改包内默认 prefab）；
5. 保存后重新打开场景，确认 Canvas 仍显示在背景之上。

不要手工编辑 `.unity` YAML 来做复杂的 prefab override 或动态背景装配。YAML 解析失败可能使 Unity 只加载场景的一部分；应在 Unity Editor 中复制节点、设置 Inspector 字段并保存。

## 9. 验收清单

- [ ] Package 导入后没有编译错误、Missing Script 或旧项目运行时依赖。
- [ ] 所有 UI 节点在编辑器可见且可调整，运行时不生成 UI。
- [ ] Canvas / EventSystem 使用 Unity 6 新 Input System 配置。
- [ ] 720 × 1280 下布局、贴图、排序和锚点与源 UI 一致。
- [ ] 刘海屏安全区只影响需要避让的 HUD 容器。
- [ ] 所有按钮点击只输出调试日志或派发语义事件，不执行游戏业务。
- [ ] 主菜单全屏底图透明时，宿主背景可正常显示，UI 始终在背景之上。
- [ ] 重启 Unity、重新导入包、重新打开 Demo 场景后，界面依旧完整显示。

## 10. 本次迁移经验

- 静态 prefab 迁移比代码搭 UI 更稳定、可审查，也更适合跨项目复用；
- 同时刷新包和引用该包的 Sample 场景时，应等待包导入完成后再打开场景；
- 不要在 `OnValidate` 中直接或间接执行不允许的组件创建操作；
- 自动化编辑器工具只能在可重复验证后保留。背景这种已有场景资源，优先使用 Unity 的复制/粘贴和 Inspector；
- Console 中出现的宿主项目调试桥接日志（例如背包调试桥）不属于主菜单 UPM 包，应与 UI 迁移逻辑隔离。
