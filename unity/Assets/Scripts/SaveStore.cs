#nullable enable
using System.IO;
using UnityEngine;

namespace Statecraft.Game
{
    /// <summary>Where the autosave lives (spec section 7: autosave every turn). On WebGL it is a fixed folder under the
    /// /idbfs IndexedDB mount, not Application.persistentDataPath, because that path hashes the page's URL and itch.io
    /// serves every upload from a new URL, which would hide old saves (tech notes section 3). The template sets
    /// autoSyncPersistentDataPath, so closing the file persists it. Writes go to a .tmp file first, then replace.</summary>
    public class SaveStore
    {
        public const string WebDirectory = "/idbfs/statecraft-saves";
        public const string AutosaveName = "autosave.txt";

        public string Directory { get; }
        public SaveStore(string directory) { Directory = directory; }

        public static SaveStore ForPlatform() => new SaveStore(DirectoryFor(Application.platform, Application.persistentDataPath));

        /// <summary>The save folder for a platform. On the web it is the fixed folder and ignores persistentDataPath, which
        /// is /idbfs/&lt;hash of the page URL&gt;.</summary>
        public static string DirectoryFor(RuntimePlatform platform, string persistentDataPath) =>
            platform == RuntimePlatform.WebGLPlayer ? WebDirectory : Path.Combine(persistentDataPath, "saves");

        public string AutosavePath => Path.Combine(Directory, AutosaveName);

        public virtual void WriteAutosave(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = AutosavePath + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(AutosavePath)) File.Delete(AutosavePath);
            File.Move(tmp, AutosavePath);
        }

        /// <summary>The autosave text, or null. WriteAutosave deletes the old file only after the .tmp is completely written, so
        /// a crash between the delete and the move leaves just the .tmp, which is complete: it is read and promoted. When both
        /// exist the .tmp may be partial and is ignored.</summary>
        public virtual string? ReadAutosave()
        {
            if (File.Exists(AutosavePath)) return File.ReadAllText(AutosavePath);
            var tmp = AutosavePath + ".tmp";
            if (!File.Exists(tmp)) return null;
            var text = File.ReadAllText(tmp);
            try { File.Move(tmp, AutosavePath); } catch (IOException) { }   // promoting is a convenience; the text is already read
            catch (System.UnauthorizedAccessException) { }
            return text;
        }

        /// <summary>Moves an unreadable autosave aside (autosave.bad.txt) so it is kept for a bug report.</summary>
        public virtual void SetAsideAutosave()
        {
            if (!File.Exists(AutosavePath)) return;
            var bad = Path.Combine(Directory, "autosave.bad.txt");
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(AutosavePath, bad);
        }

        public virtual void DeleteAutosave() { if (File.Exists(AutosavePath)) File.Delete(AutosavePath); }
    }
}
