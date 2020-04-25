using System.Diagnostics;
using UnityEngine;

public class FrameTimer : MonoBehaviour {
    private Stopwatch stopwatch;

    public long FrameDuration {
        get {
            if (this.stopwatch == null)
                return 0;
            else
                return this.stopwatch.ElapsedMilliseconds;
        }
    }

    void Awake() {
        this.stopwatch = new Stopwatch();
        this.stopwatch.Start();
    }

    void Update() {
        // For whatever reason, .Restart() wasn't recognized.
        this.stopwatch.Reset();
        this.stopwatch.Start();
    }
}
