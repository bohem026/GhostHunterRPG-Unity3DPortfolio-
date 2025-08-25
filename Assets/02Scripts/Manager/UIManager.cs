using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // --- Constants ---
    private const string SPELL_ICON_PRELINK = "Icon/";
    private const string COOLDOWN_IMAGE_NAME = "Image_CoolDown";
    private const float BLOOD_OVERLAY_LIFETIME = 0.15f;
    private const float SPELL_DELAY = 3f;
    private const float STATUSBOX_ANDROID_XPOS = 600f;
    private const float SATKBTN_ANDROID_YPOS = 250f;
    private const float SATKBTN_ANDROID_SCALE = 0.75f;

    // --- Singleton ---
    public static UIManager Inst;

    [Space(20)]
    [Header("ROOT")]
    [SerializeField] private GameObject _UIRoot;

    [Header("UNIVERSAL")]
    [SerializeField] private GameObject _StatusBox;
    [SerializeField] private GameObject _JoystickBox;
    [SerializeField] private GameObject[] _KBImages;
    [SerializeField] private GameObject _PauseButton;

    [Space(20)]
    [Header("STATUS\n\nHP")]
    [SerializeField] private Transform _HPBox;
    [SerializeField] private Text _HPText;

    [Header("MP")]
    [SerializeField] private Image _MPFill;
    [SerializeField] private Text _MPText;

    [Header("ICON")]
    [SerializeField] private Transform _IconBox;
    private int lastHPGuageIdx;

    [Space(20)]
    [Header("BUTTON\n\nMELEE")]
    [SerializeField] private Button button_NAttack;
    [SerializeField] private Button button_SAttack;

    [Header("SPELL")]
    [SerializeField] private Button button_ESpell;
    [SerializeField] private Button button_QSpell;

    // ��ų ��ư/��ٿ� ����
    private List<SpellEffectController> currentSpellEffects;
    private List<SpellUIGroup> spellUIGroups;
    private List<SpellDelay> spellDelays;

    [Space(20)]
    [Header("EFFECT\n\nHIT")]
    [SerializeField] private GameObject bloodOverlayMain;
    [SerializeField] private GameObject[] bloodOverlays;

    private GameObject[] bloodOverlaySet;
    private bool isBloodOverlayOnDisplay;

    // --- Components & State ---
    private PlayerController _PlyCtrl;
    private AnimController _AnimCtrl;
    private StatController _StatCtrl;
    private WeaponController _WpnCtrl;
    private SpellController _SpellCtrl;
    private bool isInitialized;

    /// <summary>
    /// �̱��� ����.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 1ȸ �ʱ�ȭ ������.
    /// </summary>
    private void Start()
    {
        if (!isInitialized) Init();
        isInitialized = true;
    }

    /// <summary>
    /// ������Ʈ ���� ����, �÷����� UI ��ȯ, ��ư �̺�Ʈ ���ε�, ��ų UI �׷� ����.
    /// </summary>
    private void Init()
    {
        // [Components]
        _PlyCtrl = GameManager.Inst._plyCtrl;
        _AnimCtrl = _PlyCtrl.GetComponent<AnimController>();
        _StatCtrl = _PlyCtrl.GetComponent<StatController>();
        _WpnCtrl = _PlyCtrl.GetComponent<WeaponController>();
        _SpellCtrl = _WpnCtrl.GetSpellController();

        // [Universal UI]
        SwitchUIFormByPlatform();

        // [UIs]
        lastHPGuageIdx = _HPBox.childCount - 1;
        int LEN = (int)GlobalValue.SkillOrder.Count;

        currentSpellEffects = new List<SpellEffectController>(LEN) { null, null };
        spellUIGroups = new List<SpellUIGroup>(LEN) { null, null };
        spellDelays = new List<SpellDelay>(LEN) { new SpellDelay(), new SpellDelay() };

        // ��ư �̺�Ʈ
        button_NAttack.onClick.AddListener(() =>
        {
            if (!_AnimCtrl.IsStillNA) _PlyCtrl.isNAttack = true;
        });
        button_SAttack.onClick.AddListener(() =>
        {
            if (!_AnimCtrl.IsStillSA) _PlyCtrl.isSAttack = true;
        });
        button_ESpell.onClick.AddListener(() =>
        {
            _PlyCtrl.isSpCast = true;
            _AnimCtrl.Play_SpCastAnim();
        });
        button_QSpell.onClick.AddListener(() =>
        {
            _PlyCtrl.isSpSelf = true;
            _AnimCtrl.Play_SpSelfAnim();
        });

        // ��ų ��ư ���� UI �׷�ȭ(E/Q)
        int E = (int)GlobalValue.SkillOrder.E;
        int Q = (int)GlobalValue.SkillOrder.Q;
        spellUIGroups[E] = new SpellUIGroup(
            button_ESpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_ESpell.GetComponentInChildren<Text>());
        spellUIGroups[Q] = new SpellUIGroup(
            button_QSpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_QSpell.GetComponentInChildren<Text>());

        // ���� �� UI ��Ȱ��
        ActivateUIRoot(false);
    }

    /// <summary>
    /// �÷���(PC/�����)�� ���� ���̽�ƽ/��ư/Ű���� �ȳ� �� ���̾ƿ� ��ȯ.
    /// </summary>
    private void SwitchUIFormByPlatform()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // 1) Joystick
        if (_JoystickBox.activeSelf) _JoystickBox.SetActive(false);
        // 2) StatusBox
        _StatusBox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        // 3) NAttack
        if (button_NAttack.gameObject.activeSelf) button_NAttack.gameObject.SetActive(false);
        // 4) SAttack
        button_SAttack.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        button_SAttack.GetComponent<RectTransform>().localScale = Vector2.one;
        // 5) Pause
        if (_PauseButton.activeSelf) _PauseButton.SetActive(false);
        // 6) KB Images
        foreach (var item in _KBImages) if (!item.activeSelf) item.SetActive(true);
