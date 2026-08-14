using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CyberTokyo.Editor
{
    /// <summary>
    /// 一键固化双端打包配置。放进代码而不是手点 Inspector：换机器、重开项目、
    /// 或者哪天设置被误改，重跑一次菜单就能回到已知状态。
    /// </summary>
    public static class BuildSetupTool
    {
        private const string BundleId = "com.sunandsong.cybertokyoflyingchess";

        [MenuItem("Tools/Cyber Tokyo/Configure Build Settings")]
        public static void Configure()
        {
            PlayerSettings.companyName = "sunandsong";
            PlayerSettings.productName = "Cyber Tokyo Flying Chess";
            PlayerSettings.bundleVersion = "0.1.0";

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);

            // 双手持机的棋盘游戏，竖屏锁定
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Android：商店要求 AAB + ARM64，ARM64 必须 IL2CPP（Mono 只出 ARMv7）
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Unity 6.3 的底线就是 25（再低的枚举已废弃），跟着底线走
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            // 只有 Game 一个场景进包，SampleScene 不带
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildSetupTool] 打包配置就绪: {BundleId} v{PlayerSettings.bundleVersion}, " +
                      "竖屏, Android IL2CPP/ARM64, 构建场景=Game.unity");
        }

        /*
         * iOS 目标 SDK 切换。模拟器包和真机包是两种二进制，Xcode 工程要在切换后重新 Build。
         * 模拟器：不需要签名/Apple ID/真机，且共享 Mac 网络（localhost 后台直接可用）。
         */

        [MenuItem("Tools/Cyber Tokyo/iOS Target - Simulator")]
        public static void TargetSimulator()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            Debug.Log("[BuildSetupTool] iOS 目标 = 模拟器。重新 Build 后在 Xcode 里选一个 iPhone 模拟器运行");
        }

        [MenuItem("Tools/Cyber Tokyo/iOS Target - Device")]
        public static void TargetDevice()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            Debug.Log("[BuildSetupTool] iOS 目标 = 真机。需要在 Xcode 里配签名（免费 Apple ID 即可）");
        }

        /// <summary>
        /// 模拟器包一条龙：配置 + 切模拟器 SDK + 出 Xcode 工程到 Builds/iOS。
        /// 编辑器里点菜单能用，命令行批处理也能用：
        ///   Unity -batchmode -quit -projectPath &lt;path&gt; -buildTarget iOS \
        ///     -executeMethod CyberTokyo.Editor.BuildSetupTool.BuildIOSSimulator
        /// 失败时抛异常 —— 批处理模式下这样才有非零退出码，外面的脚本能感知。
        /// </summary>
        public static void BuildIOSSimulator()
        {
            // 场景是 Phase3SceneBuilder 的生成物，出包前重建一次，保证场景装配
            // 逻辑的改动（如新挂组件）不会因为忘了手动重跑菜单而漏进包里
            Phase3SceneBuilder.Build();

            Configure();
            TargetSimulator();

            var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            });

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"[BuildSetupTool] iOS 模拟器包构建失败: {report.summary.result}");
            }
            Debug.Log("[BuildSetupTool] Xcode 工程已生成: Builds/iOS");
        }
    }
}
