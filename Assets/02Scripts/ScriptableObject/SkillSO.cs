using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Skill Asset")]
public class SkillSO : ScriptableObject
{
    /// <summary>
    /// 스킬 능력치 유형 (일부는 특정 원소 컨셉과 연계)
    /// </summary>
    public enum AbilityType
    {
        DMG,   // 기본 대미지
        ITV,   // 간격(예: Light)
        RNG,   // 범위(예: Fire)
        DUR,   // 지속시간(예: Ice)
        CNT,   // 타격/개수(예: Poison)
        Count
    }

    public enum ValueType
    {
        Num,   // 정수/수치
        Sec,   // 초 단위
        Rate,  // 비율(0~1)
        Count
    }

    [Header("ID")]
    [SerializeField] private SkillWindowController.SKType Outer; // 스킬 분류(패시브/디펜스/액티브)
    [SerializeField] private SkillWindowController.ELType Inner; // 원소 타입
    [SerializeField] private int Id;                             // 고유 ID

    [Header("UI")]
    [SerializeField] private Sprite Icon;   // 아이콘
    [SerializeField] private string Name;   // 스킬명
    [SerializeField] private string Intro;  // 소개/설명

    [Header("VALUE")]
    public SkillAbility MainAbility;  // 주 능력치
    [Space(10)]
    public SkillAbility SubAbility;   // 보조 능력치
    [Space(10)]
    [SerializeField] private int MaxLv; // 최대 레벨
    [SerializeField] private int Cost;  // 연구/강화 비용

    #region GET
    public SkillWindowController.SKType OUTER => Outer;
    public SkillWindowController.ELType INNER => Inner;
    public int ID => Id;
    public Sprite ICON => Icon;
    public string NAME => Name;
    public string INTRO => Intro;
    public int MAXLV => MaxLv;
    public int COST => Cost;
    #endregion

    [Serializable]
    public class SkillAbility
    {
        public string NAME;        // 능력치 표기명
        public AbilityType TYPE;   // 능력 타입
        public ValueType FORMAT;   // 값 표현 형식
        public float BASE;         // 기본값
        public float PER;          // 레벨당 증가량
    }
}
