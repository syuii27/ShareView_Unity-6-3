# PROGRESS.md

只记未完成的事：当前目标及验收方式与批准的计划；进展（已验证的注明证据位置）；下一步；待人定或确认的事；阻塞与当前目标的未验证事项；试过不行的做法及原因；搁置的目标与范围外发现的问题，各一句。
本节之后每次覆盖为当前快照：完成的删除，仍有价值的先归入所属文档；不记流水账，不复制决策或验证记录。

## 当前目标
两个功能改动，期限为 2026-10-29 之前（用户，2026-10-24）：运动补偿点找出最关键的 1 到 3 个变量改动，提升缓解效果；两段录制的回放都明显延长到约 250 至 300 秒，两段回放帧率一致、不要求时长相等。用户预计都不涉及 FovOnly 与 FullMask。验收写在下面的计划里。

## 进展
调研、独立审查与按实时约束的重新评估已完成（2026-10-24；调研记录按用户决定为临时文件，不进项目），结论已写入 docs/architecture.md，决定见 DECISIONS.md D-008 至 D-010。
时长改动已在 main 上实现，未提交（2026-10-24）：VRHostCameraControl.cs 加 13FPS 档（case 5）、默认档改为 5、SessionMeta 加 playbackSpeed；场景中 FPS 下拉框末尾加 13FPS、两个组件的默认档改为 5。已验证（编辑器，CLI 与 eval）：编译无错；Local Replay 默认为 13FPS（间隔 1/13），切到 15FPS 为 1/15；录制 1 完整回放的 SessionMeta 为 sampleCount 3776、replayFrameStep 1、effectiveFps 13、playbackSpeed 0.867、durationSeconds 290.52，PaFrameIndex 0 至 3775 连续。证据：项目根目录 ObserverLogs/SessionMeta_Rec1_20261024_222450.json 等四个测试日志（不进版本库）。未验证：录制 2 的时长（StreamingAssets 中只有录制 1）与头显中的观感。

## 批准的计划（用户，2026-10-24；点先不实现，等用户通知）
顺序：时长已实现，等用户提交与头显核对；之后从 main 建本地分支 feature/dot-cues 做点。每步用 docs/unity-cli.md 中的命令自验，再交用户在头显中验收；agent 不提交、不推送。
点（A、B 并行，运行时切换）。不做：提前预告与延迟画面（D-008、D-010），改点的显示深度，改其他遮罩，动录制，构建。
1. 离线算门限起始值：在两段录制（只读）上算出不同门限下点的可见时间比例与每分钟出现次数，结果作探查。
2. 代码：遮罩编号 9 至 12 追加在下拉框末尾，依次为 PointOnly A 常显、A 按冲突、B 常显、B 按冲突；原 PointOnly 不改，作对照。共用规则：点的移动量为操作者转动减参与者转头，只用画面当前显示的操作者朝向与参与者头部朝向，可见时连续移动并轻度平滑。A：中央 22° 以外、45° 以内、约 16° 间距的点阵，整体流动并环绕，环带边缘渐隐，流速约 0.8 倍，透明度约 0.5。B：原 8 点位置，偏移累积并约 0.75 秒回位，流速约 0.4 倍。按冲突：平滑后的相对转速高于出现门限时约 0.15 秒淡入，低于消失门限约 0.5 秒后约 0.3 秒淡出，隐藏时回到起始排布。参数放在 Inspector 并记进 SessionMeta。验收：编译无错，原有遮罩的代码路径不变。
3. 场景（编辑器关闭时改文本）：遮罩下拉框选项末尾加上述 4 项。验收：diff 只含这几行。
4. 自验：用已知输入检验共用逻辑（流速倍数、回位、跟着转时不动、环绕连续、门限与淡入淡出、门限附近不闪烁、隐藏后归位）；Play 模式中逐一取样四种新规则；原 PointOnly 与 NoMask 不变；SessionMeta 新字段。之后更新 docs/architecture.md、docs/unity-cli.md 与本文件。
5. 用户在头显中用同一段录制比较原 PointOnly 与四种新规则并调参，选定、要求修改或提出新规则；之后另写清理步骤（删掉不用的规则或保留为条件），结论记进 DECISIONS.md。

## 下一步
1. 用户审阅并提交时长改动；构建后在头显中核对两段约 290.4 与 264.8 秒。
2. 用户通知后，从 main 建分支做点。

## 待人定或确认的事
何时开始做点；点的显示深度是否改（用户先在头显中看现有的点有无双像）；确认现有 PointOnly 构建在头显中看得到点。
VRHostCameraControl.cs（1482 行）按职责拆分的时机；agent 建议放在两个功能改动完成之后。

## 阻塞与未验证事项
开发环境将于 2026-11-01 换到另一台机器：换机前要推送全部改动，并按 README.md 单独拷贝不进版本库的数据；新机器上要按 docs/unity-cli.md 重新安装 CLI 并登录。
验证缺口：项目没有自动测试。未验证：遮罩效果能否从截图中看出。PointOnly 回放中，场景里有 8 个点线对象（预制体 Line，挂在 World Space Canvas 的 RawImage 下），但 Game 视图截图中看不到，原因未查。

## 搁置的目标
PaPos 不含参与者的头部位置（现状见 docs/architecture.md 数据路径）：用户定为 2026-11-01 开始解决，方案届时再讨论。
核对实验电脑上回放的实际渲染帧率：2026-11-01 起可从实验日志 SessionMeta 的 actualRenderFps 查看；用户认为暂不影响。

## 范围外发现的问题
OverlayImageRendererFeature 没有挂到任何 URP Renderer（Android Preset 与 Standalone Preset 的 Renderer Features 都为空），目前不生效。
Active Input Handling 为仅 Input System，VRNetworkInteractable、VRWeapon、VRPlayerRig 中仍有旧版 Input.GetKeyDown；据代码与场景引用判断，这些分支在核心场景中执行不到，执行到时按 Unity 的行为会抛出 InvalidOperationException（未运行验证）。
编辑器日志（2026-08-28）反复提示 16 至 32 个投射阴影的附加光源放不进 1024×1024 阴影图集，URP 自动降低了阴影分辨率。
XR Interaction Toolkit 包为 3.3.1，核心场景使用的 Starter Assets 样例为 2.3.2。
相机输出的 Render Texture 缺少深度缓冲，render graph 的相关警告持续刷屏：2026-10-24 编辑器打开后约 4 分钟内 Editor.log 记录 1411 次，一次约 5 分钟的 Play 模式回放后 Console 警告累计约 2.2 万条；疑与 Assets/MirrorExamplesVR/Textures/Render_tex.renderTexture 有关，该资产的深度格式为 None（读资产文件确认），Sub Camera 渲染到它，是否就是警告来源未核实。
回放帧率选 25FPS 或 35FPS 时每步仍只前进 1 帧，相当于 1.67 倍与 2.33 倍速播放；3、5、15FPS 为实时（读代码推断，未运行）。
按场景数值计算（显示平面在主相机前 0.28 m、边长 0.84 m），FovOnly 与 FullMask 的周边画面把 Sub Camera 80° 的画面铺在约 112° 视角上，随参与者转头转得比真实世界快约 1.1 至 1.5 倍；未在头显中验证。
