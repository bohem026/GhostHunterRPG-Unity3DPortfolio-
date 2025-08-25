using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 데미지 텍스트 풀을 관리한다.
/// - 타입별(크리티컬/회피 등) 프리팹을 정렬해 보관
/// - 비활성 오브젝트를 재사용(없으면 생성)하여 할당/GC 비용을 줄임
/// </summary>
public class DamageTextPoolManager : MonoBehaviour
{
    private const string ROOT_NAME = "DamageTextRoot";
    public static DamageTextPoolManager Inst;

    [SerializeField] private Transform box;

    [Space(20)]
    [Header("Prefabs (sorted by DamageType)")]
    [Tooltip("DamageTextController.DamageType 순서로 정렬되어 사용됨")]
    [SerializeField] private GameObject[] dmgTxtPrefabs;

    // 타입별 인스턴스 풀(각 타입마다 GameObject 리스트)
    private List<GameObject>[] dmgTxtPools;

    /// <summary>
    /// 싱글톤 레퍼런스를 설정한다.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// 풀 초기화를 수행한다.
    /// </summary>
    private void Start()
    {
        Init();
    }

    /// <summary>
    /// 프리팹을 DamageType 순으로 정렬하고,
    /// 타입 개수만큼 풀(List)을 생성한다.
    /// </summary>
    private void Init()
    {
        // DamageTextController.DamageType 순서대로 프리팹 정렬
        dmgTxtPrefabs = ResourceUtility.SortByEnum
            <DamageTextController, DamageTextController.DamageType>
            (dmgTxtPrefabs, e => e.GetDamageType);

        // 타입별 풀 배열 생성
        dmgTxtPools = new List<GameObject>[dmgTxtPrefabs.Length];
        for (int i = 0; i < dmgTxtPools.Length; i++)
        {
            dmgTxtPools[i] = new List<GameObject>();
        }
    }

    #region GET
    /// <summary>
    /// 데미지 정보(타입/값)에 맞는 텍스트 오브젝트를 반환한다.
    /// 비활성 풀에서 재사용하고, 없으면 새로 생성한다.
    /// </summary>
    public GameObject Get(Calculator.DamageInfo info)
    {
        DamageTextController.DamageType type = info.Type;
        float value = info.Value;

        GameObject selected = null;

        // 1) 비활성 오브젝트 재사용
        foreach (GameObject item in dmgTxtPools[(int)type])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 2) 없으면 새로 생성하여 풀에 추가
        if (!selected)
        {
            selected = Instantiate(dmgTxtPrefabs[(int)type], box);
            dmgTxtPools[(int)type].Add(selected);
        }

        // 3) 텍스트 갱신(회피 타입은 숫자 표시 없음)
        if (type != DamageTextController.DamageType.Evaded)
            selected.GetComponent<Text>().text = Mathf.CeilToInt(value).ToString();

        return selected;
    }

    /// <summary>
    /// 타깃 트랜스폼 하위에서 데미지 텍스트 루트(부모)를 찾는다.
    /// </summary>
    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
