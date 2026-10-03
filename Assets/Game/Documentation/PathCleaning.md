# A002 山路打扫

已接入第二章 A002_Minigame，完成后沿原连接进入收入对白。实现使用独立 PathCleaningModule + 专属薄节点（script 级）：首次无限时、动态累计次数、多字段结果和会话锁需要由专属节点协调，玩法仍在模块中。

- 脚本 ID：`Game.PathCleaning.PathCleaningStep`
- 脚本资产：`Assets/GameResource/Authoring/Narrative/Scripts/PathCleaning/PathCleaningStep.asset`，已登记剧情自定义脚本清单。
- 请求键：`cmh.a002.path_cleaning`，JSON 参数 `Visit`。
- UI：`Assets/GameResource/Resources/UI/Module/PathCleaning/Prefabs/EUIPathCleaningItem.prefab`，经 EUI 中心创建、正式生成绑定。全部为 Image 色块，无生成图片。
- A001、A002 分别注册 `IOfferingSortService`、`IPathCleaningService`，均继承框架 INovelStepService，互不覆盖。

## 配表

修改源表后通过 Ember 配表中心烘焙。生成绑定、bytes 和步骤配置指纹自动同步，不手工编辑产物。

| 源表 | 可调内容 |
| --- | --- |
| `Assets/GameResource/TableSources/path_cleaning.etable.csv` | 次数区间、污渍数量、时限、类型池、教程开关/阈值、奖励时间分档与奖励 ID、落叶/泥渍单次扫动距离、泥渍扩散长度和不透明度 |
| `Assets/GameResource/TableSources/path_cleaning_types.etable.csv` | 类型 ID、类型池、匹配工具 ID、生成权重、颜色、尺寸、可选 Sprite 的 Resources 路径、内心话 Key |
| `Assets/GameResource/TableSources/path_cleaning_tools.etable.csv` | 工具 ID、名称 Key、颜色、可选 Sprite 的 Resources 路径 |
| `Assets/GameResource/TableSources/novel_rewards.etable.csv` | 奖励目标变量、数值、倍率与上下限；`cmh_clean_*` 为本小游戏奖励 |
| `Assets/GameResource/TableSources/novel_content_text.etable.csv` | `ui.pathCleaning.*` 界面和教程文案 |

`visitFrom/visitTo` 是小游戏自身累计次数区间，`visitTo=0` 表示后续全部次数。区间须从 1 连续覆盖且不重叠。`timeLimitSeconds=0` 表示不限时；数量支持 1 至 20，当前布局每局最多 6 种工具。污渍在随机、不重叠的网格位置生成，类型按权重抽取，不保证每轮包含全部类型。

默认首轮 6 处、无限时；第 2 次起 10 处、45 秒。扫帚清落叶，刷子清泥渍，夹子捡垃圾；这些对应关系全部在类型表，不靠代码判断类型名称。点击右侧工具后鼠标跟随同色工具 Image，再点击目标。未选工具或工具不匹配不清除，短暂显示反馈；点已清理目标无效。

首轮默认连续 3 次错误点击，或 12 秒没有清除进展，显示针对污渍的内心旁白。成功清除后重置教程计数和提示。开关和阈值均可配表。

成功用时 ≤15 秒奖励 30，≤30 秒奖励 20，其余成功奖励 10，超时保底 5；目标与数值均为奖励表默认值。当前先累计至 `ch2_aCleaningMonthlyBonus`，月结支付至玩家金钱并清零；原清扫基础月结收入保留。

## 泥渍扩散与后续换图（2026-10-02）

扫帚碰到泥渍时，按扫动方向铺开可见的泥痕，尾端柔和变淡。原图主体保持原位，扩散使用固定精度的画布，不再随着边界变大反复缩放、拉花。刷子可擦除主体和扩散区域。

每处泥渍、每片落叶在一次按下到松开之间都有独立的拖动上限。达到上限后继续按住或改变方向都不会继续移动/扩散；松开、重新按下才恢复预算。边界还有柔和衰减，避免反复扫动出现矩形硬边。

在 `path_cleaning.etable.csv` 的 `first` / `repeat` 行分别配置，修改后通过 Ember 配表中心烘焙并重新进入小游戏：

| 字段 | 当前值 | 含义与范围 |
| --- | --- | --- |
| `leafSweepDistance` | `0.25` | 落叶单次移动上限，以路面高度为单位，横向按宽高比换算；范围 `(0,1]`。原固定值为 `0.10`，当前为其 2.5 倍。 |
| `mudSweepDistance` | `0.22` | 泥渍单次扫动位移预算，单位同上；范围 `(0,1]`。拖过空白处不消耗该处泥渍的预算。 |
| `mudSpreadLength` | `1.0` | 相对污渍原图尺寸的最大扩散位移，范围 `(0,1.5]`；最终还受画布边界柔和限制。 |
| `mudSpreadOpacity` | `0.9` | 扩散泥痕的不透明度，范围 `(0,1]`；仍乘原图透明度和边缘衰减。越大越清晰。 |

