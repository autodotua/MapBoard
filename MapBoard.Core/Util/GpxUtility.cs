using MapBoard.IO.Gpx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapBoard.Util
{
    public static class GpxUtility
    {
        public static void Smooth(GpxPointCollection points, int level, Func<GpxPoint, double> get, Action<GpxPoint, double> set)
        {
            int count = points.Count;
            Queue<double> queue = new Queue<double>(level);

            for (int headIndex = 0; headIndex < count; headIndex++)
            {
                GpxPoint headPoint = points[headIndex];
                if (queue.Count == level)
                {
                    queue.Dequeue();
                }
                queue.Enqueue(get(headPoint));
                if (headIndex < level)
                {
                    set(points[headIndex / 2], queue.Average());
                }
                else
                {
                    set(points[headIndex - level / 2], queue.Average());
                }
            }
            for (int tailIndex = count - level; tailIndex < count - 1; tailIndex++)
            {
                queue.Dequeue();
                set(points[tailIndex + (count - tailIndex) / 2], queue.Average());
            }
        }
    }
}