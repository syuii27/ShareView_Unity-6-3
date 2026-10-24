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
Local Replay 不启动任何 Mirror 角色，isServer 与 isClient 都为 false，由 VRCanvasHUD 直接调用 ServerActionRecording 与 VRHostCameraControl 的本地初始化。入口为 HUD 的 Local Replay 按钮，或右手柄 A 与键盘 A；进入后，未在播放时再按 A 回到第 0 帧，按 B 播放下拉框所选的记录。

## 核心场景中的相机与遮罩
VRHostCameraControl 与 ServerActionRecording 在核心场景中各有两个实例。引用完整的一组都在 Interactable Kinematic Torus（fileID 6559198439968760670）上；另外两个实例在其他对象上，缺少 White_Circle、border、syncButton 等引用。Local Replay 由 VRCanvasHUD 按引用是否完整打分，选出主实例。
主实例的字段与场景对象：subcamera 为 Sub Camera，fovcamera 为 FOVCamera，Image_Mask 为 Image_Mask，Image_Mask1 为 RawImage，White_Circle 为 WhiteCircle，Mark 为 Mark，border 为 Border，endBlackMask 为 EndBlackMask。
主相机带 MainCamera 标签与 TrackedPoseDriver，位于预制体 Complete XR Origin Set Up 嵌套的 XR Origin Preconfigured 中。场景中所有 Canvas 都是 World Space。

## 数据路径
录制：写入 Application.dataPath/RecordData，编辑器中即 Assets/RecordData；文件为 MainPosData.json、MainRotData.json、BoxPosData.json、BoxRotData.json。
联网回放：Client 播放完一条记录时，把观察者位姿写入 Application.dataPath/RecordData 下的 PaPosData.json 与 PaRotData.json。
本地回放：读取 Application.streamingAssetsPath/RecordData 下与录制同名的四个文件。播放完一条记录时写观察者日志到 Application.dataPath 上一级目录中的 ObserverLogs：编辑器中为项目根目录，Windows 构建中为 exe 所在目录。文件为 PaPosData_、PaRotData_、PaFrameIndex_ 与 SessionMeta_，后缀为记录序号与时间戳。
