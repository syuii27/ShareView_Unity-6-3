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
unity command script recompile：强制重新编译，之后轮询 unity command script recompile_status，结果为纯文本 idle、triggered、compiling、completed 或 up_to_date。编辑器不在前台时也能发现新增或改动的脚本，本项目一次增量编译约 10 秒。有错误时结果为 completed, N error: 加上 文件(行,列): error CSxxxx 与说明；console status 中 compilationFailed 为 true，console get --level error 能读到同样的条目。没有脚本改动时直接返回 up_to_date，不会真的编译。
unity command editor.playmode start 与 unity command editor.playmode stop：进入与退出 Play 模式，用 editor status 的 playMode 字段确认（playing 或 stopped）。本项目进入 Play 模式约需 9 秒。
unity command capture game_view --max_resolution 960 --json：截 Game 视图，PNG 以 base64 放在结果的 base64 字段中，由 agent 解码后保存在项目之外。加 --source camera --camera "Sub Camera" 可单独渲染指定相机。
unity command script eval_file --file <绝对路径>.cs：执行文件中的 C# 代码，代码文件放在项目之外。代码写成方法体，用 return 返回结果；编辑器会自动加上 System、System.Collections.Generic、System.Linq、UnityEngine 与 UnityEditor 的 using，项目脚本中的类型可以直接使用，私有字段用反射读取。结果在返回 JSON 的 result 字段中，编译错误在 diagnostics 字段中。
unity command script eval --code "<代码>"：直接传一段代码。Windows PowerShell 5.1 会弄乱传给外部程序的参数中嵌套的双引号，代码里有字符串字面量时改用 eval_file。
unity command diagnostics get_performance：帧时间（CPU、GPU、主线程）、draw call 与内存。
未验证：test list 与 test run（项目目前没有测试）。

## 用 eval 驱动 Local Replay
以下做法于 2026-10-24 跑通，都是 Play 模式内的临时操作，退出 Play 模式后场景未被标记为已修改。
进入回放：在 Play 模式中调用场景里 VRCanvasHUD 的 ButtonLocalReplay()，约 1 秒后完成初始化。
主实例：VRCanvasHUD 的 hostCamera 字段就是回放使用的 VRHostCameraControl。
切换遮罩：VRHostCameraControl 的私有字段 mask（TMP_Dropdown），value 为 0 到 4，依次对应 masktype 0、6、8、7、5（NoMask、SimpleMask、FovOnly、PointOnly、FullMask）；设置 value 会触发与界面操作相同的处理。
切换帧率：VRHostCameraControl 的私有字段 dropdownfps（TMP_Dropdown），value 0 到 5 依次为 3、5、15、25、35、13FPS，Local Replay 默认为 5；设置 value 会触发与界面操作相同的处理（2026-10-24 核对）。
播放与进度：调用 PlaytheRecord() 播放下拉框所选记录；IsReplayPlaying 表示是否在播，私有字段 index 为当前帧号，masktype 与 recordtype 为当前遮罩与记录。
数据：ServerActionRecording 的 NumOfRecords() 与 Numofactions(记录号) 给出记录数与帧数，两段录制的帧数与回放时长见 docs/architecture.md 的数据路径。播完在 ObserverLogs 生成四个文件，SessionMeta 中 sampleCount 与 recordedFrameCount 相等。
runtime simulate_key 返回成功，但应用没有反应。项目的 Input System 设置 editorInputBehaviorInPlayMode 为 PointersAndKeyboardsRespectGameViewFocus，推断是 Game 视图没有焦点时键盘输入不送进游戏（未在 Game 视图有焦点时复测）。驱动流程用上面的方法调用，不用模拟按键。

## 注意
capture 的 save_path 按 Assets 目录解析，例如 Temp/x.png 会写到 Assets/Temp/x.png，并被 Unity 当作资产导入。不要使用 save_path，改用上面的 base64 方式。
script eval、script eval_file 与 script run 会在编辑器内执行任意 C#。编辑器默认允许，只靠令牌保护：所有请求都要带编辑器启动时生成的令牌，令牌保存在 Library/Pipeline/.unity-pipeline-port 中（Library 不进版本库），由 CLI 自动读取。允许 agent 使用，理由见 DECISIONS.md D-007；它与 editor save_all，以及 asset、settings、prefab、scene 等分组中会写入的命令，都按 AGENTS.md 项目特有规则中操作编辑器的那一条处理。
eval 的超时只作用于编译阶段，执行阶段不受限制。代码在主线程上运行，避免无上限的循环和阻塞调用，否则会卡住编辑器。
每次 eval 会在本机 Library/Pipeline/eval-usage.jsonl 记一条使用统计，默认不记代码原文，不向外发送。
在 Play 模式中驱动 Local Replay 跑完，会在项目根目录的 ObserverLogs 下生成观察者日志，这些是测试产物，在报告中列出。不触发录制与同步，它们会写 RecordData。
Pipeline 包自带文档，位于 Library/PackageCache/com.unity.pipeline@*/Documentation~。
