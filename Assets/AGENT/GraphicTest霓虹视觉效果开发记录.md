# GraphicTest 霓虹飞机视觉效果

## 目标

在 `Assets/Scenes/GraphicTest.unity` 中预览类似《几何战争》与 Nova Drift 的 2D 霓虹视觉：深色宇宙背景、飞机内部压暗冷白、淡蓝色清晰外轮廓，以及由 Bloom 产生的柔和光晕。

本次工作只涉及画面表现，不包含游戏逻辑。

## 场景与后处理

- 项目使用 URP，渲染管线已支持 HDR。
- `Main Camera` 需要启用 **Post Processing**。
- 为该预览场景创建或使用 Global Volume，并启用 **Bloom**。
- 推荐起始值：Background `#050816`、Bloom Threshold `0.8`、Bloom Intensity `1.2`、Bloom Scatter `0.7`。

## 飞机 Shader

- Shader：`Assets/Shaders/Graphics/NeonSpriteOutline.shader`
- Material：`Assets/Shaders/Graphics/MAT_TestFlightNeon.mat`
- 使用位置：`GraphicTest` 场景中的 `TestFlight` Sprite Renderer。

Shader 使用飞机贴图的 Alpha 进行八方向采样，扩张出轮廓遮罩：

- 原始贴图内部以 `Core Color × Core Intensity` 显示，形成暗白机身。
- 透明区中紧贴机身的像素使用 `Outline Color × Outline Intensity` 显示，形成 HDR 蓝色轮廓。
- HDR 轮廓由 Bloom 向外扩散，形成范围柔和的光晕。

## 材质调节

| 参数 | 用途 | 推荐起始值 |
| --- | --- | --- |
| Core Color | 机身内部的冷白色调 | `#B8C7DB` |
| Core Intensity | 机身亮度 | `0.35` |
| Outline Color | 外轮廓的发光色 | 淡蓝色 |
| Outline Intensity | HDR 发光强度 | `3`；更强可设 `4–5` |
| Outline Width (Pixels) | 清晰轮廓厚度 | `1.5`；更粗可设 `2–3` |

## 后续视觉迭代

- 通过增加低频噪声或正弦脉冲，让轮廓亮度轻微呼吸。
- 在飞机移动时叠加尾焰、粒子与短暂残影。
- 为敌我单位建立不同的轮廓配色材质预设。
