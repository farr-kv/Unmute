namespace Unmute.Core
{
    public static class PeriodicTask
    {
        public static IDisposable Run(TimeSpan delay, Action action)
        {
            var cts = new CancellationTokenSource();
            var timer = new PeriodicTimer(delay);

            Task.Run(async () =>
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    action.Invoke();
                }
            }, cts.Token);

            return new DisposableAction(cts.Cancel);
        }

        public static IDisposable Run(TimeSpan delay, Func<Task> action)
        {
            var cts = new CancellationTokenSource();
            var timer = new PeriodicTimer(delay);

            Task.Run(async () =>
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    await action.Invoke();
                }
            }, cts.Token);

            return new DisposableAction(cts.Cancel);
        }
    }   
}
