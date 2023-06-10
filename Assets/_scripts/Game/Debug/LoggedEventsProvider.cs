using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UniRx;
using UnityEngine;

public class LoggedEventsProvider : CustomGameInputEventsProviderComponent {

    [Tooltip("Relative path to file in persistent storage")]
    public string logFilePath;

    override public IObservable<IGameInputEvent> GetInputStream() => _input;

    const string ON = "On";
    const string OFF = "Off";

    readonly IFormatter formatter = new BinaryFormatter();

    ISubject<IGameInputEvent> _input = new Subject<IGameInputEvent>();

    IList<IGameInputEvent> gameEventList = new List<IGameInputEvent>();

    uint frameNumber;
    int totalFrameNumber;

    int eventIndex;
    float actualTime;
    float loggedTime;

    bool autoplay;

    bool error;

    private string goToFrameText;

    private void Awake() {
        if (string.IsNullOrEmpty(logFilePath)) {
            Debug.LogWarning("Event log file path is undefined!");
            return;
        }
        using (FileStream logFileStream = new FileStream(
            Application.persistentDataPath + Path.DirectorySeparatorChar + logFilePath,
            FileMode.Open
        )) {
            try {
                IGameInputEvent inputEvent = null;
                do {
                    var e = formatter.Deserialize(logFileStream);
                    inputEvent = (IGameInputEvent)e;
                    gameEventList.Add(inputEvent);
                    if (inputEvent is FrameUpdateEvent) {
                        totalFrameNumber++;
                    }
                } while (logFileStream.Position < logFileStream.Length);
            } catch {
                error = true;
            }
        }
    }

    void OnGUI() {

        GUI.Label(new Rect(10, 10, 150, 50), $"Frame {frameNumber.ToString()}");

        if (error) {
            return;
        }

        if (GUI.Button(new Rect(10, 70, 110, 50), "Frame ->")) {
            readFrameEvents();
        }
        if (GUI.Button(new Rect(10, 130, 110, 50), "10 Frames =>")) {
            goFurther(10);
        }
        if (GUI.Button(new Rect(10, 190, 110, 50), "100 Frames ==>")) {
            goFurther(100);
        }

        goToFrameText = GUI.TextField(new Rect(10, 250, 110, 50), goToFrameText);
        if (GUI.Button(new Rect(120, 250, 50, 50), "==>>")) {
            if (uint.TryParse(goToFrameText, out var goToFrame)) {
                if (goToFrame > frameNumber) {
                    goFurther(goToFrame - frameNumber);
                }
            }
        }

        var onOff = autoplay ? ON : OFF;
        if (GUI.Button(new Rect(10, 310, 150, 50), $"Autoplay is {onOff}")) {
            autoplay = !autoplay;
        }
    }


    void Update() {
        if (error) {
            return;
        }
        if (frameNumber >= totalFrameNumber || !autoplay) {
            return;
        }
        actualTime += Time.deltaTime;
        while (loggedTime < actualTime && frameNumber < totalFrameNumber) {
            loggedTime += readFrameEvents();
        }
    }



    private float readFrameEvents() {
        if (frameNumber >= totalFrameNumber || eventIndex >= gameEventList.Count) {
            return 0;
        }
        float delta = 0;
        IGameInputEvent inputEvent = null;
        FrameUpdateEvent frameEvent = null;
        do {
            var e = gameEventList[eventIndex];
            inputEvent = (IGameInputEvent)e;
            _input.OnNext(inputEvent);
            frameEvent = inputEvent as FrameUpdateEvent;
            eventIndex++;
        } while (frameEvent == null && eventIndex < gameEventList.Count);
        if (frameEvent != null) {
            delta = frameEvent.deltaTime;
            frameNumber++;
        }
        return delta;
    }

    private void goFurther(uint framesDelta) {
        for (int i = 0; i < framesDelta; i++) {
            if (readFrameEvents() == 0) {
                return;
            }
        }
    }
}
