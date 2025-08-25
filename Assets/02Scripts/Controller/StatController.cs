using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터(플레이어/몬스터) 스탯과 원소 도트(지속 피해) 처리, MP 자연회복을 관리한다.
/// - 초기화: 플레이어/몬스터 스탯 초기화
/// - 런타임: MP 회복, 원소 도트 타이머(DEL/ DUR) 갱신 및 피해 적용
/// - 유틸: 장비/강화/기본 수치를 합산해 최종 스탯 계산, UI 아이콘 제어
/// </summary>
public class StatController : MonoBehaviour
{
    // ── Constants
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const float DUR_RESET_VALUE = 0f;
    private const float DEL_RESET_VALUE = -1f;
    private const float DEL_START_VALUE = 0f;
    public static int MAX_STAT_LEVEL = 99;

    // ── Serialized
    [SerializeField] private BaseStatSO baseStatAsset;

    // ── Types
    public enum StatType { HP, MP, MPITV, MATK, SATK, DEF, CTKR, EVDR, STBR, Count }

    // ── Final Stats (합산 결과)
    public float HP { get; set; }     // 최대 체력
    public float MP { get; set; }     // 최대 마나
    public float MPITV { get; set; }  // 마나 회복 주기(초당 틱 간격)
    public float MATK { get; set; }   // 물리 공격력
    public float SATK { get; set; }   // 마법 공격력
    public float DEF { get; set; }    // 방어력
    public float CTKR { get; set; }   // 치명률
    public float EVDR { get; set; }   // 회피 수치
    public float STBR { get; set; }   // 안정 수치

    // ── Runtime Stats
    public float HPCurrent { get; set; }
    public float MPCurrent { get; set; }
    public ElementalManager.ElementalType ELResist { get; set; }
    public ElementalManager.ElementalType ELOnAttack { get; set; }

    // 현재 적용 중인 원소 도트/효과 및 타이머 컨테이너
    private Dictionary<ElementalManager.ElementalType, ELDamageInfo> ELOnAttackContainer;
    private Dictionary<ElementalManager.ElementalType, Delta> ELDeltaContainer;

    // ── State
    private bool isInitialized;
    private float MPdelta = 0f;

    // ── Unity Lifecycle

    private void OnEnable()
    {
        if (isInitialized) return;

        Init();

        // 오너 타입에 맞는 초기 스탯 세팅
        if (baseStatAsset.Owner == BaseStatSO.OWNER.Player)
        {
            InitStat();
        }
        else if (baseStatAsset.Owner == BaseStatSO.OWNER.Other)
        {
            int level = StageManager.Inst.Asset.NUM_STAGE;
            InitStat(level);
        }

        isInitialized = true;
    }

    private void Update()
    {
        // 플레이어 입력 불가(예: 일시정지/인트로) 시 갱신 생략
        if (GetComponent<PlayerController>())
            if (!GetComponent<PlayerController>().inputEnabled)
                return;

        UpdateMP();
        UpdateDUR();
        UpdateDEL();
    }

    // ── Public: 초기화

    /// <summary>
    /// 플레이어 스탯 초기화(기본+강화+장비 합산).
    /// </summary>
    public void InitStat()
    {
        HP = GetTotalOfStat(StatType.HP);
        MP = 80 + (int)(GlobalValue.Instance.GetStatLevelByType(StatType.MPITV) / 5);
        MPITV = GetTotalOfStat(StatType.MPITV);
        DEF = GetTotalOfStat(StatType.DEF);
        MATK = GetTotalOfStat(StatType.MATK);
        SATK = GetTotalOfStat(StatType.SATK);
        CTKR = GetTotalOfStat(StatType.CTKR);
        EVDR = GetTotalOfStat(StatType.EVDR);
        STBR = GetTotalOfStat(StatType.STBR);

        HPCurrent = HP;
        MPCurrent = 0f;
        ELResist = ElementalManager.ElementalType.Count;
        ELOnAttack = ElementalManager.ElementalType.Count;
    }

