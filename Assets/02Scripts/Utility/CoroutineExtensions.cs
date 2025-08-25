using System.Collections;
using System.Threading.Tasks;

public static class CoroutineExtensions
{
    /// <summary>
    /// Task를 Unity Coroutine(IEnumerator)으로 변환합니다.
    /// 완료될 때까지 매 프레임 null을 반환하고, 실패 시 예외를 그대로 던집니다.
    /// </summary>
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }

    /// <summary>
    /// 제네릭 형식입니다.
    /// </summary>
    public static IEnumerator AsCoroutine<T>(this Task<T> task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            throw task.Exception;
    }
}
