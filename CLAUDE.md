# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个使用 Godot 4.6.2 引擎和 C# (.NET 8.0) 开发的《植物大战僵尸》复刻项目。项目采用面向对象设计，包含完整的游戏逻辑、实体管理系统和状态效果系统。

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

### 动画格式转换
用于将 PVZ 原版 reanim 格式转换为 Godot 动画资源。

```bash
# 基本格式
C:\Users\HYTomZ\试验场\所有工具\PVZ_reanim2godot_animation.exe <input_file> <anim_godot_path> <resource_godot_path> <"tscn_by_anim" || "anim_tres" || "auto">

# 示例：转换双发射手动画
C:\Users\HYTomZ\试验场\所有工具\PVZ_reanim2godot_animation.exe C:\Users\HYTomZ\Documents\Godot\plants-vs-zombies\art\MainGame\Plants\PeaShooter\PeaShooter.reanim res://MainGame/Plants/PeaShooter/ res://art/MainGame/Plants/PeaShooter/ auto

# 示例：转换大嘴花动画
C:\Users\HYTomZ\试验场\所有工具\PVZ_reanim2godot_animation.exe C:\Users\HYTomZ\Documents\Godot\plants-vs-zombies\art\MainGame\Plants\Chomper\Chomper.reanim res://MainGame/Plants/Chomper/ res://art/MainGame/Plants/Chomper/ auto
```

**参数说明：**
- `<input_file>`: 输入的 .reanim 文件路径
- `<anim_godot_path>`: Godot 动画资源路径前缀 (用于 .tres 文件)
- `<resource_godot_path>`: Godot 资源路径前缀 (用于纹理引用)
- `<mode>`: 转换模式，通常使用 "auto"

**输出文件：**
- 生成的 .tscn 场景文件在 art 目录下，需要手动移动到 MainGame 对应目录
- 生成的 .tres 动画资源文件在 art 目录下，需要手动移动到 MainGame 对应目录

## 代码架构

### 核心类层次结构

```
Entity (Node2D)
├── HealthEntity (抽象类)
│   ├── Plants (抽象类)
│   │   ├── PeaShooterSingle
│   │   ├── PeaShooter (双发射手)
│   │   ├── Sunflower
│   │   ├── CherryBomb
│   │   ├── PotatoMine
│   │   ├── WallNut
│   │   ├── SnowPea
│   │   ├── Squash
│   │   └── Chomper (新添加)
│   └── Zombie
│       ├── NormalZombie
│       ├── ConeheadZombie
│       ├── BucketheadZombie
│       └── ScreendoorZombie
└── 其他实体 (LawnMower, Sun 等)
```

### 关键系统

#### 1. 主游戏管理器 (`MainGame/MainGame.cs`)
- 游戏循环和状态管理
- 植物和僵尸的栈式数组管理 (Plants[1000], Zombies[1000])
- 波次生成系统 (`ZombieWeightsAndGrades`, `ZombieType`)
- 阳光生成和资源管理
- 草坪机系统

#### 2. 状态效果系统 (`MainGame/Effects/`)
- `StatusEffectManager`: 管理实体上的状态效果
- `StatusEffect` (抽象类): 状态效果基类
- 当前支持的效果类型: `Slow` (减速), `Freeze` (冻结), `Rage` (狂暴)
- 效果影响移动速度、攻击速度乘法因子

#### 3. 护甲系统 (`MainGame/Armor/`)
- `ArmorManager`: 处理僵尸护甲逻辑
- 具体护甲类型: `Cone`, `Bucket`, `Screendoor`, `FootballHelmet`, `Hardhat`, `Newspaper`, `Ladder`, `Balloon`, `Tallnut`, `Wallnut`
- 护甲集成在僵尸的伤害处理流程中

#### 4. 场景系统 (`MainGame/Common/Scene.cs`)
- `Scene` (抽象类): 场景基类
- `LawnDayScene`: 白天草坪场景
- 处理草坪单元布局、背景、BGM 等

