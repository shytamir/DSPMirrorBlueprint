using BepInEx;

namespace DSPMirrorBlueprint
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.shytamir.dspmirrorblueprint";
        public const string PluginName = "DSP Mirror Blueprint";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }
    }
}
