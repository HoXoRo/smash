## 箭头迷宫玩法与代码结构说明

### 1. 总体概览

- **核心概念**：
  - **网格（Grid）**：由 `GridManager` 和 `Dot` 节点组成的规则点阵，是所有箭头路径的承载空间。
  - **箭头线（ArrowLine）**：一条由若干个网格点串联而成的折线，由 `ArrowLineEntity` 负责显示、点击与移动。
  - **关卡数据（ArrowLevelData）**：描述一个关卡中网格大小、有效格子集合和所有箭头路径的静态数据。
  - **玩法 UI**：`ArrowMazeUIForm` 显示生命、箭头数量、缩放信息，并提供切关与缩放按钮。
  - **玩法场景管理**：`ArrowMazeManager` 统一持有 `GridManager` / `LevelManager` / `CameraController`。

- **基础流程**：
  1. 关卡编辑器在 Unity Editor 中编辑网格与箭头，生成 `ArrowLevelData` Json。
  2. 玩法场景通过 `ArrowLevelDataUtility.LoadFromLevelDataJson` 加载 Json，交给 `LevelManager.LoadLevel`。
  3. `GridManager.RebuildGrid` 重建网格与 `Dot` 点阵；`LevelManager` 基于关卡数据生成所有 `ArrowLineEntity`。
  4. `CameraController.SetupForLevel` 根据网格边界自动适配相机大小与拖拽范围。
  5. 玩家点击箭头，`ArrowLineEntity` 做点击检测、阻挡判定，并以连续动画推动箭头前进。

---

### 2. 关卡数据与编辑器

#### 2.1 关卡数据结构（`ArrowLevelData.cs`）

- **`GridDef`**：网格几何配置
  - `width` / `height`：网格宽高（格子数量）。
  - `spacing`：相邻网格点在世界坐标中的间距。
  - `origin`：网格原点在世界空间中的位置。

- **`ArrowLevelData`**：单个关卡的完整描述
  - `levelName`：关卡名称。
  - `grid`：一个 `GridDef` 实例。
  - `validGrids`：`HashSet<Vector2Int>`，哪些格子是「有效可用」的（只能在这些格子上摆放箭头）。
  - `arrows`：`List<ArrowLineDef>`，每条箭头线的定义。
  - `fillConfig`：`ArrowFillConfig`，用于自动填充 / 生成箭头时的全局参数（最小/最大长度、是否允许拐弯等）。
  - `cameraSize`：设计时参考用的相机大小（运行时实际大小由 `CameraController` 动态计算）。
  - `difficulty`：1–5 的关卡难度系数，仅作为策划侧描述与编辑器提示。
  - `createTime` / `modifyTime`：创建与最后修改时间。

- **`ArrowLineDef`**：一条箭头线的逻辑路径
  - `path`：`List<Vector2Int>`，以逻辑网格坐标表示的路径点序列（尾 → 头）。
  - `serializedPath`：`List<SerializableVector2Int>`，仅用于 Json 序列化。
  - `stepTime`：**走一格所需时间（秒）**，运行时用来推导箭头的连续移动速度。
  - `lineColor` / `hitColor`：箭头正常显示颜色与碰撞时的高亮颜色。
  - `occupyAllNodes`：是否占用整条路径上的全部节点（影响阻挡判定）。
  - `startIndex`：从路径中的哪个索引开始显示 / 生效，可用于只显示路径的一部分。

- **`ArrowFillConfig`**：自动填充策略的参数容器
  - `minLength` / `maxLength`：自动生成箭头时的长度范围。
  - `allowTurn`：是否允许路径出现拐弯。
  - `autoFill`：是否启用自动填充模式。

#### 2.2 关卡编辑器（`ArrowLevelEditor.cs`）

> 关卡编辑器仅在 **Unity Editor** 下工作，不参与运行时逻辑。

- **功能概览**：
  - 在编辑器窗口中绘制网格，使用框选方式批量设置「有效格子」。
  - 在网格上以鼠标绘制箭头路径，支持编辑、删除已有箭头。
  - 设置关卡名称、难度、网格大小、格距、原点位置等参数。
  - 通过 `ArrowLevelDataUtility.SaveToJson / LoadFromJson` 读写 Json 文件。
  - 支持自动填充算法，根据 `fillConfig` 批量生成箭头（复杂逻辑集中在编辑器内部实现）。

