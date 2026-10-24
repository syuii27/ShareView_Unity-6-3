# 架构

记录目录、关键脚本职责、运行模式、核心场景对象与数据路径。改脚本、场景、网络或数据格式前读。

## 目录
Assets/MirrorExamplesVR：主目录。Scripts 为核心脚本，Scenes/SceneVR-UnityDemo 为核心场景，Prefabs、Materials、Textures、Shader、Sounds 为玩家、交互物、遮罩与渲染资源。
Assets/RecordData 与 Assets/StreamingAssets/RecordData：实验录制数据，不进版本库，见 README.md。
Assets/Mirror：Mirror 网络库源码。Assets/Samples/XR Interaction Toolkit/2.3.2：XRI Starter Assets 样例，核心场景的 XR Origin 来自这里。
Packages 与 ProjectSettings：依赖与 Unity 配置。Library、Logs、Temp、UserSettings 为 Unity 生成的目录，不修改。
Tools/RecordDataVisualizer：录制数据质量检查工具（.NET 10 控制台程序）。用法为 dotnet run --project Tools/RecordDataVisualizer -- [输入目录] [输出 HTML]，默认读取 Assets/RecordData，并把 HTML 写在输入目录中；为免在录制数据目录中新增文件，运行时用第二个参数指定其他输出路径。
Build Settings 中只启用核心场景 Assets/MirrorExamplesVR/Scenes/SceneVR-UnityDemo/SceneVR-UnityDemo.unity。

## 关键脚本
位于 Assets/MirrorExamplesVR/Scripts。改动前先判断所属领域，再读对应脚本。
共享视觉与晕动症缓解：VRHostCameraControl.cs，含主相机、子相机、FOV 相机、FPS 切换、遮罩、边框提示、运动补偿点、本地回放播放、观察者日志与箱体同步。
记录与回放数据：ServerActionRecording.cs，含相机与箱体动作的记录、加载、JSON 存储与记录列表同步。
网络：VRCanvasHUD.cs（Host、Server、Client、Auto 与 Local Replay 的入口）、VRNetworkDiscovery.cs、VRNetworkManager.cs（在 VRNetworkDiscovery.prefab 上）、VRNetworkScene.cs。
玩家与交互：VRNetworkPlayerScript.cs（在 VRPlayerPrefab 上）、VRPlayerRig.cs、VRNetworkInteractable.cs（核心场景中有 10 个实例）、VRWeapon.cs（只在枪类预制体上，不在核心场景中）。
渲染辅助：OverlayImageRendererFeature.cs 为 URP 遮罩叠加，目前没有挂到任何 URP Renderer，不生效；FPSDisplay.cs 为帧率显示，目前没有被任何场景、预制体或资产引用。

## 运行模式
Host、Server、Client 由 VRCanvasHUD 启动。Client 在局域网中发现服务器后自动连接发现的第一个；Auto 先搜索约 3 秒，找不到就以 Host 启动。
实时数据流：Host 每帧用 ClientRpc（SyncTransform）发送主相机姿态，Mirror 按 sendRate 批量发出（场景中覆盖为 60，预制体默认为 30）；Client 把收到的姿态存入 syncedRotation，到显示步才复制到 TempRotation 并显示，所以最新收到的姿态领先画面 0 至 1 个显示步。运动补偿点目前用的是 TempRotation。
Local Replay 不启动任何 Mirror 角色，isServer 与 isClient 都为 false，由 VRCanvasHUD 直接调用 ServerActionRecording 与 VRHostCameraControl 的本地初始化。入口为 HUD 的 Local Replay 按钮，或右手柄 A 与键盘 A；进入后，未在播放时再按 A 回到第 0 帧，按 B 播放下拉框所选的记录。
实验用法（用户，2026-10-24）：实验只用 Local Replay，固定 13FPS（DECISIONS.md D-009；此前为 15FPS）。帧率下拉框依次为 3、5、15、25、35、13FPS，13FPS 追加在末尾（索引 5），是 Local Replay 的默认值。构建 Windows Standalone 的 exe，把整个构建文件夹拷到实验用的 Windows 电脑，经 Quest Link 串流到头显；头显本身算力不足，不用 Android 构建。录制今后基本不再使用，只回放现有的两段录制，见数据路径。

