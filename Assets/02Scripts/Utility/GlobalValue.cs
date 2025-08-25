using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 전역 데이터(스탯/스킬/인벤토리/정보)를 로드·저장하고 접근하는 싱글턴.
/// - Firestore(Json)에서 비동기 로드/저장
/// - 스탯/스킬 레벨 조회·증감
/// - 장비 수량/장착 상태 및 장비로 인한 합연산 스탯 관리
/// - 장착 스킬 ID 관리
/// </summary>
public class GlobalValue : MonoBehaviour
{
    //===== Singleton & State =====
    public static GlobalValue Instance { get; private set; }
    public bool IsDataLoaded { get; private set; }

    //===== Enums =====
    public enum GearCommand { Get, Sell, Equip, Unequip, Count }
    public enum SkillOrder { E, Q, Count }

    //===== Data Containers =====
    [HideInInspector] public StatLevel _StatLevel;
    [HideInInspector] public SkillLevel _SkillLevel;
    [HideInInspector] public Inven _Inven;
    [HideInInspector] public Info _Info;

    //===== Unity Lifecycle =====
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

    //===== Load / Save =====

    /// <summary>
    /// 모든 전역 데이터를 Firestore에서 비동기 로드합니다.
    /// </summary>
    public async Task LoadData()
    {
        await LoadStatLevel();
        await LoadSkillLevel();
        await LoadInven();
        await LoadInfo();

        IsDataLoaded = true;
    }

    /// <summary>
    /// Info 데이터를 로드(없으면 기본값 생성 후 저장).
    /// </summary>
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

    /// <summary>
    /// 인벤토리 데이터를 로드(없으면 기본값 생성 후 저장).
    /// </summary>
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

    /// <summary>
    /// 스탯 레벨 데이터를 로드(없으면 기본값 생성 후 저장).
    /// </summary>
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

    /// <summary>
    /// 스킬 레벨 데이터를 로드(없으면 기본값 생성 후 저장).
    /// </summary>
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

    //===== Stat Levels =====

    /// <summary>
    /// 스탯 타입별 현재 레벨을 반환합니다.
    /// </summary>
    public int GetStatLevelByType(StatController.StatType type)
    {
        switch (type)
        {
            case StatController.StatType.HP: return _StatLevel.HP_LV;
            case StatController.StatType.MPITV: return _StatLevel.MPITV_LV;
            case StatController.StatType.DEF: return _StatLevel.DEF_LV;
            case StatController.StatType.MATK: return _StatLevel.MATK_LV;
            case StatController.StatType.SATK: return _StatLevel.SATK_LV;
            case StatController.StatType.CTKR: return _StatLevel.CTKR_LV;
            case StatController.StatType.EVDR: return _StatLevel.EVDR_LV;
            case StatController.StatType.STBR: return _StatLevel.STBR_LV;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }
    }

    /// <summary>
    /// 스탯 레벨을 증감(클램프 포함)하고 저장합니다.
    /// </summary>
    public int ElapseStatLevelByType(StatController.StatType type, int max, int value)
    {
        int result;
        switch (type)
        {
            case StatController.StatType.HP:
                result = (_StatLevel.HP_LV = Mathf.Clamp(_StatLevel.HP_LV + value, 0, max)); break;
            case StatController.StatType.MPITV:
                result = (_StatLevel.MPITV_LV = Mathf.Clamp(_StatLevel.MPITV_LV + value, 0, max)); break;
            case StatController.StatType.DEF:
                result = (_StatLevel.DEF_LV = Mathf.Clamp(_StatLevel.DEF_LV + value, 0, max)); break;
            case StatController.StatType.MATK:
                result = (_StatLevel.MATK_LV = Mathf.Clamp(_StatLevel.MATK_LV + value, 0, max)); break;
            case StatController.StatType.SATK:
                result = (_StatLevel.SATK_LV = Mathf.Clamp(_StatLevel.SATK_LV + value, 0, max)); break;
            case StatController.StatType.CTKR:
                result = (_StatLevel.CTKR_LV = Mathf.Clamp(_StatLevel.CTKR_LV + value, 0, max)); break;
            case StatController.StatType.EVDR:
                result = (_StatLevel.EVDR_LV = Mathf.Clamp(_StatLevel.EVDR_LV + value, 0, max)); break;
            case StatController.StatType.STBR:
                result = (_StatLevel.STBR_LV = Mathf.Clamp(_StatLevel.STBR_LV + value, 0, max)); break;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }
        SaveStatLevel();
        return result;
    }

