using System;
using System.Diagnostics;
using System.Timers;

namespace Nexus.Classes
{
    /// <summary>
    /// Used to detect when an app is launched
    /// </summary>
    public class ProcessWatcher : IDisposable
    {
        public event OnProcessCreatedDelegate? Created;
        private readonly Timer _timer;
        private readonly string _processname;
        private bool _disposed = false;
        private Process? _process;

        public ProcessWatcher(string processName)
        {
            _processname = processName;

            _timer = new Timer();
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
        }

        private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(_processname);

            if (processes.Length == 1)
            {
                OnProcessCreated(processes[0]);
            }
        }

        protected virtual void OnProcessCreated(Process process)
        {
            _timer.Stop();
            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited += (self, e) => _timer.Start();

            Created?.Invoke(this, process);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public delegate void OnProcessCreatedDelegate(object sender, Process process);
    }
}