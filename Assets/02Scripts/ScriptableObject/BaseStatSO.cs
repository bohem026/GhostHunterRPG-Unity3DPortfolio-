using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Base Stat Asset")]
public class BaseStatSO : ScriptableObject
{
    public enum OWNER
    { Player, Other }
    [Header("STAT: ACTOR")]
    [SerializeField] private OWNER owner;      // 대상
    [SerializeField] private MonsterController.MonType monType; // 몬스터 타입

    [Header("STAT: BASE")]
    [SerializeField] private float baseHP;      // 체력
    [SerializeField] private float baseMP;      // 마나(몬스터: 공격 주기)
    [SerializeField] private float baseMPITV;      // 마나 회복력(몬스터: x)
    [SerializeField] private float baseMATK;     // 물리 공격력
    [SerializeField] private float baseSATK;     // 마법 공격력
    [SerializeField] private float baseDEF;     // 방어력
    [SerializeField] private float baseCTKR;    // 치명률
    [SerializeField] private float baseEVDR;     // 회피 수치
    [SerializeField] private float baseSTBR;     // 안정 수치(몬스터: x)

    //[Header("STAT: PER LEVEL")]
    //[SerializeField] private float perHP;
    //[SerializeField] private float perMP;
    //[SerializeField] private float perMPITV;
    //[SerializeField] private float perMATK;
    //[SerializeField] private float perSATK;
    //[SerializeField] private float perDEF;
    //[SerializeField] private float perCTKR;
    //[SerializeField] private float perEVDR;
    //[SerializeField] private float perSTBR;

    #region Get
    public OWNER Owner => owner;
    public MonsterController.MonType MonType => monType;
    public float BaseHP => baseHP;
    public float BaseMP => baseMP;
    public float BaseMPITV => baseMPITV;
    public float BaseMATK => baseMATK;
    public float BaseSATK => baseSATK;
    public float BaseDEF => baseDEF;
    public float BaseCTKR => baseCTKR;
    public float BaseEVDR => baseEVDR;
    public float BaseSTBR => baseSTBR;
    #endregion

    //#region Get
    //public OWNER Owner => owner;
    //public MonsterController.MonType MonType => monType;
    //public float BaseHP(int level) => baseHP + perHP * level;
    //public float BaseMP(int level) => baseMP + perMP * level;
    //public float BaseMPITV(int level) => baseMPITV - perMPITV * level;
    //public float BaseMATK(int level) => baseMATK + perMATK * level;
    //public float BaseSATK(int level) => baseSATK + perSATK * level;
    //public float BaseDEF(int level) => baseDEF + perDEF * level;
    //public float BaseCTKR(int level) => baseCTKR + perCTKR * level;
    //public float BaseEVDR(int level) => baseEVDR + perEVDR * level;
    //public float BaseSTBR(int level) => baseSTBR + perSTBR * level;
    //#endregion
}
