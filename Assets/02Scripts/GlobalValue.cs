using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

public class GlobalValue : MonoBehaviour
{
    public static GlobalValue Instance { get; private set; }
    public bool IsDataLoaded { get; private set; }

    public enum GearCommand { Get, Sell, Equip, Unequip, Count }
    public enum SkillOrder { E, Q, Count }

    [HideInInspector] public StatLevel _StatLevel;
    [HideInInspector] public SkillLevel _SkillLevel;
    [HideInInspector] public Inven _Inven;
    [HideInInspector] public Info _Info;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task LoadData()
    {
        await LoadStatLevel();
        await LoadSkillLevel();
        await LoadInven();
        await LoadInfo();

        IsDataLoaded = true;
    }

    private async Task LoadInfo()
    {
        string json = await FBFirestoreManager.Instance.GetJsonField("info");
        if (!string.IsNullOrEmpty(json))
        {
            _Info = JsonUtility.FromJson<Info>(json);
        }
        else
        {
            _Info = new Info();
            await SaveInfo();
        }
    }

    private async Task LoadInven()
    {
        string json = await FBFirestoreManager.Instance.GetJsonField("inven");
        if (!string.IsNullOrEmpty(json))
        {
            _Inven = JsonUtility.FromJson<Inven>(json);
        }
        else
        {
            _Inven = new Inven();
            await SaveInven();
        }
    }

    private async Task LoadStatLevel()
    {
        string json = await FBFirestoreManager.Instance.GetJsonField("stat");
        if (!string.IsNullOrEmpty(json))
        {
            _StatLevel = JsonUtility.FromJson<StatLevel>(json);
        }
        else
        {
            _StatLevel = new StatLevel();
            await SaveStatLevel();
        }
    }

    private async Task LoadSkillLevel()
    {
        string json = await FBFirestoreManager.Instance.GetJsonField("skill");
        if (!string.IsNullOrEmpty(json))
        {
            _SkillLevel = JsonUtility.FromJson<SkillLevel>(json);
        }
        else
        {
            _SkillLevel = new SkillLevel();
            await SaveSkillLevel();
        }
    }

    public Task SaveInfo() => FBFirestoreManager.Instance.SaveJsonField("info", _Info);
    public Task SaveInven() => FBFirestoreManager.Instance.SaveJsonField("inven", _Inven);
    public Task SaveStatLevel() => FBFirestoreManager.Instance.SaveJsonField("stat", _StatLevel);
    public Task SaveSkillLevel() => FBFirestoreManager.Instance.SaveJsonField("skill", _SkillLevel);

    public int GetStatLevelByType(StatController.StatType type)
    {
        switch (type)
        {
            case StatController.StatType.HP:
                return _StatLevel.HP_LV;
            case StatController.StatType.MPITV:
                return _StatLevel.MPITV_LV;
            case StatController.StatType.DEF:
                return _StatLevel.DEF_LV;
            case StatController.StatType.MATK:
                return _StatLevel.MATK_LV;
            case StatController.StatType.SATK:
                return _StatLevel.SATK_LV;
            case StatController.StatType.CTKR:
                return _StatLevel.CTKR_LV;
            case StatController.StatType.EVDR:
                return _StatLevel.EVDR_LV;
            case StatController.StatType.STBR:
                return _StatLevel.STBR_LV;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }
    }

