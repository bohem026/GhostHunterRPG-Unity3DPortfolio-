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
    private float delta = 0f;               // ���� ��Ŀ �Է� ��� �ð�
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

    /// <summary>��Ÿ�� ���������̾��ũ������ ���� �ʱ�ȭ.</summary>
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

    /// <summary>���� ���� ��ų(E/Q)�� GlobalValue/SpellManager���� �о� ĳ��.</summary>
    private void UpdateCurSpell()
    {
        List<int> spellIDs = GlobalValue.Instance.GetSkillIDsFromInfo();
        SpellEffectController spellEffect;

        var E = GlobalValue.SkillOrder.E;
        var Q = GlobalValue.SkillOrder.Q;
        int e = (int)E;
        int q = (int)Q;

        // E ��ų
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

        // Q ��ų
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

    /// <summary>��Ŀ ��带 �ڵ� ����(�ð� ����) ó��.</summary>
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

    /// <summary>��Ŀ ��ġ ���� �� ��� ���̶���Ʈ/Ŭ�� Ȯ�� ó��.</summary>
    private void SetPickerPos()
    {
        int E = (int)GlobalValue.SkillOrder.E;
        if (!isPickerInputMode || !_CurrentSpellInfo[E].Picker)
            return;

        // ��Ŀ ���� ���� ���콺/�г� ���� ����
        GameManager.Inst.ChangeMouseInputMode(1);
        _plyCtrl.GetHRCurrentRect().GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
        pickerPanel.SetActive(true);

        ray = Camera.main.ScreenPointToRay(InputExtension.PointerPosition);
        Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask_Field);

        if (!picker)
        {
            picker = Instantiate(_CurrentSpellInfo[E].Picker, _plyCtrl.transform.position, Quaternion.identity);
        }

        // ĳ���ͷκ��� SPELL_RANGE ���� ����
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

        // �Է� Ȯ��(Ŭ��/��ġ ��)
        if (InputExtension.PointerUp)
        {
            pos = new Vector3(prevPos.x, _plyCtrl.transform.position.y, prevPos.z);
            ResetPickMons();
            ExitPIMode();
        }
    }

    /// <summary>��Ŀ ���� �� ���͸� �����ű� �������� �����ϸ� ���� ���̶���Ʈ.</summary>
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

    /// <summary>�̹� �����ӿ��� ��� ������ ���¸� ����.</summary>
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

    /// <summary>��Ŀ�� ���� ���� ��� ���� ���¸� ����.</summary>
    private void ResetPickMons()
    {
        var temp = new List<MonsterController>(pickMonsBase.Keys);
        foreach (MonsterController mon in temp)
        {
            mon.SetFSM(pickMonsBase[mon]);
        }

        pickMonsBase.Clear();
    }

    /// <summary>��Ŀ ��� ����(���콺��UI ���� �� ��Ŀ ����).</summary>
    private void ExitPIMode()
    {
        delta = 0f;

        GameManager.Inst.ChangeMouseInputMode(0);
        pickerPanel.SetActive(false);
        isPickerInputMode = false;

        Destroy(picker);
    }

    /// <summary>
    /// E/Q �Է¿� ���� ���� ����Ʈ�� �����Ѵ�.
    /// - E: Prompt(���� ��� ��Ÿ��)
    /// - Q: Self(�ڱ� ��ȭ/ȸ��)
    /// </summary>
    public void InstSpellEffect(GlobalValue.SkillOrder order)
    {
        GameObject effect = _CurrentSpellInfo[(int)order].Effect;
        var spellEffect = effect.GetComponent<SpellEffectController>();

        // �ش� Ÿ�Ը� ó��
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

                // ������ �Ӽ�: ��� ȸ��
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

    /// <summary>Prompt ����: ����Ʈ ��� �� ������ �ΰ� ��󿡰� ���� ����.</summary>
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

    /// <summary>Cast ����: ��Ŀ �Է��� �޵��� ���� ������ ����.</summary>
    public void InstDelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        Time.timeScale = 0.0f;
        StartCoroutine(DelayedSpellEffect(order));
    }

    /// <summary>Cast ����: ��Ŀ �Է� Ȯ�� �� ���� ������ ����Ʈ ����.</summary>
    private IEnumerator DelayedSpellEffect(GlobalValue.SkillOrder order)
    {
        isPickerInputMode = true;
        delta = pickerInputModeLimit;

        // �Է� Ȯ������ ���
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

// ���� ������ ����(ȿ�� ������/��Ŀ/���ӽð�)
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
