# PROGRESS.md

只记未完成的事：当前目标及验收方式与批准的计划；进展（已验证的注明证据位置）；下一步；待人定或确认的事；阻塞与当前目标的未验证事项；试过不行的做法及原因；搁置的目标与范围外发现的问题，各一句。
本节之后每次覆盖为当前快照：完成的删除，仍有价值的先归入所属文档；不记流水账，不复制决策或验证记录。

## 当前目标
让 agent 能连接并操作用户已打开的 Unity 编辑器，减少每次改动都要用户打开 Unity 检查。
验收：agent 在用户打开的编辑器中完成一次重新编译并读取结果、进入与退出 Play 模式、截图，并把所用命令与输出交给用户核对。
计划：未写。D-005 定案后写出，交用户批准。

## 进展
工作流文件已建立，待用户审阅提交：README.md 补充安装、运行、验证与不进版本库的数据；AGENTS.md 改为工作规则的唯一来源；新建 PROGRESS.md、DECISIONS.md 与 docs/architecture.md。

## 下一步
1. 用户审阅并提交工作流文件。
2. 用户决定 D-005。采纳方案 1 时，agent 写出计划（含用户要执行的安装与登录步骤），用户批准并完成这些步骤后，agent 完成连接验收。
3. 用户给出两个功能改动的内容，agent 逐个写计划，批准后实现，期限为 2026-10-29 之前。

## 待人定或确认的事
D-005：agent 连接 Unity 编辑器的方式。
两个功能改动：各自的目标、现在与期望的行为、涉及的模式（Host、Client 或 Local Replay）、验收方式与参数。
编辑器连接未能及时打通时，两个功能改动是否改由用户在编辑器中验收、照常推进，以保 2026-10-29 的期限。
VRHostCameraControl.cs（1476 行）按职责拆分的时机；agent 建议放在两个功能改动完成之后。

## 阻塞与未验证事项
开发环境将于 2026-11-01 换到另一台机器：换机前要推送全部改动，并按 README.md 单独拷贝不进版本库的数据。
验证缺口：项目没有自动测试，也没有可在编辑器外运行的编译检查。最近一次编辑器会话（2026-08-28）编译无错误，依据为本机 %LOCALAPPDATA%\Unity\Editor\Editor.log 中没有 error CS，该日志会被下次编辑器会话覆盖；此后脚本未改动。
未验证：Unity CLI 与 Pipeline 包在本项目中的兼容性与实际命令清单。

## 范围外发现的问题
OverlayImageRendererFeature 没有挂到任何 URP Renderer（Android Preset 与 Standalone Preset 的 Renderer Features 都为空），目前不生效。
Active Input Handling 为仅 Input System，VRNetworkInteractable、VRWeapon、VRPlayerRig 中仍有旧版 Input.GetKeyDown；据代码与场景引用判断，这些分支在核心场景中执行不到，执行到时按 Unity 的行为会抛出 InvalidOperationException（未运行验证）。
编辑器日志（2026-08-28）反复提示 16 至 32 个投射阴影的附加光源放不进 1024×1024 阴影图集，URP 自动降低了阴影分辨率。
XR Interaction Toolkit 包为 3.3.1，核心场景使用的 Starter Assets 样例为 2.3.2。
