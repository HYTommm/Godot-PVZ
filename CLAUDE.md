# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个使用 Godot 4.6.2 引擎和 C# (.NET 8.0) 开发的《植物大战僵尸》复刻项目。项目采用面向对象设计，包含完整的游戏逻辑、实体管理系统和状态效果系统。

关卡已 Resource 化：波次、僵尸池、场景类型、初始阳光全部是可配置的 `.tres`（见「关卡系统」）。

## 开发环境

- **引擎**: Godot 4.6.2
- **编程语言**: C# (.NET 8.0, Android 构建使用 .NET 9.0)
- **项目文件**: `project.godot` (主配置文件)
- **解决方案文件**: `Plants vs Zombies.csproj` (C# 项目文件)

## 常用命令

### 运行项目
```bash
# 使用 Godot 编辑器打开项目
godot --editor

# 直接运行项目
godot

# 在 Windows 上，也可以双击 project.godot 文件
```

### 构建项目
```bash
# 构建 C# 代码
dotnet build

# 清理构建
dotnet clean
```

### Android 构建
```bash
# 进入 Android 构建目录
cd android

# 使用 Gradle 构建
gradlew assembleDebug

# 或使用 Godot 导出功能
```

### 关卡数据工具

关卡数值的唯一真相是 `MainGame/Levels/LevelDataSpec.cs`（代码里的表），
`.tres` 由它生成、并与它比对。两个工具靠命令行参数触发，正常游戏不受影响：

```bash
# 按 LevelDataSpec 重建全部关卡 .tres
godot --headless --quit --path <项目目录> -- --generate-levels

# 比对运行时生效值与表，检查数据漂移
godot --headless --quit --path <项目目录> -- --verify-levels
```

**改关卡数值改 `LevelDataSpec`，再跑 `--generate-levels`。不要手改 `.tres`。**

### 动画格式转换
用于将 PVZ 原版 reanim 格式转换为 Godot 动画资源。

工具用 cpp 重写版：
`C:\Users\HYTomZ\source\repos\PVZ_reanim2godot_animation\cpp\x64\Release\PVZ_reanim2godot_animation_cpp.exe`

```bash
# 基本格式
<exe> <reanim 文件> <动画资源路径> <素材路径> [选项]

# 常用选项
#   -om  auto | tscn_by_anim | anim_tres   输出模式，默认 auto
#   -fm  inherit | keyframe                帧模式，默认 inherit
#   -tm  separate | transform              轨道模式，默认 separate
#   -of  <路径>                             只取目录部分，用来指定输出目录
#   -cf  <配置文件>                         如 R2Ga.cf

# 示例：转换双发射手动画
<exe> C:\Users\HYTomZ\Documents\Godot\plants-vs-zombies\src\plants-vs-zombies\art\MainGame\Plants\PeaShooter\PeaShooter.reanim res://MainGame/Plants/PeaShooter/ res://art/MainGame/Plants/PeaShooter/ -fm keyframe
```

**参数说明：**
- 第 1 个（位置）：输入的 .reanim 文件路径
- 第 2 个（位置）：动画资源路径，如 `res://MainGame/Plants/PeaShooter/`
- 第 3 个（位置）：素材路径，即贴图实际所在的目录

**模式说明：**
- `-fm keyframe`：`<t>` 里的空字段保持为空、交给引擎插值。reanim 侧已经做过关键帧处理时必须用它；
  默认的 `inherit` 会把空字段填成上一帧的值，运动变成阶梯状。
- `-tm separate`：pos/rot/scale/skew 分开成四条轨道（**默认，别改成 transform**）。
  合并成 `Transform2D` 后，跨动画交叉淡入淡出时 Godot 按**矩阵相乘**混合而不是加权平均，
  切换瞬间姿态会跳——同一个动画内部的关键帧插值则不受影响。

**输出文件：**
- 默认输出到输入文件旁边；用 `-of <目录>` 可以直接指定输出目录。
- `auto` 模式下第 0 个动画段出 `.tscn`，其余段各出一个 `.tres`（只有一个动画段时就只有 tscn）。

## 代码架构

### 核心类层次结构

```
Entity (Node2D)
├── HealthEntity (抽象类)
│   ├── Plants (抽象类)                    PlantTypeEnum 共 10 种
│   │   ├── PeaShooterSingle               单发豌豆
│   │   ├── PeaShooter                     双发射手
│   │   ├── SuperPeaShooter                调试用超频豌豆
│   │   ├── SnowPeaShooter
│   │   └── Sunflower / WallNut / CherryBomb / PotatoMine / Squash / Chomper
│   └── Zombie (抽象类)
│       └── RegularZombie (抽象类)         动画、焦炭动画等公共实现
│           ├── TieZombie (抽象类)         走路 / 啃食 / 死亡状态机
│           │   ├── NormalZombie
│           │   ├── ConeheadZombie
│           │   ├── BucketheadZombie
│           │   ├── ScreendoorZombie
│           │   └── FlagZombie
│           ├── FootballZombie
│           ├── NewspaperZombie
│           └── PolevaulterZombie
└── 其他实体 (LawnMower, Sun 等)
```

`ZombieTypeEnum` 共 9 种。`MainGame/Zombies/Abc.cs` 是遗留 stub，无人引用。

### 关键系统

#### 1. 主游戏管理器 (`MainGame/MainGame.cs`)
- 游戏循环和状态管理
- 植物和僵尸的栈式数组管理（索引重用）
- 波次调度：参数全部来自 `LevelData`（见下节）
- 阳光生成和资源管理
- 草坪机系统

#### 2. 关卡系统 (`MainGame/Levels/`)
- `LevelData`（`Resource`）：一关一个 `.tres`，管波次、僵尸池、场景类型、初始阳光
- `LevelList`（`Resource`）：关卡索引，`LevelList.tres`
- `ZombieWaveEntry`：僵尸池条目（类型 / 权重 / 等级 / 首现波次）
- `LevelDataSpec`：**数值的唯一真相**（代码里的表）
- `LevelDataGenerator` / `LevelDataVerifier`：生成与校验，走命令行参数
- 装配链路：`MainMenu_SelectorScreen.EnterLevel()` → `Global.CurrentLevelData` → `MainGame._Ready()`
- 现有 1-1 ~ 1-10 十个正式关 + 调试图关 `Level_Debug.tres`

#### 3. 选卡系统
- `PlantTypes` (`MainGame/Plants/PlantTypes.cs`)：植物注册表，枚举 → 场景 + 显示名
- `ZombieType` (`MainGame/Zombies/ZombieTypes.cs`)：僵尸注册表，与上者对称
- `SeedSelectScreen` / `SeedBank` / `SeedSelection` / `SeedType` / `SeedPacketLarger`：
  选卡面板与种子栏（均在 `MainGame/` 下）
- 卡面的阳光花费与冷却不单独存，从植物场景实例上读（`Plants.SunCost` / `Plants.CDtime`）
- 调试图关可开 `LevelData.CanPlaceZombies`，把僵尸当种子卡直接摆到草坪上

#### 4. 子弹系统 (`MainGame/Plants/Bullet/`)
**数据集合 + 直渲**，不用节点：
- `BulletData`（`struct`）：纯数值（位置 / 速度 / 行 / 伤害……），不含节点引用
- `BulletSystem`：预分配 `BulletData[8192]`，栈式 swap-remove 压实。
  `StepPhysics()` 跑三趟（移动 → 碰撞 → 压实），`SubmitRender()` 在渲染帧
  经 `RenderingServer` 逐行提交 MultiMesh
- `HitEffectSystem`：命中粒子与音效池化，运行时零节点创建
- `Pea.cs` / `SnowPea.cs` / `Bullet.cs` 只剩命中粒子的贴图载体，不在主路径上

#### 5. 状态效果系统 (`MainGame/Effects/`)
- `StatusEffectManager`: 管理实体上的状态效果
- `StatusEffect` (抽象类): 状态效果基类
- 当前支持的效果类型: `Slow` (减速), `Freeze` (冻结), `Rage` (狂暴)
- 效果影响移动速度、攻击速度乘法因子

#### 6. 护甲系统 (`MainGame/Armor/`)
- `ArmorManager`: 处理僵尸护甲逻辑
- 具体护甲类型: `Cone`, `Bucket`, `Screendoor`, `FootballHelmet`, `Hardhat`, `Newspaper`, `Ladder`, `Balloon`, `Tallnut`, `Wallnut`
- 护甲集成在僵尸的伤害处理流程中

#### 7. 场景系统 (`MainGame/Common/`)
- `Scene` (抽象类): 草坪单元格布局、背景、BGM
- 子类: `LawnDayScene` (白天草坪)、`PoolDayScene` (白天泳池)、`MainMenuScene`
- `SceneKind` 枚举 (`Day` / `Pool`) 由 `LevelData.SceneType` 决定
- `Scene.Create(SceneKind, Node)` 是工厂入口。场景差异只到"选哪一个子类"为止，
  草坪尺寸、原点坐标、贴图仍由子类构造函数自己决定

#### 8. 资源数据库 (`ResourceDB.cs`)
- 集中管理游戏资源 (图片、音效)
- 通过静态属性访问，例如 `ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper`

#### 9. 日志系统 (`Log.cs`)
- 全项目唯一的日志出口，代码里不再出现 `GD.Print()` / `GD.PrintErr()`
- 等级 `Trace` < `Debug` < `Info` < `Warn` < `Error`，低于阈值的在写出之前就丢掉
- 三个旋钮：`Log.Enabled` 一键开关、`Log.MinLevel` 阈值、`Log.Prefix` / `Log.Suffix` 前后缀
- 阈值也能从命令行给，导出后不重编译就能调：`--quiet` 全关，`--log-level=<等级>` 指定阈值
  （都是同一件事，同时给时后写的生效）
- `Warn` / `Error` 交给 `GD.PushWarning` / `GD.PushError`（编辑器进 Errors 面板、导出后进 stderr），
  其余走 stdout
- 参数是 `params object[]`，和 `GD.Print` 一样直接往下排；参数在调用处就求值，
  所以逐帧路径上先问 `Log.IsOn(level)` 再拼消息

### 物理层配置
在 `project.godot` 中配置的 2D 物理层:
- `layer_1`: ground (地面)
- `layer_2`: plants (植物)
- `layer_3`: zombies (僵尸)
- `layer_4`: bullets (子弹)
- `layer_5`: packets (种子包)

### 实体管理策略

#### 栈式索引重用
- 植物和僵尸使用索引数组管理，删除时重用索引
- `PlantStack` 和 `ZombieStack` 跟踪当前栈顶
- 移除实体时交换索引以实现 O(1) 删除

#### 伤害系统
- `Hurt` 类: 包装伤害值、伤害类型
- 伤害类型: `Direct`, `Eating`, `LawnMower`, `AshExplosion`, `Explosion`, `Squash`, `Dying`
- 护甲系统在 `Zombie.Hurt()` 中处理伤害减免

#### 动画和粒子系统
- 僵尸使用 `AnimationPlayer` 控制动画
- 粒子效果用于断臂、掉头等视觉效果
- 状态效果改变着色器参数实现视觉反馈 (如减速时的蓝色色调)

## 文件组织结构

```
根目录/
├── MainGame/           # 核心游戏逻辑
│   ├── Armor/         # 护甲系统
│   ├── Common/        # 通用类 (Scene, SceneKind, Lawn)
│   ├── Drops/         # 掉落物 (Sun)
│   ├── Effects/       # 状态效果系统
│   ├── Entities/      # 实体基类
│   ├── HitBox/        # 碰撞区域接口
│   ├── LawnMower/     # 草坪机
│   ├── Levels/        # 关卡数据 (.tres) 与生成/校验工具
│   ├── Plants/        # 植物相关
│   │   ├── Bullet/    # 子弹数据集合与直渲系统
│   │   └── [各种植物]/
│   └── Zombies/       # 僵尸相关
├── MainMenu/          # 主菜单、选关界面
├── Menu/              # 通用菜单组件
├── art/               # 美术资源
├── fonts/             # 字体文件
├── sounds/            # 音效资源
├── particles/         # 粒子效果
└── shader/            # 着色器
```

## 工程工具链

- `.editorconfig` + `.githooks/pre-commit`：缩进统一为 4 空格（pre-commit 负责把 tab 换掉）
- `src/GodotPvzSourceGenerator/`：Roslyn 源生成器，csproj 以 Analyzer 引用。
  目前提供 `[StructInheritance]`（结构体继承），**尚无 struct 使用它**

## 开发注意事项

### 添加新植物
1. 在 `MainGame/Plants/` 下创建新目录
2. 继承 `Plants` 抽象类
3. 实现 `_Idle()` 抽象方法
4. 可选重写 `_Plant()`, `_SetColor()`, `_SetAlpha()`
5. 设置 `SunCost` 和 `CDtime` 属性

### 添加新僵尸
1. 在 `MainGame/Zombies/` 下创建新类，继承 `TieZombie`（普通形态）或 `RegularZombie`
2. 在 `ZombieTypeEnum`（`ZombieTypes.cs`）中加枚举值
3. 在 `ZombieType` 的注册表里补上场景与显示名
4. 在 `LevelDataSpec` 里补该关的 `WavePool` 条目（权重 / 等级 / 首现波次），
   再跑 `--generate-levels`

权重与等级按关卡配置，**不要手改 `ZombieWeightsAndGrades`**——它只是全局兜底默认值。

### 添加新状态效果
1. 在 `StatusEffectTypeEnum` 中添加新类型
2. 创建继承 `StatusEffect` 的类
3. 实现 `OnApply()`, `OnRemove()`, `OnTick()` 方法
4. 重写 `MovementMultiplier`, `AttackMultiplier`, `DisableMovement` 等属性

### 动画命名约定
- 僵尸动画: `Zombie_walk` (行走), `Zombie_eat` (啃食), `Zombie_death` (死亡)
- 植物动画: 根据具体植物定义
- 使用 `customSpeed` 参数结合状态效果乘法因子

## 调试技巧

### 控制台输出
- 调试输出一律走 `Log`，写法与 `GD.Print` 一致：

```csharp
Log.Debug("[MainGame] 僵尸 ", zombie.Name, " 落位第 ", row, " 行");
```

- 等级按输出频率选：逐帧、逐发子弹用 `Log.Trace`；构造与状态迁移用 `Log.Debug`；
  一局里值得留痕的节点（关卡装载、选卡落定、游戏结束、工具结论）用 `Log.Info`；
  能继续跑但不对的状态用 `Log.Warn`；真正的失败用 `Log.Error`
- 排查时把阈值放开：`Log.MinLevel = LogLevel.Trace;`
- 一键关掉全部输出：`Log.Disable();`

### 场景树查看
```csharp
// 在代码中打印场景树
GetNode<Node>("/root").PrintTreePretty();
```

### 僵尸波次调试

波次参数全在 `LevelData` 里（每关一份 `.tres`）。调试时改 `LevelDataSpec` 再重新生成：

- 波次容量公式：`zombieMaxGrade = int(int(波索引 * WaveCapacityBase) / 2) + 1`
  - `WaveCapacityBase` 默认 `2/3`，**波索引 0 起算**（1 起算时该系数不成立）
- 大波：`波索引 % FlagWaveInterval == FlagWaveInterval - 1`（即每 10 波的最后一波），
  容量乘 `BigWaveMultiplier`（默认 2.5），随后再乘关卡级 `WaveCapacityMultiplier`
- 僵尸池：`LevelData.WavePool`，每条 `ZombieWaveEntry` 决定类型 / 权重 / 等级 / 首现波次
- 调试图关后门：`LevelData.DebugOnlyFlagZombie`（每波只投一只旗帜僵尸）、
  `LevelData.CanPlaceZombies`（僵尸当种子卡直接摆）

## 已知设计模式

1. **单例模式**: `Global.Instance`, `MainGame.Instance`
2. **状态模式**: 状态效果系统、僵尸的 `StateMachine<T>`
3. **策略模式**: 伤害处理流程中的护甲系统
4. **对象池模式**: 植物和僵尸的栈式数组管理、子弹与命中表现
5. **工厂模式**: `Scene.Create(SceneKind, Node)`
6. **数据导向**: 子弹走数据集合 + 遍历，不走节点
7. **观察者模式**: 状态效果变化事件通知

## 扩展性考虑

- 状态效果系统设计为可扩展，新增效果只需继承 `StatusEffect`
- 护甲系统通过 `ArmorManager` 集中管理，支持添加新护甲类型
- 新增场景类型：加 `SceneKind` 枚举值 + 新 `Scene` 子类 + `Scene.Create` 加分支
- 关卡数值改动走 `LevelDataSpec` + `--generate-levels`，不要手改 `.tres`

## Claude Code 协作规则

### 读写范围
1. 读场景/资源文件（`.tscn`/`.tres`/`.res`）已授权，但要用 Grep 按关键字定位着读
   （节点名、`[node name`、`[connection`、属性名），**不要整文件读**——文件大，撑上下文。
2. 优先修改 `.cs`。改动若必须落到场景资源上，走清单。

### 任务执行流程
- 能通过修改 `.cs` 完成 → 直接改，完成后报告
- 必须改场景/资源文件 → 输出【手动操作清单】，停止，等用户确认

### 需要输出清单的情况
任何必须改 `.tscn` / `.tres` 才能完成的改动。常见的：
添加/删除节点、修改父子关系、修改节点序列化属性（如 position）、
修改信号连接、修改动画关键帧、修改未通过脚本暴露的着色器参数

### 手动操作清单格式
```
<notice>
需要手动修改场景文件：
- 文件: [路径]
- 节点: [节点名]
- 操作: [具体操作]
</notice>
```

### 禁止行为
- 绕过 `.cs` 直接改场景结构
- 输出清单后继续尝试其他方法

### git
- 用户要求时才提交/推送，不自行发起
- 提交信息：一行式中文，`<模块>：<做了什么>`。不写实现细节、不写数值来源、
  **不加 `Co-Authored-By` 之类的尾注**

### 输出规范
- **改代码类任务**：直接给结果，不解释背景、不寒暄、不复述
- **分析/盘点类任务**：正常总结与提问，不要为了"简洁"省掉必要结论
- 禁止"当然"、"很高兴"、"好的"、"我来帮您"这类语气词

### 仓库内文字
- 代码注释解释"为什么"，不写数据来源、`文件名:行号`、内部符号名
- 判据：任何准备写进仓库的文字，先问"如果这个仓库公开，这段话透露了什么"
- **改内容时直接替换，不留痕迹**：不写"已改为"、"原先"、"（已修）"、"改回"这类词。
  在文档里描述旧做法，等于把旧做法留在上下文里，读的人（包括 AI）不会自动忽略它。
  只写当前是什么；道理若仍成立（"不这样会崩"）就保留，但去掉时间词——
  描述"工具会怎样"的留，描述"我曾经怎样"的删

### C# 规范
类名/方法名/属性: PascalCase | 私有字段: _camelCase | 局部变量: camelCase | Godot API: GD.Print()

### 示例
用户: “把玩家速度改成500” → 直接改 `Player.cs`，简短报告。

用户: “把玩家位置改为(100,200)” → 位置序列化在场景文件里，输出手工清单，停下等确认。
是否继续？回复“是”我将读取并修改。