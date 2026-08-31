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

## 2. 已确认的测试包配置

### 2.1 启动场景与场景清单

测试包固定启用以下两个场景：

1. `Assets/Samples/PlanetWar Reusable Main Menu/0.1.0/Main Menu Demo/MainMenuDemo.unity`
2. `Assets/Scenes/SampleScene.unity`

构建后的游戏从 `MainMenuDemo` 启动，再进入 `SampleScene`。该顺序已经确认。

### 2.2 屏幕方向

测试包固定为正向竖屏，禁止倒置竖屏和两个横屏方向，并关闭 Android 可调整窗口模式。

### 2.3 图标、启动图与显示名称

测试包使用 `Assets/Art/Images/icon.png` 配置 Android 传统和圆形图标。该图为 110×110 透明 PNG；测试阶段不配置需要独立前景和背景素材的自适应图标。

### 2.4 签名密钥

测试 APK 可以先使用 Unity 调试签名。正式 AAB 必须创建并长期保存专用 keystore：

- keystore 不放在项目目录，不提交 Git；
- 密码使用密码管理器保存；
- 备份 keystore、alias 和密码；
- 发布后丢失密钥可能导致无法更新应用。

### 2.5 Unity 6 Sprite 兼容修复

原项目调用了 Unity 6 已移除的两个内置 Sprite 路径：

- `Assets/Scripts/BackpackPrototype/AircraftEquipmentMarkerDisplay.cs:107` 请求 `UI/Skin/UISprite.psd`；
- `Assets/Scripts/Input/CircleMarker2D.cs:60` 请求 `UI/Skin/Knob.psd`。

装备色块改用 UGUI 默认白色纹理；圆形标记改用项目内 `NVPaginationDot.png`。构建前测试会确认 Console 不再出现上述资源错误。

### 2.6 首个测试 APK 验证结果（2026-08-28）

- 两个相关 EditMode 测试均通过，Unity 编译无 C# 错误；
- Unity BuildReport 为 Success，APK 已输出到 `Builds/Android/Development/ProjectBackpackHero-dev.apk`；
- APK 文件大小为 67,598,772 字节，SHA-256 为 `F48597BABF370830634220E10DA653FDD335957E78F3558C4EC62DEC2F134CB1`；
- Android SDK 检查确认包名 `com.lanternfoxgames.projectbackpackhero`、应用名 `ProjectBackpackHero`、版本 `1.0 (1)`、最低 API 26、Target API 36、仅 ARM64；
- 主 Activity 为 `com.unity3d.player.UnityPlayerGameActivity`，方向固定为 portrait、不可调整窗口，传统和圆形图标资源均已打入 APK；
- APK 使用 Android Debug 证书和 APK Signature Scheme v2 签名；
- 当前未连接 Android 设备，覆盖安装、启动、竖屏锁定和核心流程冒烟测试仍待真机执行。

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

1. 运行 Unity 编辑器测试并确认 Sprite 兼容修复通过。
2. 执行菜单 `工具/Android/构建测试 APK`。
3. 检查 APK 包名、版本、竖屏声明和图标资源。
4. 连接 Android 设备后完成安装和真机冒烟测试。
5. 测试包通过后，再创建正式 keystore 并生成签名 AAB。

## 5. 版本记录规则

建议每次构建记录：构建日期、Git 提交、版本名、Version Code、构建类型、签名证书、测试设备和结果。示例：

`2026-08-28 | <git-sha> | 1.0.0 | 1 | AAB Release | upload-key-v1 | PASS`
