using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Skill Asset")]
public class SkillSO : ScriptableObject
{
    public enum AbilityType
    {
        DMG,    /*Default*/
        ITV,    /*Light*/
        RNG,    /*Fire*/
        DUR,    /*Ice*/
        CNT,    /*Poison*/
        Count   /*Length*/
    }

    public enum ValueType
    { Num, Sec, Rate, Count/*Length*/}

    [Header("ID")]
    [SerializeField] private SkillWindowController.SKType Outer;
    [SerializeField] private SkillWindowController.ELType Inner;
    [SerializeField] private int Id;

    [Header("UI")]
    [SerializeField] private Sprite Icon;
    [SerializeField] private string Name;
    [SerializeField] private string Intro;

    [Header("VALUE")]
    public SkillAbility MainAbility;
    [Space(10)]
    public SkillAbility SubAbility;
    [Space(10)]
    [SerializeField] private int MaxLv;
    [SerializeField] private int Cost;

    #region Get
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
        public string NAME;
        public AbilityType TYPE;
        public ValueType FORMAT;
        public float BASE;
        public float PER;
    }
}
