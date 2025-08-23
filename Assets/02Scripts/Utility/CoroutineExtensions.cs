using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class CoroutineExtensions
{
    /// <summary>
    /// Task를 Coroutine 형식으로 바꿔주는 확장 메서드 입니다.
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
    /// Task를 Coroutine 형식으로 바꿔주는 확장 메서드 입니다.
    /// 제네릭 형식 입니다.
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
