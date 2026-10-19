using System;
using UnityEngine;

[Serializable]
public class GameTimer
{
    [SerializeField, Min(1)] int duration = 120;

    [SerializeField] int remaining;

    public event Action<int> Ticked;
    public event Action Finished;

    public int Duration => duration;
    public int Remaining => remaining;
    public bool IsRunning { get; private set; }

    float elapsed;

    public void Begin()
    {
        remaining = duration;
        elapsed = 0f;
        IsRunning = true;
        Ticked?.Invoke(remaining);
    }

    public void Stop() => IsRunning = false;

    public void Tick(float deltaTime)
    {
        if (!IsRunning) return;

        elapsed += deltaTime;
        while (elapsed >= 1f && remaining > 0)
        {
            elapsed -= 1f;
            remaining--;
            Ticked?.Invoke(remaining);
        }

        if (remaining > 0) return;
        IsRunning = false;
        Finished?.Invoke();
    }
}
