# 远程配表拉取逻辑分析与改造基线

记录日期：2026-10-22。

本文记录当前源码的实际行为，供后续修改方案时参考。仅做静态代码分析，未请求远程服务，未修改运行时代码。配置及表名单是记录时的快照，后续以实际项目文件为准。

## 1. 当前配置

配置文件：`Assets/Resources/AppSettings.asset`。

| 配置 | 当前值 |
| --- | --- |
| EnableRemoteTableSync | 0，关闭远程同步 |
| RemoteTableBaseUrl | https://xpxy.oss-us-west-1.aliyuncs.com/arrowMeadow/android/ |
| RemoteTableSyncTimeoutSeconds | 20 秒 |
| EnableRemoteTableEncryption | 1 |

`ProjectSettings/ProjectSettings.asset` 中的 bundleVersion 为 `1.0.0`。运行时使用 `Application.version`；构建流程可能修改版本，因此最终包以构建结果为准。

注意：关闭同步仅阻止网络拉取，不阻止加载之前已下载的缓存。

## 2. 启动调用链

```text
GameStartPreloadProcedure.OnEnter
  → PreloadAndInitData
    → AppConfigs.GetInstanceSync
    → RemoteTableManager.EnsureInitialized：注册同步白名单
    → RemoteTableManager.IsEnabled：检查开关及 URL
    → CreateGFExtension
  → OnLoadGFExtensionSuccess
    → 开启同步时 await RemoteTableManager.SyncAsync
      → RemoteTableSyncService.SyncAsync
    → LoadConfigsAndDataTables
      → ConfigExtension.LoadConfig / DataTableExtension.LoadDataTable
      → RemoteTableLoader 优先读取本地远程缓存
      → 缓存读取或解析失败时回退资源加载
  → 完成其他初始化，进入游戏
```

远程同步发生在配置表正式加载之前，因此本次成功下载的配置会在本次启动生效。当前找到的同步调用点位于启动预加载流程，没有定时轮询。

## 3. 同步范围与地址

白名单来自 `Assets/ArrowGame/ScriptableAssets/Core/AppConfigs.asset` 的 Configs 和 DataTables。名称按 Ordinal 精确匹配，包含 `Core/` 等目录前缀。

当前配置表：`GameConfig`。

当前数据表：

- ArrowGuideTable
- ArrowLocalizationMoney
- LevelCompleteReward
- MakeupTask
- TaskReward
- Core/ArrowLanguagesTable
- Core/ArrowSoundGroupTable
- Core/ArrowUIGroupTable
- Core/ArrowUITable

远程清单中不在白名单内的表会被忽略。`AppConfigs.Languages` 中的语言资源不参与这套同步，`Core/ArrowLanguagesTable` 本身参与。

```text
清单 URL = BaseUrl / Application.version / manifest.json
单表 URL = BaseUrl / Application.version / manifestEntry.path
```

若开启同步且应用版本仍是 1.0.0，清单地址为：

```text
https://xpxy.oss-us-west-1.aliyuncs.com/arrowMeadow/android/1.0.0/manifest.json
```

不会自动寻找服务器上的最新应用版本目录。

## 4. 清单比较规则

清单顶层包含 version 和 files。files 按表名索引，各表记录 version、md5、size、path。

每次同步先下载远程 manifest，再读取本地 manifest。满足以下任一条件则加入更新列表：

1. 本地清单没有该表记录。
2. 远程 md5 非空，且与本地 md5 不同（忽略大小写）。
3. 远程表 version 非空，且与本地 version 不同（精确字符串比较）。

补充行为：

- 版本只比较是否相等，不判断新旧，远端回退版本也会触发下载。
- 顶层 version 不直接决定是否更新。
- size 不参与更新判断或下载完整性校验。
- 不检查对应 bytes 文件是否实际存在或损坏。

## 5. 下载、校验、转换与落盘

表按更新列表串行下载，不是并发下载。

```text
UnityWebRequest.Get 下载 JSON
  → UTF-8 解码并处理 BOM
  → 对处理后的 JSON 文本计算 MD5
  → 若远程 md5 非空则校验，不匹配时跳过该表
  → JsonTableConverter 提取内容
    → 遇到加密包装时使用 DES 解密
    → Config：键值 JSON 转二进制，或还原 bytesBase64
    → DataTable：还原 Base64 二进制内容
  → 保存 .bytes 缓存
  → 将成功表的记录合并到本地清单
  → 整轮结束时保存合并后的清单
```

DataTable 并不是将任意 JSON 行数据动态映射成表，主要传输的是已生成二进制数据的 Base64 包装。是否解密根据收到的 JSON 包装判断；不要将加密开关理解为客户端拒绝明文的校验开关。

本地缓存按应用版本隔离：

```text
Application.persistentDataPath/RemoteTables/{Application.version}/
├─ manifest.json
├─ config/GameConfig.bytes
└─ datatable/...
```

