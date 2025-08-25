using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Gear Asset")]
public class GearSO : ScriptableObject
{
    public enum StatType
    {
        HP,
        DEF,
        MATK,
        SATK,
        CTKR,
        EVDR,
        Count /*Length*/
    }

    public enum ValueType
    {
        Num,
        Rate,
        Count /*Length*/
    }

    [Header("ID")]
    [SerializeField] private GearController.Rarity Outer;     // 희귀도
    [SerializeField] private GearController.GearType Inner;   // 장비 부위

    [Header("UI")]
    [SerializeField] private Sprite Icon;     // 인벤/상세 아이콘
    [SerializeField] private Button Button;   // UI 버튼 프리팹
    [SerializeField] private string Name;     // 장비명
    [SerializeField] private string Intro;    // 설명

    [Header("VALUE")]
    public GearStat MainStat;  // 주 옵션
    [Space(10)]
    public GearStat SubStat;   // 부 옵션

    #region GET
    public GearController.Rarity OUTER => Outer;
    public GearController.GearType INNER => Inner;
    public Sprite ICON => Icon;
    public Button BUTTON => Button;
    public string NAME => Name;
    public string INTRO => Intro;
    #endregion

    [Serializable]
    public class GearStat
    {
        public string NAME;          // 옵션명(표기용)
        public StatType TYPE;        // 적용 대상 스탯
        public ValueType FORMAT;     // 값 형식(수치/비율)
        public float BASE;           // 기본값
    }
}