    /// <summary>
    /// 몬스터 스탯 초기화(스테이지 레벨 기반).
    /// </summary>
    public void InitStat(int level)
    {
        HP = baseStatAsset.BaseHP * 0.33f * (2 + level);
        MP = Mathf.Clamp(baseStatAsset.BaseMP / 100 * (100 - 3.3f * level), 5.66f, baseStatAsset.BaseMP);
        MPITV = baseStatAsset.BaseMPITV;
        MATK = baseStatAsset.BaseMATK * 0.33f * (2 + level);
        SATK = baseStatAsset.BaseSATK * 0.33f * (2 + level);
        DEF = baseStatAsset.BaseDEF * 0.33f * (2 + level);
        CTKR = baseStatAsset.BaseCTKR * 0.25f * (3 + level);
        EVDR = baseStatAsset.BaseEVDR * 0.25f * (3 + level);
        STBR = baseStatAsset.BaseSTBR * 0.25f * (3 + level);

        HPCurrent = HP;
        MPCurrent = MP;
        ELResist = ElementalManager.ElementalType.Count;
        ELOnAttack = ElementalManager.ElementalType.Count;
    }

    // ── Public: 원소 피해 적용

    /// <summary>
    /// 원소 지속효과 진입 시(피격 순간) 도트/디버프를 등록한다.
    /// </summary>
    public void OnELDamage(ElementalController attacker)
    {
        ELDamageInfo elAttackInfo = new ELDamageInfo(attacker.GetAsset);

        // 현재 원소 효과 최신화
        ELOnAttackContainer[elAttackInfo.ELTYPE] = elAttackInfo;

        // 도트(간헐 피해) 타입이면 델타 컨테이너 등록
        if (elAttackInfo.ITV > 0f)
        {
            ELDeltaContainer[elAttackInfo.ELTYPE] = new Delta();
        }
        else
        {
            // 즉시성/지속 디버프형 처리(Ice/Light)
            switch (elAttackInfo.ELTYPE)
            {
                case ElementalManager.ElementalType.Ice:
                    switch (GetAsset.Owner)
                    {
                        case BaseStatSO.OWNER.Player: MPITV *= elAttackInfo.DMGR; break;
                        case BaseStatSO.OWNER.Other: MP *= elAttackInfo.DMGR; break;
                    }
                    break;
                case ElementalManager.ElementalType.Light:
                    EVDR = 0f;
                    break;
            }
        }

        ELOnAttack = elAttackInfo.ELTYPE;

        // 메인 원소 아이콘 활성화
        switch (GetAsset.Owner)
        {
            case BaseStatSO.OWNER.Player:
                UIManager.Inst.ActivateELIcon(ELOnAttack, true);
                break;
            case BaseStatSO.OWNER.Other:
                GetComponent<MonsterUIController>().ActivateELIcon(ELOnAttack, true);
                break;
        }
    }

    // ── Private: 초기구성/유틸

    /// <summary>
    /// 컨테이너 초기화 및 무속성 슬롯 등록.
    /// </summary>
    private void Init()
    {
        ELOnAttackContainer = new Dictionary<ElementalManager.ElementalType, ELDamageInfo>();
        ELDeltaContainer = new Dictionary<ElementalManager.ElementalType, Delta>();

        ELOnAttackContainer[ElementalManager.ElementalType.Count] = new ELDamageInfo();
        ELDeltaContainer[ElementalManager.ElementalType.Count] = new Delta();
    }

    /// <summary>
    /// 플레이어일 때 MP 자연회복(주기 MPITV마다 1 회복).
    /// </summary>
    private void UpdateMP()
    {
        if (GetAsset.Owner != BaseStatSO.OWNER.Player) return;

        MPdelta += Time.deltaTime;
        if (MPdelta > MPITV)
        {
            MPdelta = 0f;
            MPCurrent = Mathf.Clamp(MPCurrent + 1f, 0, MP);
        }
    }

