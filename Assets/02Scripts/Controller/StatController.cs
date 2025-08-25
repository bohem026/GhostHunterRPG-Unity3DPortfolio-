using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ����(�÷��̾�/����) ���Ȱ� ���� ��Ʈ(���� ����) ó��, MP �ڿ�ȸ���� �����Ѵ�.
/// - �ʱ�ȭ: �÷��̾�/���� ���� �ʱ�ȭ
/// - ��Ÿ��: MP ȸ��, ���� ��Ʈ Ÿ�̸�(DEL/ DUR) ���� �� ���� ����
/// - ��ƿ: ���/��ȭ/�⺻ ��ġ�� �ջ��� ���� ���� ���, UI ������ ����
/// </summary>
public class StatController : MonoBehaviour
{
    // ���� Constants
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const float DUR_RESET_VALUE = 0f;
    private const float DEL_RESET_VALUE = -1f;
    private const float DEL_START_VALUE = 0f;
    public static int MAX_STAT_LEVEL = 99;

    // ���� Serialized
    [SerializeField] private BaseStatSO baseStatAsset;

    // ���� Types
    public enum StatType { HP, MP, MPITV, MATK, SATK, DEF, CTKR, EVDR, STBR, Count }

    // ���� Final Stats (�ջ� ���)
    public float HP { get; set; }     // �ִ� ü��
    public float MP { get; set; }     // �ִ� ����
    public float MPITV { get; set; }  // ���� ȸ�� �ֱ�(�ʴ� ƽ ����)
    public float MATK { get; set; }   // ���� ���ݷ�
    public float SATK { get; set; }   // ���� ���ݷ�
    public float DEF { get; set; }    // ����
    public float CTKR { get; set; }   // ġ���
    public float EVDR { get; set; }   // ȸ�� ��ġ
    public float STBR { get; set; }   // ���� ��ġ

    // ���� Runtime Stats
    public float HPCurrent { get; set; }
    public float MPCurrent { get; set; }
    public ElementalManager.ElementalType ELResist { get; set; }
    public ElementalManager.ElementalType ELOnAttack { get; set; }

    // ���� ���� ���� ���� ��Ʈ/ȿ�� �� Ÿ�̸� �����̳�
    private Dictionary<ElementalManager.ElementalType, ELDamageInfo> ELOnAttackContainer;
    private Dictionary<ElementalManager.ElementalType, Delta> ELDeltaContainer;

    // ���� State
    private bool isInitialized;
    private float MPdelta = 0f;

    // ���� Unity Lifecycle

    private void OnEnable()
    {
        if (isInitialized) return;

        Init();

        // ���� Ÿ�Կ� �´� �ʱ� ���� ����
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
        // �÷��̾� �Է� �Ұ�(��: �Ͻ�����/��Ʈ��) �� ���� ����
        if (GetComponent<PlayerController>())
            if (!GetComponent<PlayerController>().inputEnabled)
                return;

        UpdateMP();
        UpdateDUR();
        UpdateDEL();
    }

    // ���� Public: �ʱ�ȭ

    /// <summary>
    /// �÷��̾� ���� �ʱ�ȭ(�⺻+��ȭ+��� �ջ�).
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
    /// ���� ���� �ʱ�ȭ(�������� ���� ���).
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

    // ���� Public: ���� ���� ����

    /// <summary>
    /// ���� ����ȿ�� ���� ��(�ǰ� ����) ��Ʈ/������� ����Ѵ�.
    /// </summary>
    public void OnELDamage(ElementalController attacker)
    {
        ELDamageInfo elAttackInfo = new ELDamageInfo(attacker.GetAsset);

        // ���� ���� ȿ�� �ֽ�ȭ
        ELOnAttackContainer[elAttackInfo.ELTYPE] = elAttackInfo;

        // ��Ʈ(���� ����) Ÿ���̸� ��Ÿ �����̳� ���
        if (elAttackInfo.ITV > 0f)
        {
            ELDeltaContainer[elAttackInfo.ELTYPE] = new Delta();
        }
        else
        {
            // ��ü�/���� ������� ó��(Ice/Light)
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

        // ���� ���� ������ Ȱ��ȭ
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

    // ���� Private: �ʱⱸ��/��ƿ

    /// <summary>
    /// �����̳� �ʱ�ȭ �� ���Ӽ� ���� ���.
    /// </summary>
    private void Init()
    {
        ELOnAttackContainer = new Dictionary<ElementalManager.ElementalType, ELDamageInfo>();
        ELDeltaContainer = new Dictionary<ElementalManager.ElementalType, Delta>();

        ELOnAttackContainer[ElementalManager.ElementalType.Count] = new ELDamageInfo();
        ELDeltaContainer[ElementalManager.ElementalType.Count] = new Delta();
    }

    /// <summary>
    /// �÷��̾��� �� MP �ڿ�ȸ��(�ֱ� MPITV���� 1 ȸ��).
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
    /// ���� ȿ���� ���� ���ӽð�(DUR) ���� �� ���� ó��.
    /// ���� �� Ÿ�Ժ� ����(Ice/Light) �� ������/���� ����.
    /// </summary>
    private void UpdateDUR()
    {
        foreach (var item in ELOnAttackContainer)
        {
            if (item.Key == ElementalManager.ElementalType.Count) continue;

            // ���� �ð� ����
            if (item.Value.DUR > DUR_RESET_VALUE)
            {
                item.Value.DUR -= Time.deltaTime;
            }
            else
            {
                // ȿ�� ���� �� Ÿ�Ժ� ���� ó��
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
                                MPITV = GetTotalOfStat(StatType.EVDR); // ���� �ڵ� ����
                                break;
                            case BaseStatSO.OWNER.Other:
                                MP = GetAsset.BaseEVDR;                 // ���� �ڵ� ����
                                break;
                        }
                        break;
                }

                ResetFromAllContainer(item.Key);
            }
        }
    }

