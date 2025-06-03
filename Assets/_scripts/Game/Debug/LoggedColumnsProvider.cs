using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tetra4bica.Debugging
{
    public class LoggedColumnsProvider : CustomCellColumnGeneratorComponent
    {

        [SerializeField, Tooltip("Relative path to file in persistent storage"), FormerlySerializedAs("logFilePath")]
        private string _logFilePath;

        private FileStream _logFileStream;
        private readonly IFormatter _formatter = new BinaryFormatter();

        private bool _finished;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                Debug.LogWarning("Event log file path is undefined!");
                return;
            }
            try
            {
                _logFileStream = new FileStream(
                    Application.persistentDataPath + Path.DirectorySeparatorChar + _logFilePath,
                    FileMode.Open
                );
            } catch
            {
                Debug.LogWarning("Could not open event log file!");
                _finished = true;
            }
        }

        override public void GenerateCells(CellColor[] arrayToFill)
        {
            IEnumerable<CellColor> readCells = readFrameEvents();
            Array.Copy(readCells.ToArray(), arrayToFill, Math.Min(readCells.ToArray().Length, arrayToFill.Length));
        }


        private IEnumerable<CellColor> readFrameEvents()
        {
            if (_finished)
            {
                return Enumerable.Empty<CellColor>();
            }
            if (_logFileStream != null && _logFileStream.Position < _logFileStream.Length)
            {
                try
                {
                    var cellList = _formatter.Deserialize(_logFileStream);
                    return (IEnumerable<CellColor>)cellList;
                } catch
                {
                    _finished = true;
                }
            }
            _finished = true;
            return Enumerable.Empty<CellColor>();
        }

        private void OnDestroy()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }

        ~LoggedColumnsProvider()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }
    }
}
