using System;
using System.Globalization;
using Splatform;
using UnityEngine;

namespace minortom.Continue;

internal static class SessionRecorder
{
    public static void RecordLocalWorld(FejdStartup startup)
    {
        try
        {
            var profile = GetSelectedProfile(startup);
            var world = startup.m_world;
            if (profile is null || world is null)
            {
                return;
            }

            var open = IsOn(startup.m_openServerToggle);
            var pub = IsOn(startup.m_publicServerToggle);
            var crossplay = IsOn(startup.m_crossplayServerToggle);

            SessionStore.Save(LastSession.LocalWorld(
                profile.GetFilename(),
                profile.m_fileSource.ToString(),
                world.m_name,
                world.m_fileSource.ToString(),
                open,
                pub,
                crossplay));
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Failed to record local session: {ex.Message}");
        }
    }

    public static void RecordServerJoin(FejdStartup startup)
    {
        try
        {
            if (!startup.HasServerToJoin())
            {
                return;
            }

            var profile = GetSelectedProfile(startup);
            if (profile is null)
            {
                return;
            }

            var data = startup.GetServerToJoin();
            if (!data.IsValid)
            {
                return;
            }

            if (!TryDescribeServer(data, out var kind, out var address, out var joinCode, out var displayName))
            {
                return;
            }

            SessionStore.Save(LastSession.Server(
                profile.GetFilename(),
                profile.m_fileSource.ToString(),
                kind,
                address,
                joinCode,
                displayName));
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Failed to record multiplayer session: {ex.Message}");
        }
    }

    private static PlayerProfile GetSelectedProfile(FejdStartup startup)
    {
        if (startup.m_profiles != null
            && startup.m_profileIndex >= 0
            && startup.m_profileIndex < startup.m_profiles.Count)
        {
            return startup.m_profiles[startup.m_profileIndex];
        }

        var filename = PlatformPrefs.GetString("profile", string.Empty);
        if (string.IsNullOrEmpty(filename))
        {
            return null;
        }

        return SaveSystem.GetAllPlayerProfiles()
            .Find(p => p.GetFilename() == filename);
    }

    private static bool IsOn(UnityEngine.UI.Toggle toggle)
    {
        return toggle != null && toggle.isOn;
    }

    private static bool TryDescribeServer(
        ServerJoinData data,
        out ServerKind kind,
        out string address,
        out string joinCode,
        out string displayName)
    {
        kind = ServerKind.None;
        address = string.Empty;
        joinCode = string.Empty;
        displayName = string.Empty;

        switch (data.m_type)
        {
            case ServerJoinDataType.Dedicated:
                kind = ServerKind.Dedicated;
                address = data.Dedicated.ToString();
                break;
            case ServerJoinDataType.SteamUser:
                kind = ServerKind.SteamUser;
                address = data.SteamUser.m_joinUserID.m_SteamID.ToString(CultureInfo.InvariantCulture);
                break;
            case ServerJoinDataType.PlayFabUser:
                kind = ServerKind.PlayFab;
                address = data.PlayFabUser.m_remotePlayerId;
                joinCode = MultiBackendMatchmaking.GetServerMatchmakingData(data, default).m_joinCode ?? string.Empty;
                break;
            default:
                return false;
        }

        displayName = MultiBackendMatchmaking.GetServerName(data);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = address;
        }

        return !string.IsNullOrEmpty(address);
    }
}