    /// <summary>
    /// ���� ��Ʈ�� ���� ������ Ÿ�̸�(DEL) ���� �� ƽ ������ ����.
    /// </summary>
    private void UpdateDEL()
    {
        foreach (var item in ELDeltaContainer)
        {
            // ���Ӽ�, ��-��Ʈ Ÿ�� ��ŵ
            if (item.Key == ElementalManager.ElementalType.Count) continue;
            if (item.Key != ElementalManager.ElementalType.Fire &&
                item.Key != ElementalManager.ElementalType.Poison) continue;

            // ���ӽð��� ���� ��� ����
            if (ELOnAttackContainer[item.Key].DUR <= DUR_RESET_VALUE)
            {
                ResetFromAllContainer(item.Key);
                continue;
            }

            // ��Ÿ ����
            if (item.Value.DEL >= DEL_START_VALUE)
                item.Value.DEL += Time.deltaTime;

            // ����(ITV)�� ���� �� ƽ ������ ����
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
    /// Ư�� ���� Ÿ���� ��Ʈ/ǥ�� ���¸� �ʱ�ȭ�ϰ� ���� ���Ҹ� �����Ѵ�.
    /// </summary>
    private void ResetFromAllContainer(ElementalManager.ElementalType key)
    {
        ELOnAttackContainer[key].DUR = DUR_RESET_VALUE;
        if (ELDeltaContainer.ContainsKey(key))
            ELDeltaContainer[key].DEL = DEL_RESET_VALUE;

        // ���� ���� ������ ��Ȱ��ȭ
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
    /// �ܿ� ���ӽð��� ���� ª�� ���Ҹ� ���� ����(ELOnAttack)�� ����.
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
    /// �⺻/��ȭ/��� ��ġ�� �ջ��� ���� ������ ��ȯ�Ѵ�.
    /// </summary>
    private float GetTotalOfStat(StatType SType)
    {
        StatSO selectedStatAsset = ResourceUtility.GetResourceByType<StatSO>(GetAssetPath(SType));
        float result = 0f;

        // 1) �⺻
        result += selectedStatAsset.BASE;

        // 2) ��ȭ(���� ����)
        int level = GlobalValue.Instance.GetStatLevelByType(SType);
        result += selectedStatAsset.PER * level;

        // 3) ���(�ش� Ÿ�Ը�)
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
    /// ���� Ÿ���� ��� �ɷ�ġ Ÿ������ �����Ѵ�(�̴��� �� Count).
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

    // ���� Inner Types

    /// <summary>���� ����/��Ʈ ������ ����.</summary>
    private class ELDamageInfo
    {
        public ElementalManager.ElementalType ELTYPE;
        public float DMGR;
        public float DUR;
        public float ITV;

        // ���� ���� ������
        public ELDamageInfo(ElementalStatSO attacker)
        {
            ELTYPE = attacker.GetELTYPE();
            DMGR = attacker.GetDMGR(0);
            DUR = attacker.GetDUR(0);
            ITV = attacker.GetITV(0);
        }

        // ���Ӽ� ���� ������
        public ELDamageInfo()
        {
            ELTYPE = ElementalManager.ElementalType.Count;
            DMGR = 0f;
            DUR = float.MaxValue;
            ITV = 0f;
        }
    }

    /// <summary>��Ʈ ƽ Ÿ�̸� �����̳�.</summary>
    private class Delta { public float DEL { get; set; } }

    // ���� Getters

    public BaseStatSO GetAsset => baseStatAsset;
    private string GetAssetPath(StatType type) => STAT_SO_PRELINK + type + STAT_SO_POSTLINK;
}
