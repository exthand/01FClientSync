using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _01FClientSync
{
    public class Spinner : IDisposable
    {
        private const string Sequence = @"/-\|";
        private int counter = 0;
        private int left;
        private int top;
        private readonly int delay;
        private bool active;
        private readonly Thread thread;

        public Spinner(int delay = 100)
        {
            this.left = 9;
            this.top = 0;
            this.delay = delay;
            thread = new Thread(Spin);
        }

        public void Start(int top)
        {
            this.top = top;
            active = true;
            if (!thread.IsAlive)
                thread.Start();
        }

        public void Stop()
        {
            active = false;
            Draw('|');
            Console.SetCursorPosition(0, top + 1);
        }

        private void Spin()
        {
            while (active)
            {
                Turn();
                Thread.Sleep(delay);
            }
        }

        private void Draw(char c)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(c);
            Console.CursorVisible = true;
        }

        private void Turn()
        {
            Draw(Sequence[++counter % Sequence.Length]);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

