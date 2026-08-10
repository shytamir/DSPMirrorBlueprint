using BepInEx;
using BepInEx.Configuration;
using System;
using UnityEngine;

namespace DSPMirrorBlueprint
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.shytamir.dspmirrorblueprint";
        public const string PluginName = "DSP Mirror Blueprint";
        public const string PluginVersion = "0.2.0";

        private ConfigEntry<bool> enableGeometryDump;
        private ConfigEntry<KeyboardShortcut> geometryDumpKey;

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

            Logger.LogInfo(
                PluginName + " " + PluginVersion + " loaded. Geometry dumps are " +
                (enableGeometryDump.Value ? "enabled on " + geometryDumpKey.Value + "." : "disabled."));
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
