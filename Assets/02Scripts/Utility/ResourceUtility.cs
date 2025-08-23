using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor;
using UnityEngine;

public class ResourceUtility : MonoBehaviour
{
    /// <summary>
    /// Resources 폴더 하위의 경로에서 특정 타입의 리소스 개수를 반환합니다.
    /// </summary>
    /// <typeparam name="T">불러올 리소스 타입</typeparam>
    /// <param name="folderPath">Resources 기준 하위 폴더 경로</param>
    /// <returns>해당 타입의 오브젝트 개수</returns>
    public static int CountResourcesOfType<T>(string folderPath) 
        where T : UnityEngine.Object
    {
        T[] resources = Resources.LoadAll<T>(folderPath);
        return resources.Length;
    }

    /// <summary>
    /// Resources 폴더 하위의 경로에서 리소스를 반환합니다.
    /// </summary>
    /// <typeparam name="T">불러올 리소스 타입</typeparam>
    /// <param name="path">Resources 기준 하위 리소스 경로</param>
    /// <returns>리소스</returns>
    public static T GetResourceByType<T>(string path) 
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// GameObject[]를 관련 Enum형 변수로 오름차순 정렬합니다.
    /// </summary>
    /// <typeparam name="TComponent">제너릭 컴포넌트</typeparam>
    /// <typeparam name="TEnum">제너릭 열거형</typeparam>
    /// <param name="objects">대상 배열</param>
    /// <param name="enumSelector">컴포넌트 내부 열거형 탐색기</param>
    /// <returns></returns>
    public static GameObject[] SortByEnum<TComponent, TEnum>(
        GameObject[] objects,
        Func<TComponent, TEnum> enumSelector
    )
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