- **数据流向**：
  1. 编辑器内部维护一个 `ArrowLevelData` 实例作为当前编辑状态。
  2. 每次修改网格、箭头或难度等，直接更新这个实例。
  3. 保存时，调用 `ArrowLevelDataUtility.SaveToJson(levelData, filePath)` 写入工程内的 Json。
  4. 玩法场景只需要通过 `levelName` 取回该 Json，并恢复为相同的网格与箭头布局。

---

### 3. 玩法场景与模块协作

#### 3.1 总管理器（`ArrowMazeManager`）

- 作为场景内的单例入口，负责保证三大核心模块存在：
  - `GridManager`：负责网格与 Dot 节点。
  - `LevelManager`：负责关卡加载、箭头生成与胜负判定。
  - `CameraController`：负责玩法相机的视野、缩放与拖拽。

- **初始化流程**：
  1. 若场景中不存在 `GridRoot`，创建之并挂上 `GridManager`。
  2. 若场景中不存在 `LevelManager` GameObject，创建并挂上 `LevelManager`。
  3. 若没有主相机，则创建 `GameCamera`，设为正交相机并挂上 `CameraController`。
  4. 调用 `CameraController.SyncWithUICamera()` 完成与 UI 相机的渲染堆叠配置。

- 对外暴露：
  - `GridManager GridManager { get; }`
  - `LevelManager LevelManager { get; }`
  - `CameraController CameraController { get; }`

#### 3.2 关卡加载与游戏状态（`LevelManager`）

- **字段与状态**：
  - `m_CurrentLevelData`：当前关卡数据。
  - `m_ArrowLineEntityIds`：当前场景中所有箭头实体的 EntityId 列表。
  - `m_Lives`：剩余生命数，初始为 3。
  - `m_IsGameOver` / `m_IsGameWon`：游戏结束与胜利标记。

- **加载关卡（`LoadLevel(ArrowLevelData levelData)`）**：
  1. 校验 `levelData` 非空，记录到 `m_CurrentLevelData`。
  2. 重置生命和胜负状态。
  3. `ClearAllArrows()` 回收旧箭头实体。
  4. 调用 `ArrowMazeManager.Instance.GridManager.RebuildGrid(levelData)` 重建网格与 Dot。
  5. `SpawnAllArrows()` 基于 `levelData.arrows` 生成所有 `ArrowLineEntity`。
  6. 利用 `GridManager.GetGridBounds()` 计算网格世界边界，并调用 `CameraController.SetupForLevel(bounds)` 完成相机适配。

- **箭头生成（`SpawnArrowLine`）**：
  - 将 `ArrowLineDef.path` 中的每个网格坐标转换为 `Dot` 引用。
  - 使用 `ArrowLineEntityParams.Create` 构造参数，设置：
    - `ArrowDef` / `DotPath`。
    - 回调：`OnArrowExited` / `OnArrowHit`。
    - 初始位置：首个 Dot 的世界坐标。
  - 调用 `GF.Entity.ShowEntity<ArrowLineEntity>` 实例化箭头实体，并记录 EntityId。

- **胜负逻辑**：
  - `OnArrowExited(ArrowLineEntity arrow)`：
    - 箭头完全离开屏幕后回调。
    - 将其 EntityId 从 `m_ArrowLineEntityIds` 移除。
    - 当列表变为空，且当前未 GameOver，则标记胜利。
  - `OnArrowHit(ArrowLineEntity arrow)`：
    - 某次点击导致箭头被前方其他箭头阻挡时回调。
    - 生命数 `m_Lives--`，若降到 0 且未 GameOver，则标记为失败。

---

### 4. 网格与坐标体系（`GridManager` / `Dot`）

#### 4.1 坐标约定

- **逻辑网格坐标（`Vector2Int`）**：
  - `x`：从左到右递增。
  - `y`：编辑器与数据层按「向下递增」（便于与贴图 / 关卡编辑 UI 对齐）。

- **世界坐标（`Vector3`）**：
  - 采用 Unity 标准坐标系：`Y` 向上。