    //===== Skill Levels =====

    /// <summary>
    /// 스킬(외/내 타입) 레벨을 반환합니다.
    /// </summary>
    public int GetSkillLevelByEnum(SkillWindowController.SKType outer, SkillWindowController.ELType inner)
    {
        if ((int)outer >= (int)SkillWindowController.SKType.Count
            || (int)inner >= (int)SkillWindowController.ELType.Count)
        {
            Debug.Log("[!!ERROR!!] UNDEFINED SKILL");
            return 0;
        }
        return _SkillLevel.skillLevelList[(int)outer].levels[(int)inner];
    }

    /// <summary>
    /// 스킬 레벨을 증감하고 저장합니다. (주의: 전달받은 max는 사용하지 않습니다)
    /// </summary>
    public int ElapseSkillLevelByEnum(
        SkillWindowController.SKType outer,
        SkillWindowController.ELType inner,
        int max,
        int value)
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

    //===== Skill Equip (Info) =====

    /// <summary>
    /// 현재 장착된 스킬 ID 목록을 반환합니다. (E, Q 순)
    /// </summary>
    public List<int> GetSkillIDsFromInfo() => _Info.skills;

    /// <summary>
    /// 지정 슬롯의 스킬 ID를 교체하고 이전 ID를 반환합니다.
    /// </summary>
    public int SwitchSkillIDFromInfo(int id, SkillOrder order)
    {
        int preID = _Info.skills[(int)order];
        _Info.skills[(int)order] = id;
        SaveInfo();
        return preID;
    }

    /// <summary>
    /// 지정 슬롯의 스킬을 해제하고 이전 ID를 반환합니다. (해제 시 -1)
    /// </summary>
    public int RemoveSkillIDFromInfo(SkillOrder order)
    {
        int preID = _Info.skills[(int)order];
        _Info.skills[(int)order] = -1;
        SaveInfo();
        return preID;
    }

    //===== Stage Info =====

    /// <summary>
    /// 최고 도달 스테이지 반환.
    /// </summary>
    public int GetBestStageFromInfo() => _Info.BEST_STAGE;

    //===== Gear: Counts / Equip =====

    /// <summary>
    /// 장비 수량을 조회합니다. (인벤/장착 구분)
    /// </summary>
    public int GetGearCountByEnum(
        GearController.Rarity outer,
        GearController.GearType inner,
        GearWindowController.GearLocation location)
    {
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

    /// <summary>
    /// 장비 수량/장착 상태를 변경하고 저장합니다.
    /// </summary>
    public void ElapseGearCountByEnum(
        GearController.Rarity outer,
        GearController.GearType inner,
        GearCommand command,
        int value = 1)
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
                // 같은 타입 장비 전부 해제 후 새 장비를 장착
                for (int o = 0; o < OUTER_LEN; o++)
                {
                    if (_Inven.gearEquipCountList[o].counts[(int)inner] > 0)
                        _Inven.gearInvenCountList[o].counts[(int)inner]++;

                    _Inven.gearEquipCountList[o].counts[(int)inner] = 0;
                }
                if (--_Inven.gearInvenCountList[(int)outer].counts[(int)inner] < 0)
                    _Inven.gearInvenCountList[(int)outer].counts[(int)inner] = 0;
                _Inven.gearEquipCountList[(int)outer].counts[(int)inner]++;
                break;

            case GearCommand.Unequip:
                // 같은 타입 장비 전부 해제 후 인벤으로 반환
                foreach (var count in _Inven.gearEquipCountList)
                {
                    count.counts[(int)inner] = 0;
                }
                _Inven.gearInvenCountList[(int)outer].counts[(int)inner]++;
                break;
        }

