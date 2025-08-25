using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffectPoolManager : MonoBehaviour
{
    // --- Constants ---
    public const float HIT_EFFECT_DURATION = 0.5f;
    public const string ROOT_NAME = "HitEffectRoot";
    private const string BOX_NAME = "HitEffectBox";

    // --- Singleton ---
    public static HitEffectPoolManager Inst;

    // --- Prefabs ---
    // ���� Ÿ�Ժ� ��Ʈ ����Ʈ ������ ��ųʸ�(EffectType.Hit ����)
    private Dictionary<ElementalManager.ElementalType, GameObject> EHitEffectPrefabs;
    // �Ϲ�(���) ��Ʈ ����Ʈ ������
    [SerializeField] private GameObject NHitEffectPrefab;

    // --- Pools ---
    // ���� Ÿ�Ժ� ��Ʈ ����Ʈ Ǯ
    private List<GameObject>[] EHitEffectPools;
    // ��� ��Ʈ ����Ʈ Ǯ
    private List<GameObject> NHitEffectPool;

    // --- Hierarchy Box ---
    // ������ ����Ʈ���� ���� �θ� Transform
    private Transform box;

    /// <summary>
    /// �̱��� ���۷��� ����.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// �ʱ�ȭ �ڷ�ƾ ����.
    /// </summary>
    private void Start()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// ElementalManager �غ� ��� �� ������ ���� ȹ�� �� Ÿ�Ժ� Ǯ ���� �� ���� �ڽ� ã��.
    /// </summary>
    private IEnumerator Init()
    {
        // ElementalManager �ν��Ͻ��� �غ�� ������ ���
        yield return new WaitUntil(() => ElementalManager.Inst);

        // 1) ���� Ÿ�Ժ� ��Ʈ ����Ʈ ������ �� ȹ��
        EHitEffectPrefabs = ElementalManager.Inst
            .GetEffectsByType(ElementalManager.EffectType.Hit);

        // 2) ���� Ÿ�Ժ� Ǯ �迭 �ʱ�ȭ
        EHitEffectPools = new List<GameObject>[EHitEffectPrefabs.Count];
        for (int i = 0; i < EHitEffectPools.Length; i++)
        {
            EHitEffectPools[i] = new List<GameObject>();
        }

        // 3) ��� Ǯ �ʱ�ȭ
        NHitEffectPool = new List<GameObject>();

        // 4) ������ ������ �ڽ� ã��
        box = transform.Find(BOX_NAME);
    }

    /// <summary>
    /// ����� ROOT ��ġ�� ��Ʈ ����Ʈ�� ��ġ�ϰ�, ������ Ÿ�Կ� ���� �������� ������ ��
    /// ���� �ð� �� ��Ȱ��ȭ�Ѵ�.
    /// </summary>
    /// <param name="target">��Ʈ ����Ʈ�� ǥ���� ���(�ڽĿ� ROOT_NAME�� �����ؾ� ��)</param>
    /// <param name="elType">���� Ÿ��(Count �̸� �Ϲ� ����Ʈ ���)</param>
    /// <param name="dmgType">������ Ÿ��(ũ��Ƽ�� ���� �� �����Ͽ� �ݿ�)</param>
    public IEnumerator InstHitEffect(
        Transform target,
        ElementalManager.ElementalType elType,
        DamageTextController.DamageType dmgType)
    {
        // 1) ���� Ÿ�԰� ��Ī�Ǵ� ����Ʈ ����(Count�� ���)
        GameObject obj = (elType != ElementalManager.ElementalType.Count)
            ? Get(elType)  // ���� ����Ʈ
            : Get();       // ��� ����Ʈ

        // 2) ��ġ/������ ����
        obj.transform.position = target.Find(ROOT_NAME).position;
        obj.transform.localScale = Vector3.one *
            (dmgType == DamageTextController.DamageType.Critical ? 1.5f : 0.85f);

        // 3) ���� �ð� �� ��Ȱ��ȭ
        yield return new WaitForSecondsRealtime(HIT_EFFECT_DURATION);
        obj.SetActive(false);
    }

    #region GET
    /// <summary>
    /// ���� Ÿ�Կ� �´� ��Ʈ ����Ʈ ������Ʈ�� ��ȯ�Ѵ�.
    /// ��Ȱ�� Ǯ���� �����ϰ�, ������ ���� �����Ѵ�.
    /// </summary>
    public GameObject Get(ElementalManager.ElementalType type)
    {
        GameObject selected = null;

        // ��Ȱ�� ������Ʈ ����
        foreach (GameObject item in EHitEffectPools[(int)type])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // ������ �ű� ���� �� Ǯ�� �߰�
        if (!selected)
        {
            selected = Instantiate(EHitEffectPrefabs[type], box);
            EHitEffectPools[(int)type].Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// ��� ��Ʈ ����Ʈ ������Ʈ�� ��ȯ�Ѵ�.
    /// ��Ȱ�� Ǯ���� �����ϰ�, ������ ���� �����Ѵ�.
    /// </summary>
    public GameObject Get()
    {
        GameObject selected = null;

        // ��Ȱ�� ������Ʈ ����
        foreach (GameObject item in NHitEffectPool)
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // ������ �ű� ���� �� Ǯ�� �߰�
        if (!selected)
        {
            selected = Instantiate(NHitEffectPrefab, box);
            NHitEffectPool.Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// ��� Ʈ������ �������� ��Ʈ ����Ʈ ��Ʈ(�θ�)�� ã�´�.
    /// </summary>
    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
