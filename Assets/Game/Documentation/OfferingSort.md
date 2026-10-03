# A001 盲盒祭品排序

新 `OfferingSortModule` 持有玩法、独立计时和请求生命周期；`OfferingSortStepSO` 只读取次数、启动服务、锁定会话及接收结果。脚本 ID：`Game.OfferingSort.OfferingSortStep`，请求键：`cmh.a001.offering_sort`。脚本已登记一次，插入原 A001_Minigame 节点，保留原下一节点连接。

## 难度配表

源表：`Assets/GameResource/TableSources/offering_sort.etable.csv`。修改后通过 Ember 配表中心烘焙；生成绑定、运行时 bytes 与步骤指纹由正式流水线更新，不手工修改生成物。

| 字段 | 含义 |
| --- | --- |
| visitFrom / visitTo | 小游戏累计次数闭区间；visitTo=0 表示没有上限。区间须从1开始连续，不重叠 |
| itemCount | 需要排序的祭品数量，支持2至10件 |
| timeLimitSeconds | 0 表示不限时，正数为限时秒数 |
| smallestFirst / largestLast | 初始把最小放最左、最大放最右；布置后仍可拖动这些祭品 |
| minimumSize / maximumSize | 占位 Image 尺寸范围，底部对齐 |
| hintEnabled / hintAfterNoProgress | 是否显示教程，以及连续多少次有效交换没有达到更佳排列时显示 |
| fastSeconds / mediumSeconds | 成功奖励时间分档，含边界 |
| fastRewardId / mediumRewardId / slowRewardId / timeoutRewardId | 各结果对应 novel_rewards 奖励行 |
| hintKey | 教程原文的多语言 Key |

默认首轮6件、0秒、首尾特殊摆放；第2次起6件、45秒、全量打乱。必须有至少两件可打乱的祭品，因此同时特殊首尾时至少4件。洗牌保证不是已完成状态。大小决定升序，颜色不表示顺序，未显示数字答案。拖出有效区域或原位放下不计操作；多指拖动只接受当前指针。

## 奖励与教程

所有对象、数量、倍率和上下限在 `novel_rewards.etable.csv` 中配置。默认成功用时≤15秒奖励30，≤30秒奖励20，其余成功奖励10；超时保底5。首次不限时，也记录用时并参与奖励分档。奖励先增加全局 `ch2_aBoxMonthlyBonus`，月结支付到 `player_money` 并清零月累计。原有清扫基础月结收益保留。

首次达到提示条件后，在底部显示原文：是不是需要将他按照顺序排列。无效操作不计数；比较历史最佳排列，避免反复交换同两件祭品永远不出提示。

## 会话与 UI

限时结束后，小游戏保持显示“时间到了！”2 秒，停用操作并继续持有剧情输入锁；提示结束后才回传超时结果、发奖并进入下一节点。提示期间退出或取消不会继续剧情。超时提示文案使用 `novel_content_text` 的 `ui.minigame.TimeUp`。

全局变量：`ch2_aBoxVisits` / `ch2_aBoxElapsedMs` / `ch2_aBoxMonthlyBonus` 为 Int；`ch2_aBoxOutcome` / `ch2_aBoxRewardId` 为 String。小游戏次数跨月保留，月累计奖励月结清零。结果为 success 或 timeout。

正式 UI 经 EUI 中心创建 Business Item 并生成绑定，挂在现有阅读页，不使用会暂停宿主的 Popup。图形为 Image 占位祭坛与祭品。游戏期间会话暂停凭据与推进锁生效，现有阅读按钮禁用，输入图临时切 UI；玩法的 IEmberUpdate 独立推进，不靠节点 OnTick。关闭和取消清理精确持有的 UI、拖拽、回调与输入映射；迟到/重复结果不回写。游戏中不能存档，完成后稳定对白可以保存。

参数、配表指纹与新增变量改变剧情语义，旧存档由现有兼容检查拒绝并保留文件。缺服务、错误配置或资源故障显式失败，不伪造成正常成功。

## 验证

超时提示补充验证：两个小游戏及正式 UI 的 71 项 EditMode 测试通过，覆盖显示“时间到了！”、倒计时归零、提示期间不回传结果、停留结束仅回传一次，以及提示期间取消不推进剧情。

Unity 编译无错误，真实剧情校验通过。221项 EditMode 测试通过（玩法、真实A001会话、自定义节点注册与NovelSession回归）；隔离预览场景中实际拖拽事件成功交换一次，uGUI布局已渲染检查。实际月结验证确认累计奖励支付、清零和跨月次数保留。

仍请在真实 Game 视图手动试玩首轮、限时轮与超时轮，检查 UI、按钮/快捷键禁用及恢复、月结结果，再决定是否封存业务与发布。本批未执行封存或发布。

完整文件清单、new/project-owned归属、GUID、稳定ID、哈希与验证结果见 `NarrativeImports/20261001-a001-offering-sort.json`。本批没有修改模板叙事运行器或框架包源码。
