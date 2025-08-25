using System;
using System.Linq;
using UnityEngine;

public class ResourceUtility : MonoBehaviour
{
    /// <summary>
    /// Resources ���� ��ο��� ���� Ÿ�� ���ҽ� ������ ��ȯ�մϴ�.
    /// </summary>
    public static int CountResourcesOfType<T>(string folderPath)
        where T : UnityEngine.Object
    {
        T[] resources = Resources.LoadAll<T>(folderPath);
        return resources.Length;
    }

    /// <summary>
    /// Resources ���� ��ο��� ���� Ÿ�� ���ҽ��� �ε��մϴ�.
    /// </summary>
    public static T GetResourceByType<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// GameObject �迭�� �� ��ü�� ������Ʈ �� Enum �� �������� �������� �����մϴ�.
    /// </summary>
    /// <typeparam name="TComponent">���� Ű�� �����ϴ� ������Ʈ</typeparam>
    /// <typeparam name="TEnum">���Ŀ� ���� ������</typeparam>
    /// <param name="objects">���� ��� �迭</param>
    /// <param name="enumSelector">������Ʈ���� Enum ���� �����ϴ� ������</param>
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
