using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ������ �ؽ�Ʈ Ǯ�� �����Ѵ�.
/// - Ÿ�Ժ�(ũ��Ƽ��/ȸ�� ��) �������� ������ ����
/// - ��Ȱ�� ������Ʈ�� ����(������ ����)�Ͽ� �Ҵ�/GC ����� ����
/// </summary>
public class DamageTextPoolManager : MonoBehaviour
{
    private const string ROOT_NAME = "DamageTextRoot";
    public static DamageTextPoolManager Inst;

    [SerializeField] private Transform box;

    [Space(20)]
    [Header("Prefabs (sorted by DamageType)")]
    [Tooltip("DamageTextController.DamageType ������ ���ĵǾ� ����")]
    [SerializeField] private GameObject[] dmgTxtPrefabs;

    // Ÿ�Ժ� �ν��Ͻ� Ǯ(�� Ÿ�Ը��� GameObject ����Ʈ)
    private List<GameObject>[] dmgTxtPools;

    /// <summary>
    /// �̱��� ���۷����� �����Ѵ�.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// Ǯ �ʱ�ȭ�� �����Ѵ�.
    /// </summary>
    private void Start()
    {
        Init();
    }

    /// <summary>
    /// �������� DamageType ������ �����ϰ�,
    /// Ÿ�� ������ŭ Ǯ(List)�� �����Ѵ�.
    /// </summary>
    private void Init()
    {
        // DamageTextController.DamageType ������� ������ ����
        dmgTxtPrefabs = ResourceUtility.SortByEnum
            <DamageTextController, DamageTextController.DamageType>
            (dmgTxtPrefabs, e => e.GetDamageType);

        // Ÿ�Ժ� Ǯ �迭 ����
        dmgTxtPools = new List<GameObject>[dmgTxtPrefabs.Length];
        for (int i = 0; i < dmgTxtPools.Length; i++)
        {
            dmgTxtPools[i] = new List<GameObject>();
        }
    }

    #region GET
    /// <summary>
    /// ������ ����(Ÿ��/��)�� �´� �ؽ�Ʈ ������Ʈ�� ��ȯ�Ѵ�.
    /// ��Ȱ�� Ǯ���� �����ϰ�, ������ ���� �����Ѵ�.
    /// </summary>
    public GameObject Get(Calculator.DamageInfo info)
    {
        DamageTextController.DamageType type = info.Type;
        float value = info.Value;

        GameObject selected = null;

        // 1) ��Ȱ�� ������Ʈ ����
        foreach (GameObject item in dmgTxtPools[(int)type])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 2) ������ ���� �����Ͽ� Ǯ�� �߰�
        if (!selected)
        {
            selected = Instantiate(dmgTxtPrefabs[(int)type], box);
            dmgTxtPools[(int)type].Add(selected);
        }

        // 3) �ؽ�Ʈ ����(ȸ�� Ÿ���� ���� ǥ�� ����)
        if (type != DamageTextController.DamageType.Evaded)
            selected.GetComponent<Text>().text = Mathf.CeilToInt(value).ToString();

        return selected;
    }

    /// <summary>
    /// Ÿ�� Ʈ������ �������� ������ �ؽ�Ʈ ��Ʈ(�θ�)�� ã�´�.
    /// </summary>
    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
