ShareView 项目说明

每轮对话开始前，先重新查看 README、核心场景、关键脚本、Packages/manifest.json 和 ProjectSettings/ProjectVersion.txt。不要只依赖历史记忆。

项目目标是 Unity VR 中的共享视觉任务模拟和晕动症减轻实验。核心场景是 Assets/MirrorExamplesVR/Scenes/SceneVR-UnityDemo/SceneVR-UnityDemo.unity。

当前 Unity 版本是 6000.3.10f1。主要依赖是 Mirror、Meta XR SDK、XR Interaction Toolkit、URP、TextMesh Pro 和 Newtonsoft Json。目标平台以 Meta Quest Android 和 Windows Standalone 为主。

Assets/MirrorExamplesVR 是主项目目录，Scripts 内是核心脚本，Scenes/SceneVR-UnityDemo 是核心场景，Prefabs、Materials、Textures 放玩家、交互物、遮罩和渲染资源。Assets/RecordData 是实验记录数据，已被 .gitignore 排除，文件很大，除非任务明确要求不要修改。Packages 和 ProjectSettings 是依赖与 Unity 配置。Library、Logs、UserSettings 是生成目录，不作为主要修改位置。

关键脚本：
VRHostCameraControl.cs 处理共享视觉核心逻辑，包括主相机、子相机、FOV 相机、FPS 切换、遮罩、边框提示、记录回放和箱体同步。
ServerActionRecording.cs 处理相机和箱体动作的记录、加载、JSON 存储以及记录列表同步。
VRCanvasHUD.cs、VRNetworkDiscovery.cs、VRNetworkManager.cs 处理 Host、Server、Client、网络发现和 Mirror 管理。
VRNetworkPlayerScript.cs、VRPlayerRig.cs、VRNetworkInteractable.cs、VRWeapon.cs 处理联网玩家、头手追踪、交互物和武器。
OverlayImageRendererFeature.cs 处理 URP 遮罩叠加。FPSDisplay.cs 辅助显示帧率。

修改前先判断任务属于共享视觉、晕动症缓解、网络同步、记录回放还是交互玩法。涉及 Mirror 时分清 isServer、isClient、Host 和纯 Client。涉及相机或遮罩时检查 MainCamera、Sub Camera、FOVCamera、TrackedPoseDriver、XR Origin、Image_Mask、White_Circle、Mark 和 border 的关系。

保留 Unity 的 .meta 文件和资产 GUID。不要随意改动 Assets/RecordData 下的 JSON 或压缩包。完成后尽量用 Unity 打开核心场景或运行可用的编译、测试检查；如果无法验证，要说明原因和剩余风险。