    public int ElapseStatLevelByType
        (
        StatController.StatType type
        , int max
        , int value
        )
    {
        int result;

        switch (type)
        {
            case StatController.StatType.HP:
                result = (_StatLevel.HP_LV
                    = Mathf.Clamp(_StatLevel.HP_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.MPITV:
                result = (_StatLevel.MPITV_LV
                    = Mathf.Clamp(_StatLevel.MPITV_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.DEF:
                result = (_StatLevel.DEF_LV
                    = Mathf.Clamp(_StatLevel.DEF_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.MATK:
                result = (_StatLevel.MATK_LV
                    = Mathf.Clamp(_StatLevel.MATK_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.SATK:
                result = (_StatLevel.SATK_LV
                    = Mathf.Clamp(_StatLevel.SATK_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.CTKR:
                result = (_StatLevel.CTKR_LV
                    = Mathf.Clamp(_StatLevel.CTKR_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.EVDR:
                result = (_StatLevel.EVDR_LV
                    = Mathf.Clamp(_StatLevel.EVDR_LV + value
                    , 0
                    , max));
                break;
            case StatController.StatType.STBR:
                result = (_StatLevel.STBR_LV
                    = Mathf.Clamp(_StatLevel.STBR_LV + value
                    , 0
                    , max));
                break;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }

        SaveStatLevel();
        return result;
    }

    public int GetSkillLevelByEnum
        (
        SkillWindowController.SKType outer
        , SkillWindowController.ELType inner
        )
    {
        if ((int)outer >= (int)SkillWindowController.SKType.Count
            || (int)inner >= (int)SkillWindowController.ELType.Count)
        {
            Debug.Log("[!!ERROR!!] UNDEFINED SKILL");
            return 0;
        }

        return _SkillLevel.skillLevelList[(int)outer].levels[(int)inner];
    }

    public int ElapseSkillLevelByEnum
        (
        SkillWindowController.SKType outer
        , SkillWindowController.ELType inner
        , int max
        , int value
        )
    {
        int result;

        if ((int)outer >= _SkillLevel.skillLevelList.Capacity
            || (int)inner >= _SkillLevel.skillLevelList[0].levels.Capacity)
        {
            Debug.Log("[!!ERROR!!] UNDEFINED SKILL");
            return 0;
        }

        result = _SkillLevel.skillLevelList[(int)outer].levels[(int)inner];
        result += value;
        _SkillLevel.skillLevelList[(int)outer].levels[(int)inner] = result;

        SaveSkillLevel();
        return result;
    }

    public List<int> GetSkillIDsFromInfo()
    {
        return _Info.skills;
    }

    public int SwitchSkillIDFromInfo
        (
        int id,
        SkillOrder order
        )
    {
        int preID = _Info.skills[(int)order];
        _Info.skills[(int)order] = id;
        SaveInfo();

        return preID;
    }

    public int RemoveSkillIDFromInfo(SkillOrder order)
    {
        int preID = _Info.skills[(int)order];
        _Info.skills[(int)order] = -1;
        SaveInfo();

        return preID;
    }

    public int GetBestStageFromInfo()
    {
        return _Info.BEST_STAGE;
    }

    public int GetGearCountByEnum
        (
        GearController.Rarity outer
        , GearController.GearType inner
        , GearWindowController.GearLocation location
        )
    {
        //switch (location)
        //{
        //    case GearWindowController.GearLocation.Inven:
        //        return _Inven.gearInvenCountList[(int)outer].counts[(int)inner];
        //    case GearWindowController.GearLocation.Equip:
        //        return _Inven.gearEquipCountList[(int)outer].counts[(int)inner];
        //    default:
        //        return 0;
        //}

        try
        {
            switch (location)
            {
                case GearWindowController.GearLocation.Inven:
                    return _Inven.gearInvenCountList[(int)outer].counts[(int)inner];
                case GearWindowController.GearLocation.Equip:
                    return _Inven.gearEquipCountList[(int)outer].counts[(int)inner];
                default:
                    return 0;
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Debug.LogError($"[EXCEPTION] Invalid gear index: outer={(int)outer}, inner={(int)inner}, location={location}\n{ex}");
            return 0;
        }
    }

    public void ElapseGearCountByEnum
        (
        GearController.Rarity outer
        , GearController.GearType inner
        , GearCommand command
        , int value = 1
        )
    {
        if (!IsGearAvailable(outer, inner))
        {
            Debug.Log("[!!ERROR!!] UNDEFINED GEAR");
            return;
        }

        int OUTER_LEN = (int)_Inven.gearEquipCountList.Count;

        switch (command)
        {
            case GearCommand.Get:
                _Inven.gearInvenCountList[(int)outer].counts[(int)inner] += value;
                break;
            case GearCommand.Sell:
                _Inven.gearInvenCountList[(int)outer].counts[(int)inner] -= value;
                break;
            case GearCommand.Equip:
                //1. Unequip all same type of gear.
                for (int o = 0; o < OUTER_LEN; o++)
                {
                    if (_Inven.gearEquipCountList[o].counts[(int)inner] > 0)
                        _Inven.gearInvenCountList[o].counts[(int)inner]++;

                    _Inven.gearEquipCountList[o].counts[(int)inner] = 0;
                }
                //2. Bring gear from inven.
                if (--_Inven.gearInvenCountList[(int)outer].counts[(int)inner] < 0)
                    _Inven.gearInvenCountList[(int)outer].counts[(int)inner] = 0;
                //3. Put gear in equip.
                _Inven.gearEquipCountList[(int)outer].counts[(int)inner]++;
                break;
            case GearCommand.Unequip:
                //1. Unequip all same type of gear.
                foreach (var count in _Inven.gearEquipCountList)
                {
                    count.counts[(int)inner] = 0;
                }
                //2. Put gear in equip.
                _Inven.gearInvenCountList[(int)outer].counts[(int)inner]++;
                break;
            default:
                break;
        }

        SaveInven();
        return;
    }

    public float GetEquipStatByEnum(GearSO.StatType type)
    {
        switch (type)
        {
            case GearSO.StatType.HP:
                return _Inven._EquipStat.HP;
            case GearSO.StatType.DEF:
                return _Inven._EquipStat.DEF;
            case GearSO.StatType.MATK:
                return _Inven._EquipStat.MATK;
            case GearSO.StatType.SATK:
                return _Inven._EquipStat.SATK;
            case GearSO.StatType.CTKR:
                return _Inven._EquipStat.CTKR;
            case GearSO.StatType.EVDR:
                return _Inven._EquipStat.EVDR;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }
    }

    public float ElapseEquipStatByEnum
        (
        GearSO.StatType type,
        float value
        )
    {
        float result = 0f;

        switch (type)
        {
            case GearSO.StatType.HP:
                _Inven._EquipStat.HP
                    = Mathf.Clamp(_Inven._EquipStat.HP + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.HP;
                break;
            case GearSO.StatType.DEF:
                _Inven._EquipStat.DEF
                    = Mathf.Clamp(_Inven._EquipStat.DEF + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.DEF;
                break;
            case GearSO.StatType.MATK:
                _Inven._EquipStat.MATK
                    = Mathf.Clamp(_Inven._EquipStat.MATK + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.MATK;
                break;
            case GearSO.StatType.SATK:
                _Inven._EquipStat.SATK
                    = Mathf.Clamp(_Inven._EquipStat.SATK + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.SATK;
                break;
            case GearSO.StatType.CTKR:
                _Inven._EquipStat.CTKR
                    = Mathf.Clamp(_Inven._EquipStat.CTKR + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.CTKR;
                break;
            case GearSO.StatType.EVDR:
                _Inven._EquipStat.EVDR
                    = Mathf.Clamp(_Inven._EquipStat.EVDR + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.EVDR;
                break;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0f;
        }

        SaveInven();
        return result;
    }

    private bool IsGearAvailable
        (
        GearController.Rarity outer
        , GearController.GearType inner
        )
    {
        return !((int)outer >= _Inven.gearEquipCountList.Capacity
                || (int)inner >= _Inven.gearEquipCountList.Capacity
                || (int)outer >= _Inven.gearInvenCountList.Capacity
                || (int)inner >= _Inven.gearInvenCountList.Capacity);
    }
}

[Serializable]
public class StatLevel
{
    public int HP_LV;
    public int MP_LV;
    public int MPITV_LV;
    public int MATK_LV;
    public int SATK_LV;
    public int DEF_LV;
    public int CTKR_LV;
    public int EVDR_LV;
    public int STBR_LV;

    public StatLevel()
    {
        HP_LV = 0;
        MP_LV = 0;
        MPITV_LV = 0;
        MATK_LV = 0;
        SATK_LV = 0;
        DEF_LV = 0;
        CTKR_LV = 0;
        EVDR_LV = 0;
        STBR_LV = 0;
    }
}

[Serializable]
public class SkillLevel
{
    public List<SkillLevelInnerList> skillLevelList;

    public SkillLevel()
    {
        skillLevelList = new List<SkillLevelInnerList>(3)
        {
            new SkillLevelInnerList(),
            new SkillLevelInnerList(),
            new SkillLevelInnerList()
        };
    }
}

[Serializable]
public class Inven
{
    public int STGEM_CNT;
    public int SKGEM_CNT;
    public List<GearCountInnerList> gearInvenCountList;
    public List<GearCountInnerList> gearEquipCountList;
    public EquipStat _EquipStat;

    public Inven()
    {
        STGEM_CNT = 99; //10
        SKGEM_CNT = 99;  //3
        gearInvenCountList = new List<GearCountInnerList>(4)
        {
            new GearCountInnerList(0),
            new GearCountInnerList(),
            new GearCountInnerList(),
            new GearCountInnerList()
        };
        gearEquipCountList = new List<GearCountInnerList>(4)
        {
            new GearCountInnerList(),
            new GearCountInnerList(),
            new GearCountInnerList(),
            new GearCountInnerList()
        };
        _EquipStat = new EquipStat();
    }
}

[Serializable]
public class SkillLevelInnerList
{
    public List<int> levels;

    public SkillLevelInnerList()
    {
        levels = new List<int>(4) { 0, 0, 0, 0 };
    }
}

[Serializable]
public class GearCountInnerList
{
    public List<int> counts;

    public GearCountInnerList()
    {
        counts = new List<int>(4) { 0, 0, 0, 0 };
    }

    public GearCountInnerList(int firstAid)
    {
        counts = new List<int>(4) { 1, 1, 1, 1 };
    }
}

[System.Serializable]
public class EquipStat
{
    public float HP;
    public float DEF;
    public float MATK;
    public float SATK;
    public float CTKR;
    public float EVDR;

    public EquipStat()
    {
        HP = 0f;
        DEF = 0f;
        MATK = 0f;
        SATK = 0f;
        CTKR = 0f;
        EVDR = 0f;
    }
}

[System.Serializable]
public class Info
{
    //Stage
    public bool FIRST_IN_GAME;
    public int BEST_STAGE;
    public bool STGEM_CLAIMED;
    public bool SKGEM_CLAIMED;
    public bool GEAR_CLAIMED;
    //Inven
    public List<int> skills;
    public int weapon;

    public Info()
    {
        //Stage
        FIRST_IN_GAME = true;
        BEST_STAGE = 1; //1
        STGEM_CLAIMED = true;
        SKGEM_CLAIMED = true;
        GEAR_CLAIMED = true;
        //Inven
        int LEN = (int)GlobalValue.SkillOrder.Count;
        skills = new List<int>(LEN) { -1, -1 };
        weapon = (int)ElementalManager.ElementalType.Count;
    }
}