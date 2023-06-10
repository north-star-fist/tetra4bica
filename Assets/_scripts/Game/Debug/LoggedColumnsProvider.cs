using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;

public class LoggedColumnsProvider : CustomCellColumnGeneratorComponent {

    [Tooltip("Relative path to file in persistent storage")]
    public string logFilePath;

    FileStream logFileStream;
    readonly IFormatter formatter = new BinaryFormatter();

    bool finished;

    private void Awake() {
        if (string.IsNullOrEmpty(logFilePath)) {
            Debug.LogWarning("Event log file path is undefined!");
            return;
        }
        try {
            logFileStream = new FileStream(
                Application.persistentDataPath + Path.DirectorySeparatorChar + logFilePath,
                FileMode.Open
            );
        } catch {
            Debug.LogWarning("Could not open event log file!");
            finished = true;
        }
    }

    override public void GenerateCells(CellColor[] arrayToFill) {
        IEnumerable<CellColor> readCells = readFrameEvents();
        Array.Copy(readCells.ToArray(), arrayToFill, Math.Min(readCells.ToArray().Length, arrayToFill.Length));
    }


    private IEnumerable<CellColor> readFrameEvents() {
        if (finished) {
            return Enumerable.Empty<CellColor>();
        }
        if (logFileStream != null && logFileStream.Position < logFileStream.Length) {
            try {
                var cellList = formatter.Deserialize(logFileStream);
                return (IEnumerable<CellColor>)cellList;
            } catch {
                finished = true;
            }
        }
        finished = true;
        return Enumerable.Empty<CellColor>();
    }

    private void OnDestroy() {
        if (logFileStream != null) {
            logFileStream.Close();
        }
    }

    ~LoggedColumnsProvider() {
        if (logFileStream != null) {
            logFileStream.Close();
        }
    }
}
