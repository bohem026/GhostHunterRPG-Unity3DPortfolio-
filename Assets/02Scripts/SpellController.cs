using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static BaseStatSO;

public class SpellController : MonoBehaviour
{
    private const float SPELL_RANGE = 15f;

    /*X*/
    ////GameObject curSpellEffect;
    //List<GameObject> curSpellEffects;
    ////GameObject curSpellPicker;
    //List<GameObject> curSpellPickers;
    //GameObject curSpellEffect;
    //GameObject curSpellPicker;
    /*X*/

    List<CurrentSpellInfo> _CurrentSpellInfo;

    float curSpellDuration;
    GameObject castEffect;
    GameObject picker;

    PlayerController _plyCtrl;
    StatController _statCtrl;

    Ray ray;
    RaycastHit hit;
    LayerMask layerMask_Field;
    LayerMask layerMask_Mon;
    Vector3 pos;

    bool isPickerInputMode;

    [Space(20)]
    [Header("Audio clip")]
    [SerializeField] private AudioClip sfxClip_Self;
    [SerializeField] private AudioClip sfxClip_Cast;
    [SerializeField] private AudioClip sfxClip_Pmpt;

    /*Test*/
    [Space(20)]
    [Header("TEST")]
    public int curID;
    public GameObject spellSelf;

    public GameObject EEffect;
    public GameObject QEffect;

    float delta = 0f;
    float pickerInputModeLimit = 5f;
    bool isInitialized;
    bool testFlag;
    /*Test*/

    /*Test*/
    [SerializeField] GameObject pickerPanel;
    /*Test*/

    // Start is called before the first frame update
    void Start()
    {
        if (!isInitialized)
            Init();
        UpdateCurSpell();

        isInitialized = true;
    }

    private void Init()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _statCtrl = _plyCtrl.GetComponent<StatController>();

        layerMask_Field = 1 << LayerMask.NameToLayer("Field");
        layerMask_Mon = 1 << LayerMask.NameToLayer("Mon");