#### 5. 资源数据库 (`ResourceDB.cs`)
- 集中管理游戏资源 (图片、音效)
- 通过静态属性访问，例如 `ResourceDB.Images.Zombies.ImageZombie_OuterarmUpper`

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
│   ├── Common/        # 通用类 (Scene, Lawn)
│   ├── Drops/         # 掉落物 (Sun)
│   ├── Effects/       # 状态效果系统
│   ├── Entities/      # 实体基类
│   ├── HitBox/        # 碰撞区域接口
│   ├── LawnMower/     # 草坪机
│   ├── Plants/        # 植物相关
│   │   ├── Bullet/    # 子弹 (Pea, SnowPea)
│   │   └── [各种植物]/
│   └── Zombies/       # 僵尸相关
├── MainMenu/          # 主菜单界面
├── Menu/              # 通用菜单组件
├── art/               # 美术资源
├── fonts/             # 字体文件
├── sounds/            # 音效资源
└── particles/         # 粒子效果
```

## 开发注意事项

### 添加新植物
1. 在 `MainGame/Plants/` 下创建新目录
2. 继承 `Plants` 抽象类
3. 实现 `_Idle()` 抽象方法
4. 可选重写 `_Plant()`, `_SetColor()`, `_SetAlpha()`
5. 设置 `SunCost` 和 `CDtime` 属性

### 添加新僵尸
1. 在 `MainGame/Zombies/` 下创建新类
2. 继承 `Zombie` 类
3. 在 `ZombieTypeEnum` 和 `ZombieType` 类中注册新类型
4. 更新 `ZombieWeightsAndGrades` 中的权重和等级

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
- 使用 `GD.Print()` 进行调试输出
- 僵尸速度、状态效果变化等重要信息已包含调试输出

### 场景树查看
```csharp
// 在代码中打印场景树
GetNode<Node>("/root").PrintTreePretty();
```

### 僵尸波次调试
- `ZombieWeightsAndGrades` 控制僵尸类型权重和等级
- 波次容量计算公式: `(int)((int)(当前波数 * 0.8) / 2.0) + 1`
- 大波 (10的倍数) 容量乘 2.5

## 已知设计模式

1. **单例模式**: `Global.Instance`, `MainGame.Instance`
2. **状态模式**: 状态效果系统
3. **策略模式**: 伤害处理流程中的护甲系统
4. **对象池模式**: 植物和僵尸的栈式数组管理
5. **观察者模式**: 状态效果变化事件通知

## 扩展性考虑

- 状态效果系统设计为可扩展，新增效果只需继承 `StatusEffect`
- 护甲系统通过 `ArmorManager` 集中管理，支持添加新护甲类型
- 场景系统支持添加新场景类型 (夜间、泳池等)
- 僵尸生成系统支持权重配置和类型扩展

## Claude Code 协作规则

### 核心规则
1. 禁止读取 .tscn, .tres, .res 文件。除非用户明确说“允许读tscn”。
2. 优先读取和修改 .cs 文件。
3. 每次任务最多读取3个文件。
4. 严禁操作git，如提交、推送、拉取等。
5. 回答必须简洁。禁止使用“当然”、“很高兴”、“好的”等语气词。直接输出内容，不寒暄。

### 任务执行流程
- 能通过修改.cs完成 → 直接改，完成后报告
- 不能通过修改.cs完成 → 输出【手动操作清单】，停止，等用户确认

### 需要输出清单的情况
添加/删除节点、修改父子关系、修改非Export属性、修改信号连接、修改动画关键帧、修改着色器参数(未通过脚本暴露)

### 手动操作清单格式
```
<notice>
需要手动修改场景文件：
- 文件: [路径]
- 节点: [节点名]
- 操作: [具体操作]
</notice>
```
然后询问: “是否继续？回复‘是’我将读取并修改。”

### 禁止行为
- 未经许可读取场景文件
- 未经许可提交或推送代码
- 绕过.cs直接改场景
- 输出清单后继续尝试其他方法
- 输出“我来帮您”等废话

### 输出规范
- 不解释背景
- 不总结
- 不反问（除了询问是否继续）
- 直接给代码或清单

### C# 规范
类名/方法名/属性: PascalCase | 私有字段: _camelCase | 局部变量: camelCase | Godot API: GD.Print()

### 示例
用户: “把玩家速度改成500”

正确输出:
Player.cs 中 Speed 改为500。

用户: “把玩家位置改为(100,200)”

正确输出:
```
<notice>
需要手动修改场景文件：
- 文件: scenes/Player.tscn
- 节点: Player
- 操作: 修改 position 为 Vector2(100,200)
</notice>
```
是否继续？回复“是”我将读取并修改。