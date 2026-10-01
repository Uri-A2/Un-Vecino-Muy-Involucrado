using System;


public static class TaskStats
{
    public static int Completed { get; private set; }
    public static event Action<int> OnCompleted;

    public static void RegisterCompletion()
    {
        Completed++;
        OnCompleted?.Invoke(Completed);
    }


    public static void Reset()
    {
        Completed = 0;
    }
}
