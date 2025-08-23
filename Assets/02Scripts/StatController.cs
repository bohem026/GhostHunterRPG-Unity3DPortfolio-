using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.InputSystem;
using static Cinemachine.DocumentationSortingAttribute;
//using static UnityEditor.Progress;

public class StatController : MonoBehaviour
{
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const float DUR_RESET_VALUE = 0f;
    private const float DEL_RESET_VALUE = -1f;
    private const float DEL_START_VALUE = 0f;
    public static int MAX_STAT_LEVEL = 99;

    [SerializeField] private BaseStatSO baseStatAsset;

    public enum StatType
    {
        HP, MP, MPITV, MATK, SATK,
        DEF, CTKR, EVDR, STBR, Count/*Length*/
    }

    public float HP { get; set; }       // 최대 체력
    public float MP { get; set; }       // 최대 마나
    public float MPITV { get; set; }       // 마나 회복 주기
    public float MATK { get; set; }      // 물리 공격력
    public float SATK { get; set; }      // 마법 공격력
    public float DEF { get; set; }      // 방어력
    public float CTKR { get; set; }     // 치명률
    public float EVDR { get; set; }      // 회피 수치
    public float STBR { get; set; }      // 안정 수치

    //--- Current Stat.
    public float HPCurrent { get; set; }       // 현재 체력
    public float MPCurrent { get; set; }       // 현재 마나
    public ElementalManager.ElementalType
        ELResist
    { get; set; }   // 대표 속성 저항
    public ElementalManager.ElementalType
        ELOnAttack
    { get; set; }   // 대표 속성 피해

    private Dictionary
        <ElementalManager.ElementalType
        , ELDamageInfo>
        ELOnAttackContainer;    // 현재 속성 피해

    private Dictionary
        <ElementalManager.ElementalType
        , Delta>
        ELDeltaContainer;

    private bool isInitialized;
    private float MPdelta = 0f;
    //---

