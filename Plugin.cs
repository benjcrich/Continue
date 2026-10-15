using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace minortom.Continue;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "minortom.Continue";
    public const string PluginName = "Continue";
    public const string PluginVersion = "1.0.0";

    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; }
    internal static ConfigEntry<string> ButtonLabel { get; private set; }

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        ButtonLabel = Config.Bind(
            "General",
            "ButtonLabel",
            "Continue",
            "Text shown on the main-menu Continue button.");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
