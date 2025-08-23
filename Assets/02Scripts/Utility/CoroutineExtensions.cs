using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class CoroutineExtensions
{
    /// <summary>
    /// Task�� Coroutine �������� �ٲ��ִ� Ȯ�� �޼��� �Դϴ�.
    /// </summary>
    /// <param name="task">Task</param>
    /// <returns>Coroutine</returns>
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }

    /// <summary>
    /// Task�� Coroutine �������� �ٲ��ִ� Ȯ�� �޼��� �Դϴ�.
    /// ���׸� ���� �Դϴ�.
    /// </summary>
    /// <param name="task">Task</param>
    /// <returns>Coroutine</returns>
    public static IEnumerator AsCoroutine<T>(this Task<T> task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }
}
