using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardGame.EditorTools
{
    // Headless project setup + Android build entry points, invoked by Claude Code via -executeMethod.
    public static class CIBuild
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string AppId = "com.vagabond.cardhigherlower";

        // Absolute <project>/Builds/CardGame.apk, independent of the editor's working directory.
        private static string ApkPath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, "Builds", "CardGame.apk");
            }
        }

        // Creates the single game scene and registers it in Build Settings.
        public static void SetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("Game");
            go.AddComponent<GameBootstrap>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("CIBuild: scene created at " + ScenePath);
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Vagabond";
            PlayerSettings.productName = "Card Higher Lower";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // Use Unity's auto-generated debug keystore — no signing setup needed for a sample build.
            PlayerSettings.Android.useCustomKeystore = false;
        }

        // Full Android build: configure, switch platform, produce an APK.
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("CIBuild: BUILD_FAILED Android build support is not installed/active in this editor.");
                EditorApplication.Exit(2);
                return;
            }

            SetupScene();
            ConfigurePlayer();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB
            string apk = ApkPath;
            Directory.CreateDirectory(Path.GetDirectoryName(apk));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            bool fileWritten = File.Exists(apk);

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && fileWritten)
            {
                long size = new FileInfo(apk).Length;
                Debug.Log($"CIBuild: BUILD_OK apk={apk} sizeBytes={size} time={summary.totalTime}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"CIBuild: BUILD_FAILED result={summary.result} errors={summary.totalErrors} fileWritten={fileWritten} apk={apk}");
                EditorApplication.Exit(1);
            }
        }
    }
}
