using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using Zenject;

public class ScrollColumnLogger : MonoBehaviour {

    [Tooltip("Relative path to file in persistent storage")]
    public string logFilePath;

    [Inject]
    IGameEvents gameEvents;

    FileStream logFileStream;
    readonly IFormatter formatter = new BinaryFormatter();

    private void Awake() {
        if (string.IsNullOrEmpty(logFilePath)) {
            Debug.LogWarning("Event log file path is undefined!");
            return;
        }

        logFileStream = new FileStream(
            Application.persistentDataPath + Path.DirectorySeparatorChar + logFilePath,
            FileMode.Create
        );
        gameEvents.TableScrollStream.Subscribe(wall => logWall(wall));
    }

    void logWall(IEnumerable<CellColor> wall) {
        if (logFileStream != null) {
            formatter.Serialize(logFileStream, wall);
        }
    }

    private void OnDestroy() {
        if (logFileStream != null) {
            logFileStream.Close();
        }
    }

    ~ScrollColumnLogger() {
        if (logFileStream != null) {
            logFileStream.Close();
        }
    }
}
