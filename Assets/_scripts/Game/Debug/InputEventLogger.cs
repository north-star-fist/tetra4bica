using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Debugging
{
    public class InputEventLogger : MonoBehaviour
    {

        [SerializeField, Tooltip("Relative path to file in persistent storage")]
        private string logFilePath;

        [Inject]
        IGameInputEventProvider eventProvider;

        FileStream logFileStream;
        readonly IFormatter formatter = new BinaryFormatter();

        private void Awake()
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Debug.LogWarning("Event log file path is undefined!");
                return;
            }
            logFileStream = new FileStream(
                Application.persistentDataPath + Path.DirectorySeparatorChar + logFilePath,
                FileMode.Create
            );
            eventProvider.GetInputStream().Subscribe(logEvent);
        }

        void logEvent(IGameInputEvent e)
        {
            if (logFileStream != null)
            {
                formatter.Serialize(logFileStream, e);
            }
        }

        private void OnDestroy()
        {
            if (logFileStream != null)
            {
                logFileStream.Close();
            }
        }

        ~InputEventLogger()
        {
            if (logFileStream != null)
            {
                logFileStream.Close();
            }
        }
    }
}
