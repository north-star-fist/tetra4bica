using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tetra4bica.Debugging
{

    public class LoggedEventsProvider : CustomGameInputEventsProviderComponent
    {

        [SerializeField, Tooltip("Relative path to file in persistent storage"), FormerlySerializedAs("logFilePath")]
        private string _logFilePath;

        override public IObservable<IGameInputEvent> GetInputStream() => _input;

        const string ON = "On";
        const string OFF = "Off";

        private readonly IFormatter _formatter = new BinaryFormatter();

        private readonly ISubject<IGameInputEvent> _input = new Subject<IGameInputEvent>();

        private readonly IList<IGameInputEvent> _gameEventList = new List<IGameInputEvent>();

        private uint _frameNumber;
        private int _totalFrameNumber;

        private int _eventIndex;
        private float _actualTime;
        private float _loggedTime;

        private bool _autoplay;

        private bool _error;

        private string _goToFrameText;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                Debug.LogWarning("Event log file path is undefined!");
                return;
            }
            using (FileStream logFileStream = new FileStream(
                Application.persistentDataPath + Path.DirectorySeparatorChar + _logFilePath,
                FileMode.Open
            ))
            {
                try
                {
                    IGameInputEvent inputEvent = null;
                    do
                    {
                        var e = _formatter.Deserialize(logFileStream);
                        inputEvent = (IGameInputEvent)e;
                        _gameEventList.Add(inputEvent);
                        if (inputEvent is FrameUpdateEvent)
                        {
                            _totalFrameNumber++;
                        }
                    } while (logFileStream.Position < logFileStream.Length);
                } catch
                {
                    _error = true;
                }
            }
        }

        void OnGUI()
        {

            GUI.Label(new Rect(10, 10, 150, 50), $"Frame {_frameNumber.ToString()}");

            if (_error)
            {
                return;
            }

            if (GUI.Button(new Rect(10, 70, 110, 50), "Frame ->"))
            {
                readFrameEvents();
            }
            if (GUI.Button(new Rect(10, 130, 110, 50), "10 Frames =>"))
            {
                goFurther(10);
            }
            if (GUI.Button(new Rect(10, 190, 110, 50), "100 Frames ==>"))
            {
                goFurther(100);
            }

            _goToFrameText = GUI.TextField(new Rect(10, 250, 110, 50), _goToFrameText);
            if (GUI.Button(new Rect(120, 250, 50, 50), "==>>"))
            {
                if (uint.TryParse(_goToFrameText, out var goToFrame))
                {
                    if (goToFrame > _frameNumber)
                    {
                        goFurther(goToFrame - _frameNumber);
                    }
                }
            }

            var onOff = _autoplay ? ON : OFF;
            if (GUI.Button(new Rect(10, 310, 150, 50), $"Autoplay is {onOff}"))
            {
                _autoplay = !_autoplay;
            }
        }


        void Update()
        {
            if (_error)
            {
                return;
            }
            if (_frameNumber >= _totalFrameNumber || !_autoplay)
            {
                return;
            }
            _actualTime += Time.deltaTime;
            while (_loggedTime < _actualTime && _frameNumber < _totalFrameNumber)
            {
                _loggedTime += readFrameEvents();
            }
        }



        private float readFrameEvents()
        {
            if (_frameNumber >= _totalFrameNumber || _eventIndex >= _gameEventList.Count)
            {
                return 0;
            }
            float delta = 0;
            IGameInputEvent inputEvent = null;
            FrameUpdateEvent frameEvent = null;
            do
            {
                inputEvent = _gameEventList[_eventIndex];
                _input.OnNext(inputEvent);
                frameEvent = inputEvent as FrameUpdateEvent;
                _eventIndex++;
            } while (frameEvent == null && _eventIndex < _gameEventList.Count);
            if (frameEvent != null)
            {
                delta = frameEvent.DeltaTime;
                _frameNumber++;
            }
            return delta;
        }

        private void goFurther(uint framesDelta)
        {
            for (int i = 0; i < framesDelta; i++)
            {
                if (readFrameEvents() == 0)
                {
                    return;
                }
            }
        }
    }
}
