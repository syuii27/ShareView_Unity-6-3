# DECISIONS.md

只记有真实取舍、长期影响项目、理由从代码或成果本身看不出、遗忘会导致返工的选择；允许为空，不为填格式虚构备选方案。
每条 D-NNN 只定一个可单独推翻的结论，含状态（采纳、排除、待定）、问题与影响范围、备选方案及未选原因、决策、依据与代价；人定的写明日期、是谁与原话要点，要人定的在其同意前只能是待定。
只追加，不改写历史结论，不改变原意的路径、拼写等机械修正除外；结论改变时追加新条目并完整写出现行结论，依据有错追加更正，新旧条目的状态互相标明取代或更正关系；引用旧结论的正文同时改正。

## D-001 提交与推送只由用户本人完成
状态：采纳。2026-10-23 用户定，2026-10-24 补充提交说明的做法。
问题与影响范围：工作规则默认由 agent 在结束时提交全部改动；本仓库公开，提交历史对外可见。影响每次会话的结束步骤。
备选方案：agent 按规则默认直接提交；未选，用户要求亲自掌握提交与提交说明。
决策：commit、push 与提交说明只由用户做；agent 结束时给出变更说明、建议的提交拆分与沿用历史风格的提交说明草稿。
依据与代价：用户原话要点：所有 commit 和 push 都必须由本人来做并写备注；提交由用户来做，agent 按以往的提交说明风格给建议。代价：未提交的改动不进版本库，换会话或换机器前要提醒用户提交。

## D-002 README.md 使用英文
状态：采纳。2026-10-24 用户定。
问题与影响范围：工作规则要求文档用中文；README.md 是公开仓库的入口页。只影响 README.md。
备选方案：README.md 也用中文；未选，公开读者以英文为主。
决策：README.md 用英文，其他文档用中文。
依据与代价：用户原话要点：按 agent 的建议来。代价：README.md 与中文文档写到同一事实时要保持一致，细节只写在一处。

## D-003 进度与决策文档留在公开仓库，只写工程信息
状态：采纳。2026-10-24 用户定。
问题与影响范围：PROGRESS.md 与 DECISIONS.md 随仓库公开。影响这两个文件能写的内容。
备选方案：放在仓库之外的私有位置；未选，新会话将不能只凭本项目文件接续工作。
决策：两份文档留在仓库中，只写工程信息，不写敏感信息。
依据与代价：用户原话要点：只写工程信息，不要包含敏感信息。代价：非工程的背景不在仓库中记录。

## D-004 开始时按需读文件，不再每轮通读
状态：采纳。2026-10-24 用户定。取代原 AGENTS.md 中"每轮对话开始前查看本文件、核心场景、关键脚本、Packages/manifest.json 与 ProjectSettings/ProjectVersion.txt"的要求。
问题与影响范围：核心场景约 2.3MB，VRHostCameraControl.cs 有 1476 行，每轮通读成本高，且多数内容与当次任务无关。影响每次会话的开始步骤。
备选方案：保留每轮通读；未选，成本高。
决策：开始时只读 AGENTS.md 与 PROGRESS.md，其余按 AGENTS.md 的文件职责表按需读，场景文件按对象名或 GUID 检索。原 AGENTS.md 的内容按归属拆到 README.md、docs/architecture.md 与项目特有规则。
依据与代价：用户原话要点：按工作规则并结合本项目的特征来。代价：文件职责表与 docs/architecture.md 要随改动保持准确。

## D-005 agent 连接 Unity 编辑器的方式
状态：待定，已被 D-006 取代。
问题与影响范围：agent 看不到编辑器的编译结果与画面，每次改动都要用户打开 Unity 检查。选定的方案会给项目增加开发依赖。
备选方案：
方案 1，Unity CLI 加 Unity Pipeline 包（com.unity.pipeline）：Unity 官方方案。包在打开的编辑器内启动只接受本机连接的服务，CLI 用 unity command 进出 Play 模式、截图、重新编译与运行测试，eval 在编辑器内执行 C#，需要访问令牌。免费，不需要 Unity AI 订阅；CLI 为 beta，包为实验版 0.9.0-exp.1。安装 CLI 与登录 Unity 账号属于用户级操作；按 Unity 文档，CLI 用 irm https://unity.com/install.ps1 | iex 或 winget install Unity.CLI 安装，然后运行 unity auth login，并在编辑器打开本项目时于项目根目录运行 unity pipeline install。
方案 2，com.unity.ai.assistant 内置的 MCP 服务器：未选，已被 Unity 弃用，由方案 1 取代。
方案 3，社区 MCP 插件，如 CoplayDev unity-mcp（需要 Python 与 uv）或 CoderGamester mcp-unity（需要 Node.js 18 以上）：第三方，安全与维护要自行评估。
方案 4，批处理模式（-batchmode）脚本：不需要新依赖，但编辑器打开时无法运行，也看不到用户正在用的编辑器。
决策：未定。agent 推荐方案 1；方案 1 半天内跑不通时，再由用户决定是否改用方案 3。
依据与代价：依据为 Unity 文档（2026-10-23 查阅）https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli 与 https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package 。未验证：方案 1 在本项目中的兼容性与实际命令清单。代价：方案 1 会改动 Packages/manifest.json 与 packages-lock.json；eval 相当于在编辑器内执行任意代码，访问令牌不得进入版本库。

## D-006 agent 连接 Unity 编辑器采用 Unity CLI 加 Unity Pipeline 包
状态：采纳。2026-10-24 用户定，取代 D-005（待定）。
问题与影响范围：同 D-005，agent 看不到编辑器的编译结果与画面。项目将新增开发依赖 com.unity.pipeline，Packages/manifest.json 与 packages-lock.json 随之改变。
备选方案：见 D-005。方案 2 已被 Unity 弃用；方案 3 为第三方，安全与维护要自行评估；方案 4 看不到用户正在用的编辑器。
决策：采用 D-005 的方案 1。方案 1 半天内跑不通时，由用户决定是否改用方案 3。
依据与代价：用户原话要点：D-005 采用方案 1。代价：CLI 为 beta、包为实验版，行为可能随版本变化；eval 相当于在编辑器内执行任意代码，访问令牌不得进入版本库；安装 CLI 与登录属于用户级操作，换机后要重做。
