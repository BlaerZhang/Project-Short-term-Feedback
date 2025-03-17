# 3D等距游戏移动系统

本文档介绍如何设置和使用半即时回合制移动系统。

## 系统概述

该系统实现了以下功能：
- 鼠标悬停在地面上显示移动路径，点击地面确认路径并移动角色
- 基于角色速度的转向限制系统
- 使用圆弧指示器显示可移动范围
- 基于贝塞尔曲线的自然移动路径
- 半即时回合制游戏流程：规划阶段和执行阶段
- 角色朝向和速度系统

## 设置步骤

### 1. 导入脚本

确保以下脚本已经导入到项目中：
- `PlayerController.cs` - 玩家角色控制器
- `ArcIndicator.cs` - 移动范围圆弧指示器
- `IsometricCameraController.cs` - 等距视角相机控制器
- `GameManager.cs` - 游戏流程管理器

### 2. 场景设置

#### 创建地面

1. 创建一个平面作为地面：GameObject -> 3D Object -> Plane
2. 将地面的Layer设置为"Ground"（如果不存在，需要创建此Layer）
3. 根据需要调整地面大小和材质

#### 设置玩家角色

1. 导入角色模型或创建一个简单的3D模型（如胶囊体）作为角色
2. 将`PlayerController`脚本添加到角色GameObject上
3. 设置角色初始朝向（建议面向Z轴正方向）

#### 设置相机

1. 选择主相机（Main Camera）
2. 将`IsometricCameraController`脚本添加到相机上
3. 在Inspector中设置相机参数：
   - 设置`Target`为玩家角色
   - 调整`Offset`以获得理想的视角（推荐 (0, 10, -10)）
   - 确保`Use Orthographic`已勾选
   - 调整`Orthographic Size`以适应场景规模（推荐5-10）

#### 设置移动范围指示器

1. 创建一个空GameObject，命名为"ArcIndicator"
2. 将`ArcIndicator`脚本添加到这个GameObject上
3. 将ArcIndicator拖到玩家控制器的`Arc Indicator`字段中

#### 设置游戏管理器

1. 创建一个空GameObject，命名为"GameManager"
2. 将`GameManager`脚本添加到这个GameObject上
3. 将玩家角色拖到`Player Controller`字段中

### 3. 配置参数

#### 玩家控制器参数

调整`PlayerController`中的以下参数以适应游戏需求：
- `Max Speed`/`Min Speed` - 角色最大/最小移动速度
- `Current Speed` - 初始速度
- `Max Turn Angle`/`Min Turn Angle` - 最小/最大速度时的最大转向角度
- `Movement Radius` - 移动圆的半径
- `Move Time` - 完成一次移动的时间

创建两种材质用于路径显示：
1. 有效路径材质（绿色半透明）
2. 无效路径材质（红色半透明）
将这两种材质分别赋给`Valid Path Material`和`Invalid Path Material`

#### 圆弧指示器参数

调整`ArcIndicator`中的参数：
- `Arc Resolution` - 圆弧分辨率（点数越多越平滑）
- `Arc Width` - 圆弧线宽
- `Arc Color` - 圆弧颜色（建议使用半透明颜色）

### 4. 运行和测试

1. 确保所有组件都已正确配置
2. 运行游戏
3. 使用鼠标在地面上移动，应该能看到移动路径预览
4. 点击在有效范围内的地面，角色应该沿着预览路径移动

## 系统使用说明

### 基本游戏循环

1. **规划阶段**：时间静止，玩家可以使用鼠标在地面上选择移动目标。
   - 悬停在有效范围内会显示移动路径预览
   - 点击确认移动

2. **执行阶段**：时间流动，角色执行移动。
   - 角色会沿着计算出的路径移动
   - 移动完成后自动返回规划阶段

### 速度与转向系统

- 角色速度越高，移动距离越远，但转弯能力受限
- 角色速度越低，移动距离越近，但转弯能力更强
- 根据需要通过`PlayerController.ChangeSpeed()`方法调整速度

### 扩展功能

- 可以通过修改`PlayerController`中的`CalculateMovementPath`方法来改变路径计算方式
- 可以在`GameManager`中添加更多游戏状态和逻辑
- 可以为角色添加动画状态机，根据移动状态播放不同动画

## 注意事项

- 确保场景中有一个设置为"Ground"层的物体作为地面
- 如果移动不正常，检查相机是否正确设置并指向角色
- 确保Input System已经配置并启用

## 故障排除

1. **角色不移动**
   - 确认地面Layer设置为"Ground"
   - 检查GameManager状态是否正确
   - 确认鼠标点击在可移动范围内

2. **路径不显示**
   - 检查LineRenderer组件是否正确配置
   - 确认材质已设置
   - 检查相机是否正确设置

3. **圆弧指示器不显示**
   - 确认ArcIndicator对象已激活
   - 检查LineRenderer组件
   - 确认材质已设置 