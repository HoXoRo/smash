# 物品 AB 拆分

本次只调整手工 `ResourceCollection.xml` 的物品及专属依赖归属。
UI、场景、音频、关卡数据和原有压缩/加密策略不变。运行时按需加载改造见下文。
`UseResourceRule` 当前为 0；不要直接启用旧 ResourceRuleEditor 规则覆盖本集合。

| 包 | 内容 | 显式资源数 |
| --- | --- | ---: |
| obj_box | 箱子各形状/尺寸及专属碎片、爆炸特效 | 158 |
| obj_can | 方罐、圆罐、锥形罐及专属特效 | 151 |
| obj_stone | 石头及专属特效 | 155 |
| obj_ice | 冰块及专属特效 | 197 |
| obj_jamjar | 各色果酱罐及专属特效 | 225 |
| obj_tnt | TNT 和通用 TNT 爆炸效果 | 14 |
| obj_wormhole | 虫洞和专属特效 | 20 |
| booster_rocket | 火箭及专属特效 | 3 |
| gameplay_core | Table、Blocker、CannonBall、通用命中/落地特效及专属依赖 | 42 |
| gameplay_shared | 多个物品家族共用、且未发现非物品消费者的依赖 | 49 |

数量包含显式分配的预制体、材质、贴图、模型等，不代表物体种类数或构建后体积。
逐项明细见 `item-bundle-assets.csv`。

## 归属原则

- 家族专属依赖随家族；多家族共同引用的依赖进入 gameplay_shared。
- Box_Tnt、Ice_Tnt 等被 TNT 击碎的专属效果归对应物体家族。
- 非物品预制体、场景、其他配置和 SpriteAtlas 的可追踪依赖受保护，保留原有归属。
- Shader 不迁移。资源路径和 GUID 不改变。
- 新包沿用 LoadType=3、Packed=True，未同时改变加载协议。

## 重复执行与验证

项目根目录运行 `python Tools/split_item_bundles.py` 预览；添加 `--apply` 才写入。
脚本根据当前资源及 meta 文件的序列化 GUID 引用计算归属，校验 GUID 唯一、
未涉及资源和 UI 预制体归属不变。重复执行应报告 Changed assignments: 0。
脚本不会清理旧包或修改 StreamingAssets。

静态引用分析不能替代 Unity AssetDatabase 和构建报告，特别是模型导入依赖、
图集实际打包结果和包间依赖环。下一次发布前必须重新构建目标平台 AB，核对新包
及依赖，再验证加载、材质、各家族破碎效果和火箭/TNT/虫洞效果。
现有 StreamingAssets 仍是旧构建产物，本次未宣称新 AB 已生成或真机验证通过。

## 按需加载流程（已实现）

1. GameController 先读取当前关 JSON，再加载 ObjectData / ParticleData 轻量索引。
2. 两份索引仅序列化预制体路径；运行时 prefab 字段不序列化，不再牵引全量物品。
3. 收集当前关全部阶段的类型、尺寸并去重，只加载匹配的预制体。根据现有
   ObjectParticleMaps、果酱罐破碎映射及 TNT/虫洞规则加载所需特效，保留通用命中和火箭特效。
4. Table/Blocker 按关卡需要加载。轻量索引、物理材质、通用物体/特效在会话中复用。
5. 每个资源路径只发起一次 UGF 加载；当前关和下一关通过引用计数共享资源。
   关卡销毁或准备失败时释放对应租约。取消不能中断 Unity IO，但迟到结果无人使用时会归还 UGF。
6. 关卡展示后等待至少 2 秒，且应用有焦点、无点击/拖动、无在场炮弹、不处于开场锁定时，
   顺序、逐帧预加载下一关资源。新进关取消背景任务；已完成的下一关缓存保留到前台取得资源引用。
   始终只保留至多一份完整下一关租约，不递归预加载后续关卡。
7. 关卡 JSON TextAsset 读出文本后归还 UGF；特效与碎片归属关卡层级，避免退出后遗留实例。

### 维护和发布

- 新增物品：在 ObjectData.items 中填写 type、size、prefabPath，在 ParticleData.items
  中填写 type、prefabPath，并将资源纳入 ResourceCollection。路径必须含 Assets/ 前缀及扩展名。
- 打 AB 前自动检查索引是否仍直接引用预制体、路径是否有效且已纳入资源集合，失败时停止打包。
- 本次修改了资源序列化结构，必须重新构建 AB 和匹配的 Hotfix 程序集；旧 StreamingAssets
  不能与新代码混用。UI 不需要重新设计或调整归属。
- 没有引入新的对象池；仍按原逻辑实例化关卡物体。UGF 自身对象池可能延迟回收资源，
  归还引用不等于立刻释放全部原生内存。

### 已执行验证

- `python Tools/test_gameplay_catalog.py`：5 项检查通过，覆盖全部 1000 关所有阶段、
  159 个物体变体、全部现有特效映射、路径存在性及 AB 注册。
- 使用 Unity 自带 Roslyn 和当前编辑器编译参数：Hotfix 与编辑器打包代码编译通过。
- Android 独立编译尝试受到旧 player 构建缓存缺失 AdjustSdk / MaxSdk 引用的限制，
  不代表完整 Android 构建已通过；本次未改 SDK 配置。
- 未运行真机性能对比，未生成新 AB/APK。

### 真机验收

查看日志 `[GameplayLoad]` 的 JSON、assets、instantiate 分段耗时，以及
`[GameplayAssets]` 的预制体/特效数量和缓存数。第 1 关预期 1 种物体变体，第 100 关预期 5 种。
对比首次进入、重复进入、连续下一关；验证多阶段、TNT/火箭炸毁、虫洞、果酱罐落地特效。
同时检查快速退出/重试、加载失败和切到后台后的恢复。具体耗时、内存峰值和包体重复率
仍以重建后的报告和真机测试为准。
