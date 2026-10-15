using HarmonyLib;

namespace minortom.Continue;

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui))]
internal static class SetupGuiPatch
{
    private static void Postfix(FejdStartup __instance)
    {
        ContinueButton.TryAdd(__instance);
    }
}

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnWorldStart))]
internal static class OnWorldStartPatch
{
    private static void Prefix(FejdStartup __instance)
    {
        SessionRecorder.RecordLocalWorld(__instance);
    }
}

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.JoinServer))]
internal static class JoinServerPatch
{
    private static void Prefix(FejdStartup __instance)
    {
        SessionRecorder.RecordServerJoin(__instance);
    }
}