这四个字段由正式生成绑定读取烘焙数据；原 `LeafSweepLimit` / `MudPushLimit` 固定常量已移除。非法、非有限或越界的配置会明确报错。

更换泥渍图片：将带透明背景的图片导入为 Sprite，放在 Resources 目录，在 `path_cleaning_types.etable.csv` 的 `mud` 行填写 `spritePath`（Resources 相对路径，不带扩展名），然后按既有流程烘焙。若直接替换同路径图片，只需等待 Unity 重新导入。`size` 控制原图显示尺寸；不需要另画扩散贴图、擦除遮罩，也不需要开启 Read/Write。

运行时通过 Sprite 实际网格和 UV 采样颜色、透明度及长宽比例，源图采样为 128×128，外侧预留扩散空间组成 256×256 画布。透明区域不计入初始清理量，扩散颜色来自被扫到的原图像素；图片原有颜色和半透明边缘被保留。无图片时使用配表颜色生成柔边泥渍。全透明图片会明确报错。

真实鼠标会把一次拖动拆成许多小段。扩散从本次按下时的污渍快照计算，累计整次拖动位移和接触压力，避免逐帧重复衰减导致“拖了却几乎没有变化”。松开重新按下后使用新的快照；单次拖动上限保留。

本次验证：42 项 PathCleaning EditMode 测试和 2 项正式 EUI Prefab 指针事件测试通过；覆盖配置驱动的移动距离/扩散长度/不透明度、非法配置拒绝、单次拖动上限、换方向不能绕过上限、重新按下恢复、50/150 段拖动与整段拖动的可见扩散量、横竖比例与透明孔洞、扩散后擦净，以及真实 A002 会话结算。UI 测试实际点击刷子/扫帚按钮，通过 GraphicRaycaster 命中路面后发送 100 段拖动，检查刷子擦除后的纹理透明度和扫帚产生的可见拖尾，并要求扩散区域具备足够数量的高不透明度像素。另用非 Read/Write 的横图、竖图、方图和非中心 Pivot 的子 Sprite 做了渲染检查，并在正式 EUI Prefab 的隔离预览中核对显示尺寸。新字段已通过 Ember Table 正式生成和烘焙，运行时回读与源表一致。

## 状态与中止

垃圾采用点击夹取：选中夹子后点击垃圾，垃圾图像停在工具光标前端跟随鼠标；再次点击垃圾桶才清理并计数。持有时不能同时夹第二件，切换工具或超时会放回未投放的垃圾，关闭 UI 会清理持有状态。图片沿用垃圾原始 Sprite；无需按住鼠标拖拽。提示区隐藏自带底框，沿用阅读页对话框底图。

限时结束后，小游戏保持显示“时间到了！”2 秒，停用操作并继续持有剧情输入锁；提示结束后才回传超时结果、发奖并进入下一节点。提示期间退出或取消不会继续剧情。超时提示文案使用 `novel_content_text` 的 `ui.minigame.TimeUp`。

全局 Int：`ch2_aCleaningVisits`、`ch2_aCleaningElapsedMs`、`ch2_aCleaningMonthlyBonus`；全局 String：`ch2_aCleaningOutcome`、`ch2_aCleaningRewardId`。次数跨月保留，结果为 success / timeout。

UI 准备完成后开始计时，由模块 IEmberUpdate 独立推进。时间到先按超时结算，不能通过最后一帧点击绕过时限。节点持有暂停和输入锁，保留阅读 UI，临时切换 UI 输入图，禁用阅读推进、自动、存档和菜单等操作；完成后释放。节点结束/取消调用 Abort，清理 UI、回调、原鼠标可见性/锁定状态和输入图。取消不发奖，重复或跨局迟到回调无效。资源/配置故障显式失败。

新节点、全局变量和三张表的配置指纹改变剧情语义，旧存档会由现有兼容检查拒绝；请从新档验收。小游戏进行中不允许保存，结束后稳定对白可保存。

## 验证记录

超时提示补充验证：两个小游戏及正式 UI 的 74 项 EditMode 测试通过，覆盖显示“时间到了！”、倒计时归零、提示期间不回传结果、停留结束仅回传一次，以及提示期间取消不推进剧情；另覆盖垃圾点击夹取、投放、切换工具和超时放回。

Unity MCP 编译检查无错误。238 项 EditMode 测试通过：两个小游戏玩法、真实 A001/A002 会话、服务共存、取消/迟到回调、自定义节点注册和 NovelSession 回归。真实剧情定义校验通过。

合并月结验证：原金钱 25、当月基础计算 63、清扫基础奖励 20、排序累计 55、打扫累计 35，最终 198；两项月累计清零，两项累计次数保留。隔离 uGUI 预览中使用真实工具按钮与污渍按钮成功清除一次，中文标签/教程和布局已渲染检查。

待手动验收：真实 Game 视图一起试玩两个小游戏的首轮、后续限时和超时；检查工具光标跟随、阅读 UI 按钮/快捷键禁用和恢复、月结结果。手动验收后再决定是否封存业务或发布，本批未封存或发布。

完整批次、文件归属、确认和验证信息：`NarrativeImports/20261001-a002-path-cleaning.json`。
