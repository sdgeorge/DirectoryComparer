using System;

namespace DirectoryComparer.Models
{
    public class ErrorEventArgs : EventArgs
    {
        private Exception error;
        private string message;
        private object source;      // The source of the error

        public ErrorEventArgs(Exception error, string message, object source = null)
        {
            this.error = error;
            this.message = message;
            this.source = source;
        }

        public Exception Error { get { return error; } }
        public string Message { get { return message; } }
        public object Source { get { return source; } }
    }

    public class EstimateEventArgs : EventArgs
    {
        private DateTime endTime;
        private TimeSpan totalTime;
        private TimeSpan elapsedTime;
        private TimeSpan remainingTime;
        private string message;

        private double percentage; // From 0.0 - 1.0
        private int counter;
        private int total;

        private object source; // The object associated with this estimate - i.e FileDetails of last run file

        public EstimateEventArgs(int total, int counter, long elapsedMs, object source = null, string message = null)
        {
            this.total = total;
            this.counter = counter;
            this.source = source;

            this.percentage = this.total <= 0 ? 0.0d : (double)counter / (double)total;

            this.message = message;

            long msPerItem = counter == 0 ? 0 : elapsedMs / counter;
            long totalMs = msPerItem * total;
            long remainingMs = totalMs - elapsedMs;

            this.totalTime = TimeSpan.FromMilliseconds(totalMs);
            this.elapsedTime = TimeSpan.FromMilliseconds(elapsedMs);
            this.remainingTime = TimeSpan.FromMilliseconds(remainingMs);

            this.endTime = DateTime.Now.Add(this.remainingTime);
        }

        public string Message { get { return message; } }
        public TimeSpan RemainingTime { get { return remainingTime; } }
        public TimeSpan ElapsedTime { get { return elapsedTime; } }
        public TimeSpan TotalTime { get { return totalTime; } }
        public DateTime EndTime { get { return endTime; } }

        public double Percentage { get { return percentage; } }
        public int Counter { get { return counter; } }
        public int Total { get { return total; } }


        public object Source { get { return source; } }
    }
}