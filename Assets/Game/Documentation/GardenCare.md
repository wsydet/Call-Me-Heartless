# 菜地照料与上午 B/C 接入

2026-10-02，按用户明确确认的导入方案与模块桥接方案接入。长期来源、原文行号、节点/台词/命令 ID、选项、确认摘要保存在 `NarrativeImports/20261002-morning-bc-01.json`；未完成的剧情设计见 `MorningBranchLogicGaps.md`。

## 实际玩法与接入位置

- B001 第 2 行后、B002 第 5 行后直接开始，无开始按钮或规则页。4×3 共 12 株菜苗，教程 6 株、最高档 11 株杂草随机伴生在部分菜苗的左侧或右侧；两者独立点击，误点菜苗会实际拔掉并单独记录损失，不计入拔草进度。将配表指定的虫子（当前教程 2 只、最高档 4 只）拖入右侧草丛，放错位置会归位。
- B001/B002 分别按进入次数选择 `garden_care` 的五档参数，首次教学不限时；进入即加次数，超时或退出也计入，超过第五档沿用最高档。教学完成标记仅作记录。数量、时限、提示延迟和结果停留均走表，见 [小游戏数值配表](MiniGameTables.md)。
- 与 A 支线一致，玩法位于阅读页上部，移除全屏遮罩、外框以及菜地和草丛的矩形底色，下部保留剧情提示区域。正式 Prefab 经 UI 中心重新生成绑定。
- 完成与时间到均回到对应事件的原开支提示，再进入 B_Complete；只增加一次 ch2_monthPlant，沿用每次种地月结算省 5 的既有规则。操作成绩没有额外金钱或好感奖励。
- C001→C002→C003→C004 按完成次数播放，之后重复 C004。C003 选项 1 通过奖励行 cmh_c003_option1 给 player_affection_shen 加 15；选项 2 没有奖励。C_Complete 只增加一次 ch2_cVisits，傍晚 F 的既有条件仍为该值 ≥ 2。

## 模块桥接与归属

采用第二级“模块桥接”：实际玩法位于 Game.GardenCare 模块，剧情继续复用框架的 INovelStepService / NovelStepServiceBridgeSO；未增加命令枚举或修改调度器。

| 文件/资产 | 归属与作用 |
| --- | --- |
| Assets/Game/Module/GardenCare/ | new：配置、纯玩法状态、服务模块、序列化适配及测试 |
| Assets/Game/UI/Runtime/Module/GardenCare/ | new：UI 中心生成的 Item/Binding，业务交互、拖拽和 UI 宿主 |
| Assets/GameResource/Resources/UI/Module/GardenCare/Prefabs/EUIGardenCareItem.prefab | new：经官方 UI 中心创建、绑定生成的正式 Prefab |
| Assets/GameResource/Authoring/Narrative/Scripts/GardenCare/GardenCareBridge.asset | new：登记进 Story 自定义节点清单的桥接资产 |
| Assets/GameResource/Resources/Config/Narrative/CallMeHeartless/Chapters/CH03_Chapter02/ | 新增 21 个 B/C 节点；既有 B_Action/C_Action 与 Chapter 为 template-owned 路径中的本地剧情资产，保留原 ID |
| Assets/GameResource/TableSources/novel_content_text.etable.csv、novel_ui_text.etable.csv、novel_rewards.etable.csv | template-owned 路径中的本地源表：追加 114 个正文/角色名键、19 个 UI 键与 1 个奖励行，原翻译保留 |
| Assets/Game/Module/Narrative/Tests/CallMeHeartlessBCLinesTests.cs | new：真实剧情会话回归测试 |

GardenCareBridgeSO 保留原桥接 ScriptId 与配置字段，负责读取教学标记、传递本次配置、写回结果并清理输入锁。玩法仍由模块负责。首次教学不应用桥接超时，后续限时局保留等待上限；模板 NovelStepService.cs 未修改。

- ScriptId：`Game.Narrative.NovelStepServiceBridgeSO`；Story 中只登记一个此 ID 的资产，两个入口共享配置，运行状态由模块和桥接上下文持有。
- 请求键：`cmh.b.garden_care`；非教学局桥接等待上限 180 秒，教学局不限时。
- 结果：Chapter String `b_gardenOutcome`，取 `completed` / `timeout`；随机路由使用 Chapter Int `b_roll`（0–99）。
- 特殊事件变量：Global Bool `ch2_metMurong`、`ch2_hasOutingWithYan`、`ch2_b003Played`、`ch2_b004Played`，初始均 false。前两个等待真实相遇剧情补齐。
- Abort 按 executionId 清理本局 UI、回调和状态；过期输入不会影响新局。技术失败进入剧情故障路径，不经过正常结算。UI 关闭时恢复原输入映射与光标设置。

## 验证与验收

2026-10-03：去除独立外框、菜苗增加为 12 株、杂草随机伴生并允许误拔。小游戏 49 项与护菜剧情会话 5 项 EditMode 测试通过；正式 UI 实际射线检查全部 18 个植物的点击区域，渲染检查误拔及清草状态。画面、绑定报告和六槽存档重建记录位于 `.utmp/minigame-controls-20261003/`。

2026-10-02 本次节点融合修正：Unity MCP 编译无错误，小游戏专项 48 项及护菜真实剧情会话 5 项通过。正式 Prefab 重新生成绑定；阅读页预览验证拔除全部杂草后六株菜苗及完整地面保留，截图在 `.utmp/minigame-node-fixes-20261002/`。未做独立 Player 人工试玩。以下为初次导入时的历史验证记录。

Unity MCP 编译及完整 Story/实际表校验通过。最终 227 项 EditMode 测试全部通过（0 失败、0 跳过），批次 ID 记录在来源清单；覆盖框架自定义节点注册、会话、桥接，以及本次玩法和 B/C 实际图。103 行文本、117 条命令、8 条路由和 A 支线 82 个文件已回读验证。

正式 UI 在 Unity 预览场景中使用实际 EUIItemFactory、GraphicRaycaster 和指针事件验证；准备、操作、超时画面位于本批 `.utmp/vn-story-import/20261002-morning-bc-01/garden-*.png`。尚未进行玩家在完整 PlayMode 流程中的人工操作验收，也未制作独立菜苗/虫子美术。

剧情节点、变量和共用奖励表指纹发生变化，旧存档可能被兼容性检查拒绝；未删除、改写或迁移任何现有存档。请在 Unity 中手动走一遍进入小游戏、点击/拖拽、结果返回和继续剧情，再决定是否封存业务或发布。本次未执行 SaveTemplate、Bump 或发布。