- **关键转换**：
  - `GridToWorld(Vector2Int gridPos)`：
    - 以网格中心为基准，将 `(x, y)` 转换为相对偏移，并对 `y` 做一次翻转，保证「数据上向下」等价于「画面中向下」。
  - `WorldToGrid(Vector3 worldPos)`：
    - 当前主要用于设计与调试，按 `spacing` 做 RoundToInt 还原到最近的网格坐标。

#### 4.2 网格重建与 Dot 节点

- `RebuildGrid(ArrowLevelData levelData)`：
  1. 赋值 `m_GridDef = levelData.grid`。
  2. 调用 `Cleanup()` 清空旧 `DotsRoot` 与所有 `Dot` 组件。
  3. 若未存在 `m_DotsRoot`，创建名为 `Dots_{levelName}` 的根节点并挂在 `GridManager` 下。
  4. 初始化 `m_Dots` 数组与 `m_DotByGrid` 字典。
  5. 遍历 `levelData.validGrids`，对每个合法坐标调用 `CreateDot(gridPos)`。

- `CreateDot`：
  - 用 `GridToWorld` 求取世界坐标。
  - 若配置了预制体 `m_DotPrefab`，则实例化并命名为 `"Dot_x_y"`。
  - 获取或添加 `Dot` 组件，调用 `dot.Initialize(gridPos, this)`。
  - 将 `Dot` 存入二维数组与查找字典中。

- `Dot` 组件：
  - `G`：记录自身网格坐标。
  - `Occupant`：当前占用该点的箭头线实体。
  - `IsFree`：是否为空闲点。

---

### 5. 箭头实体与点击 / 移动逻辑（`ArrowLineEntity`）

#### 5.1 初始化与视觉

- **初始化入口**：实体系统在 `OnShow(userData)` 中接收 `ArrowLineEntityParams`，从中取出：
  - `ArrowDef`（`ArrowLineDef`）
  - `DotPath`（`List<Dot>`）
  - 回调：`OnExited` / `OnHit`
  - 初始世界位置（可选）

- **`Initialize(ArrowLineDef arrowDef, List<Dot> dotPath, ...)`**：
  1. 将 `dotPath` 拷贝至 `m_Nodes`（顺序：尾 → 头），记录 `m_ArrowDef`、`startIndex`、颜色等配置。
  2. 调 `InitializeVisuals()`：
     - 准备 `LineRenderer`（`useWorldSpace = true`，设置宽度、颜色、排序层等）。
     - 准备 `EdgeCollider2D`（`isTrigger = true`，用于点击检测）。
     - 自动查找 `Head` 子节点，配置 `SpriteRenderer` 与初始旋转偏移。
  3. 获取玩法相机引用（优先 `Camera.main`）。
  4. 根据 `LineRenderer` 宽度计算点击检测半径 `m_ClickDetectionRadius`。
  5. 调用 `ClaimNodes()` 将路径上从 `startIndex` 到末尾的 `Dot.Occupant` 指向自己。
  6. 调 `InitializeContinuousMovement()` 构建连续移动使用的数据结构：
     - `m_GridSpacing`：来自 `GridManager.GridDef.spacing`，无则默认 1。
     - 根据 **关卡中配置的 `stepTime`** 计算移动速度：
       - `stepTime = (m_ArrowDef.stepTime > 0) ? m_ArrowDef.stepTime : 0.05f;`
       - `m_MoveSpeed = m_GridSpacing / stepTime;`
     - 将每个 `Dot` 的世界坐标填入 `m_PathPoints`，并复制到 `m_CurrentPositions` 中。
  7. 调 `SyncVisualImmediate()` → `UpdateVisuals()`，同步初始 `LineRenderer` 和 `EdgeCollider2D`。

> **简化说明**：早期版本中存在基于「格子跳动」的实现和多套点击检测方式，目前已统一为：
> - 连续插值推进（头部平滑前进 + 尾部缩短）；
> - 单一入口的 `HandleClick`，统一次使用世界坐标做点击判定。

#### 5.2 点击检测（`HandleClick`）

