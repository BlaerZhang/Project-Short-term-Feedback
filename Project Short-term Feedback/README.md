# 回合制移动系统

这个系统实现了一个基于回合的移动机制，包含跑步、跳跃和转向三种动作，以及心理状态（信心和自我怀疑）模拟系统。

## 系统特点

- **回合制系统**：每回合只能执行一个动作，动作结束后自动进入下一回合
- **多种移动方式**：跑步、跳跃和转向
- **心理状态系统**：信心值和自我怀疑层数影响行动成功率
- **动作检定系统**：基于d20骰子的随机检定，决定动作成功或失败
- **视觉反馈**：路径预览、移动范围指示和落点标记
- **音效反馈**：脚步声、跳跃音效等

## 快速设置指南

### 1. 场景设置
在场景中放置以下预制体：

- `SceneInitializer` - 负责初始化UI和其他管理器
- `ArcIndicator` - 显示移动范围的圆弧指示器
- 确保角色有 `PlayerController` 组件

### 2. 图层设置
确保场景中有以下图层：

- `Ground` - 用于地面检测
- `Obstacle` - 用于障碍物碰撞检测

### 3. 材质设置
确保以下材质存在：

- 有效路径材质 (绿色)
- 无效路径材质 (红色)
- 转向指示器材质

### 4. 预制体引用
在 `PlayerController` 组件中设置以下引用：

- `landingMarkerPrefab` - 落点标记预制体
- `arcIndicator` - 圆弧指示器
- `validPathMaterial` - 有效路径材质
- `invalidPathMaterial` - 无效路径材质
- `turnIndicatorMaterial` - 转向指示器材质

### 5. 动画设置
确保角色动画控制器支持以下参数：

- `IsMoving` (bool) - 控制跑步动画
- `Jump` (trigger) - 控制跳跃动画
- `IsTurning` (bool) - 控制转向动画
- `Speed` (float) - 控制移动速度

## 使用方法

1. 在游戏中按 `Q` 键选择跑步动作
2. 在游戏中按 `W` 键选择跳跃动作
3. 在游戏中按 `E` 键选择转向动作
4. 在游戏中按空格键跳过当前回合
5. 在选择动作后，使用鼠标指向目标位置并左键点击执行动作
6. 使用数字键1、2、3快速切换移动速度

## 常见问题解决

### 问题：UI不显示
解决方案：
- 确保场景中有 `SceneInitializer` 预制体
- 检查控制台中是否有UI相关的错误信息

### 问题：执行动作后无法继续移动
解决方案：
- 确保在 `RunningCoroutine` 方法末尾调用了 `EndTurn()` 方法
- 检查 `GameManager` 是否正确处理了执行阶段结束后的状态转换

### 问题：无法看到移动范围指示器
解决方案：
- 确保场景中有 `ArcIndicator` 预制体
- 检查 `PlayerController` 中的 `arcIndicator` 引用是否正确

### 问题：动作检定始终失败或成功
解决方案：
- 检查 `PlayerController` 中的 `enableActionCheck` 是否为 true
- 调整 `veryEasyDifficulty` 到 `veryHardDifficulty` 的数值来改变难度

## 扩展和自定义

1. 调整 `PlayerController` 组件中的参数来自定义移动范围、速度和检定难度
2. 修改 `UIManager` 中的UI元素布局来自定义界面
3. 在 `AudioManager` 中添加新的音效来增强游戏体验 