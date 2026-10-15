using Splatform;

namespace minortom.Continue;

internal static class SessionStore
{
    private const string Key = "minortom.Continue.LastSession";

    public static LastSession Load()
    {
        var stored = LastSessionFormat.Read(PlatformPrefs.GetString(Key, string.Empty));
        if (stored != null)
        {
            return stored;
        }

        return FromVanillaPrefs();
    }

    public static void Save(LastSession session)
    {
        if (session is null || !session.IsComplete)
        {
            return;
        }

        PlatformPrefs.SetString(Key, LastSessionFormat.Write(session));
        PlatformPrefs.Save();
        Plugin.Log?.LogInfo($"Recorded session {session}.");
    }

    public static bool HasAnySession()
    {
        return Load() != null;
    }

    private static LastSession FromVanillaPrefs()
    {
        var profile = PlatformPrefs.GetString("profile", string.Empty);
        var world = PlatformPrefs.GetString("world", string.Empty);
        if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(world))
        {
            return null;
        }

        var source = FileHelpers.FileSource.Local.ToString();
        return LastSession.LocalWorld(
            profile,
            source,
            world,
            source,
            openServer: false,
            publicServer: false,
            crossplay: false);
    }
}