单文件写入采用先写 `.tmp`、再删除旧文件、最后移动临时文件的方式。虽然方法名包含 Atomic，但删除旧文件与移动新文件之间仍有空档，不是整批事务提交。

## 6. 超时、失败与进度

- 当前 20 秒是整轮同步共享的取消预算，清单及所有表下载共用。
- 单个 UnityWebRequest 还设置了 `Max(配置超时, 10)` 秒请求超时。
- 网络等待阶段检查取消并 Abort；转换和磁盘写入是同步操作，因此不是所有工作都受严格的硬截止时间约束。
- 单张表下载、校验或转换失败，记录 Warning 后继续下一张。
- 整轮取消时停止后续下载，已经成功落盘的表保留。
- finally 中在有成功更新时保存合并清单；失败表保留旧清单记录。
- 没有自动重试，通常等下次启动再次判断。
- 清单占同步进度的 10%，表占 90%；finally 会把同步进度报为 100%，不代表全部表同步成功。
- 清单保存位于 finally，若保存本身抛出异常，仍可能向调用方传播；不能认为所有磁盘异常都被完全兜底。

## 7. 加载优先级

ConfigExtension 和 DataTableExtension 先处理已有 A/B 资源选择，再尝试 RemoteTableLoader。

```text
选定表名在白名单内，且缓存存在、可解析
  → 使用缓存并发送框架加载成功事件
否则
  → 走原有 ReadData 资源加载路径
```

实际优先级：本次更新后的缓存或原有缓存 → 包内/资源系统中的表。

Loader 不检查 EnableRemoteTableSync，只检查表白名单和缓存。A/B 选择后的表名也必须匹配白名单，才会使用对应远程缓存。

## 8. 发布侧关联

编辑器菜单：`Game Framework/GameTools/Export Remote Table Json`。

导出器输出到：`RemoteTableExport/{Application.version}/`，并生成 manifest.json。

各表 version 使用本次导出的 UTC 时间戳。因此即使表内容 MD5 未变化，重新导出导致 version 变化，也会触发重新下载。仓库中已有历史导出目录不等于线上实际部署状态。

## 9. 后续改造需要明确的决策

以下为待讨论项，尚未确定方案或实施：

| 现状/问题 | 改造时需要决定 |
| --- | --- |
| 关闭同步仍使用旧缓存 | 是否拆分“允许网络更新”和“允许缓存覆盖”，是否支持强制使用包内配置 |
| manifest 相同就跳过，不检查 bytes | 是否增加缓存存在性、完整性检查与自动补下 |
| 部分表更新成功即生效 | 是否要求整批校验、原子切换版本、失败整体回滚 |
| 远端删除表不清理本地缓存 | 如何表达删除/停用，是否清理旧表 |
| 导出时间戳改变会触发全量下载 | 是否以内容哈希为主，或保留未变化表的版本 |
| 远端版本只比较字符串是否相同 | 是否允许回滚，如何定义版本单调性及发布兼容范围 |
| 所有表串行、共用启动等待预算 | 是否并发、分级加载、重试，是否保留启动前同步 |
| 进度完成不代表同步成功 | 是否返回结构化结果并提供日志/监控统计 |
| 数据可转换不代表框架解析一定成功 | 是否在提交缓存前做解析验证，避免坏缓存覆盖旧可用表 |
| 应用版本隔离缓存和远程目录 | 是否维持严格隔离，如何处理包升级后的配置迁移 |

## 10. 关键代码索引

以下路径均相对项目根目录，行号以分析时源码为参考，修改后可能变化。

| 文件 | 职责/入口 |
| --- | --- |
| Assets/ArrowGame/Scripts/Procedures/GameStartPreloadProcedure.cs | 170 行附近初始化白名单；234 行附近框架扩展成功后 await 同步 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableManager.cs | 白名单、开关、版本目录、同步入口、MD5 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableSyncService.cs | 18 行同步主流程；152 行更新比较；227 行附近网络下载 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableManifest.cs | 清单数据结构 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableLocalStore.cs | 缓存路径、文件读写、清单持久化 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableLoader.cs | 缓存解析与加载成功事件 |
| Assets/ArrowGame/Scripts/RemoteTable/JsonTableConverter.cs | JSON/加密包装转换成二进制 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableCrypto.cs | DES 加解密 |
| Assets/ArrowGame/Scripts/RemoteTable/RemoteTableTextUtility.cs | UTF-8 与 BOM 处理 |
| Assets/ArrowGame/Scripts/Extension/ConfigExtension.cs | 配置加载及回退 |
| Assets/ArrowGame/Scripts/Extension/DataTableExtension.cs | 数据表加载及回退 |
| Assets/ArrowGame/ScriptsBuiltin/Editor/RemoteTableJsonExporter.cs | 远程配表导出与版本生成 |
| Assets/Resources/AppSettings.asset | 同步开关、地址、超时等配置 |
| Assets/ArrowGame/ScriptableAssets/Core/AppConfigs.asset | 同步/加载表名单 |
| ProjectSettings/ProjectSettings.asset | 应用版本 |
