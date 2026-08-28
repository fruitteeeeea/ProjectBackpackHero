# ProjectBackpackHero Android 打包计划

## 1. 当前基线

| 项目 | 当前值 | 结论 |
| --- | --- | --- |
| Unity 版本 | 6000.5.3f1 | 使用同版本编辑器打包 |
| 公司名 | Lantern Fox Games | 已设置 |
| 产品名 | ProjectBackpackHero | 已设置 |
| Android 包名 | `com.lanternfoxgames.projectbackpackhero` | 已设置；发布后不要再修改 |
| 版本名 | `1.0` | 首次测试可用，正式发布前确认 |
| Version Code | `1` | 每次上传商店必须递增 |
| 脚本后端 | IL2CPP | 符合正式 Android 构建要求 |
| CPU 架构 | ARM64 | 符合 Google Play 64 位要求 |
| 最低系统版本 | Android 8.0 / API 26 | 当前配置 |
| Target API | Automatic / Unity 默认最高已安装版本 | 正式发布前按商店要求确认 |
| Android 工具链 | SDK、NDK、OpenJDK 已随 Unity 安装 | 已检查本机安装 |

## 2. 打包前必须确认

### 2.1 启动场景与场景清单

当前 Build Profiles/Build Settings 中启用了两个场景，且第一个场景是插件示例：

1. `Assets/Samples/PlanetWar Reusable Main Menu/0.1.0/Main Menu Demo/MainMenuDemo.unity`
2. `Assets/Scenes/SampleScene.unity`

构建后的游戏会从第一个场景启动。正式打包前需要确认它是否确实是主菜单；如果不是，应将真正的启动场景放到索引 0，并只保留游戏实际使用的场景。

### 2.2 屏幕方向

当前为自动旋转，并允许横屏和竖屏。需要根据游戏设计确定以下其中一种：

- 只允许横屏；
- 只允许竖屏；
- 保持自动旋转。

### 2.3 图标、启动图与显示名称

当前没有发现 Android 平台图标配置。正式包需要补齐自适应图标、传统图标和启动画面，并在真机上检查刘海屏、安全区及不同宽高比。

### 2.4 签名密钥

测试 APK 可以先使用 Unity 调试签名。正式 AAB 必须创建并长期保存专用 keystore：

- keystore 不放在项目目录，不提交 Git；
- 密码使用密码管理器保存；
- 备份 keystore、alias 和密码；
- 发布后丢失密钥可能导致无法更新应用。

### 2.5 当前运行日志中的待处理项

最近一次编辑器试玩能够进入 `SampleScene`，但日志持续报告 Unity 内置 Sprite 路径不存在：

- `Assets/Scripts/BackpackPrototype/AircraftEquipmentMarkerDisplay.cs:107` 请求 `UI/Skin/UISprite.psd`；
- `Assets/Scripts/Input/CircleMarker2D.cs:60` 请求 `UI/Skin/Knob.psd`。

这类资源在 Unity 6 中可能不再可用，Android 包中可能表现为标记图形缺失。生成首个 APK 前，应改为引用项目内的 Sprite，并重新检查 Console 没有相关错误。

## 3. 两阶段构建流程

### 阶段 A：开发测试 APK

目标：尽快得到可安装包，验证项目能在 Android 真机启动和游玩。

1. Unity Hub 确认使用 `6000.5.3f1` 打开项目。
2. 在 Build Profiles 中切换到 Android。
3. 确认启动场景和游戏所需场景。
4. 保持 IL2CPP + ARM64。
5. 关闭 Development Build、Script Debugging 和 Autoconnect Profiler，先做接近正式环境的测试包。
6. 输出 APK 到 `Builds/Android/Development/ProjectBackpackHero-dev.apk`。
7. 安装到至少一台 ARM64 Android 设备，完成冒烟测试。

开发包验收项：

- 能安装、启动、进入第一局并正常退出；
- UI 在目标方向和常见宽高比下不裁切；
- 触摸、音频、震动、存档和场景切换正常；
- 无持续报错、闪退、黑屏或明显卡顿；
- 卸载重装与覆盖安装行为符合预期。

### 阶段 B：正式签名 AAB

目标：生成可上传 Google Play 或其他支持 AAB 渠道的正式包。

1. 创建正式 keystore 并完成离线备份。
2. 在 Publishing Settings 中选择自定义 keystore 和 alias。
3. 设置版本名，例如 `1.0.0`；设置并记录递增的 Version Code。
4. 确认 Target API 满足目标商店当期要求。
5. 开启 Build App Bundle，保持 IL2CPP + ARM64。
6. 关闭 Development Build、调试符号外发和测试开关。
7. 输出到 `Builds/Android/Release/ProjectBackpackHero-1.0.0-1.aab`。
8. 上传到商店内部测试轨道，再通过商店生成的 APK 在真机验证。

正式包验收项：

- AAB 使用正确证书签名；
- 包名固定为 `com.lanternfoxgames.projectbackpackhero`；
- Version Code 高于历史上传版本；
- 商店预检查无目标 API、权限、64 位或包体错误；
- 内部测试安装、更新、存档兼容和核心流程通过。

## 4. 推荐执行顺序

1. 确认正式启动场景。
2. 确认横竖屏策略。
3. 补齐 Android 图标和启动图。
4. 替换 Unity 6 中缺失的内置 Sprite 引用。
5. 切换 Android 平台并解决首次编译错误。
6. 生成开发 APK，完成真机冒烟测试。
7. 创建正式 keystore，生成签名 AAB。
8. 上传内部测试轨道，完成发布前回归。

## 5. 版本记录规则

建议每次构建记录：构建日期、Git 提交、版本名、Version Code、构建类型、签名证书、测试设备和结果。示例：

`2026-08-28 | <git-sha> | 1.0.0 | 1 | AAB Release | upload-key-v1 | PASS`