## 核心场景中的相机与遮罩
VRHostCameraControl 与 ServerActionRecording 在核心场景中各有两个实例。引用完整的一组都在 Interactable Kinematic Torus（fileID 6559198439968760670）上；另外两个实例在其他对象上，缺少 White_Circle、border、syncButton 等引用，其中另一个 VRHostCameraControl 组件处于禁用状态。Local Replay 由 VRCanvasHUD 按引用是否完整打分，选出主实例。
主实例的字段与场景对象：subcamera 为 Sub Camera，fovcamera 为 FOVCamera，Image_Mask 为 Image_Mask，Image_Mask1 为 RawImage，White_Circle 为 WhiteCircle，Mark 为 Mark，border 为 Border，endBlackMask 为 EndBlackMask。
主相机带 MainCamera 标签与 TrackedPoseDriver，位于预制体 Complete XR Origin Set Up 嵌套的 XR Origin Preconfigured 中。场景中所有 Canvas 都是 World Space。
Local Replay 播放时主相机由记录驱动，TrackedPoseDriver 关闭，不跟随头显。Sub Camera 挂在 XR Origin 的 CameraOffset 下，在复制的 SubRoom 中渲染：FovOnly、PointOnly、FullMask 下代码把它放在操作者位置加 SubCameraRoomOffset (6.5, 0, 42)，NoMask 与 SimpleMask 下不更新位置；旋转由它的 TrackedPoseDriver（只跟随旋转）取参与者的头部转动。Sub Camera 输出到 Render_tex（2048×2048，深度格式为 None），Render_tex 经 LimFov3.mat（着色器 TransparentCircle，挖空中央椭圆）显示在 Image_Mask1 上。Image_Mask1 所在的 Canvas 是主相机的子对象，所以椭圆内是操作者视角，椭圆外是随参与者转头的 SubRoom 画面；PointOnly 下关闭这张 RawImage，只保留其下的运动补偿点。
运动补偿点按操作者与参与者头部转动之差算出相对运动方向，并沿该方向移动，用于提示相对运动的风险；这个方向是设计意图（用户确认，2026-10-24）。提示算法须按实时系统设计，只用显示时刻已有的数据，见 DECISIONS.md D-008。
运动补偿点的现状（改动前；2026-10-24 读代码、场景与离线计算，经独立复算）：点平面距主相机约 0.278 m，起点离心度 20.7° 至 35.4°，视野中心处点径约 2.88°，贴图为不透明的白环黑心。约每 1/60 秒调用一次，72、90、120 Hz 渲染下实际间隔为 27.8、22.2、16.7 ms；每次比较视线变化角是否达到 2.7°，不是角速度门限，所以参与者自己转头要达到约 97、122、162 °/s 才触发，插值回放下极少触发。每次移动约为该步转角的 0.27 倍，累计只有约 0.12 倍；约每 1.5 秒全部清空一次。
评估中的运动补偿点（2026-10-24 加入，原 PointOnly 不变，作对照）：遮罩编号 9 至 12，即下拉框索引 5 至 8 的 PointA Always、PointA Gated、PointB Always、PointB Gated，相机与遮罩设置与 PointOnly 相同。点的移动量为画面当前显示的操作者朝向变化减去参与者头部转动（TempRotation 与 Sub Camera 的旋转），每帧更新并轻度平滑。A 为中央 22° 以外、45° 以内、约 16° 间距的六边形点阵，整体流动、环绕，并在环带边缘渐隐，流速约为相对转速的 0.8 倍，透明度约 0.5；B 沿用原 8 点位置，偏移累积并约 0.75 秒回位，流速约 0.4 倍。Gated 按平滑后的相对转速出现：起始门限 20°/s，低于 10°/s 保持 0.5 秒后淡出，淡入 0.15 秒、淡出 0.3 秒，隐藏时回到起始排布；起始门限来自两段录制上的离线探查。参数在 VRHostCameraControl 的 Inspector 中可调，所用值写进 SessionMeta 的 dotCue；回放结束时这些点隐藏，不显示在黑屏上。

