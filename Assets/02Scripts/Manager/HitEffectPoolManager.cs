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
    // 원소 타입별 히트 이펙트 프리팹 딕셔너리(EffectType.Hit 기준)
    private Dictionary<ElementalManager.ElementalType, GameObject> EHitEffectPrefabs;
    // 일반(노멀) 히트 이펙트 프리팹
    [SerializeField] private GameObject NHitEffectPrefab;

    // --- Pools ---
    // 원소 타입별 히트 이펙트 풀
    private List<GameObject>[] EHitEffectPools;
    // 노멀 히트 이펙트 풀
    private List<GameObject> NHitEffectPool;

    // --- Hierarchy Box ---
    // 생성된 이펙트들을 담을 부모 Transform
    private Transform box;

    /// <summary>
    /// 싱글톤 레퍼런스 설정.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// 초기화 코루틴 시작.
    /// </summary>
    private void Start()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// ElementalManager 준비 대기 → 프리팹 참조 획득 → 타입별 풀 생성 → 계층 박스 찾기.
    /// </summary>
    private IEnumerator Init()
    {
        // ElementalManager 인스턴스가 준비될 때까지 대기
        yield return new WaitUntil(() => ElementalManager.Inst);

        // 1) 원소 타입별 히트 이펙트 프리팹 맵 획득
        EHitEffectPrefabs = ElementalManager.Inst
            .GetEffectsByType(ElementalManager.EffectType.Hit);

        // 2) 원소 타입별 풀 배열 초기화
        EHitEffectPools = new List<GameObject>[EHitEffectPrefabs.Count];
        for (int i = 0; i < EHitEffectPools.Length; i++)
        {
            EHitEffectPools[i] = new List<GameObject>();
        }

        // 3) 노멀 풀 초기화
        NHitEffectPool = new List<GameObject>();

        // 4) 계층상 수납용 박스 찾기
        box = transform.Find(BOX_NAME);
    }

    /// <summary>
    /// 대상의 ROOT 위치에 히트 이펙트를 배치하고, 데미지 타입에 따라 스케일을 조정한 뒤
    /// 일정 시간 후 비활성화한다.
    /// </summary>
    /// <param name="target">히트 이펙트를 표시할 대상(자식에 ROOT_NAME이 존재해야 함)</param>
    /// <param name="elType">원소 타입(Count 이면 일반 이펙트 사용)</param>
    /// <param name="dmgType">데미지 타입(크리티컬 여부 등 스케일에 반영)</param>
    public IEnumerator InstHitEffect(
        Transform target,
        ElementalManager.ElementalType elType,
        DamageTextController.DamageType dmgType)
    {
        // 1) 원소 타입과 매칭되는 이펙트 선택(Count면 노멀)
        GameObject obj = (elType != ElementalManager.ElementalType.Count)
            ? Get(elType)  // 원소 이펙트
            : Get();       // 노멀 이펙트

        // 2) 위치/스케일 설정
        obj.transform.position = target.Find(ROOT_NAME).position;
        obj.transform.localScale = Vector3.one *
            (dmgType == DamageTextController.DamageType.Critical ? 1.5f : 0.85f);

        // 3) 일정 시간 후 비활성화
        yield return new WaitForSecondsRealtime(HIT_EFFECT_DURATION);
        obj.SetActive(false);
    }

    #region GET
    /// <summary>
    /// 원소 타입에 맞는 히트 이펙트 오브젝트를 반환한다.
    /// 비활성 풀에서 재사용하고, 없으면 새로 생성한다.
    /// </summary>
    public GameObject Get(ElementalManager.ElementalType type)
    {
        GameObject selected = null;

        // 비활성 오브젝트 재사용
        foreach (GameObject item in EHitEffectPools[(int)type])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 없으면 신규 생성 후 풀에 추가
        if (!selected)
        {
            selected = Instantiate(EHitEffectPrefabs[type], box);
            EHitEffectPools[(int)type].Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// 노멀 히트 이펙트 오브젝트를 반환한다.
    /// 비활성 풀에서 재사용하고, 없으면 새로 생성한다.
    /// </summary>
    public GameObject Get()
    {
        GameObject selected = null;

        // 비활성 오브젝트 재사용
        foreach (GameObject item in NHitEffectPool)
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 없으면 신규 생성 후 풀에 추가
        if (!selected)
        {
            selected = Instantiate(NHitEffectPrefab, box);
            NHitEffectPool.Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// 대상 트랜스폼 하위에서 히트 이펙트 루트(부모)를 찾는다.
    /// </summary>
    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