        SaveInven();
    }

    /// <summary>
    /// 장착으로 누적된 능력치 값을 조회합니다.
    /// </summary>
    public float GetEquipStatByEnum(GearSO.StatType type)
    {
        switch (type)
        {
            case GearSO.StatType.HP: return _Inven._EquipStat.HP;
            case GearSO.StatType.DEF: return _Inven._EquipStat.DEF;
            case GearSO.StatType.MATK: return _Inven._EquipStat.MATK;
            case GearSO.StatType.SATK: return _Inven._EquipStat.SATK;
            case GearSO.StatType.CTKR: return _Inven._EquipStat.CTKR;
            case GearSO.StatType.EVDR: return _Inven._EquipStat.EVDR;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0;
        }
    }

    /// <summary>
    /// 장착 능력치 값을 증감(0 이상 클램프)하고 저장합니다.
    /// </summary>
    public float ElapseEquipStatByEnum(GearSO.StatType type, float value)
    {
        float result = 0f;

        switch (type)
        {
            case GearSO.StatType.HP:
                _Inven._EquipStat.HP = Mathf.Clamp(_Inven._EquipStat.HP + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.HP; break;
            case GearSO.StatType.DEF:
                _Inven._EquipStat.DEF = Mathf.Clamp(_Inven._EquipStat.DEF + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.DEF; break;
            case GearSO.StatType.MATK:
                _Inven._EquipStat.MATK = Mathf.Clamp(_Inven._EquipStat.MATK + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.MATK; break;
            case GearSO.StatType.SATK:
                _Inven._EquipStat.SATK = Mathf.Clamp(_Inven._EquipStat.SATK + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.SATK; break;
            case GearSO.StatType.CTKR:
                _Inven._EquipStat.CTKR = Mathf.Clamp(_Inven._EquipStat.CTKR + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.CTKR; break;
            case GearSO.StatType.EVDR:
                _Inven._EquipStat.EVDR = Mathf.Clamp(_Inven._EquipStat.EVDR + value, 0f, float.MaxValue);
                result = _Inven._EquipStat.EVDR; break;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return 0f;
        }

        SaveInven();
        return result;
    }

    /// <summary>
    /// 장비 인덱스가 유효한지 검사합니다.
    /// </summary>
    private bool IsGearAvailable(GearController.Rarity outer, GearController.GearType inner)
    {
        return !((int)outer >= _Inven.gearEquipCountList.Capacity
              || (int)inner >= _Inven.gearEquipCountList.Capacity
              || (int)outer >= _Inven.gearInvenCountList.Capacity
              || (int)inner >= _Inven.gearInvenCountList.Capacity);
    }
}

//===== Serializable Data Types =====

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
        STGEM_CNT = 99;
        SKGEM_CNT = 99;
        gearInvenCountList = new List<GearCountInnerList>(4)
        {
            new GearCountInnerList(0), // 초깃값: 각 타입 1개씩 소지
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

[Serializable]
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

[Serializable]
public class Info
{
    // Stage
    public bool FIRST_IN_GAME;
    public int BEST_STAGE;
    public bool STGEM_CLAIMED;
    public bool SKGEM_CLAIMED;
    public bool GEAR_CLAIMED;

    // Inven
    public List<int> skills; // [E, Q]
    public int weapon;

    public Info()
    {
        FIRST_IN_GAME = true;
        BEST_STAGE = 1;
        STGEM_CLAIMED = true;
        SKGEM_CLAIMED = true;
        GEAR_CLAIMED = true;

        int LEN = (int)GlobalValue.SkillOrder.Count;
        skills = new List<int>(LEN) { -1, -1 };
        weapon = (int)ElementalManager.ElementalType.Count;
    }
}