    /// <summary>
    /// 원소 효과의 남은 지속시간(DUR) 감소 및 만료 처리.
    /// 만료 시 타입별 원복(Ice/Light) 및 아이콘/상태 정리.
    /// </summary>
    private void UpdateDUR()
    {
        foreach (var item in ELOnAttackContainer)
        {
            if (item.Key == ElementalManager.ElementalType.Count) continue;

            // 남은 시간 감소
            if (item.Value.DUR > DUR_RESET_VALUE)
            {
                item.Value.DUR -= Time.deltaTime;
            }
            else
            {
                // 효과 만료 시 타입별 원복 처리
                switch (item.Key)
                {
                    case ElementalManager.ElementalType.Ice:
                        switch (GetAsset.Owner)
                        {
                            case BaseStatSO.OWNER.Player:
                                MPITV = GetTotalOfStat(StatType.MPITV);
                                break;
                            case BaseStatSO.OWNER.Other:
                                MP = GetAsset.BaseMP;
                                break;
                        }
                        break;

                    case ElementalManager.ElementalType.Light:
                        switch (GetAsset.Owner)
                        {
                            case BaseStatSO.OWNER.Player:
                                MPITV = GetTotalOfStat(StatType.EVDR); // 원본 코드 유지
                                break;
                            case BaseStatSO.OWNER.Other:
                                MP = GetAsset.BaseEVDR;                 // 원본 코드 유지
                                break;
                        }
                        break;
                }

                ResetFromAllContainer(item.Key);
            }
        }
    }

