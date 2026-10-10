#nullable enable
using System.IO;
using UnityEngine;

namespace Statecraft.Game
{
    /// <summary>Where the autosave lives (spec section 7: autosave every turn). On WebGL it is a fixed folder under the
    /// /idbfs IndexedDB mount, not Application.persistentDataPath, because that path hashes the page's URL and itch.io
    /// serves every upload from a new URL, which would hide old saves (tech notes section 3). The template sets
    /// autoSyncPersistentDataPath, so closing the file persists it. Writes go to a .tmp file first, then replace.</summary>
    public sealed class SaveStore
    {
        public const string WebDirectory = "/idbfs/statecraft-saves";
        public const string AutosaveName = "autosave.txt";

        public string Directory { get; }
        public SaveStore(string directory) { Directory = directory; }

        public static SaveStore ForPlatform() => new SaveStore(Application.platform == RuntimePlatform.WebGLPlayer
            ? WebDirectory : Path.Combine(Application.persistentDataPath, "saves"));

        public string AutosavePath => Path.Combine(Directory, AutosaveName);

        public void WriteAutosave(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = AutosavePath + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(AutosavePath)) File.Delete(AutosavePath);
            File.Move(tmp, AutosavePath);
        }

        public string? ReadAutosave() => File.Exists(AutosavePath) ? File.ReadAllText(AutosavePath) : null;

        /// <summary>Moves an unreadable autosave aside (autosave.bad.txt) so it is kept for a bug report.</summary>
        public void SetAsideAutosave()
        {
            if (!File.Exists(AutosavePath)) return;
            var bad = Path.Combine(Directory, "autosave.bad.txt");
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(AutosavePath, bad);
        }

        public void DeleteAutosave() { if (File.Exists(AutosavePath)) File.Delete(AutosavePath); }
    }
}