- **输入来源**：在 `Update()` 中轮询：
  - 鼠标左键按下：`HandleClick(Input.mousePosition)`。
  - 触摸开始：`HandleClick(Input.GetTouch(0).position)`。

- **处理流程**：
  1. 若相机或碰撞体缺失，直接返回。
  2. 将屏幕坐标转换为世界坐标 `worldPos`（对 Z 轴对齐到箭头所在平面）。
  3. **方法1：EdgeCollider2D.OverlapPoint（主方案）**
     - 直接使用 `m_EdgeCollider.OverlapPoint(worldPos)` 判定是否点击到箭头。
  4. **方法2：点到线段的最小距离（备用）**
     - 遍历 `LineRenderer` 中的世界坐标点 `(p1, p2)`，计算 `worldPos` 到线段的距离最小值。
     - 若最小距离小于 `m_ClickDetectionRadius`，也视为点中箭头。
  5. 一旦任一方法判定命中，则调用 `TryAdvance()` 推动箭头前进。

> **冗余清理**：旧版本中存在基于 `OnMouseDown` + `Physics2D` Raycast 的备用点击路径，以及对外的 `CheckClick` 接口。  
> 这些接口已从 `ArrowLineEntity` 中移除，避免多入口导致行为不一致，只保留 `HandleClick` 这一套逻辑。

#### 5.3 点击后的阻挡判断与移动启动（`TryAdvance`）

- **核心职责**：
  - 对一次点击进行「是否允许前进」与「是否被阻挡」的判定。
  - 在允许前进时启动连续移动，在被阻挡时触发碰撞反馈。

- **流程**：
  1. 若 `m_IsMoving` 为真，说明箭头已在运动中，忽略本次点击。
  2. 记录调试日志：
     - 触发点击的箭头名（`gameObject.name`）。
     - 通过 `m_Nodes` 的最后两个 `Dot.G` 计算前进方向，将 `(1,0)/(-1,0)/(0,1)/(0,-1)` 映射为「右 / 左 / 下 / 上」。
  3. 调用 `IsBlockedInForwardDirection(out Dot blockingDot)`：
     - 若返回 `true`，则 `blockingDot` 为第一块阻挡当前箭头的 `Dot`。
     - 若返回 `false`，`blockingDot` 为 `null`。
  4. 记录「是否允许运动」「是否被阻挡」以及「阻挡的点位名字」等日志信息。
  5. 若被阻挡：
     - 调 `m_OnHitCallback?.Invoke(this)`（交由 `LevelManager` 扣减生命与判定失败）。
     - 启动 `FlashHitColor()` 协程，让箭头短暂变为碰撞颜色后恢复。
  6. 若未被阻挡：
     - 调 `StartContinuousMovement()` 开启连续移动。

#### 5.4 连续移动（`UpdateContinuousMovement`）

- **基本思想**：`m_CurrentPositions` 维护了一条从尾到头的折线，每帧：
  - 头部沿最后一段方向前进一小段距离；
  - 尾部按总长度相同的距离向前「吃掉」自身；
  - 这样在视觉上就像整条箭头沿路径被平滑地向前「推移」。

- **具体步骤**：
  1. 若只剩一个点，沿着上一帧记录的方向继续前进，直到整条线完全离开屏幕（调用 `OnExited` 回调并隐藏实体）。
  2. 若至少有两个点：
     - 计算 `headDir`（最后两点连线方向），让头点向 `headDir` 前进 `moveDist = m_MoveSpeed * Time.deltaTime`。
     - 从尾部开始按段缩短：依次消耗各段长度，直到总共「吃掉」`moveDist` 的长度。
     - 合并过短段（距离小于阈值）。
  3. 每帧调用 `UpdateVisuals()` 同步 `LineRenderer` 与 `EdgeCollider2D` 的形状，以及 `Head` 的位置与朝向。
  4. 若整条线完全离开屏幕视口，调用 `OnExited` 回调，并通过实体系统隐藏自身。

---

### 6. 相机逻辑（`CameraController`）

#### 6.1 与 UI 相机堆叠

- `SyncWithUICamera()`：
  - 将玩法相机设置为 `MainCamera`，渲染类型为 `Base`，使用正交投影。
  - 将 UI 相机（来自 `GFBuiltin.RootCanvas.worldCamera`）设置为 `Overlay`，并添加到玩法相机的 `cameraStack` 中。
  - 保证所有 UI 在箭头迷宫玩法画面之上正确渲染。

