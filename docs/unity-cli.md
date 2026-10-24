# 用 Unity CLI 连接编辑器

agent 通过 Unity CLI 和项目中的 Unity Pipeline 包（com.unity.pipeline）操作用户已打开的 Unity 编辑器，选型理由见 DECISIONS.md D-006。改动代码后需要验证、或换机后安装与连接时读。

## 安装与连接
安装 CLI 与登录属于用户级操作，由用户做，换机后要重做。
1. 用户安装 Unity CLI：PowerShell 运行 irm https://unity.com/install.ps1 | iex，或 winget install Unity.CLI。安装位置为 %LOCALAPPDATA%\Unity\bin；agent 的终端找不到 unity 命令时，重启 Claude 桌面端。
2. 用户运行 unity auth login 登录；可选运行 unity analytics opt-out 关闭使用数据上报。
3. 用户在 Unity 中打开本项目。Pipeline 包已写在 Packages/manifest.json 中，打开项目时自动安装；给别的项目首次安装时在其根目录运行 unity pipeline install。
4. 编辑器窗口要获得一次焦点，Unity 才会解析新包并重新编译；窗口不在前台时不会自动进行。
验收：unity --version 与 unity auth status 正常输出；unity status 显示本项目为 ready 并带端口号（本机为 7800）。

## 已核对的命令
以下命令于 2026-10-24 在本项目中实际运行并核对过。都在项目根目录运行，并设置环境变量 UNITY_NO_PAGER=1，避免输出进入分页器。
unity status：连接状态、端口与编辑器版本。
unity command：列出编辑器开放的命令分组；unity command <分组> 列出组内命令与参数。
unity command editor status：编辑器状态，JSON 中的 compiling 与 playMode 字段用于轮询。
unity command console status：Console 计数与 compilationFailed 标志。
unity command console get --level error --tail 10：最近的错误条目。
unity command script recompile：强制重新编译，之后轮询 unity command script recompile_status，结果为纯文本 idle、triggered、compiling、completed 或 up_to_date。没有脚本改动时直接返回 up_to_date，不会真的编译。
unity command editor.playmode start 与 unity command editor.playmode stop：进入与退出 Play 模式，用 editor status 的 playMode 字段确认（playing 或 stopped）。本项目进入 Play 模式约需 9 秒。
unity command capture game_view --max_resolution 960 --json：截 Game 视图，PNG 以 base64 放在结果的 base64 字段中，由 agent 解码后保存在项目之外。
unity command script eval_file --file <绝对路径>.cs：执行文件中的 C# 代码，代码文件放在项目之外。代码写成方法体，用 return 返回结果；编辑器会自动加上 System、System.Collections.Generic、System.Linq、UnityEngine 与 UnityEditor 的 using，项目脚本中的类型可以直接使用，私有字段用反射读取。结果在返回 JSON 的 result 字段中，编译错误在 diagnostics 字段中。
未验证：script eval --code（直接传一行代码）、runtime simulate_key（模拟 Input System 按键，可用来按 Local Replay 的 A、B 键）、test run、diagnostics get_performance。

## 注意
capture 的 save_path 按 Assets 目录解析，例如 Temp/x.png 会写到 Assets/Temp/x.png，并被 Unity 当作资产导入。不要使用 save_path，改用上面的 base64 方式。
script eval、script eval_file 与 script run 会在编辑器内执行任意 C#。编辑器默认允许，只靠令牌保护：所有请求都要带编辑器启动时生成的令牌，令牌保存在 Library/Pipeline/.unity-pipeline-port 中（Library 不进版本库），由 CLI 自动读取。允许 agent 使用，理由见 DECISIONS.md D-007；它与 editor save_all，以及 asset、settings、prefab、scene 等分组中会写入的命令，都按 AGENTS.md 项目特有规则中操作编辑器的那一条处理。
eval 的超时只作用于编译阶段，执行阶段不受限制。代码在主线程上运行，避免无上限的循环和阻塞调用，否则会卡住编辑器。
每次 eval 会在本机 Library/Pipeline/eval-usage.jsonl 记一条使用统计，默认不记代码原文，不向外发送。
在 Play 模式中驱动 Local Replay 跑完，会在项目根目录的 ObserverLogs 下生成观察者日志，这些是测试产物，在报告中列出。不触发录制与同步，它们会写 RecordData。
Pipeline 包自带文档，位于 Library/PackageCache/com.unity.pipeline@*/Documentation~。
