using Microsoft.VisualStudio.Threading;

namespace HA;

public static class AsyncHelper
{
    private static JoinableTaskFactory _joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());

    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return _joinableTaskFactory.Run(async () => await func());
    }

    public static void RunSync(Func<Task> func)
    {
        _joinableTaskFactory.Run(async () => await func());
    }
}