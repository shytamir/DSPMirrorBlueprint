using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

namespace DSPMirrorBlueprint
{
    [BepInPlugin(PluginGuid, PluginName, BuildVersion.BepInPluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dspmirrorblueprint";
        public const string PluginName = "DSP Mirror Blueprint";
        public const string PluginVersion = BuildVersion.PluginVersion;

        private ConfigEntry<bool> enableGeometryDump;
        private ConfigEntry<KeyboardShortcut> geometryDumpKey;
        private ConfigEntry<bool> enableInputDiagnostics;
        private Harmony harmony;

        private void Awake()
        {
            enableGeometryDump = Config.Bind(
                "Diagnostics",
                "EnableGeometryDump",
                false,
                "Allow an explicit keypress to save active blueprint geometry for development diagnostics."
            );
            geometryDumpKey = Config.Bind(
                "Diagnostics",
                "GeometryDumpKey",
                new KeyboardShortcut(KeyCode.F9),
                "Save active blueprint geometry while blueprint deployment is open."
            );
            enableInputDiagnostics = Config.Bind(
                "Diagnostics",
                "EnableInputDiagnostics",
                false,
                "Log DSP-captured K and Shift+K mirror events and their deployment result."
            );

            harmony = new Harmony(PluginGuid);
            string mirrorError;
            bool mirrorInstalled = BlueprintRuntimeMirror.Install(
                harmony,
                Logger,
                () => enableInputDiagnostics.Value,
                out mirrorError);

            if (!mirrorInstalled)
                Logger.LogError("Blueprint mirroring is unavailable: " + mirrorError);

            Logger.LogInfo(
                PluginName + " " + PluginVersion + " loaded. " +
                (mirrorInstalled
                    ? "Press K for horizontal mirror or Shift+K for vertical mirror. "
                    : String.Empty) +
                "Geometry dumps are " +
                (enableGeometryDump.Value ? "enabled on " + geometryDumpKey.Value + "." : "disabled.") +
                " Input diagnostics are " +
                (enableInputDiagnostics.Value ? "enabled." : "disabled."));
        }

        private void OnDestroy()
        {
            BlueprintRuntimeMirror.Uninstall();
            if (harmony != null) harmony.UnpatchSelf();
        }

        private void Update()
        {
            try
            {
                if (!enableGeometryDump.Value || !geometryDumpKey.Value.IsDown())
                    return;

                string path;
                string error;
                if (BlueprintGeometryDumper.TryDump(out path, out error))
                    Logger.LogInfo("Blueprint geometry exported: " + path);
                else
                    Logger.LogWarning("Blueprint geometry export skipped: " + error);
            }
            catch (Exception ex)
            {
                Logger.LogError("Blueprint geometry export failed: " + ex);
            }
        }
    }
}
