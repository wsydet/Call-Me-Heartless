# 第二章 E / G / H / I 与饮品图形选择

2026-10-02，批次 `20261002-eghi-01`。用户已通过剧情导入页与自定义节点页确认完整计划。源表 revision 3053，执行前重读 E/G/H/I 内容无变化。

## 已完成

- 导入 E001–E002、G001–G008、H001–H004、I001，共 205 行对白/旁白，44 个新增节点。另加余额不足的系统提示与取消/回观操作，不作为源文计数。
- 逐行回读 254 条新命令、24 条新节点路线、242 个新文本 Key；源文、说话人、显式换行和原始文本对应一致。其它语言列保留。源行与节点/命令/文本 ID、GUID 的完整对应见 `NarrativeImports/20261002-eghi-01.json`。
- E001 完成才写 `ch2_hasOutingWithYan`，G003 完成才写 `ch2_metMurong`，分别接通现有 B004/B003。A/B/C 的 120 个资产及 meta 与执行前备份一致。共用奖励步骤按新奖励表同步指纹。
- E 按首次外出、再次市集推进，之后重复市集；G 依编号选择符合前置条件的未播放事件，没有时用 G008；H 按前四个事件推进，之后重复 H003。以上为本批明确确认的暂行规则。
- H001/2/3 各扣 10；H004 未写费用，按确认方案不另收费。H002 矿泉水 5、柠檬可乐 10、顶级龙井 100。I 猫薄荷 200、师父好感 +20；G006 选项 1 慕容盈袖 +15，沿用 0–100 的奖励上限。
- 镇上完成 H 或购买 I 后返回选择，各最多一次；取消购买或入场钱不足不占未完成活动次数，选“回观”结束当天。下次月结算重置活动标记。

## H 图形选择

购物按节点进入次数选择 `shop_settings` 和 `shop_products` 的五档配置。首次不限时，其后选择时限逐步缩短；每档价格可独立调整，目前保留 5/10/100 原价。成交价写入章节 `h_drinkPrice`，剧情按该变量扣款。详见 [小游戏数值配表](MiniGameTables.md)。

2026-10-03：移除全屏遮罩与独立商店面板底色，将商品选择区域对齐 A 支线小游戏的阅读页上部，提示移到阅读页下方；经 UI 中心重新生成绑定，并渲染核对。购买、取消、余额与剧情结算逻辑保持原有实现。

正式 Prefab：`Assets/GameResource/Resources/UI/Module/ShopSelection/Prefabs/EUIShopSelectionItem.prefab`。

三个商品都是 Unity `Image` 的纯色占位，有名称、价格、余额、鼠标悬浮/选择高亮，以及“不购买”按钮。没有生成或下载任何商品图片。可后续替换 `ShopPanel/Product0..2` 的 Sprite 并按美术需要调整 Image 颜色。

已通过 EUI 中心 `TryBuildPlan → Create` 创建，再由 `EUIBindingCodeGenUtility.TryRegenerateCode` 生成实际绑定。用户逻辑按生成字段编写；没有手拼 Prefab YAML 或 Binding。宿主在小说阅读页挂载 Item，关闭/取消时释放 Item、恢复输入映射和鼠标状态。

选择流程采用确认的第三级专属薄脚本。通用 `INovelStepService` 已由护菜模块占用，且现成桥接只带固定参数；独立服务可传当前余额并保持两个模块互不覆盖。

- 业务模块：`Assets/Game/Module/ShopSelection/`，注册 `IShopSelectionService`。
- 脚本：`Narrative/ShopSelectionStepSO.cs`；固定 ScriptId `Game.ShopSelection.DrinkSelection`。
- 脚本资产：`Assets/GameResource/Authoring/Narrative/Scripts/ShopSelection/HDrinkSelection.asset`，已加入 Story 自定义步骤清单。
- 请求键：`cmh.h002.drink_selection`；H 第 41 行后进入 `H002_Shop`。结果为 `water / cola / tea / cancel`，写 Chapter String `h_drinkChoice`。
- 模块只负责选择，剧情原生分支再次检查余额并扣款，写 Global String `ch2_lastDrink`。购买成功才播放第 43 行感谢；取消不扣款、不播放感谢。
- 有效等待上限 300 秒，超时按取消处理；小说暂停时冻结等待并禁用按钮。计时由 Module Update 推进。
- Abort、退出、读档、故障清理请求及 UI；执行 ID 和完成门屏蔽迟到或重复回调。异常不会冒充购买成功。

## 新增状态

Global Bool：`ch2_metYan`、`ch2_askedYan`、`ch2_hasVisitedTown`、`ch2_catnipBought`、`ch2_g001Played` 至 `ch2_g007Played`。Global String：`ch2_lastDrink`。

Chapter Bool：`town_hDone`、`town_iDone`。Chapter String：`h_drinkChoice`。

现有 `ch2_eVisits`、`ch2_hVisits` 改为事件完整结束时增加；`player_money` 使用现有全局整数。奖励目标使用项目实际变量 `player_affection_lin`、`player_affection_murong`。前置与次数未通过默认值伪造完成。

## 验证

- Unity MCP 编译检查通过；自定义步骤注册、会话与购物专项共 233 项测试通过。
- 实际烘焙表经 `Resources.Load` 加载，完整 Story `TryReadDefinition` 通过，无错误。
- 实际资产回读：44 节点、205 源文、254 命令、24 路线、242 新 Key；旧文本行及其他语言列无改动。
- 真实 `NarrativeRunner` 跑通 17 条 E/G/H/I 路径，覆盖三饮品、取消、入场钱不足、H004 免费、H003 循环、猫薄荷边界、G005 两分支和 G006 奖励。此层对自定义展示结果进行注入，奖励计算使用真实配表；不冒充完整人工游玩。
- 另验证 B003/B004 真实条件路由，H/I 完成后镇上按钮不重复出现、取消仍可选择。
- 正式 EUI Prefab 在 Unity 预览场景渲染并实际触发鼠标射线、悬浮、点击；验证不可支付商品、暂停时按钮、重复选择。画面为 `.utmp/vn-story-import/20261002-eghi-01/shop-ready.png` 和 `shop-hover.png`。

新增节点、变量、奖励与脚本参数改变剧情指纹，旧存档可能不兼容；没有清除原存档。请在实机走一遍进入 H002、悬浮选物、购买/取消并返回剧情，再决定是否封存与发布。本批未执行模板封存或发布。

## 留待后续

完整缺失记录见 `MorningBranchLogicGaps.md`。饮品好感增减没有数值，暂不结算；G005 选项 1 与 E001 告诫引用不一致，保留源分支并记录；空事件、I 第二商品、猫薄荷彩蛋及库存未编造。独立背景、CG、表情/动作演出和商品美术待后续完善。
