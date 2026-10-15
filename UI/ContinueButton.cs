using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace minortom.Continue;

internal static class ContinueButton
{
    private const string ButtonName = "minortom_Continue";
    private const string SubtitleName = "minortom_ContinueSubtitle";

    public static void TryAdd(FejdStartup menu)
    {
        try
        {
            Add(menu);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"Could not add Continue button: {ex.Message}");
        }
    }

    private static void Add(FejdStartup menu)
    {
        var menuList = menu.m_menuList;
        if (menuList == null)
        {
            return;
        }

        var existing = menuList.GetComponentsInChildren<Button>(true);
        foreach (var button in existing)
        {
            if (button.name == ButtonName)
            {
                return;
            }
        }

        var session = SessionStore.Load();
        if (session is null)
        {
            Plugin.Log?.LogInfo("No previous session; Continue button hidden.");
            return;
        }

        var start = FindStartGameButton(existing);
        if (start == null)
        {
            Plugin.Log?.LogWarning("Could not find Start game button; Continue not added.");
            return;
        }

        var clone = UnityEngine.Object.Instantiate(start.gameObject, start.transform.parent);
        try
        {
            clone.name = ButtonName;
            clone.transform.SetSiblingIndex(start.transform.GetSiblingIndex());

            foreach (var localize in clone.GetComponentsInChildren<Localize>(true))
            {
                UnityEngine.Object.DestroyImmediate(localize);
            }

            var labels = clone.GetComponentsInChildren<TMP_Text>(true);
            var primaryLabel = labels.Length > 0 ? labels[0] : null;
            var buttonText = Plugin.ButtonLabel?.Value ?? "Continue";
            foreach (var label in labels)
            {
                label.text = buttonText;
                label.alignment = TextAlignmentOptions.Center;
                label.horizontalAlignment = HorizontalAlignmentOptions.Center;
                label.verticalAlignment = VerticalAlignmentOptions.Middle;
                label.margin = Vector4.zero;
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            var button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                var current = SessionStore.Load();
                ContinueLauncher.Resume(menu, current, button);
            });

            const float subtitleReserve = 22f;

            // Manual layout menus place Start at a fixed Y; shift Continue above it.
            if (start.transform.parent.GetComponent<LayoutGroup>() == null)
            {
                var startRect = start.GetComponent<RectTransform>();
                var continueRect = button.GetComponent<RectTransform>();
                var step = EstimateVerticalStep(existing, start, startRect);
                continueRect.anchoredPosition = startRect.anchoredPosition + new Vector2(0f, step + subtitleReserve);
            }

            AddSubtitle(clone, primaryLabel, session);

            menu.m_menuButtons = menuList.GetComponentsInChildren<Button>();
            WireNavigation(button, start);

            if (menu.m_merchStoreButton != null)
            {
                button.navigation = WithRight(button.navigation, menu.m_merchStoreButton);
            }

            Plugin.Log?.LogInfo($"Continue button added for {session}.");
        }
        catch
        {
            UnityEngine.Object.Destroy(clone);
            throw;
        }
    }

    private static void AddSubtitle(GameObject continueButton, TMP_Text styleSource, LastSession session)
    {
        if (styleSource == null || styleSource.font == null)
        {
            Plugin.Log?.LogWarning("Continue subtitle skipped; menu font was unavailable.");
            return;
        }

        var character = ResolveCharacterName(session);
        var destination = string.IsNullOrWhiteSpace(session.DisplayName)
            ? (session.Kind == SessionKind.LocalWorld ? session.WorldName : session.ServerAddress)
            : session.DisplayName;
        var subtitleText = Truncate($"{character} — {destination}", 48);

        // Clone the real menu label so we keep Valheim's TMP font asset.
        // Keep inactive until styled so TMP doesn't warn about LiberationSans.
        var go = UnityEngine.Object.Instantiate(styleSource.gameObject, continueButton.transform.parent);
        go.SetActive(false);
        go.name = SubtitleName;

        foreach (var localize in go.GetComponentsInChildren<Localize>(true))
        {
            UnityEngine.Object.DestroyImmediate(localize);
        }

        // Subtitle is not a button — strip interactables if the label lived under one.
        foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        var tmp = go.GetComponent<TMP_Text>() ?? go.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
        {
            UnityEngine.Object.Destroy(go);
            Plugin.Log?.LogWarning("Continue subtitle skipped; cloned label had no TMP_Text.");
            return;
        }

        tmp.font = styleSource.font;
        tmp.fontSharedMaterial = styleSource.fontSharedMaterial;
        tmp.fontSize = Mathf.Max(12f, styleSource.fontSize * 0.45f);
        tmp.color = new Color(1f, 1f, 1f, 0.6f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.margin = Vector4.zero;
        tmp.text = subtitleText;
        tmp.richText = false;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        var continueRect = continueButton.GetComponent<RectTransform>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = continueRect.anchorMin;
        rect.anchorMax = continueRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(continueRect.rect.width, 22f);
        rect.anchoredPosition = continueRect.anchoredPosition
                                - new Vector2(0f, continueRect.rect.height * 0.5f + 2f);

        go.transform.SetSiblingIndex(continueButton.transform.GetSiblingIndex() + 1);
        go.SetActive(true);
    }

    private static string ResolveCharacterName(LastSession session)
    {
        var profile = ContinueLauncher.FindProfile(SaveSystem.GetAllPlayerProfiles(), session);
        if (profile != null)
        {
            var name = profile.GetName();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return string.IsNullOrEmpty(session.CharacterFile) ? "?" : session.CharacterFile;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        var keep = Math.Max(1, maxLength - 1);
        if (char.IsHighSurrogate(text[keep - 1]))
        {
            keep--;
        }

        return text.Substring(0, keep) + "…";
    }

    private static float EstimateVerticalStep(Button[] buttons, Button start, RectTransform startRect)
    {
        var height = startRect.rect.height + 8f;
        var best = float.MaxValue;
        foreach (var other in buttons)
        {
            if (other.transform.parent != start.transform.parent)
            {
                continue;
            }

            var delta = Mathf.Abs(other.GetComponent<RectTransform>().anchoredPosition.y - startRect.anchoredPosition.y);
            if (delta > 1f)
            {
                best = Mathf.Min(best, delta);
            }
        }

        return best < float.MaxValue ? best : height;
    }

    private static void WireNavigation(Button continueButton, Button start)
    {
        var continueNav = continueButton.navigation;
        continueNav.selectOnDown = start;
        continueButton.navigation = continueNav;

        var startNav = start.navigation;
        startNav.selectOnUp = continueButton;
        start.navigation = startNav;
    }

    private static Navigation WithRight(Navigation navigation, Selectable right)
    {
        navigation.selectOnRight = right;
        return navigation;
    }

    private static Button FindStartGameButton(Button[] buttons)
    {
        foreach (var button in buttons)
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == "OnStartGame")
                {
                    return button;
                }
            }
        }

        foreach (var button in buttons)
        {
            if (button.name is "StartGame" or "Start")
            {
                return button;
            }
        }

        foreach (var button in buttons)
        {
            if (button.gameObject.activeInHierarchy)
            {
                return button;
            }
        }

        return null;
    }
}
