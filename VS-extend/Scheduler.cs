using System.Threading;
using System;

public class Scheduler
{
    private Timer _timer;
    private TimerCallback _timerCallback;
    private object _state;
    private int _dueTime;
    private int _period;

    public Scheduler(Action callback, object state, int dueTime, int period)
    {
        _timerCallback = new TimerCallback((obj) => callback());
        _state = state;
        _dueTime = dueTime;
        _period = period;
    }
    public void StartTask()
    {
        _timer = new Timer(_timerCallback, _state, _dueTime, _period);
    }

    public void StopTask()
    {
        _timer?.Dispose();
    }
}