using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using static Expression;

public abstract class Agent : MonoBehaviour
{
    public MentalState MentalState;
    public Actuator Actuator;

    public bool locked;

    protected Thread thread;

    protected Queue<Action> queue;

    protected virtual void Start()
    {
        queue = new Queue<Action>();
        thread = new Thread(() => {
            if (locked) {
                return;
            }
            while (true) {
                try {
                    while (queue.Count > 0) {
                        queue.Dequeue()();
                    }

                    if (Actuator != null && !Actuator.IsBusy()) {
                        queue.Enqueue(Actuator.ExecutePlan);
                    }

                    Thread.Sleep(16 * 30);
                } catch (NullReferenceException e) {}
            }
        });

        thread.Start();
    }

    public void EnqueueAction(Action action) {
        queue.Enqueue(action);
    }

    void OnApplicationQuit() {
        if (thread != null) {
            thread.Abort();
        }
    }
}
