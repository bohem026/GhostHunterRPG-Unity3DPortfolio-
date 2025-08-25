using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원소(속성) 이펙트 프리팹을 타입별로 로드/보관/제공하는 매니저.
/// </summary>
public class ElementalManager : MonoBehaviour
{
    // 리소스 경로 프리픽스
    private const string ELEMENTAL_PRELINK = "Prefabs/Elemental/";

    // 싱글톤
    public static ElementalManager Inst;

    /// <summary>플레이어 기준 원소 공격의 이펙트 타입(지속/타격).</summary>
    public enum EffectType { Dot, Hit, Count /*Length*/ }

    /// <summary>원소 속성 종류.</summary>
    public enum ElementalType { Fire, Ice, Light, Poison, Count /*Length*/ }

    // 스테이지 기본 원소(외부에서 설정)
    public ElementalType STAGE_ELEMENT { get; set; }

    // [이펙트 타입] → ( [원소] → 프리팹 ) 매핑 컨테이너
    private Dictionary<ElementalType, GameObject>[] EffectContainer;

    /// <summary>
    /// 싱글톤 레퍼런스를 설정한다.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// 컨테이너 초기화 후 리소스를 로드한다.
    /// </summary>
    private void Start()
    {
        Init();
        LoadRcsToContainer();
    }

    /// <summary>
    /// 이펙트 컨테이너 배열을 생성한다. (타입 수만큼 슬롯 확보)
    /// </summary>
    private void Init()
    {
        EffectContainer = new Dictionary<ElementalType, GameObject>[(int)EffectType.Count];
    }

    /// <summary>
    /// 리소스를 경로 규칙에 따라 로드해 컨테이너에 적재한다.
    /// </summary>
    private void LoadRcsToContainer()
    {
        for (int i = 0; i < (int)EffectType.Count; i++)
        {
            EffectContainer[i] = new Dictionary<ElementalType, GameObject>();

            for (int j = 0; j < (int)ElementalType.Count; j++)
            {
                EffectType efType = (EffectType)i;
                ElementalType elType = (ElementalType)j;

                // 경로 예: "Prefabs/Elemental/Hit_Fire"
                string path = GetEffectPath(efType, elType);

                GameObject resource = ResourceUtility.GetResourceByType<GameObject>(path);
                EffectContainer[i][elType] = resource;
            }
        }
    }

    /// <summary>
    /// 단일 이펙트 프리팹을 반환한다.
    /// </summary>
    public GameObject GetEffect(EffectType efType, ElementalType elType)
    {
        return EffectContainer[(int)efType][elType];
    }

    /// <summary>
    /// 특정 이펙트 타입에 대한 전체 [원소→프리팹] 딕셔너리를 반환한다.
    /// </summary>
    public Dictionary<ElementalType, GameObject> GetEffectsByType(EffectType type)
    {
        if (type == EffectType.Count) return null;
        return EffectContainer[(int)type];
    }

    /// <summary>
    /// 경로 규칙을 생성한다. (예: "Prefabs/Elemental/Hit_Fire")
    /// </summary>
    private string GetEffectPath(EffectType efType, ElementalType elType)
    {
        string path = ELEMENTAL_PRELINK + efType + "_" + elType;
        return path;
    }
}