    /// <summary>
    /// 원소 도트의 간헐 데미지 타이머(DEL) 갱신 및 틱 데미지 적용.
    /// </summary>
    private void UpdateDEL()
    {
        foreach (var item in ELDeltaContainer)
        {
            // 무속성, 비-도트 타입 스킵
            if (item.Key == ElementalManager.ElementalType.Count) continue;
            if (item.Key != ElementalManager.ElementalType.Fire &&
                item.Key != ElementalManager.ElementalType.Poison) continue;

            // 지속시간이 끝난 경우 정리
            if (ELOnAttackContainer[item.Key].DUR <= DUR_RESET_VALUE)
            {
                ResetFromAllContainer(item.Key);
                continue;
            }

            // 델타 증가
            if (item.Value.DEL >= DEL_START_VALUE)
                item.Value.DEL += Time.deltaTime;

            // 간격(ITV)에 도달 시 틱 데미지 적용
            if (item.Value.DEL >= ELOnAttackContainer[item.Key].ITV)
            {
                item.Value.DEL = DEL_START_VALUE;

                switch (GetAsset.Owner)
                {
                    case BaseStatSO.OWNER.Player:
                        GetComponent<PlayerController>().OnDamage(
                            new Calculator.DamageInfo(
                                item.Key == ElementalManager.ElementalType.Fire
                                    ? DamageTextController.DamageType.Fire
                                    : DamageTextController.DamageType.Poison,
                                HP * ELOnAttackContainer[item.Key].DMGR));
                        break;

                    case BaseStatSO.OWNER.Other:
                        GetComponent<MonsterController>().OnDamage(
                            new Calculator.DamageInfo(
                                item.Key == ElementalManager.ElementalType.Fire
                                    ? DamageTextController.DamageType.Fire
                                    : DamageTextController.DamageType.Poison,
                                HP * ELOnAttackContainer[item.Key].DMGR));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 특정 원소 타입의 도트/표시 상태를 초기화하고 메인 원소를 재평가한다.
    /// </summary>
    private void ResetFromAllContainer(ElementalManager.ElementalType key)
    {
        ELOnAttackContainer[key].DUR = DUR_RESET_VALUE;
        if (ELDeltaContainer.ContainsKey(key))
            ELDeltaContainer[key].DEL = DEL_RESET_VALUE;

        // 메인 원소 아이콘 비활성화
        switch (GetAsset.Owner)
        {
            case BaseStatSO.OWNER.Player:
                UIManager.Inst.ActivateELIcon(key, false);
                break;
            case BaseStatSO.OWNER.Other:
                GetComponent<MonsterUIController>().ActivateELIcon(key, false);
                break;
        }

        RefreshElOnAttack();
    }

    /// <summary>
    /// 잔여 지속시간이 가장 짧은 원소를 메인 원소(ELOnAttack)로 갱신.
    /// </summary>
    private void RefreshElOnAttack()
    {
        ELDamageInfo temp = new ELDamageInfo();

        foreach (var item in ELOnAttackContainer)
        {
            if (item.Value.ELTYPE == ElementalManager.ElementalType.Count) continue;
            if (item.Value.DUR <= DUR_RESET_VALUE) continue;

            if (temp.DUR > item.Value.DUR)
                temp = item.Value;
        }

        ELOnAttack = temp.ELTYPE;
    }

    /// <summary>
    /// 기본/강화/장비 수치를 합산해 최종 스탯을 반환한다.
    /// </summary>
    private float GetTotalOfStat(StatType SType)
    {
        StatSO selectedStatAsset = ResourceUtility.GetResourceByType<StatSO>(GetAssetPath(SType));
        float result = 0f;

        // 1) 기본
        result += selectedStatAsset.BASE;

        // 2) 강화(스탯 레벨)
        int level = GlobalValue.Instance.GetStatLevelByType(SType);
        result += selectedStatAsset.PER * level;

        // 3) 장비(해당 타입만)
        GearSO.StatType gType = ConvertStatToGearStat(SType);
        switch (gType)
        {
            case GearSO.StatType.HP:
            case GearSO.StatType.DEF:
            case GearSO.StatType.MATK:
            case GearSO.StatType.SATK:
            case GearSO.StatType.CTKR:
            case GearSO.StatType.EVDR:
                result += GlobalValue.Instance.GetEquipStatByEnum(gType);
                break;
        }

        return result;
    }

    /// <summary>
    /// 스탯 타입을 장비 능력치 타입으로 매핑한다(미대응 시 Count).
    /// </summary>
    private GearSO.StatType ConvertStatToGearStat(StatType SType)
    {
        switch (SType)
        {
            case StatType.HP: return GearSO.StatType.HP;
            case StatType.DEF: return GearSO.StatType.DEF;
            case StatType.MATK: return GearSO.StatType.MATK;
            case StatType.SATK: return GearSO.StatType.SATK;
            case StatType.CTKR: return GearSO.StatType.CTKR;
            case StatType.EVDR: return GearSO.StatType.EVDR;
            default: return GearSO.StatType.Count;
        }
    }

    // ── Inner Types

    /// <summary>원소 피해/도트 정보를 보관.</summary>
    private class ELDamageInfo
    {
        public ElementalManager.ElementalType ELTYPE;
        public float DMGR;
        public float DUR;
        public float ITV;

        // 원소 전용 생성자
        public ELDamageInfo(ElementalStatSO attacker)
        {
            ELTYPE = attacker.GetELTYPE();
            DMGR = attacker.GetDMGR(0);
            DUR = attacker.GetDUR(0);
            ITV = attacker.GetITV(0);
        }

        // 무속성 전용 생성자
        public ELDamageInfo()
        {
            ELTYPE = ElementalManager.ElementalType.Count;
            DMGR = 0f;
            DUR = float.MaxValue;
            ITV = 0f;
        }
    }

    /// <summary>도트 틱 타이머 컨테이너.</summary>
    private class Delta { public float DEL { get; set; } }

    // ── Getters

    public BaseStatSO GetAsset => baseStatAsset;
    private string GetAssetPath(StatType type) => STAT_SO_PRELINK + type + STAT_SO_POSTLINK;
}
