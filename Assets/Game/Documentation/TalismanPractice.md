# D：三选一学习小游戏

## 玩法

入口为下午“找师父学习”的 D 路线。原生剧情选项提供识符找不同、画符接单、静心吹羽毛三选一；选择后立即进入对应玩法，完成后返回 D Return，每次只进行一项。没有开始按钮、规则页或下一项按钮。所有画面使用 UI 占位图形，没有生成图片。

1. 找不同：三种符咒，每种两处差异。点击任意一侧的差异，重复点击不重复计分。
2. 接单：按顾客要求分别选择“底纹、中层、细节”，每层三种选项。可以单独替换或全部清空，三部分匹配后交付。没有手绘输入。顾客耐心耗尽会离开并轮到下一位。
3. 吹羽毛：按住按钮或空格逐渐增强气流，松手停止施力，羽毛受重力与阻力缓缓落下并左右漂移。整根羽毛在框内才累计时间，出框暂停累计。第 1 次为宽松教程，第 2–3 次提高操控难度，第 4 次开始移动范围框，第 5 次及以后沿用最高档。具体物理和框参数全部见 `feather_difficulty.etable.csv`。暂停、失焦、关闭或指针移出按钮会释放输入。

每个项目按自己的进入次数独立选档，首次不限时，接单教程也不消耗顾客耐心。进入即计数，取消或超时也计入；读档恢复存档中的次数。教学完成标记不再决定难度。`study_settings` 配置找不同和接单的五档时限、人数、耐心与提示；`feather_difficulty` 配置羽毛的五档完整参数。游戏直接在节点内开始，下方自然提示规则，结果停留时间也走配表。详见 [小游戏数值配表](MiniGameTables.md)。

## 结构与配置

- 运行模块：`Assets/Game/Module/TalismanPractice/`。
- UI：`Assets/Game/UI/Runtime/Module/TalismanPractice/`。
- 正式 Prefab：`Assets/GameResource/Resources/UI/Module/TalismanPractice/Prefabs/EUITalismanPracticeItem.prefab`。
- 步骤资产：`Assets/GameResource/Authoring/Narrative/Scripts/TalismanPractice/TalismanPracticeStep.asset`，只保留剧情注册与表指纹；在 CSV 源表调整时限、人数和羽毛参数，再用“小游戏/烘焙数值配表”菜单生成。
- 共享图案定义：`TalismanPatterns.cs`；学习图案、订单参考、部件缩略图及组合预览使用同一份几何定义。

`TalismanPracticeGame` 管理纯玩法状态；`TalismanPracticeModule` 管理请求和生命周期；UI Host 管理页面挂载、输入映射与光标恢复；叙事步骤只校验、调用服务、接收结果并恢复剧情。使用独立的 `ITalismanPracticeService`，不占用已有花园小游戏服务。

EUI Prefab 通过开发中心创建，22 项绑定通过公共生成接口生成。`EUITalismanPracticeItem.Binding.cs` 为生成文件，不应手工维护。编辑器菜单“Call Me Heartless/D 练习/创建或重新生成 UI”可重新生成绑定；已有布局不会因重复执行而重建。后续美术可替换占位视觉，保留交互控件与绑定关系。

## 剧情接入与结果

- 自定义步骤：`Game.TalismanPractice.Training`。
- 请求键：`cmh.d.talisman_practice`。
- D Action → `D_StudyChoice` → `D_Study0/1/2` → D Return。三个步骤的 `IntegerOperand` 为 0/1/2，传递所选玩法；首个步骤保留命令 ID `cmh_d_talisman_training_v1`。
- 结果变量：Chapter 作用域 String `d_talismanOutcome`。
- JSON 字段：`differences`、`orders`、`feather` 为项目状态，未选项目为 `not_selected` 且计数为零；`differencesFound`、`ordersCompleted`、`holdMilliseconds` 为完成数量与累计毫秒。

保留原有 `ch2_lastAction = D` 及通向 D Return 的连线。结束、取消、请求失败均清理界面与输入锁；执行 ID 和结束标志阻止旧回调重复结束或写入已取消会话。请求时限采用未暂停的经过时间。

只在原剧情中追加步骤注册、章节变量及 D Action 命令；没有改动 GardenCare、通用 NovelStepService、D Return 或全局 EUI 绑定设置。

## 验证与试玩

2026-10-03：移除独立外框，羽毛改为渐强的持续吹气，增加空格、阻力与左右飘动，扩大目标框并降低累计时长。小游戏 49 项及护菜剧情 5 项 EditMode 测试通过；学习正式 Prefab 覆盖三种分辨率、鼠标/空格接续、松开/移出按钮和暂停恢复。画面与最新六槽存档报告位于 `.utmp/minigame-controls-20261003/`，供用户继续试玩手感。

2026-10-02 本次改为原生三选一，并统一自动开始、首次不限时与下方自然提示。Unity MCP 编译无错误；小游戏专项 48 项与护菜剧情 5 项通过，学习专项覆盖三个选项的实际 Story 会话、未选项目不运行、自动结束、无限时教学、迟到回调和 800×600/1280×800/1920×1080 的正式 Prefab 点击。预览在 `.utmp/minigame-node-fixes-20261002/`。更广的编辑器测试因 PlayMode 切换挂起已取消，未宣称全套通过。以下为初版的历史验证记录。

2026-10-02 完成 Unity 编译检查，无 C# 编译错误：

- D 专项 EditMode：18 项通过，包括图案与组合匹配、耐心与阶段时限、暂停、羽毛控制、取消与迟到回调、真实 Story 会话推进和结果写入。
- 框架自定义节点注册、会话和桥接回归：22 项通过。
- 正式 Prefab 在 800×600、1280×800、1920×1080 下通过实际相机渲染、GraphicRaycaster 命中及 ExecuteEvents 操作验证。
- 四张界面预览和报告：`.utmp/vn-custom-node/20261002-d-talisman-practice-01/results/`。

这些是 Editor 内模型、会话和正式 UI 的验证，尚未执行独立 Player 构建验收。新增步骤会改变 Story 指纹，旧存档可能不兼容；请保留旧存档，从新会话进入 D 路线试玩。建议依次体验找不同、三层组合交付与顾客超时、羽毛稳定、暂停恢复与退出，确认操作手感后再进行封板或发布。