#elif UNITY_ANDROID || UNITY_IOS
        // 1) Joystick
        if (!_JoystickBox.activeSelf) _JoystickBox.SetActive(true);
        // 2) StatusBox
        _StatusBox.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(STATUSBOX_ANDROID_XPOS, 0f);
        // 3) NAttack
        if (!button_NAttack.gameObject.activeSelf) button_NAttack.gameObject.SetActive(true);
        // 4) SAttack
        button_SAttack.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(0f, SATKBTN_ANDROID_YPOS);
        button_SAttack.GetComponent<RectTransform>().localScale =
            Vector2.one * SATKBTN_ANDROID_SCALE;
        // 5) Pause
        if (!_PauseButton.activeSelf) _PauseButton.SetActive(true);
        // 6) KB Images
        foreach (var item in _KBImages) if (item.activeSelf) item.SetActive(false);
#endif
    }

    /// <summary>
    /// ������ ��ų�� ���� ��ų ��ư(E/Q)�� �ʱ�ȭ�Ѵ�.
    /// </summary>
    internal void InitSpellButton(SpellEffectController spellEffect, GlobalValue.SkillOrder order)
    {
        switch (order)
        {
            case GlobalValue.SkillOrder.E:
                {
                    int E = (int)GlobalValue.SkillOrder.E;

                    if (!spellEffect)
                    {
                        ActivateButton(button_ESpell, false);
                    }
                    else
                    {
                        // 1) ��ư Ȱ��
                        ActivateButton(button_ESpell, true);
                        // 2) ������ ����
                        var outer = spellEffect.OUTER;
                        var inner = spellEffect.INNER;
                        Sprite icon = Resources.Load<Sprite>(GetSPIconPath(outer, inner));
                        button_ESpell.GetComponent<Image>().sprite = icon;
                        // 3) ��ٿ� ���
                        spellDelays[E].DELAY = SPELL_DELAY;
                    }
                    currentSpellEffects[E] = spellEffect;
                    break;
                }
            case GlobalValue.SkillOrder.Q:
                {
                    int Q = (int)GlobalValue.SkillOrder.Q;

                    if (!spellEffect)
                    {
                        ActivateButton(button_QSpell, false);
                    }
                    else
                    {
                        ActivateButton(button_QSpell, true);
                        var outer = spellEffect.OUTER;
                        var inner = spellEffect.INNER;
                        Sprite icon = Resources.Load<Sprite>(GetSPIconPath(outer, inner));
                        button_QSpell.GetComponent<Image>().sprite = icon;
                        spellDelays[Q].DELAY = SPELL_DELAY;
                    }
                    currentSpellEffects[Q] = spellEffect;
                    break;
                }
            default:
                break;
        }
    }

    /// <summary>
    /// ���� ��ư�� Ȱ��/��ȣ�ۿ� ���� ���¸� ��ȯ�Ѵ�.
    /// </summary>
    private void ActivateButton(Button button, bool command)
    {
        button.interactable = command;
        button.gameObject.SetActive(command);
    }

    /// <summary>
    /// �� ������: HP/MP �ؽ�Ʈ, ��ų ��ٿ� ���� ������Ʈ.
    /// </summary>
    private void Update()
    {
        UpdateHPBarIndex();
        UpdateHPText();
        UpdateMPText();
        UpdateSpellCooldown();
    }

    /// <summary>
    /// �� ������ ����: HP/MP ��, ��ų ��ư UI(���� ��/���� �䱸ġ) �ݿ�.
    /// </summary>
    private void LateUpdate()
    {
        UpdateHPBar();
        UpdateMPBar();
        UpdateSpellButton();
    }

    /// <summary>
    /// ��ϵ� ��ų���� ��ٿ� ���� �� ����(������ ����) ó��.
    /// </summary>
    private void UpdateSpellCooldown()
    {
        foreach (var item in spellDelays)
        {
            if (item.DELAY == 0) continue; // �̵�� ��ų
            if (item.WAIT) continue;       // ��ٿ� ����(�Է� ���)

            item.DELTA += Time.deltaTime;

            if (item.DELTA >= item.DELAY)
            {
                item.DELTA = item.DELAY;
                item.WAIT = true;
            }
        }
    }

    /// <summary>
    /// ��� ��ų�� ��ٿ��� �ʱ�ȭ�Ѵ�.
    /// (�Ű������� �����ϵ� ���� ������ ���� ����)
    /// </summary>
    public void ResetSpellCoolDown(GlobalValue.SkillOrder order)
    {
        foreach (var item in spellDelays)
        {
            item.DELTA = 0f;
            item.WAIT = false;
        }
    }

    /// <summary>
    /// MP �ؽ�Ʈ ����.
    /// </summary>
    private void UpdateMPText()
    {
        _MPText.text = Mathf.CeilToInt(_StatCtrl.MPCurrent) + "/" + ((int)_StatCtrl.MP).ToString();
    }

    /// <summary>
    /// HP �ؽ�Ʈ ����.
    /// </summary>
    private void UpdateHPText()
    {
        _HPText.text = Mathf.CeilToInt(_StatCtrl.HPCurrent) + "/" + ((int)_StatCtrl.HP).ToString();
    }

    /// <summary>
    /// ��ų ��ư UI(���� �ð�/MP �䱸ġ/���ͷ��� ���� ����) ����.
    /// </summary>
    private void UpdateSpellButton()
    {
        int LEN = (int)GlobalValue.SkillOrder.Count;
        int delayTextValue = 0;
        float fillAmountValue = 0f;

        for (int index = 0; index < LEN; index++)
        {
            int capturedIndex = index;

            if (!currentSpellEffects[capturedIndex]) continue; // ������ ��ų

            // 1) ���� ��ٿ� �ؽ�Ʈ
            if (spellDelays[capturedIndex].WAIT)
            {
                spellUIGroups[capturedIndex].TextDelay.text = "";
            }
            else
            {
                delayTextValue = Mathf.CeilToInt(
                    spellDelays[capturedIndex].DELAY - spellDelays[capturedIndex].DELTA);
                spellUIGroups[capturedIndex].TextDelay.text = delayTextValue.ToString();
            }

            // 2) MP ������ ��� ��ٿ� �̹��� ä���(0=����, 1=����)
            fillAmountValue =
                (currentSpellEffects[capturedIndex].Cost - _StatCtrl.MPCurrent) /
                currentSpellEffects[capturedIndex].Cost;
            fillAmountValue = Mathf.Clamp(fillAmountValue, 0f, 1f);
            spellUIGroups[capturedIndex].ImageCoolDown.fillAmount = fillAmountValue;

            // 3) ��ư ���ͷ��� ���� ����(��ٿ� ���� & MP ���)
            GetSpellButtonByIndex(capturedIndex).interactable =
                (spellDelays[capturedIndex].WAIT && fillAmountValue == 0f);
        }
    }

    /// <summary>
    /// HP ������ �� ������ Ȱ�� �ε����� ����(���� �� ���� �����ӿ��� �� ĭ�� ����).
    /// </summary>
    private void UpdateHPBarIndex()
    {
        if (lastHPGuageIdx < 0) return;

        for (int i = 0; i < _HPBox.childCount; i++)
        {
            if (!_HPBox.GetChild(i).gameObject.activeSelf)
            {
                lastHPGuageIdx = i - 1;
                break;
            }
        }
    }

    /// <summary>
    /// �ε����� E/Q ��ư ������ ��´�.
    /// </summary>
    private Button GetSpellButtonByIndex(int index)
    {
        int LEN = (int)GlobalValue.SkillOrder.Count;
        if (index < 0 || index > LEN) return null;

        GlobalValue.SkillOrder order = (GlobalValue.SkillOrder)index;
        switch (order)
        {
            case GlobalValue.SkillOrder.E: return button_ESpell;
            case GlobalValue.SkillOrder.Q: return button_QSpell;
            default: return null;
        }
    }

    /// <summary>
    /// MP �� ����.
    /// </summary>
    private void UpdateMPBar()
    {
        _MPFill.fillAmount = _StatCtrl.MPCurrent / _StatCtrl.MP;
    }

    /// <summary>
    /// HP �� ����: ����/�ʿ� �ε��� ���̸�ŭ �� ĭ�� on/off.
    /// </summary>
    private void UpdateHPBar()
    {
        int requiredHPGuageIdx = Mathf.CeilToInt(
            (_StatCtrl.HPCurrent / _StatCtrl.HP) * _HPBox.childCount) - 1;

        if (requiredHPGuageIdx < 0)
            requiredHPGuageIdx = -1;

        if (requiredHPGuageIdx < lastHPGuageIdx)
            _HPBox.GetChild(lastHPGuageIdx).gameObject.SetActive(false);
        else if (requiredHPGuageIdx > lastHPGuageIdx)
            _HPBox.GetChild(lastHPGuageIdx + 1).gameObject.SetActive(true);
    }

    /// <summary>
    /// �ǰ� �������� ǥ��(ª�� �ð� ����).
    /// </summary>
    public void DisplayBloodOverlay()
    {
        if (!isBloodOverlayOnDisplay)
            StartCoroutine(InstBloodOverlay());
    }

    /// <summary>
    /// �ǰ� �������� �ν��Ͻ� ����/ǥ��/����.
    /// </summary>
    public IEnumerator InstBloodOverlay()
    {
        isBloodOverlayOnDisplay = true;

        int ranIdx = Random.Range(0, bloodOverlays.Length);
        bloodOverlaySet = GetBloodOverlaySet(ranIdx);

        foreach (GameObject item in bloodOverlaySet) item.SetActive(true);

        yield return new WaitForSecondsRealtime(BLOOD_OVERLAY_LIFETIME);

        foreach (GameObject item in bloodOverlaySet) item.SetActive(false);

        isBloodOverlayOnDisplay = false;
    }

    /// <summary>
    /// ����+���� �ϳ��� ������ �ǰ� �������� ��Ʈ ��ȯ.
    /// </summary>
    private GameObject[] GetBloodOverlaySet(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }

    /// <summary>
    /// ���� �� UI ��Ʈ ǥ��/����.
    /// </summary>
    public void ActivateUIRoot(bool command)
        => _UIRoot.SetActive(command);

    /// <summary>
    /// �������� ���� ������ ǥ��/����.
    /// </summary>
    public void ActivateELIcon(ElementalManager.ElementalType type, bool command)
    {
        if (type == ElementalManager.ElementalType.Count) return;

        GameObject icon = _IconBox.Find(GetELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    /// <summary>
    /// ���� ������ �̸� ��Ģ.
    /// </summary>
    private string GetELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";

    /// <summary>
    /// ��ų ������ ���ҽ� ��� ��Ģ.
    /// </summary>
    private string GetSPIconPath(SkillWindowController.SKType Outer, SkillWindowController.ELType Inner)
    {
        return $"{SPELL_ICON_PRELINK}{Outer}_{Inner}";
    }

    /// <summary>
    /// ���� ��ų�� ��ٿ��� �������� ����.
    /// </summary>
    public bool IsDelayOver(GlobalValue.SkillOrder order)
        => spellDelays[(int)order].WAIT;

    /// <summary>
    /// �Ͻ����� ��ư Ŭ�� �ڵ鷯.
    /// </summary>
    public void OnPauseButtonClick()
    {
        if (!StageManager.Inst.IsInitialized) return;
        if (!_PlyCtrl.inputEnabled) return;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
            AudioPlayerPoolManager.SFXType.Click);

        StageUIManager.Inst.TogglePauseWindow();
    }
}

[System.Serializable]
public class SpellDelay
{
    public float DELAY;
    public float DELTA;
    public bool WAIT;

    public SpellDelay()
    {
        DELAY = 0f;
        DELTA = 0f;
        WAIT = false;
    }
}

[System.Serializable]
public class SpellUIGroup
{
    public Image ImageCoolDown;
    public Text TextDelay;

    public SpellUIGroup(Image imageCoolDown = null, Text textDelay = null)
    {
        ImageCoolDown = imageCoolDown;
        TextDelay = textDelay;
    }
}
