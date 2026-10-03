# 小游戏难度与数值配表

所有小游戏都在进入实际游玩节点时将自己的 Global Int 次数加一，再按次数选择本局配置。成功、超时、取消都不在结算时重复计数；只看前置展示或学习选项不计数。次数属于当前剧情存档，读旧档会恢复该档的次数，不是跨存档永久统计。

目前各表提供五档：第 1 次是不限时教学，第 2–5 次依次升档，第 6 次起沿用第 5 档。A 支线以 `visitFrom/visitTo` 控制区间，末行 `visitTo=0` 覆盖后续全部次数；其余表以连续 `level=1..N` 控制档位，超过 N 沿用末档。可继续在表内追加档位。

## 编辑入口

修改 `Assets/GameResource/TableSources/*.etable.csv`，然后在 Unity 执行 **Call Me Heartless → 小游戏 → 烘焙数值配表**。该菜单调用正式 Ember Table 管线，生成绑定、二进制表和清单，并同步小游戏步骤指纹。不要手改 `Table/Generated`、`.bytes` 或步骤指纹。运行中的局使用进入时的配置快照，改表后重新进入测试。

| 玩法 | 源表 | 独立次数变量 | 难度与数值 |
|---|---|---|---|
| A001 祭品排序 | `offering_sort` | `ch2_aBoxVisits` | 数量、大小、固定首尾、时限、提示、奖励档位与结果停留时间 |
| A002 山路清扫 | `path_cleaning`、`path_cleaning_types`、`path_cleaning_tools` | `ch2_aCleaningVisits` | 数量、类型池与权重、尺寸、工具、清除阈值、工具半径、扫动距离、污渍扩散、提示、奖励、结果停留时间 |
| B001/B002 护菜 | `garden_care` | `ch2_gardenVisits_0` / `ch2_gardenVisits_1` | 两个地块独立计数，使用相同的五档表；菜苗数、杂草数、虫数、行列、时限、提示与结果时间 |
| D 识符找不同 | `study_settings` | `ch2_studyVisits_0` | 找不同时限、提示、结果停留与服务等待上限 |
| D 画符接单 | `study_settings` | `ch2_studyVisits_1` | 顾客数量、耐心、总时限、提示、结果停留与服务等待上限 |
| D 静心吹羽毛 | `feather_difficulty` | `ch2_featherVisits` | 每档完整物理、漂移、目标框、时限与达成要求 |
| H002 购买饮料 | `shop_settings`、`shop_products` | `ch2_shopVisits` | 每档选择时限、每种商品价格；当前五档均保留 5/10/100 原价 |

`study_settings` 的两种玩法共用档位定义，但各按自己的进入次数选行。三种符样、每张两处差异、三层部件是当前内容结构，不作为可任意增减的难度字段。布局常数和数值积分精度属于实现细节。

## 羽毛五档

| 次数 | 档位 | 时限 / 框内累计 | 目标框 |
|---|---|---|---|
| 1 | 入门呼吸 | 不限时 / 10 秒 | 宽松静止框 |
| 2 | 稳住气息 | 70 / 12 秒 | 静止，操控稍难 |
| 3 | 轻重有度 | 75 / 14 秒 | 静止，范围稍窄 |
| 4 | 随风而行 | 90 / 15 秒 | 横向与纵向缓慢移动 |
| ≥5 | 气随心动 | 100 / 18 秒 | 移动稍快、范围稍窄 |

`gravity/blowAcceleration` 为归一化高度每秒平方；`maxRiseSpeed/maxFallSpeed` 为归一化高度每秒；`blowRampSeconds` 为从零气流到满气流所需秒数；`airDrag` 控制速度阻尼。松手立即令吹气力为零，保留惯性、重力与阻尼。

`frameBottom/frameTop/frameWidth`、`frameMoveAmplitudeX/Y`、`featherHalfWidth/Height`、`startY` 与漂移幅度均使用活动区 0–1 归一化坐标。`frameMovePeriod` 单位为秒；漂移频率为弧度/秒；`tiltDegrees` 为旋转角度。碰撞判定和画面共用当前移动框坐标。出框暂停累计，回框继续累计。

首档羽毛 `timeLimitSeconds=0`，其他档须为正数；框的整个运动范围须留在活动区内，吹气加速度须大于重力。第一档不限时由进入次数决定，旧 `Learned` 标记只记录是否完成过教学。

## 配置约束与测试

护菜当前正式界面可容纳最多 24 株植物、4 只虫；菜苗数须等于列×行。五档均为 12 株菜苗，杂草伴生且并非每株都有，后续增加杂草和虫子并缩短时限。首档忽略时限。购买饮料首档 `timeoutSeconds=0`；每档必须各有 water、cola、tea，商品 `id` 为唯一行键，`productId` 对应剧情分支。成交价由本局选中的表行写入 `h_drinkPrice`，实际扣款读取此变量。

使用正式测试存档生成器更新节点定位档。默认次数为 0；测某玩法第 N 次时，将该玩法对应次数变量覆盖为 N−1。不要修改检查点 JSON 或摘要。学习存档停在三选一节点，选择后才记次并开始游戏。
