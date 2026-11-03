# 远程配表 API 读取

服务根地址：`AppSettings.RemoteTableBaseUrl`，当前为 `https://x.gamedevs.pro`。
`EnableRemoteTableSync` 当前保持关闭；开启后下次启动执行远程同步。关闭时不下载、不读取远程缓存，使用项目资源。

## 请求流程

1. POST `/api/public/config-file-groups/resolve`，JSON 包含 `package_name`（Application.identifier）、`version`（Application.version）、`platform`（android/ios）、`config_id`（首次 null，后续使用本地领取 ID）。编辑器按当前构建目标选平台。
2. 成功响应体直接是 manifest，从响应头 `X-Config-Id` 取得正整数 ID。没有 code/data 包装。响应有效后保存 ID，后续下载失败也不重新抽签。
3. 以 manifest 每个文件的 `path` 匹配 AppConfigs 中注册的表，不依赖 files 的字典键；例如 `datatable/Core/ArrowUITable.json` 映射为 `Core/ArrowUITable`。
4. GET `/api/public/config-folders/{ID}/{path}`。比较 MD5、大小和路径决定更新，不依赖固定为 1 的版本字段。缓存不完整时重新下载。
5. 先校验下载原始字节的 size/MD5（包含 BOM），再去 BOM、按原有逻辑解密和转换 bytes，并用对应行解析器检查数据。解析验证不写入游戏中的表。
6. 下载、校验与解析全部通过后，原子替换有效批次指针，再进入原有启动加载流程。

## 缓存与失败行为

- 新目录为 `persistentDataPath/RemoteTablesApi/{scope-hash}/`，scope 包含服务根地址、包名、平台、应用版本。旧 OSS RemoteTables 缓存不迁移。
- `assigned-id.txt`：已领取配置 ID；`active.json`：生效配置 ID、批次、清单、bytes 校验信息；`batches/{随机批次ID}/`：该配置批次的 bytes。
- 同步失败、404/422/503、超时、错误响应头、MD5/解析失败均不切换生效批次。保留领取 ID，服务端决定何时重新分配。
- 读取批次时检查所有文件完整性，任一文件缺失或损坏则整批不可用，回退项目资源。有效数据保留在内存，避免加载中途外部文件变更导致混用。
- 新清单不再包含的表不继承旧缓存，回退对应项目资源。未注册的远程文件不下载。
- 更新使用新批次目录，提交时以同目录 File.Replace/Move 更新指针，不先删除旧指针。若平台文件系统不支持替换则本次更新失败、保留旧批次。成功后清理旧批次，失败则清理当前临时批次。
- 本功能在启动阶段同步，不在游戏进行中重载。原有本地 A/B 资源选名逻辑保持原样；服务端配置 ID 分配不会改写本地 A/B 设置。

## 验证

运行 `Tools/RemoteTables/Tests/Run-RemoteTableTests.ps1`（可传 `-UnityEditorData`）。测试使用真实同步、管理、转换和存储代码；用模拟 Unity 网络层与运行环境执行，不访问线上服务。测试加密层为替身，真实加密逻辑未改动。

覆盖首次/后续领取、配置重分配、部分失败、整批提交、固定版本但 MD5 变化、缓存损坏与重新下载、删除表、关闭开关、错误状态码、缺失响应头、无效清单、超时、BOM、路径与重复行验证、版本和环境隔离。测试脚本同时编译 Hotfix 程序集。

仍需在目标 Android/iOS 设备启用开关进行真实接口联调，确认后台已为实际包名、版本和平台配置文件，并验证移动端持久化文件系统。
