# PROGRESS.md

只记未完成的事：当前目标及验收方式与批准的计划；进展（已验证的注明证据位置）；下一步；待人定或确认的事；阻塞与当前目标的未验证事项；试过不行的做法及原因；搁置的目标与范围外发现的问题，各一句。
本节之后每次覆盖为当前快照：完成的删除，仍有价值的先归入所属文档；不记流水账，不复制决策或验证记录。

## 当前目标
两个功能改动，期限为 2026-10-29 之前（用户，2026-10-24）：运动补偿点找出最关键的 1 到 3 个变量改动，提升缓解效果；两段录制的回放都明显延长到约 250 至 300 秒，两段回放帧率一致、不要求时长相等。用户预计都不涉及 FovOnly 与 FullMask。

## 进展
调研、独立审查与按实时约束的重新评估已完成（2026-10-24；调研记录按用户决定为临时文件，不进项目）；结论在 docs/architecture.md，决定见 DECISIONS.md D-008 至 D-010。
时长：已在 main 上提交（240bdbe）。已验证（编辑器）：默认 13FPS，录制 1 完整回放 durationSeconds 290.52，PaFrameIndex 连续（ObserverLogs/SessionMeta_Rec1_20261024_222450.json，不进版本库）。
点：按用户要求改在 main 上做，不建分支（2026-10-24）；已实现，未提交。遮罩编号 9 至 12（下拉框末尾 4 项），做法见 docs/architecture.md。已验证（编辑器，CLI 与 eval）：编译无错；静态函数与整条链路的已知答案检验通过（A 流速 0.8 倍、B 回位与理论一致、参与者跟转时不动、门限两级与保持时间正确、无闪烁）；录制 1 回放中四种新规则都按预期显示或隐藏，原 PointOnly 的点仍在转动时出现（29 次取样中 5 次），NoMask 无点；SessionMeta 的 dotCue 记录了全部参数（ObserverLogs/SessionMeta_Rec1_20261024_225625.json）。门限起始值 20°/s 与 10°/s 来自两段录制的离线探查（样本内，参与者头不动）：点约 34% 与 41% 的时间可见，每分钟出现约 8.5 与 9.1 次。

## 批准的计划（用户，2026-10-24）
点的第 5 步：用户在头显中用同一段录制比较原 PointOnly 与四种新规则并调参，选定、要求修改或提出新规则；之后另写清理步骤（删掉不用的规则或保留为条件），结论记进 DECISIONS.md。

## 下一步
1. 用户提交点的改动；构建后在头显中核对两段时长（约 290.4 与 264.8 秒），并比较五种点。参数只有在编辑器的 Inspector 里能边看边调，构建出的 exe 用默认值。
2. 用户选定后写清理步骤。

## 待人定或确认的事
选用哪种点、参数是否调整；点的显示深度是否改（用户先在头显中看有无双像）；确认现有 PointOnly 构建在头显中看得到点。
VRHostCameraControl.cs（1750 行）按职责拆分的时机；agent 建议放在两个功能改动完成之后。

## 阻塞与未验证事项
开发环境将于 2026-11-01 换到另一台机器：换机前要推送全部改动，并按 README.md 单独拷贝不进版本库的数据；新机器上要按 docs/unity-cli.md 重新安装 CLI 并登录。
未验证：头显中的观感；录制 2 的时长（编辑器的 StreamingAssets 只有录制 1）；新点在联网模式下的表现（只核对了代码路径）；场景中 4 个新选项的加载（改文本时编辑器已关闭，用户下次打开时可见）。
验证缺口：项目没有自动测试。Game 视图截图中看不到点的原因未查；点对象存在且启用（eval 核对）。

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
