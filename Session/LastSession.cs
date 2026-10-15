using System;
using System.Collections.Generic;
using System.Text;

namespace minortom.Continue;

internal enum SessionKind
{
    LocalWorld,
    Server
}

internal enum ServerKind
{
    None,
    Dedicated,
    SteamUser,
    PlayFab
}

internal sealed class LastSession : IEquatable<LastSession>
{
    public SessionKind Kind { get; }
    public string CharacterFile { get; }
    public string CharacterSource { get; }
    public string WorldName { get; }
    public string WorldSource { get; }
    public ServerKind ServerKind { get; }
    public string ServerAddress { get; }
    public string JoinCode { get; }
    public string DisplayName { get; }
    public bool OpenServer { get; }
    public bool PublicServer { get; }
    public bool Crossplay { get; }

    public bool IsComplete
    {
        get
        {
            if (string.IsNullOrEmpty(CharacterFile) || string.IsNullOrEmpty(CharacterSource))
            {
                return false;
            }

            if (Kind == SessionKind.LocalWorld)
            {
                return !string.IsNullOrEmpty(WorldName) && !string.IsNullOrEmpty(WorldSource);
            }

            return ServerKind != ServerKind.None && !string.IsNullOrEmpty(ServerAddress);
        }
    }

    private LastSession(
        SessionKind kind,
        string characterFile,
        string characterSource,
        string worldName,
        string worldSource,
        ServerKind serverKind,
        string serverAddress,
        string joinCode,
        string displayName,
        bool openServer,
        bool publicServer,
        bool crossplay)
    {
        Kind = kind;
        CharacterFile = characterFile ?? string.Empty;
        CharacterSource = characterSource ?? string.Empty;
        WorldName = worldName ?? string.Empty;
        WorldSource = worldSource ?? string.Empty;
        ServerKind = serverKind;
        ServerAddress = serverAddress ?? string.Empty;
        JoinCode = joinCode ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        OpenServer = openServer;
        PublicServer = publicServer;
        Crossplay = crossplay;
    }

    public static LastSession LocalWorld(
        string characterFile,
        string characterSource,
        string worldName,
        string worldSource,
        bool openServer,
        bool publicServer,
        bool crossplay)
    {
        return new LastSession(
            SessionKind.LocalWorld,
            characterFile,
            characterSource,
            worldName,
            worldSource,
            ServerKind.None,
            string.Empty,
            string.Empty,
            worldName,
            openServer,
            publicServer,
            crossplay);
    }

    public static LastSession Server(
        string characterFile,
        string characterSource,
        ServerKind serverKind,
        string serverAddress,
        string joinCode,
        string displayName)
    {
        return new LastSession(
            SessionKind.Server,
            characterFile,
            characterSource,
            string.Empty,
            string.Empty,
            serverKind,
            serverAddress,
            joinCode,
            string.IsNullOrEmpty(displayName) ? serverAddress : displayName,
            openServer: false,
            publicServer: false,
            crossplay: false);
    }

    public bool Equals(LastSession other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind
               && CharacterFile == other.CharacterFile
               && CharacterSource == other.CharacterSource
               && WorldName == other.WorldName
               && WorldSource == other.WorldSource
               && ServerKind == other.ServerKind
               && ServerAddress == other.ServerAddress
               && JoinCode == other.JoinCode
               && DisplayName == other.DisplayName
               && OpenServer == other.OpenServer
               && PublicServer == other.PublicServer
               && Crossplay == other.Crossplay;
    }

    public override bool Equals(object obj) => Equals(obj as LastSession);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind;
            hash = (hash * 397) ^ CharacterFile.GetHashCode();
            hash = (hash * 397) ^ WorldName.GetHashCode();
            hash = (hash * 397) ^ ServerAddress.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => $"{Kind} '{DisplayName}' as {CharacterFile}";
}

internal static class LastSessionFormat
{
    private const string Version = "2";
    private const char Separator = '|';
    private const char Escape = '\\';
    private const int FieldCount = 13;

    public static string Write(LastSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var fields = new[]
        {
            Version,
            session.Kind.ToString(),
            session.CharacterFile,
            session.CharacterSource,
            session.WorldName,
            session.WorldSource,
            session.ServerKind.ToString(),
            session.ServerAddress,
            session.JoinCode,
            session.DisplayName,
            session.OpenServer ? "1" : "0",
            session.PublicServer ? "1" : "0",
            session.Crossplay ? "1" : "0"
        };

        var builder = new StringBuilder();
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(Separator);
            }

            foreach (var c in fields[i])
            {
                if (c is Separator or Escape)
                {
                    builder.Append(Escape);
                }

                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    public static LastSession Read(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var fields = Split(text);
        if (fields is null || fields.Count != FieldCount || fields[0] != Version)
        {
            return null;
        }

        if (!TryParseEnum(fields[1], out SessionKind kind))
        {
            return null;
        }

        if (!TryParseEnum(fields[6], out ServerKind serverKind))
        {
            return null;
        }

        var open = fields[10] == "1";
        var pub = fields[11] == "1";
        var crossplay = fields[12] == "1";

        LastSession session = kind == SessionKind.LocalWorld
            ? LastSession.LocalWorld(fields[2], fields[3], fields[4], fields[5], open, pub, crossplay)
            : LastSession.Server(fields[2], fields[3], serverKind, fields[7], fields[8], fields[9]);

        return session.IsComplete ? session : null;
    }

    private static bool TryParseEnum<T>(string text, out T value) where T : struct
    {
        value = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (Array.IndexOf(Enum.GetNames(typeof(T)), text) < 0)
        {
            return false;
        }

        return Enum.TryParse(text, ignoreCase: false, out value);
    }

    private static List<string> Split(string text)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case Escape:
                    if (i + 1 >= text.Length)
                    {
                        return null;
                    }

                    current.Append(text[++i]);
                    break;
                case Separator:
                    fields.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
