using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Base Stat Asset")]
public class BaseStatSO : ScriptableObject
{
    /// <summary>스탯 소유 주체(플레이어/기타)와 몬스터 유형 정의.</summary>
    public enum OWNER { Player, Other }

    [Header("STAT: ACTOR")]
    [SerializeField] private OWNER owner;                         // 스탯 소유 주체
    [SerializeField] private MonsterController.MonType monType;   // 몬스터 타입

    [Header("STAT: BASE")]
    [SerializeField] private float baseHP;      // 체력
    [SerializeField] private float baseMP;      // 마나(몬스터: 공격 주기)
    [SerializeField] private float baseMPITV;   // 마나 회복력(몬스터: 미사용)
    [SerializeField] private float baseMATK;    // 물리 공격력
    [SerializeField] private float baseSATK;    // 마법 공격력
    [SerializeField] private float baseDEF;     // 방어력
    [SerializeField] private float baseCTKR;    // 치명률
    [SerializeField] private float baseEVDR;    // 회피 수치
    [SerializeField] private float baseSTBR;    // 안정 수치(몬스터: 미사용)

    #region GET
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
}
