using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static BaseStatSO;
using static MonsterController;

[CreateAssetMenu(menuName = "Scriptable Object/Stat Asset")]
public class StatSO : ScriptableObject
{
    public enum ValueType
    { Num, Sec, Rate, Count/*Length*/}

    [Header("UI")]
    [SerializeField] private Sprite Icon;
    [SerializeField] private string Name;
    [SerializeField] private string Intro;

    [Header("VALUE")]
    [SerializeField] private StatController.StatType Type;
    [SerializeField] private ValueType Format;
    [SerializeField] private float BaseValue;
    [SerializeField] private float PerValue;
    [SerializeField] private int MaxLv;
    [SerializeField] private int Cost;

    #region Get
    public Sprite ICON => Icon;
    public string NAME => Name;
    public string INTRO => Intro;
    public StatController.StatType TYPE => Type;
    public ValueType FORMAT => Format;
    public float BASE => BaseValue;
    public float PER => PerValue;
    public int MAXLV => MaxLv;
    public int COST => Cost;
    #endregion
}