        int LEN = (int)GlobalValue.SkillOrder.Count;
        _CurrentSpellInfo = new List<CurrentSpellInfo>(LEN)
        {
            new CurrentSpellInfo(),
            new CurrentSpellInfo()
        };
    }

    // Update is called once per frame
    void Update()
    {
        SetPickerPos();
        AutoExitPIMode();
    }

    private void AutoExitPIMode()
    {
        if (delta > 0f)
        {
            delta -= Time.unscaledDeltaTime;

            if (delta < 0)
            {
                pos = new Vector3
                    (prevPos.x, _plyCtrl.GetComponent<Transform>().position.y,
                    prevPos.z);

                ResetPickMons();
                ExitPIMode();
            }
        }
    }

    /// <summary>
    /// 현재 장착한 스펠 데이터를 GlobalValue에서 추출하는 메서드 입니다.
    /// </summary>
    private void UpdateCurSpell()
    {
        List<int> _SpellIDs = GlobalValue.Instance.GetSkillIDsFromInfo();
        SpellEffectController spellEffect;

        GlobalValue.SkillOrder E = GlobalValue.SkillOrder.E;
        GlobalValue.SkillOrder Q = GlobalValue.SkillOrder.Q;
        int e = (int)E;
        int q = (int)Q;

        //--- Get E spell.
        if (_SpellIDs[e] >= 0)
        {
            //1. Update E skill effect(prefab).
            _CurrentSpellInfo[e].Effect = SpellManager.Inst.GetSpell(_SpellIDs[e]);
            spellEffect = _CurrentSpellInfo[e].Effect
                        .GetComponent<SpellEffectController>();
            //2. Update E skill picker.
            _CurrentSpellInfo[e].Picker = spellEffect.SpellPicker;
            //3. Update E skill duration.
            _CurrentSpellInfo[e].Duration = spellEffect.Duration;
            //4. Update E skill UI.
            UIManager.Inst.InitSpellButton(spellEffect, E);
        }
        /*Test*/
        else
        {
            UIManager.Inst.InitSpellButton(null, E);
        }
        /*Test*/
        //---

        //--- Get Q spell.
        if (_SpellIDs[q] >= 0)
        {
            Debug.Log($"ID {_SpellIDs[q]}");
            //1. Update Q skill effect.
            _CurrentSpellInfo[q].Effect = SpellManager.Inst.GetSpell(_SpellIDs[q]);
            spellEffect = _CurrentSpellInfo[q].Effect
                        .GetComponent<SpellEffectController>();
            //2. Update Q skill picker.
            _CurrentSpellInfo[q].Picker = spellEffect.SpellPicker;
            //3. Update Q skill duration.
            _CurrentSpellInfo[q].Duration = spellEffect.Duration;
            //4. Update E skill UI.
            UIManager.Inst.InitSpellButton(spellEffect, Q);
        }
        /*Test*/
        else
        {
            UIManager.Inst.InitSpellButton(null, Q);
        }
        /*Test*/
        //---
    }

    private static float PICKER_SPEED = 25f;
    Vector3 prevPos;
    private void SetPickerPos()
    {
        int E = (int)GlobalValue.SkillOrder.E;
        if (!isPickerInputMode || !_CurrentSpellInfo[E].Picker)
            return;

        GameManager.Inst.ChangeMouseInputMode(1);
        _plyCtrl.GetHRCurrentRect()
            .GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
        pickerPanel.SetActive(true);

        ray = Camera.main.ScreenPointToRay(InputExtension.PointerPosition);
        Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask_Field);

        if (!picker)
            picker = Instantiate
                    (_CurrentSpellInfo[E].Picker,
                    _plyCtrl.transform.position,
                    Quaternion.identity);

        // Limit area spell picker available.
        if (hit.collider != null)
            prevPos = Mathf.Abs(_plyCtrl.transform.position.z - hit.point.z) <= SPELL_RANGE
                    ? hit.point
                    : _plyCtrl.transform.position
                    + (hit.point - _plyCtrl.transform.position).normalized * SPELL_RANGE;
        picker.transform.position = Vector3.Lerp
                                    (picker.transform.position,
                                    prevPos,
                                    Time.unscaledDeltaTime * PICKER_SPEED);

        UpdatePickMons();

        if (InputExtension.PointerUp)
        {
            pos = new Vector3(prevPos.x, _plyCtrl.GetComponent<Transform>().position.y, prevPos.z);

            ResetPickMons();
            ExitPIMode();
        }
    }

    Dictionary<MonsterController, MonsterController.MonFSM> pickMonsBase = new();
    HashSet<MonsterController> curPicks;
    private void UpdatePickMons()
    {
        /*Test*/
        RaycastHit[] picks = Physics.SphereCastAll(picker.transform.position,
                                        picker.GetComponent<SphereCollider>().radius,
                                        Vector3.up,
                                        0f,
                                        layerMask_Mon);

        curPicks = new HashSet<MonsterController>();

        foreach (var pick in picks)
        {
            // pickMon = 감지된 몬스터 컨트롤러
            MonsterController pickMon = pick.collider.GetComponent<MonsterController>();
            if (pickMon != null)
            {
                // curPicks = 현재 감지된 모든 몬스터 컨트롤러
                curPicks.Add(pickMon);

                // pickMonsBase = 감지된 몬스터 기록.. 실시간 갱신되며 충돌 해제된 키 삭제 x
                if (!pickMonsBase.ContainsKey(pickMon))
                {
                    pickMonsBase[pickMon] = pickMon.GetCurFSM();
                    pickMon.SetFSM(MonsterController.MonFSM.Pick);
                }
            }
        }

        UpdateUnpickMons();
        /*Test*/
    }

    private void UpdateUnpickMons()
    {
        var unpickMonsBase = new List<MonsterController>(pickMonsBase.Keys);
        foreach (MonsterController mon in unpickMonsBase)
        {
            if (!curPicks.Contains(mon))
            {
                mon.SetFSM(pickMonsBase[mon]);
                pickMonsBase.Remove(mon);
            }
        }
    }

    private void ResetPickMons()
    {
        var temp = new List<MonsterController>(pickMonsBase.Keys);
        foreach (MonsterController mon in temp)
        {
            mon.SetFSM(pickMonsBase[mon]);
        }

        pickMonsBase.Clear();
    }

    private void ExitPIMode()
    {
        delta = 0f;

        GameManager.Inst.ChangeMouseInputMode(0);
        pickerPanel.SetActive(false);
        isPickerInputMode = false;

        Destroy(picker);
    }

    /// <summary>
    /// E, Q버튼 입력에 따른 스펠 이펙트 재생 메서드 입니다.
    /// Self, Pmpt 타입 스펠 관련 메서드 입니다.
    /// </summary>
    /// <param name="order">E, Q버튼 구분자</param>
    public void InstSpellEffect(GlobalValue.SkillOrder order)
    {
        GameObject effect = _CurrentSpellInfo[(int)order].Effect;
        SpellEffectController spellEffect
            = effect.GetComponent<SpellEffectController>();

        //Return if is not Self or Pmpt type spell.
        if (spellEffect.Type != SpellEffectController.SpellType.Self
            && spellEffect.Type != SpellEffectController.SpellType.Pmpt)
        {
            Debug.LogError($"[Invalid spell type] {spellEffect.Type}");
            return;
        }

        switch (order)
        {
            //Pmpt type spell.
            case GlobalValue.SkillOrder.E:
                GlobalValue.SkillOrder E = GlobalValue.SkillOrder.E;
                //1. Return if there's no monsters aimed.
                if (MonsterManager.Inst.InstMonsAimed() == 0) return;
                //2. Instantiate effect counted damage count.
                int atkCount = spellEffect.DamageCount;
                foreach (MonsterController mon
                        in MonsterManager.Inst.ShuffleAndGetSome
                        (MonsterManager.Inst.monsAimed,
                        atkCount))
                {
                    StartCoroutine(IntervalSpellEffect
                        (GlobalValue.SkillOrder.E, mon));
                }
                //3. Update variables.
                _statCtrl.MPCurrent -= spellEffect.Cost;
                UIManager.Inst.ResetSpellCoolDown(E);
                break;

            //Self type spell.
            case GlobalValue.SkillOrder.Q:
                GlobalValue.SkillOrder Q = GlobalValue.SkillOrder.Q;

                //Play SFX: Self.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnce
                (AudioPlayerPoolManager.SFXPoolType.Player,
                sfxClip_Self);

                //1. Destroy playing effect.
                if (QEffect) Destroy(QEffect);
                //2. Instantiate effect.
                QEffect = Instantiate
                            (_CurrentSpellInfo[(int)GlobalValue.SkillOrder.Q].Effect,
                            _plyCtrl.transform.position,
                            Quaternion.identity,
                            _plyCtrl.transform);

                //*** HEAL ***
                if (spellEffect.INNER == SkillWindowController.ELType.Poison)
                {
                    _statCtrl.HPCurrent = Mathf.Clamp
                    (_statCtrl.HPCurrent + _statCtrl.HP * spellEffect.DamageRate,
                    0f,
                    _statCtrl.HP);
                }

                //3. Update variables.
                _statCtrl.MPCurrent -= spellEffect.Cost;
                UIManager.Inst.ResetSpellCoolDown(Q);
                //4. Destroy after duration.
                Destroy(QEffect, _CurrentSpellInfo[(int)Q].Duration);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 이펙트 재생 시작과 피격까지 딜레이가 존재하는 스펠 생성 메서드 입니다.
    /// Pmpt 타입 스펠 관련 메서드 입니다.
    /// </summary>
    /// <param name="order">Q, E스킬 구분자</param>
    /// <param name="target">대상</param>
    /// <returns></returns>
    IEnumerator IntervalSpellEffect
        (
        GlobalValue.SkillOrder order,
        MonsterController target
        )
    {
        CurrentSpellInfo info = _CurrentSpellInfo[(int)order];
        GameObject effect;

        //1. Inst effect has interval before damaged.
        Destroy(effect = Instantiate
                        (info.Effect,
                        target.transform.position,
                        Quaternion.identity,
                        MonsterPoolManager.Inst.transform),
                        info.Duration);

        SpellEffectController spellEffect
            = effect.GetComponent<SpellEffectController>();

        //2. Update variables.
        _statCtrl.MPCurrent -= spellEffect.Cost;
        UIManager.Inst.ResetSpellCoolDown(order);

        yield return new WaitForSeconds(spellEffect.DamageInterval);

        //Play SFX: Pmpt.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
        (AudioPlayerPoolManager.SFXPoolType.Player,
        sfxClip_Pmpt);

        //3. Set damage after interval.
        if (target.isTarget)
        {
            spellEffect.ApplySDamageInfoToMonster(target, spellEffect.DamageRate);
        }
    }

    public void InstDelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        Time.timeScale = 0.0f;
        StartCoroutine(DelayedSpellEffect(order));
    }

    /// <summary>
    /// E버튼 입력에 따른 지연된 스펠 이펙트 재생 메서드 입니다.
    /// Cast 타입 스펠 관련 메서드 입니다.
    /// </summary>
    /// <returns></returns>
    IEnumerator DelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        isPickerInputMode = true;
        delta = pickerInputModeLimit;

        //Wait until player input detected.
        yield return new WaitUntil(() => !IsPickerInputMode());
        Time.timeScale = 1.0f;

        //Play SFX: Cast.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
        (AudioPlayerPoolManager.SFXPoolType.Player,
        sfxClip_Cast,
        1.33f);

        //1. Destroy playing effect.
        if (castEffect) Destroy(castEffect);

        //2. Instantiate effect.
        castEffect = Instantiate
                    (_CurrentSpellInfo[(int)order].Effect,
                    pos,
                    Quaternion.identity,
                    MonsterPoolManager.Inst.transform);

        SpellEffectController spellEffect
            = castEffect.GetComponent<SpellEffectController>();

        //3. Update variables.
        _statCtrl.MPCurrent -= spellEffect.Cost;
        UIManager.Inst.ResetSpellCoolDown(order);

        //3. Destroy after duration.
        Destroy(castEffect, _CurrentSpellInfo[(int)order].Duration);
    }

    public bool IsPickerInputMode()
    {
        return isPickerInputMode;
    }

    public CurrentSpellInfo Info(GlobalValue.SkillOrder order)
        => _CurrentSpellInfo[(int)order];

    public bool IsInitialized => isInitialized;
}

//[Serializable]
public class CurrentSpellInfo
{
    public GameObject Effect;
    public GameObject Picker;
    public float Duration;

    public CurrentSpellInfo()
    {
        Effect = null;
        Picker = null;
        Duration = 0f;
    }
}
