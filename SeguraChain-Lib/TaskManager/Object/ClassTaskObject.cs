using SeguraChain_Lib.Other.Object.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.TaskManager.Object
{
    public class ClassTaskObject
    {
        public long Id { get; }
        public bool Started { get; private set; }
        public bool Disposed { get; set; }

        private readonly Action _action;
        public Task Task { get; private set; }
        public CancellationTokenSource Cancellation { get; }
        public long TimestampEnd { get; }
        public ClassCustomSocket Socket { get; }

        // Générateur thread-safe d'IDs aléatoires
        private static long _idCounter = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassTaskObject(Action action, CancellationTokenSource cancellation, long timestampEnd, ClassCustomSocket socket)
        {
            // Utilisation d'Interlocked pour générer des IDs uniques sans contention
            Id = Interlocked.Increment(ref _idCounter);

            _action = action ?? throw new ArgumentNullException(nameof(action));
            Cancellation = cancellation;
            TimestampEnd = timestampEnd;
            Socket = socket;
        }

        /// <summary>
        /// Run the action into a task.
        /// </summary>
        public void Run()
        {
            if (Started)
                return;

            try
            {
                Task = Task.Factory.StartNew(
                    _action,
                    Cancellation.Token,
                    TaskCreationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.Current);

                Started = true;
            }
            catch
            {
                // Catch the exception, once the task is cancelled.
                Started = true; // Marquer comme démarré même en cas d'erreur pour éviter les tentatives répétées
            }
        }
    }
}