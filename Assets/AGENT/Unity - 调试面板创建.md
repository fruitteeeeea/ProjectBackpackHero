请为 Unity 编写一个可直接使用的 EditorWindow 调试工具。

要求：

- 使用 C# 和 Unity 内置 EditorWindow / IMGUI。
    
- EditorWindow 脚本放在 `Assets/Editor` 目录下。
    
- 创建独立的 Runtime 层，用于连接 EditorWindow 调试面板与游戏运行时实例，并对外提供稳定的调试桥接接口。
    
- Runtime 层脚本放在普通运行时代码目录中，不得放在 `Assets/Editor` 下，也不得引用 `UnityEditor`。
    
- 提供 `MenuItem` 菜单入口，便于必要时手动打开调试窗口。
    
- 进入 Play Mode 并启动对应的 Runtime 实例后，由 Runtime 层自动打开对应的 Debug 窗口，不需要用户手动从 Tools 菜单打开。
    
- 退出 Play Mode、停止游戏或对应 Runtime 实例销毁时，相关 Debug 窗口应自动关闭。
    
- 避免因为脚本重编译、场景切换或重复初始化而重复打开多个相同的 Debug 窗口。
    
- 主要用于 Play Mode 下实时查看和控制游戏数据。
    
- 调试窗口只调用 Runtime 桥接层或现有业务接口，不直接修改系统内部数据结构。
    
- 做好空引用、未进入 Play Mode、目标对象丢失、场景切换和 Runtime 实例销毁等情况的提示与保护。
    
- 窗口需要实时刷新，但不要把复杂逻辑放在 `OnGUI` 中。
    
- 自动打开和关闭窗口的逻辑应集中管理，避免业务代码直接依赖具体的 EditorWindow 类型。
    
- 代码应完整、可编译，并附上简短的目录结构和使用说明。
    
- 优先保持实现简单清晰，不要引入第三方插件或不必要的架构。
    

我的具体需求是：