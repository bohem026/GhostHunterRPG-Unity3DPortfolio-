using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SkillSO;

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
        Count   /*Length*/
    }

    public enum ValueType
    { Num, Rate, Count/*Length*/}

    [Header("ID")]
    [SerializeField] private GearController.Rarity Outer;
    [SerializeField] private GearController.GearType Inner;

    [Header("UI")]
    [SerializeField] private Sprite Icon;
    [SerializeField] private Button Button;
    [SerializeField] private string Name;
    [SerializeField] private string Intro;

    [Header("VALUE")]
    public GearStat MainStat;
    [Space(10)]
    public GearStat SubStat;

    #region Get
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
        public string NAME;
        public StatType TYPE;
        public ValueType FORMAT;
        public float BASE;
    }
}