using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GUIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace minortom.Continue;

internal static class ContinueLauncher
{
    private static Button _busyButton;

    public static PlayerProfile FindProfile(IList<PlayerProfile> profiles, LastSession session)
    {
        if (profiles is null || session is null)
        {
            return null;
        }

        foreach (var profile in profiles)
        {
            if (profile.GetFilename() == session.CharacterFile
                && profile.m_fileSource.ToString() == session.CharacterSource)
            {
                return profile;
            }
        }

        // Vanilla-prefs fallback may only have a filename.
        foreach (var profile in profiles)
        {
            if (profile.GetFilename() == session.CharacterFile)
            {
                return profile;
            }
        }

        return null;
    }

    public static World FindWorld(LastSession session)
    {
        if (session is null)
        {
            return null;
        }

        var worlds = SaveSystem.GetWorldList();
        var exact = worlds.Find(w =>
            w.m_name == session.WorldName
            && w.m_fileSource.ToString() == session.WorldSource
            && w.m_dataError == World.SaveDataError.None);
        if (exact != null)
        {
            return exact;
        }

        return worlds.Find(w => w.m_name == session.WorldName && w.m_dataError == World.SaveDataError.None);
    }

    public static void Resume(FejdStartup menu, LastSession session, Button button)
    {
        _busyButton = button;
        if (_busyButton != null)
        {
            _busyButton.interactable = false;
        }

        try
        {
            if (session is null || !session.IsComplete)
            {
                Fail(menu, "There is nothing to continue yet.");
                return;
            }

            var profiles = SaveSystem.GetAllPlayerProfiles();
            var profile = FindProfile(profiles, session);
            if (profile is null)
            {
                Fail(menu, "The saved character was not found.");
                return;
            }

            menu.m_profiles = profiles;
            menu.m_profileIndex = profiles.IndexOf(profile);
            menu.SelectCharacter(profile.GetFilename(), profile.m_fileSource);

            if (session.Kind == SessionKind.LocalWorld)
            {
                StartLocalWorld(menu, session);
            }
            else
            {
                JoinServer(menu, session);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Continue failed: {ex}");
            Fail(menu, "Could not continue. Start the game normally.");
        }
    }

    private static void StartLocalWorld(FejdStartup menu, LastSession session)
    {
        var world = FindWorld(session);
        if (world is null)
        {
            Fail(menu, "The saved world can no longer be loaded.");
            return;
        }

        menu.m_world = world;
        SetToggle(menu.m_openServerToggle, session.OpenServer);
        SetToggle(menu.m_publicServerToggle, session.PublicServer);
        SetToggle(menu.m_crossplayServerToggle, session.Crossplay);

        if (menu.m_serverPassword != null)
        {
            menu.m_serverPassword.text = string.Empty;
        }

        Plugin.Log?.LogInfo($"Continuing {session}.");
        menu.OnWorldStart();
        menu.StartCoroutine(ReenableAfterDelay(_busyButton));
    }

    private static void JoinServer(FejdStartup menu, LastSession session)
    {
        switch (session.ServerKind)
        {
            case ServerKind.Dedicated:
                Join(menu, new ServerJoinData(new ServerJoinDataDedicated(session.ServerAddress)), session);
                break;
            case ServerKind.SteamUser:
                if (!ulong.TryParse(session.ServerAddress, NumberStyles.None, CultureInfo.InvariantCulture, out var steamId))
                {
                    Fail(menu, "The saved Steam server id is invalid.");
                    return;
                }

                Join(menu, new ServerJoinData(new ServerJoinDataSteamUser(steamId)), session);
                break;
            case ServerKind.PlayFab:
            {
                var byId = new ServerJoinData(new ServerJoinDataPlayFabUser(session.ServerAddress));
                if (session.JoinCode.Length == 0 || !PlayFabManager.IsLoggedIn)
                {
                    Join(menu, byId, session);
                    return;
                }

                ZPlayFabMatchmaking.ResolveJoinCode(
                    session.JoinCode,
                    data => Join(menu, new ServerJoinData(new ServerJoinDataPlayFabUser(data.remotePlayerId)), session),
                    reason =>
                    {
                        Plugin.Log?.LogInfo(
                            $"Join code {session.JoinCode} failed ({reason}); trying saved server id.");
                        Join(menu, byId, session);
                    });
                break;
            }
            default:
                Fail(menu, "Unknown saved server type.");
                break;
        }
    }

    private static void Join(FejdStartup menu, ServerJoinData data, LastSession session)
    {
        if (menu is null || !menu.m_mainMenu.activeInHierarchy)
        {
            Plugin.Log?.LogInfo($"{session} resolved after leaving the main menu; not joining.");
            if (menu != null)
            {
                menu.StartCoroutine(ReenableAfterDelay(_busyButton));
            }

            return;
        }

        if (!data.IsValid)
        {
            Fail(menu, "The saved server address is not usable.");
            return;
        }

        Plugin.Log?.LogInfo($"Continuing {session}.");
        menu.SetServerToJoin(data);
        menu.JoinServer();
        menu.StartCoroutine(ReenableAfterDelay(_busyButton));
    }

    private static void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private static void Fail(FejdStartup menu, string message)
    {
        Plugin.Log?.LogWarning($"Continue: {message}");
        ShowWarning(message);
        if (_busyButton != null)
        {
            _busyButton.interactable = true;
        }
    }

    private static void ShowWarning(string message)
    {
        try
        {
            var title = Plugin.ButtonLabel?.Value ?? "Continue";
            UnifiedPopup.Push(new WarningPopup(title, message, () => UnifiedPopup.Pop(), false));
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Could not show Continue dialog: {ex.Message}");
        }
    }

    private static IEnumerator ReenableAfterDelay(Button button)
    {
        yield return new WaitForSecondsRealtime(3f);
        if (button != null)
        {
            button.interactable = true;
        }
    }
}
