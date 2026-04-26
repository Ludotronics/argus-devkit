// Argus SDK — ArgusPostBuildProcessor
// Automatically strips Test-mode code from Release (distribution) builds.
// Runs after every build via IPostprocessBuildWithReport.
//
// Behaviour:
//   - If build target is Release (not Development), ensures ARGUS_STRIP_TEST
//     is added to scripting defines, which causes ArgusSession to disable
//     Test mode at runtime even if the config says Test.
//   - Emits a build report warning if an ArgusConfig with mode=Test is found
//     in the build output.

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Argus.SDK.Editor
{
    public class ArgusPostBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            bool isRelease = !report.summary.options.HasFlag(BuildOptions.Development);

            if (isRelease)
            {
                // Ensure ARGUS_STRIP_TEST define is present for the target group
                var target = report.summary.platformGroup;
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
                if (!defines.Contains("ARGUS_STRIP_TEST"))
                {
                    defines += ";ARGUS_STRIP_TEST";
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                    Debug.Log("[Argus] Added ARGUS_STRIP_TEST define for release build.");
                }

                // Warn if a Test-mode config is found in the build
                var configs = Resources.FindObjectsOfTypeAll<ArgusConfig>();
                foreach (var cfg in configs)
                {
                    if (cfg.mode == ArgusMode.Test)
                    {
                        Debug.LogWarning(
                            $"[Argus] ArgusConfig '{cfg.name}' is set to Test mode but this is a Release build. " +
                            "The ARGUS_STRIP_TEST define will disable Test mode at runtime, but consider " +
                            "using a separate config asset for production.");
                    }
                }
            }
        }
    }
}
#endif
