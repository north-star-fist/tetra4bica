using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.Debugging
{

    public class ScrollColumnLogger : MonoBehaviour
    {

        [SerializeField, Tooltip("Relative path to file in persistent storage"), FormerlySerializedAs("logFilePath")]
        private string _logFilePath;

        [Inject]
        private IGameEvents _gameEvents;

        FileStream _logFileStream;
        private readonly IFormatter _formatter = new BinaryFormatter();

        private void Awake()
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                Debug.LogWarning("Event log file path is undefined!");
                return;
            }

            _logFileStream = new FileStream(
                Application.persistentDataPath + Path.DirectorySeparatorChar + _logFilePath,
                FileMode.Create
            );
            _gameEvents.TableScrollStream.Subscribe(wall => logWall(wall));
        }

        void logWall(IEnumerable<CellColor?> wall)
        {
            if (_logFileStream != null)
            {
                _formatter.Serialize(_logFileStream, wall);
            }
        }

        private void OnDestroy()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }

        ~ScrollColumnLogger()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }
    }
}
