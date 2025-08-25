using System.Collections;
using System.Threading.Tasks;

public static class CoroutineExtensions
{
    /// <summary>
    /// Task�� Unity Coroutine(IEnumerator)���� ��ȯ�մϴ�.
    /// �Ϸ�� ������ �� ������ null�� ��ȯ�ϰ�, ���� �� ���ܸ� �״�� �����ϴ�.
    /// </summary>
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }

    /// <summary>
    /// ���׸� �����Դϴ�.
    /// </summary>
    public static IEnumerator AsCoroutine<T>(this Task<T> task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }
}
