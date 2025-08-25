using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬(주문) 프리팹을 레벨별로 로드/보관하고,
/// 현재 스킬 레벨에 맞는 프리팹을 제공하는 매니저.
/// </summary>
public class SpellManager : MonoBehaviour
{
    public static SpellManager Inst;

    // 최대 스킬 레벨
    private static int MAX_SPELL_LV = 5;

    /// <summary>스킬 레벨 조정 모드(외부에서 사용할 수 있어 유지).</summary>
    public enum SpellLvMode { UP, DOWN, RESET }

    [SerializeField] private GameObject[] SpellPrototypes;   // 스킬 원형 프리팹(이름 규칙 기반 로드의 기준)
    [SerializeField] private List<GameObject>[] SpellList;   // [스킬ID][레벨] 프리팹 목록

    // 사용자 스킬트리 레벨 저장(외부 참조 가능성을 고려해 공개 유지)
    public int[] CurSpellLvArray;

    // 내부 작업용
    private GameObject spell;
    private bool isInitialized;

    /// <summary>
    /// 싱글톤 설정 및 스킬 리스트 초기화.
    /// </summary>
    private void Awake()
    {
        if (isInitialized) return;

        Inst = this;
        InitSpellList();

        isInitialized = true;
    }

    /// <summary>
    /// 스킬 목록/레벨 배열 구성 및 프리팹 로드.
    /// </summary>
    private void InitSpellList()
    {
        // 1) 컨테이너 초기화
        SpellList = new List<GameObject>[SpellPrototypes.Length];
        CurSpellLvArray = new int[SpellList.Length];
        for (int i = 0; i < SpellList.Length; i++)
        {
            SpellList[i] = new List<GameObject>();

            // -1: 미해금, 0 이상: 해금(레벨 보유). 현재 설계는 기본 0으로 시작.
            CurSpellLvArray[i] = 0;
        }

        // 2) 프로토타입을 ID 기준으로 정렬
        Array.Sort(
            SpellPrototypes,
            (a, b) => a.GetComponent<SpellPrototypeSorter>().ID
                       .CompareTo(b.GetComponent<SpellPrototypeSorter>().ID)
        );

        // 3) 레벨별 프리팹 로드 및 매핑 (규칙: "Prefabs/Spell/{이름}_Lv{레벨}")
        for (int outer = 0; outer < SpellPrototypes.Length; outer++)
        {
            for (int inner = 0; inner < MAX_SPELL_LV; inner++)
            {
                string path = $"Prefabs/Spell/{SpellPrototypes[outer].name}_Lv{inner}";
                spell = ResourceUtility.GetResourceByType<GameObject>(path);
                if (!spell) continue;

                // 레벨 인덱스에 맞게 삽입(0 기반)
                SpellList[outer].Insert(inner, spell);
            }
        }
    }

    #region --- Public API ---
    /// <summary>
    /// 스킬 ID에 해당하는 현재 레벨의 스킬 프리팹을 반환한다.
    /// (레벨 0이면 해금 전으로 간주하여 null 반환)
    /// </summary>
    public GameObject GetSpell(int ID)
    {
        if (ID >= GetSpellTypeCount())
            return null;

        // 스킬 트리에서 레벨 판독을 위해 0레벨 프리팹의 메타를 참조
        var meta = SpellList[ID][0].GetComponent<SpellEffectController>();
        var outer = meta.OUTER;
        var inner = meta.INNER;

        int level = GlobalValue.Instance.GetSkillLevelByEnum(outer, inner);

        if (level == 0) return null;
        return SpellList[ID][level - 1];
    }

    /// <summary>
    /// 스킬 종류(타입)의 총 개수를 반환한다.
    /// </summary>
    public int GetSpellTypeCount()
    {
        return SpellPrototypes.Length;
    }
    #endregion
}
