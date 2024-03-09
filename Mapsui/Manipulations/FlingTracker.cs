﻿#pragma warning disable IDE0005
using System;
using System.Collections.Generic;

namespace Mapsui.Manipulations;

public class FlingTracker
{
    private const int _maxSize = 50;
    private const long _maxTicks = 200 * 10000;  // Use only events from the last 200 ms

    private readonly Dictionary<long, Queue<(double x, double y, long time)>> _events;

    public FlingTracker()
    {
        _events = [];
    }

    public void AddEvent(long id, ScreenPosition position, long ticks)
    {
        // Save event data
        if (!_events.TryGetValue(id, out var value))
        {
            value = new Queue<(double x, double y, long time)>();
            _events.Add(id, value);
        }

        value.Enqueue((position.X, position.Y, ticks));

        // Check, if we at the end of array
        if (value.Count > 2)
        {
            while (value.Count > _maxSize || value.Peek().time < ticks - _maxTicks)
                value.Dequeue();
        }
    }

    // STOP TRACKING THIS ONE
    public void RemoveId(long id)
    {
        _events.Remove(id);
    }

    public void Restart()
    {
        _events.Clear();
    }

    private (double vx, double vy) CalcVelocity(long id, long now)
    {
        double distanceX = 0;
        double distanceY = 0;

        if (!_events.TryGetValue(id, out var eventItem) || eventItem.Count < 2)
            return (0d, 0d);

        var eventQueue = eventItem;
        var eventsArray = eventQueue.ToArray();

        (_, _, var firstTime) = eventsArray[0];

        long finalTime = 0;

        for (var i = 1; i < eventsArray.Length; i++)
        {
            (var lastX, var lastY, var lastTime) = eventsArray[i - 1];
            (var nowX, var nowY, var nowTime) = eventsArray[i];

            // Only calc velocities for last maxTicks ticks
            if (now - lastTime < _maxTicks)
            {
                // Calc velocity in pixel per sec
                distanceX += (nowX - lastX) * 10000000;// / (nowTime - lastTime);
                distanceY += (nowY - lastY) * 10000000;// / (nowTime - lastTime);
            }

            finalTime = nowTime;
        }

        var totalTime = finalTime - firstTime;

        return (distanceX / totalTime, distanceY / totalTime);
    }

    public void IfFling(long eventId, Action<double, double> onFling)
    {
        var (velocityX, velocityY) = CalcVelocity(eventId, DateTime.Now.Ticks);

        if (Math.Abs(velocityX) <= 200 && Math.Abs(velocityY) <= 200)
            return;

        onFling(velocityX, velocityY);
    }
}