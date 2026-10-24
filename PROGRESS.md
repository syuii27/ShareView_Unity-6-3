# PROGRESS.md

只记未完成的事：当前目标及验收方式与批准的计划；进展（已验证的注明证据位置）；下一步；待人定或确认的事；阻塞与当前目标的未验证事项；试过不行的做法及原因；搁置的目标与范围外发现的问题，各一句。
本节之后每次覆盖为当前快照：完成的删除，仍有价值的先归入所属文档；不记流水账，不复制决策或验证记录。

## 当前目标
两个功能改动，期限为 2026-10-29 之前。内容待用户给出；给出后逐个写计划，经批准再实现。
验收：每个改动的验收方式写在各自的计划里。

## 下一步
1. 用户审阅并提交 D-007 的改动：AGENTS.md、DECISIONS.md、PROGRESS.md、docs/unity-cli.md、docs/architecture.md。
2. 用户给出两个功能改动的内容。
3. agent 逐个写计划，批准后实现，用 docs/unity-cli.md 中的命令（含 eval）自验，再交用户在头显中验收。

## 待人定或确认的事
两个功能改动：各自的目标、现在与期望的行为、涉及的模式（Host、Client 或 Local Replay）、验收方式与参数。
VRHostCameraControl.cs（1476 行）按职责拆分的时机；agent 建议放在两个功能改动完成之后。

## 阻塞与未验证事项
开发环境将于 2026-11-01 换到另一台机器：换机前要推送全部改动，并按 README.md 单独拷贝不进版本库的数据；新机器上要按 docs/unity-cli.md 重新安装 CLI 并登录。
验证缺口：项目没有自动测试。未验证：脚本有改动时经 unity command script recompile 的实际编译与报错读取，目前只跑过脚本无改动时返回 up_to_date 的情况。

## 范围外发现的问题
OverlayImageRendererFeature 没有挂到任何 URP Renderer（Android Preset 与 Standalone Preset 的 Renderer Features 都为空），目前不生效。
Active Input Handling 为仅 Input System，VRNetworkInteractable、VRWeapon、VRPlayerRig 中仍有旧版 Input.GetKeyDown；据代码与场景引用判断，这些分支在核心场景中执行不到，执行到时按 Unity 的行为会抛出 InvalidOperationException（未运行验证）。
编辑器日志（2026-08-28）反复提示 16 至 32 个投射阴影的附加光源放不进 1024×1024 阴影图集，URP 自动降低了阴影分辨率。
XR Interaction Toolkit 包为 3.3.1，核心场景使用的 Starter Assets 样例为 2.3.2。
相机输出的 Render Texture 缺少深度缓冲，render graph 的相关警告持续刷屏：2026-10-24 编辑器打开后约 4 分钟内 Editor.log 记录 1411 次，Play 模式中仍在增加；疑与 Assets/MirrorExamplesVR/Textures/Render_tex.renderTexture 有关，未核实。
