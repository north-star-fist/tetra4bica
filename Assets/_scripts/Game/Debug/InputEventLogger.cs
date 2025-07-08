using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Debugging
{
    public class InputEventLogger : MonoBehaviour
    {

        [SerializeField, Tooltip("Relative path to file in persistent storage"), FormerlySerializedAs("logFilePath")]
        private string _logFilePath;

        [Inject]
        private IGameInputEventProvider _eventProvider;

        private FileStream _logFileStream;
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
            _eventProvider.GetInputStream().Subscribe(logEvent);
        }

        void logEvent(IGameInputEvent e)
        {
            if (_logFileStream != null)
            {
                _formatter.Serialize(_logFileStream, e);
            }
        }

        private void OnDestroy()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }

        ~InputEventLogger()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Close();
            }
        }
    }
}
