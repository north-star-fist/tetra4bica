using System.IO;
using UnityEngine;
using static Sergei.Safonov.Persistence.ISaveManager;

namespace Sergei.Safonov.Persistence {

    /// <summary>
    /// Save manager that saves everything as JSON files and storing them in Application.persistentDataPath.
    /// </summary>
    public class JsonFileSaveManager : ISaveManager {
        private const string JsonExt = ".json";

        private readonly string _dataRootFolder;

        public JsonFileSaveManager(string rootFolderName = "game_data") {
            _dataRootFolder = rootFolderName;
        }

        public LoadResult<T> Load<T>(string key) {
            var saveLocation = GetFilePath(key);

            if (!File.Exists(saveLocation)) {
                return LoadResult<T>.Fail;
            }

            var objString = File.ReadAllText(saveLocation);
            return new LoadResult<T>(JsonUtility.FromJson<T>(objString));
        }

        public void Save<T>(string key, T value) {
            var objString = JsonUtility.ToJson(value, true);
            File.WriteAllText(GetFilePath(key), objString);
        }

        private string GetFilePath(string key) {
            checkDirectory(key);
            return Path.Combine(GetPersistentDataLocation(), $"{key}{JsonExt}");

            void checkDirectory(string key) {
                var filePath = Path.Combine(GetPersistentDataLocation(), $"{key}{JsonExt}");
                var fName = Path.GetFileName(filePath);
                CreateDirectoryIfAbsent(filePath.Substring(0, filePath.Length - fName.Length));
            }
        }

        private string GetPersistentDataLocation() {
            string gameDataLocation = Path.Combine(Application.persistentDataPath, _dataRootFolder);
            CreateDirectoryIfAbsent(gameDataLocation);
            return gameDataLocation;
        }

        private static void CreateDirectoryIfAbsent(string dirPath) {
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}
