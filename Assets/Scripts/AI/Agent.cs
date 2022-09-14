using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using static Expression;

public abstract class Agent : MonoBehaviour
{
    public MentalState MentalState;
    public Actuator Actuator;

    public Thread thread;

    protected virtual void Start()
    {
        thread = new Thread(() => {
            while (true) {
                Actuator.ExecutePlan();
                Thread.Sleep(2000);
            }
        });

        thread.Start();
    }

    void OnApplicationQuit() {
        if (thread != null) {
            thread.Abort();
        }
    }
}
