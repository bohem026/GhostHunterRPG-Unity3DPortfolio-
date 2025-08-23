using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor;
using UnityEngine;

public class ResourceUtility : MonoBehaviour
{
    /// <summary>
    /// Resources ���� ������ ��ο��� Ư�� Ÿ���� ���ҽ� ������ ��ȯ�մϴ�.
    /// </summary>
    /// <typeparam name="T">�ҷ��� ���ҽ� Ÿ��</typeparam>
    /// <param name="folderPath">Resources ���� ���� ���� ���</param>
    /// <returns>�ش� Ÿ���� ������Ʈ ����</returns>
    public static int CountResourcesOfType<T>(string folderPath) 
        where T : UnityEngine.Object
    {
        T[] resources = Resources.LoadAll<T>(folderPath);
        return resources.Length;
    }

    /// <summary>
    /// Resources ���� ������ ��ο��� ���ҽ��� ��ȯ�մϴ�.
    /// </summary>
    /// <typeparam name="T">�ҷ��� ���ҽ� Ÿ��</typeparam>
    /// <param name="path">Resources ���� ���� ���ҽ� ���</param>
    /// <returns>���ҽ�</returns>
    public static T GetResourceByType<T>(string path) 
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// GameObject[]�� ���� Enum�� ������ �������� �����մϴ�.
    /// </summary>
    /// <typeparam name="TComponent">���ʸ� ������Ʈ</typeparam>
    /// <typeparam name="TEnum">���ʸ� ������</typeparam>
    /// <param name="objects">��� �迭</param>
    /// <param name="enumSelector">������Ʈ ���� ������ Ž����</param>
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
