using System;
using System.Linq;
using UnityEngine;

public class ResourceUtility : MonoBehaviour
{
    /// <summary>
    /// Resources 하위 경로에서 지정 타입 리소스 개수를 반환합니다.
    /// </summary>
    public static int CountResourcesOfType<T>(string folderPath)
        where T : UnityEngine.Object
    {
        T[] resources = Resources.LoadAll<T>(folderPath);
        return resources.Length;
    }

    /// <summary>
    /// Resources 하위 경로에서 지정 타입 리소스를 로드합니다.
    /// </summary>
    public static T GetResourceByType<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// GameObject 배열을 각 객체의 컴포넌트 내 Enum 값 기준으로 오름차순 정렬합니다.
    /// </summary>
    /// <typeparam name="TComponent">정렬 키를 제공하는 컴포넌트</typeparam>
    /// <typeparam name="TEnum">정렬에 사용될 열거형</typeparam>
    /// <param name="objects">정렬 대상 배열</param>
    /// <param name="enumSelector">컴포넌트에서 Enum 값을 추출하는 선택자</param>
    public static GameObject[] SortByEnum<TComponent, TEnum>(
        GameObject[] objects,
        Func<TComponent, TEnum> enumSelector)
        where TComponent : Component
        where TEnum : Enum
    {
        return objects
            .Where(obj => obj.TryGetComponent<TComponent>(out _))
            .OrderBy(obj =>
            {
                var comp = obj.GetComponent<TComponent>();
                return Convert.ToInt32(enumSelector(comp));
            })
            .ToArray();
    }
}