## 数据路径
录制：写入 Application.dataPath/RecordData，编辑器中即 Assets/RecordData；文件为 MainPosData.json、MainRotData.json、BoxPosData.json、BoxRotData.json。
联网回放：Client 播放完一条记录时，把观察者位姿写入 Application.dataPath/RecordData 下的 PaPosData.json 与 PaRotData.json。
本地回放：读取 Application.streamingAssetsPath/RecordData 下与录制同名的四个文件。播放完一条记录时写观察者日志到 Application.dataPath 上一级目录中的 ObserverLogs：编辑器中为项目根目录，Windows 构建中为 exe 所在目录。文件为 PaPosData_、PaRotData_、PaFrameIndex_ 与 SessionMeta_，后缀为记录序号与时间戳。
两段录制：StreamingAssets/RecordData 设计上一次只放一段，每个构建只含一段（用户，2026-10-24）。现有两段在 Assets/RecordData/1（3776 帧）与 Assets/RecordData/2（3443 帧），各含一段（2026-10-24 解析文件得到）。录制文件不带时间戳。按代码，回放每步的时刻按步长累加；每步前进 1 帧且渲染帧率高于步速时，可见的运动时长为（帧数 − 1）/ 步速，15FPS 下约 251.7 秒与 229.5 秒，13FPS 下约 290.4 秒与 264.8 秒（录制 1 在编辑器中实测 durationSeconds 为 290.52 秒，2026-10-24），最后一帧与结束黑屏在同一渲染帧出现。823b782（2026-06-10）之前，每步把下一步的时刻重设为当前时刻加间隔，每步占整数个渲染帧；而且 a75a0d4（2026-06-10 14:18）之前，15FPS 档的实际间隔是 1/17 秒。按代码模拟（2026-10-24，未在设备上核对），这些早期构建的回放时长：渲染 45、60、90、120 fps 时与现在相同；72 fps 时多 4%（262.2 秒与 239.0 秒）；36 至 37 fps 时多 22% 至 25%（约 306 至 315 秒与 279 至 287 秒），动作也相应变慢；80 fps 时少 6%。d47c984（2026-06-10 15:16）之后的构建在 SessionMeta 中记有 durationSeconds，可直接核对。
两段录制约 32% 的帧与前一帧完全相同（位置与旋转同时重复，多为每 3 帧重复 1 帧），15FPS 回放时每秒约 10.2 个不同姿态；做插值或平滑回放要先跳过重复帧。箱子数据的重复与相机的重复只在约 75% 至 79% 的帧上一致，插值时箱子要按自身数据处理。录制结束时有 20 个箱子离开了初始位置，循环、往返或拼接回放会让箱子瞬移或倒放。以上为 2026-10-24 离线计算并经独立复算；调研记录按用户决定为临时文件，未留存。
观察者日志的内容：SessionMeta 的 playbackSpeed 为每秒前进的录制帧数除以名义录制频率 15，1 为原速，13FPS 下约 0.867，即本地回放比录制慢；实时模式中 13FPS 只降低画面更新频率，动作仍为真实速度。PaPos 为记录中的操作者位置；PaRot 为 Sub Camera 的世界旋转，即参与者的头部朝向，含进入回放时的水平对齐；参与者的头部位置目前不记录。后缀中的记录序号总是 Rec1，区分两段录制看日志所在的构建文件夹或 SessionMeta 的 recordedFrameCount。日志写到 exe 同级的 ObserverLogs，用户已在实验电脑上确认（2026-10-24）。