#### 6.2 根据网格自动适配视野

- `SetupForLevel(Bounds gridBounds)`：
  - 读取网格边界（由 `GridManager.GetGridBounds` 提供），计算网格宽高与留白后的「有效显示区域」尺寸。
  - 根据屏幕宽高比与设计分辨率（1080×1920）计算基础 `orthographicSize`。
  - 设置：
    - `m_MaxSize`：刚好包含网格（含留白）的 size。
    - `m_MinSize`：允许的最大放大倍数（通常为 `baseSize` 的一部分）。
    - `m_TargetZoom` 与 `Camera.orthographicSize` 的初始值。
  - 调 `CalculatePanLimits()` 根据当前缩放与网格边界得出相机中心的可移动范围。
  - 将相机对准网格中心。

#### 6.3 缩放与拖拽

- 捏合缩放（移动端）：`HandlePinchZoom`。
- 鼠标滚轮缩放（PC）：`HandleMouseScrollZoom`，通过 `m_ScrollWheelStep` 控制每档缩放幅度。
- 拖拽平移：`HandlePanInput`，通过 `GetWorldPosition` 将屏幕拖动转换为世界坐标的位移，限制在 `m_CameraCenterMin` / `Max` 范围内。
- 平滑缩放：`ApplySmoothZoom`，将 `orthographicSize` 向 `m_TargetZoom` 过渡，并在缩放过程中更新平移限制。
- 提供给 UI 的接口：
  - `AddZoom()` / `SubZoom()`：按钮放大 / 缩小。
  - `GetZoomProportion()` / `GetZoomPercent()`：用于进度条和文本显示。

---

### 7. 玩法 UI（`ArrowMazeUIForm`）

- **打开时**：
  - 从 `PlayerDataModel` 读取当前关卡 id。
  - 通过 `ArrowMazeManager.Instance.LevelManager` 获取 `LevelManager`。
  - 隐藏胜利 / 失败面板。
  - 调用 `LoadTestLevel(levelIndex)` 异步加载并显示关卡。

- **按钮交互**：
  - 「加载关卡」：重新加载当前 `m_LevelIndex`。
  - 「关卡 + / -」：在指定区间内调整 `m_LevelIndex`，并重新加载。
  - 「放大 / 缩小」：调用 `CameraController.AddZoom` / `SubZoom`。

- **UI 更新**：
  - 显示生命：`Lives`。
  - 显示剩余箭头数：`ArrowCount`。
  - 显示当前缩放比例和百分比。
  - 根据 `IsGameWon` / `IsGameOver` 显示胜利或失败面板。

---

### 8. 冗余逻辑与接口精简说明

基于当前已实现的玩法功能，本次整理对代码做了以下简化和统一：

1. **点击检测路径统一**（`ArrowLineEntity`）：
   - 保留 `Update()` → `HandleClick()` → `TryAdvance()` 的主流程。
   - 移除旧版的 `OnMouseDown` 与 `CheckClick` 备用接口，避免多入口导致行为不一致和误触问题。

2. **移动速度计算统一**（`ArrowLineEntity`）：
   - 移除冗余的 `m_StepTime` 字段与相关注释。
   - 统一通过关卡数据中的 `ArrowLineDef.stepTime` 与网格间距 `GridDef.spacing` 计算 `m_MoveSpeed`，保证编辑器配置能够真实反映到运行时速度。

3. **坐标系使用约定收敛**：
   - 点击检测、LineRenderer 和 EdgeCollider 全部在 **世界坐标** 空间下完成，避免本地 / 世界坐标混用导致的误判与误触。
   - 日志中的上下左右方向说明明确基于「画面视觉」而非纯网格 Y 轴，避免理解偏差。

后续若有新玩法（例如箭头反弹、传送门、多目标网格等），建议：

- 在此文档中先扩展相应章节（数据结构 / 箭头行为 / 相机需求），
- 再在代码中按模块（数据、网格、箭头实体、相机、UI）分层实现，避免在实体类中直接堆叠过多场景外逻辑。

