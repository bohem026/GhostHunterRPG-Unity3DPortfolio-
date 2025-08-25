using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellController : MonoBehaviour
{
    // Constants
    private const float SPELL_RANGE = 15f;
    private static float PICKER_SPEED = 25f;

    // Serialized (Inspector)
    [Header("Audio clip")]
    [SerializeField] private AudioClip sfxClip_Self;
    [SerializeField] private AudioClip sfxClip_Cast;
    [SerializeField] private AudioClip sfxClip_Pmpt;

    [Header("Picker UI")]
    [SerializeField] private GameObject pickerPanel;

    // Runtime references
    private PlayerController _plyCtrl;
    private StatController _statCtrl;

    // Current spell cache (E, Q)
    private List<CurrentSpellInfo> _CurrentSpellInfo;

    // Picker / cast state
    private GameObject castEffect;
    private GameObject picker;
    private GameObject QEffect;

    private bool isPickerInputMode;
    private float delta = 0f;               // 남은 피커 입력 대기 시간
    private float pickerInputModeLimit = 5f;

    private Vector3 prevPos;
    private Vector3 pos;

    // Raycast / layers
    private Ray ray;
    private RaycastHit hit;
    private LayerMask layerMask_Field;
    private LayerMask layerMask_Mon;

    // Pick highlight bookkeeping
    private readonly Dictionary<MonsterController, MonsterController.MonFSM> pickMonsBase = new();
    private HashSet<MonsterController> curPicks;

    // Flags
    private bool isInitialized;

    private void Start()
    {
        if (!isInitialized)
            Init();

        UpdateCurSpell();
        isInitialized = true;
    }

    private void Update()
    {
        SetPickerPos();
        AutoExitPIMode();
    }

    /// <summary>런타임 참조·레이어마스크·스펠 구조 초기화.</summary>
    private void Init()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _statCtrl = _plyCtrl.GetComponent<StatController>();

        layerMask_Field = 1 << LayerMask.NameToLayer("Field");
        layerMask_Mon = 1 << LayerMask.NameToLayer("Mon");

        int len = (int)GlobalValue.SkillOrder.Count;
        _CurrentSpellInfo = new List<CurrentSpellInfo>(len)
        {
            new CurrentSpellInfo(), // E
            new CurrentSpellInfo()  // Q
        };
    }

    /// <summary>현재 장착 스킬(E/Q)을 GlobalValue/SpellManager에서 읽어 캐시.</summary>
    private void UpdateCurSpell()
    {
        List<int> spellIDs = GlobalValue.Instance.GetSkillIDsFromInfo();
        SpellEffectController spellEffect;

        var E = GlobalValue.SkillOrder.E;
        var Q = GlobalValue.SkillOrder.Q;
        int e = (int)E;
        int q = (int)Q;

        // E 스킬
        if (spellIDs[e] >= 0)
        {
            _CurrentSpellInfo[e].Effect = SpellManager.Inst.GetSpell(spellIDs[e]);
            spellEffect = _CurrentSpellInfo[e].Effect.GetComponent<SpellEffectController>();
            _CurrentSpellInfo[e].Picker = spellEffect.SpellPicker;
            _CurrentSpellInfo[e].Duration = spellEffect.Duration;
            UIManager.Inst.InitSpellButton(spellEffect, E);
        }
        else
        {
            UIManager.Inst.InitSpellButton(null, E);
        }

        // Q 스킬
        if (spellIDs[q] >= 0)
        {
            _CurrentSpellInfo[q].Effect = SpellManager.Inst.GetSpell(spellIDs[q]);
            spellEffect = _CurrentSpellInfo[q].Effect.GetComponent<SpellEffectController>();
            _CurrentSpellInfo[q].Picker = spellEffect.SpellPicker;
            _CurrentSpellInfo[q].Duration = spellEffect.Duration;
            UIManager.Inst.InitSpellButton(spellEffect, Q);
        }
        else
        {
            UIManager.Inst.InitSpellButton(null, Q);
        }
    }

    /// <summary>피커 모드를 자동 종료(시간 만료) 처리.</summary>
    private void AutoExitPIMode()
    {
        if (delta > 0f)
        {
            delta -= Time.unscaledDeltaTime;

            if (delta < 0f)
            {
                pos = new Vector3(prevPos.x, _plyCtrl.transform.position.y, prevPos.z);
                ResetPickMons();
                ExitPIMode();
            }
        }
    }

    /// <summary>피커 위치 갱신 및 대상 하이라이트/클릭 확정 처리.</summary>
    private void SetPickerPos()
    {
        int E = (int)GlobalValue.SkillOrder.E;
        if (!isPickerInputMode || !_CurrentSpellInfo[E].Picker)
            return;

        // 피커 진입 동안 마우스/패널 상태 유지
        GameManager.Inst.ChangeMouseInputMode(1);
        _plyCtrl.GetHRCurrentRect().GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
        pickerPanel.SetActive(true);

        ray = Camera.main.ScreenPointToRay(InputExtension.PointerPosition);
        Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask_Field);

        if (!picker)
        {
            picker = Instantiate(_CurrentSpellInfo[E].Picker, _plyCtrl.transform.position, Quaternion.identity);
        }

        // 캐릭터로부터 SPELL_RANGE 내로 제한
        if (hit.collider != null)
        {
            prevPos = Mathf.Abs(_plyCtrl.transform.position.z - hit.point.z) <= SPELL_RANGE
                    ? hit.point
                    : _plyCtrl.transform.position +
                      (hit.point - _plyCtrl.transform.position).normalized * SPELL_RANGE;
        }

        picker.transform.position = Vector3.Lerp(
            picker.transform.position, prevPos, Time.unscaledDeltaTime * PICKER_SPEED);

        UpdatePickMons();

        // 입력 확정(클릭/터치 업)
        if (InputExtension.PointerUp)
        {
            pos = new Vector3(prevPos.x, _plyCtrl.transform.position.y, prevPos.z);
            ResetPickMons();
            ExitPIMode();
        }
    }

    /// <summary>피커 영역 내 몬스터를 구·신규 집합으로 관리하며 상태 하이라이트.</summary>
    private void UpdatePickMons()
    {
        RaycastHit[] picks = Physics.SphereCastAll(
            picker.transform.position,
            picker.GetComponent<SphereCollider>().radius,
            Vector3.up,
            0f,
            layerMask_Mon
        );

        curPicks = new HashSet<MonsterController>();

        foreach (var pick in picks)
        {
            var pickMon = pick.collider.GetComponent<MonsterController>();
            if (pickMon == null) continue;

            curPicks.Add(pickMon);

            if (!pickMonsBase.ContainsKey(pickMon))
            {
                pickMonsBase[pickMon] = pickMon.GetCurFSM();
                pickMon.SetFSM(MonsterController.MonFSM.Pick);
            }
        }

        UpdateUnpickMons();
    }

    /// <summary>이번 프레임에서 벗어난 몬스터의 상태를 원복.</summary>
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

    /// <summary>피커로 강조 중인 모든 몬스터 상태를 원복.</summary>
    private void ResetPickMons()
    {
        var temp = new List<MonsterController>(pickMonsBase.Keys);
        foreach (MonsterController mon in temp)
        {
            mon.SetFSM(pickMonsBase[mon]);
        }

        pickMonsBase.Clear();
    }

    /// <summary>피커 모드 종료(마우스·UI 복구 및 피커 제거).</summary>
    private void ExitPIMode()
    {
        delta = 0f;

        GameManager.Inst.ChangeMouseInputMode(0);
        pickerPanel.SetActive(false);
        isPickerInputMode = false;

        Destroy(picker);
    }

    /// <summary>
    /// E/Q 입력에 따라 스펠 이펙트를 생성한다.
    /// - E: Prompt(지정 대상 다타격)
    /// - Q: Self(자기 강화/회복)
    /// </summary>
    public void InstSpellEffect(GlobalValue.SkillOrder order)
    {
        GameObject effect = _CurrentSpellInfo[(int)order].Effect;
        var spellEffect = effect.GetComponent<SpellEffectController>();

        // 해당 타입만 처리
        if (spellEffect.Type != SpellEffectController.SpellType.Self &&
            spellEffect.Type != SpellEffectController.SpellType.Pmpt)
        {
            Debug.LogError($"[Invalid spell type] {spellEffect.Type}");
            return;
        }

        switch (order)
        {
            // E: Prompt
            case GlobalValue.SkillOrder.E:
                if (MonsterManager.Inst.InstMonsAimed() == 0) return;

                int atkCount = spellEffect.DamageCount;
                foreach (MonsterController mon in
                    MonsterManager.Inst.ShuffleAndGetSome(MonsterManager.Inst.monsAimed, atkCount))
                {
                    StartCoroutine(IntervalSpellEffect(GlobalValue.SkillOrder.E, mon));
                }

                _statCtrl.MPCurrent -= spellEffect.Cost;
                UIManager.Inst.ResetSpellCoolDown(GlobalValue.SkillOrder.E);
                break;

            // Q: Self
            case GlobalValue.SkillOrder.Q:
                AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
                    AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_Self);

                if (QEffect) Destroy(QEffect);

                QEffect = Instantiate(
                    _CurrentSpellInfo[(int)GlobalValue.SkillOrder.Q].Effect,
                    _plyCtrl.transform.position,
                    Quaternion.identity,
                    _plyCtrl.transform
                );

                // 포이즌 속성: 즉시 회복
                if (spellEffect.INNER == SkillWindowController.ELType.Poison)
                {
                    _statCtrl.HPCurrent = Mathf.Clamp(
                        _statCtrl.HPCurrent + _statCtrl.HP * spellEffect.DamageRate,
                        0f,
                        _statCtrl.HP
                    );
                }

                _statCtrl.MPCurrent -= spellEffect.Cost;
                UIManager.Inst.ResetSpellCoolDown(GlobalValue.SkillOrder.Q);

                Destroy(QEffect, _CurrentSpellInfo[(int)GlobalValue.SkillOrder.Q].Duration);
                break;
        }
    }

    /// <summary>Prompt 스펠: 이펙트 재생 후 지연을 두고 대상에게 피해 적용.</summary>
    private IEnumerator IntervalSpellEffect(GlobalValue.SkillOrder order, MonsterController target)
    {
        var info = _CurrentSpellInfo[(int)order];

        var effect = Instantiate(
            info.Effect,
            target.transform.position,
            Quaternion.identity,
            MonsterPoolManager.Inst.transform
        );
        Destroy(effect, info.Duration);

        var spellEffect = effect.GetComponent<SpellEffectController>();

        _statCtrl.MPCurrent -= spellEffect.Cost;
        UIManager.Inst.ResetSpellCoolDown(order);

        yield return new WaitForSeconds(spellEffect.DamageInterval);

        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_Pmpt);

        if (target.isTarget)
        {
            spellEffect.ApplySDamageInfoToMonster(target, spellEffect.DamageRate);
        }
    }

    /// <summary>Cast 스펠: 피커 입력을 받도록 지연 실행을 시작.</summary>
    public void InstDelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        Time.timeScale = 0.0f;
        StartCoroutine(DelayedSpellEffect(order));
    }

    /// <summary>Cast 스펠: 피커 입력 확정 후 지정 지점에 이펙트 생성.</summary>
    private IEnumerator DelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        isPickerInputMode = true;
        delta = pickerInputModeLimit;

        // 입력 확정까지 대기
        yield return new WaitUntil(() => !IsPickerInputMode());
        Time.timeScale = 1.0f;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_Cast, 1.33f);

        if (castEffect) Destroy(castEffect);

        castEffect = Instantiate(
            _CurrentSpellInfo[(int)order].Effect,
            pos,
            Quaternion.identity,
            MonsterPoolManager.Inst.transform
        );

        var spellEffect = castEffect.GetComponent<SpellEffectController>();

        _statCtrl.MPCurrent -= spellEffect.Cost;
        UIManager.Inst.ResetSpellCoolDown(order);

        Destroy(castEffect, _CurrentSpellInfo[(int)order].Duration);
    }

    public bool IsPickerInputMode() => isPickerInputMode;
    public CurrentSpellInfo Info(GlobalValue.SkillOrder order) => _CurrentSpellInfo[(int)order];
    public bool IsInitialized => isInitialized;
}

// 현재 장착한 스펠(효과 프리팹/피커/지속시간)
[System.Serializable]
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
