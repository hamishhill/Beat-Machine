using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.Generic;
using NLog;

namespace BeatMachine
{
    public static class ExecutionQueue
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public enum Policy
        {
            Immediate,
            Queued
        }

        private static int period = 15;
        private static Timer timer = new Timer(new TimerCallback(Dequeue),
                null,
                TimeSpan.FromSeconds(period),
                TimeSpan.FromSeconds(period));
        private static List<WaitCallback> queue = new List<WaitCallback>();
        private static bool cooledDown = false;

        /// <summary>
        /// Delay between invoking WaitCallbacks passed to Enqueue when the
        /// policy is Queued, measured in seconds.
        /// </summary>
        public static int Period
        {
            get { return period; }
            set { period = value; }
        }
            
        public static void Dequeue(object stateInfo)
        {
            lock (queue)
            {
                if (queue.Count != 0)
                {
                    logger.Info("Dequeuing and running callback {0}",
                        queue[0].Method.Name);
                    ThreadPool.QueueUserWorkItem(queue[0]);
                    cooledDown = false;
                    queue.RemoveAt(0);
                }
                else
                {
                    // When we try to dequeue and the queue is empty, that 
                    // means a full period has passed until we last 
                    // executed an item
                    cooledDown = true;
                }
            }
        }

        public static void Enqueue(WaitCallback callback, Policy policy)
        {
            switch(policy)
            {
                case Policy.Immediate:
                    logger.Info("Immediately running callback {0}",
                        callback.Method.Name);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(callback));
                    break;
                case Policy.Queued:
                    lock (queue)
                    {
                        if (cooledDown)
                        {
                            logger.Info("Immediately putting callback {0}",
                                callback.Method.Name);
                            ThreadPool.QueueUserWorkItem(
                                new WaitCallback(callback));
                        }
                        else
                        {
                            logger.Debug("Queuing callback {0} for later execution",
                                callback.Method.Name);
                            queue.Add(callback);
                        }
                    }
                    break;
            }
        }

    }
}