    void OnEnable()
    {
        if (isInitialized) return;

        Init();
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

    void Update()
    {
        if (GetComponent<PlayerController>())
            if (!GetComponent<PlayerController>().inputEnabled)
                return;

        UpdateMP();
        UpdateDUR();
        UpdateDEL();
    }

    private void UpdateMP()
    {
        if (GetAsset.Owner != BaseStatSO.OWNER.Player)
            return;

        MPdelta += Time.deltaTime;
        if (MPdelta > MPITV)
        {
            MPdelta = 0f;
            MPCurrent = Mathf.Clamp(MPCurrent += 1f, 0, MP);
        }
    }

    /// <summary>
    /// 원소 도트 대미지 타이머
    /// </summary>
    private void UpdateDEL()
    {
        foreach (var item in ELDeltaContainer)
        {
            //Ignore non-elemental type.
            if (item.Key == ElementalManager.ElementalType.Count)
                continue;

            //Ignore non-dot type attack.
            if (item.Key != ElementalManager.ElementalType.Fire
                && item.Key != ElementalManager.ElementalType.Poison)
                continue;

            //Ignore finished elemental attack.
            if (ELOnAttackContainer[item.Key].DUR <= DUR_RESET_VALUE)
            {
                ResetFromAllContainer(item.Key);
                continue;
            }

            //1. Increase delta.
            if (item.Value.DEL >= DEL_START_VALUE)
                item.Value.DEL += Time.deltaTime;

            //2. Refresh delta if delta becomes more than interval.
            //   On damaged
            if (item.Value.DEL >= ELOnAttackContainer[item.Key].ITV)
            {
                item.Value.DEL = DEL_START_VALUE;

                //--- On Damaged
                switch (GetAsset.Owner)
                {
                    case BaseStatSO.OWNER.Player:
                        GetComponent<PlayerController>()
                            .OnDamage(new Calculator.DamageInfo(
                                item.Key == ElementalManager.ElementalType.Fire
                                ? DamageTextController.DamageType.Fire
                                : DamageTextController.DamageType.Poison
                                , HP * ELOnAttackContainer[item.Key].DMGR));
                        break;
                    case BaseStatSO.OWNER.Other:
                        GetComponent<MonsterController>()
                            .OnDamage(new Calculator.DamageInfo(
                                item.Key == ElementalManager.ElementalType.Fire
                                ? DamageTextController.DamageType.Fire
                                : DamageTextController.DamageType.Poison
                                , HP * ELOnAttackContainer[item.Key].DMGR));
                        break;
                    default:
                        break;
                }
                //---
            }
        }
    }

    /// <summary>
    /// 현재 중첩된 원소 도트 대미지
    /// </summary>
    private void UpdateDUR()
    {
        foreach (var item in ELOnAttackContainer)
        {
            if (item.Key == ElementalManager.ElementalType.Count)
                continue;

            //1. Decrease duration if duration more than 0.
            if (item.Value.DUR > DUR_RESET_VALUE)
            {
                item.Value.DUR -= Time.deltaTime;
            }
            //2. Remove item if duration even or less than 0.
            else
            {
                //Ice, Light: Reset.
                switch (item.Key)
                {
                    case ElementalManager.ElementalType.Ice:
                        switch (GetAsset.Owner)
                        {
                            case BaseStatSO.OWNER.Player:
                                //MPITV = GetAsset.BaseMPITV(0/*Level*/);
                                //MPITV += GameManager.Inst
                                //        ._PlayerInventory
                                //        .EquipMPITV(0/*Level*/);
                                MPITV = GetTotalOfStat(StatType.MPITV);
                                break;
                            case BaseStatSO.OWNER.Other:
                                //MP = GetAsset.BaseMP(0/*Level*/);
                                MP = GetAsset.BaseMP;
                                break;
                            default:
                                break;
                        }
                        break;
                    case ElementalManager.ElementalType.Light:
                        //EVDR = GetAsset.BaseEVDR(0/*Level*/);
                        //if (GetAsset.Owner == BaseStatSO.OWNER.Player)
                        //    EVDR += GameManager.Inst
                        //            ._PlayerInventory
                        //            .EquipEVDR(0/*Level*/);
                        switch (GetAsset.Owner)
                        {
                            case BaseStatSO.OWNER.Player:
                                MPITV = GetTotalOfStat(StatType.EVDR);
                                break;
                            case BaseStatSO.OWNER.Other:
                                MP = GetAsset.BaseEVDR;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                ResetFromAllContainer(item.Key);
            }
        }
    }

    private class ELDamageInfo
    {
        public ElementalManager.ElementalType ELTYPE;
        public float DMGR;
        public float DUR;
        public float ITV;

        /// <summary>
        /// 원소 속성 전용 생성자입니다.
        /// </summary>
        /// <param name="attacker">공격 주체의 원소 스탯 SO</param>
        public ELDamageInfo(ElementalStatSO attacker)
        {
            ELTYPE = attacker.GetELTYPE();
            DMGR = attacker.GetDMGR(0/*Level*/);
            DUR = attacker.GetDUR(0/*Level*/);
            ITV = attacker.GetITV(0/*Level*/);
        }

        /// <summary>
        /// 무속성 전용 생성자입니다.
        /// </summary>
        public ELDamageInfo()
        {
            ELTYPE = ElementalManager.ElementalType.Count;
            DMGR = 0f;
            DUR = float.MaxValue;
            ITV = 0f;
        }
    }

    private class Delta { public float DEL { get; set; } }

    private void ResetFromAllContainer(ElementalManager.ElementalType key)
    {
        ELOnAttackContainer[key].DUR = DUR_RESET_VALUE;
        if (ELDeltaContainer.ContainsKey(key))
            ELDeltaContainer[key].DEL = DEL_RESET_VALUE;

        //Deactivate main elemental attack icon.
        switch (GetAsset.Owner)
        {
            case BaseStatSO.OWNER.Player:
                UIManager.Inst.ActivateELIcon(key, false);
                break;
            case BaseStatSO.OWNER.Other:
                GetComponent<MonsterUIController>()
                    .ActivateELIcon(key, false);
                break;
            default:
                break;
        }

        RefreshElOnAttack();
    }

    private void Init()
    {
        ELOnAttackContainer = new Dictionary
            <ElementalManager.ElementalType, ELDamageInfo>();
        ELDeltaContainer = new Dictionary
            <ElementalManager.ElementalType, Delta>();

        //Initiate non-elemental's container.
        ELOnAttackContainer
            [ElementalManager.ElementalType.Count] = new ELDamageInfo();
        ELDeltaContainer
            [ElementalManager.ElementalType.Count] = new Delta();
    }

    /// <summary>
    /// 플레이어 스탯 초기화 메서드 입니다.
    /// </summary>
    public void InitStat()
    {
        //1. Initiate base stat.
        HP = GetTotalOfStat(StatType.HP);
        MP = 80 + (int)(GlobalValue.Instance.GetStatLevelByType(StatType.MPITV) / 5);
        MPITV = GetTotalOfStat(StatType.MPITV);
        DEF = GetTotalOfStat(StatType.DEF);
        MATK = GetTotalOfStat(StatType.MATK);
        SATK = GetTotalOfStat(StatType.SATK);
        CTKR = GetTotalOfStat(StatType.CTKR);
        EVDR = GetTotalOfStat(StatType.EVDR);
        STBR = GetTotalOfStat(StatType.STBR);

        //2. Initiate current stat.
        HPCurrent = HP;
        MPCurrent = 0f;
        ELResist = ElementalManager.ElementalType.Count;    //No elemental damage.
        ELOnAttack = ElementalManager.ElementalType.Count;    //No elemental damage.

        TestDebug();
    }

    private float GetTotalOfStat(StatType SType)
    {
        StatSO selectedStatAsset =
            ResourceUtility.GetResourceByType<StatSO>
            (GetAssetPath(SType));
        GearSO.StatType GType = GearSO.StatType.Count;

        float result = 0f;
        int level = 0;

        //1. Elapse base stat.
        result += selectedStatAsset.BASE;
        //2. Elapse enhanced stat.
        level = GlobalValue.Instance.GetStatLevelByType(SType);
        result += selectedStatAsset.PER * level;
        //3. Elapse equipped gear stat.
        GType = ConvertStatToGearStat(SType);
        switch (GType)
        {
            //Stats associated with gear ability.
            case GearSO.StatType.HP:
            case GearSO.StatType.DEF:
            case GearSO.StatType.MATK:
            case GearSO.StatType.SATK:
            case GearSO.StatType.CTKR:
            case GearSO.StatType.EVDR:
                result += GlobalValue.Instance.GetEquipStatByEnum(GType);
                break;
            //Stats not associated with gear ability.
            default:
                break;
        }

        return result;
    }

    private GearSO.StatType ConvertStatToGearStat(StatType SType)
    {
        GearSO.StatType GType;

        switch (SType)
        {
            case StatType.HP:
                GType = GearSO.StatType.HP;
                break;
            case StatType.DEF:
                GType = GearSO.StatType.DEF;
                break;
            case StatType.MATK:
                GType = GearSO.StatType.MATK;
                break;
            case StatType.SATK:
                GType = GearSO.StatType.SATK;
                break;
            case StatType.CTKR:
                GType = GearSO.StatType.CTKR;
                break;
            case StatType.EVDR:
                GType = GearSO.StatType.EVDR;
                break;
            default:
                GType = GearSO.StatType.Count;
                break;
        }

        return GType;
    }

    /// <summary>
    /// 몬스터 스탯 초기화 메서드 입니다.
    /// </summary>
    public void InitStat(int level)
    {
        //1. Initiate base stat.
        HP = baseStatAsset.BaseHP * 0.33f * (2 + level);
        MP = Mathf.Clamp
            (baseStatAsset.BaseMP / 100 * (100 - 3.3f * level),
            5.66f,
            baseStatAsset.BaseMP);
        MPITV = baseStatAsset.BaseMPITV;    //x
        MATK = baseStatAsset.BaseMATK * 0.33f * (2 + level);
        SATK = baseStatAsset.BaseSATK * 0.33f * (2 + level);
        DEF = baseStatAsset.BaseDEF * 0.33f * (2 + level);
        CTKR = baseStatAsset.BaseCTKR * 0.25f * (3 + level);
        EVDR = baseStatAsset.BaseEVDR * 0.25f * (3 + level);
        STBR = baseStatAsset.BaseSTBR * 0.25f * (3 + level);

        //2. Initiate current stat.
        HPCurrent = HP;
        MPCurrent = MP;
        ELResist = ElementalManager.ElementalType.Count;    //No elemental damage.
        ELOnAttack = ElementalManager.ElementalType.Count;    //No elemental damage.
    }

    //원소 도트 데미지
    public void OnELDamage(ElementalController attacker)
    {
        ELDamageInfo elAttackInfo = new ELDamageInfo(attacker.GetAsset);

        //Refresh elemental damage info.
        ELOnAttackContainer[elAttackInfo.ELTYPE] = elAttackInfo;

        //Fire, Poison: Register to delta container.
        if (elAttackInfo.ITV > 0f)
            ELDeltaContainer[elAttackInfo.ELTYPE] = new Delta();
        else
        {
            switch (elAttackInfo.ELTYPE)
            {
                //Ice: Increase MPITV(Player) and MP(Monster)
                case ElementalManager.ElementalType.Ice:
                    switch (GetAsset.Owner)
                    {
                        case BaseStatSO.OWNER.Player:
                            MPITV *= elAttackInfo.DMGR;
                            break;
                        case BaseStatSO.OWNER.Other:
                            MP *= elAttackInfo.DMGR;
                            break;
                        default:
                            break;
                    }

                    break;
                //Light: Set EVDR to 0.
                case ElementalManager.ElementalType.Light:
                    EVDR = 0f;
                    break;
                default:
                    break;
            }
        }

        //Set this elemental type as ELOnAttack.
        ELOnAttack = elAttackInfo.ELTYPE;

        //Activate main elemental attack icon.
        //Owner= Object get damage.
        switch (GetAsset.Owner)
        {
            case BaseStatSO.OWNER.Player:
                UIManager.Inst.ActivateELIcon(ELOnAttack, true);
                break;
            case BaseStatSO.OWNER.Other:
                GetComponent<MonsterUIController>()
                    .ActivateELIcon(ELOnAttack, true);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 현재 잔여 지속시간이 가장 짧은 원소 타입으로 ElOnAttack 교체.
    /// </summary>
    private void RefreshElOnAttack()
    {
        ELDamageInfo temp = new ELDamageInfo();

        foreach (var item in ELOnAttackContainer)
        {
            //Ignore non-elemental attack.
            if (item.Value.ELTYPE == ElementalManager.ElementalType.Count)
                continue;

            //Ignore finished elemental attack.
            if (item.Value.DUR <= DUR_RESET_VALUE)
                continue;

            //Update ELOnAttack to elemental type
            //has minimum remaining duration.
            if (temp.DUR > item.Value.DUR)
                temp = item.Value;
        }

        ELOnAttack = temp.ELTYPE;
    }

    #region GET
    public BaseStatSO GetAsset => baseStatAsset;

    private string GetAssetPath(StatType type)
        => STAT_SO_PRELINK + type.ToString() + STAT_SO_POSTLINK;
    #endregion

    /*Test*/
    public void TestDebug()
    {
        Debug.Log($"HP: {HP}");
        Debug.Log($"MP: {MP}");
        Debug.Log($"MPITV: {MPITV}");
        Debug.Log($"DEF: {DEF}");
        Debug.Log($"MATK: {MATK}");
        Debug.Log($"SATK: {SATK}");
        Debug.Log($"CTKR: {CTKR}");
        Debug.Log($"EVDR: {EVDR}");
        Debug.Log($"STBR: {STBR}");
        Debug.Log($"HPCurrent: {HPCurrent}");
        Debug.Log($"MPCurrent: {MPCurrent}");
    }
    /*Test*/
}
