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

    // 스킬 버튼/쿨다운 관리
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
    /// 싱글톤 설정.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 1회 초기화 진입점.
    /// </summary>
    private void Start()
    {
        if (!isInitialized) Init();
        isInitialized = true;
    }

    /// <summary>
    /// 컴포넌트 참조 수집, 플랫폼별 UI 전환, 버튼 이벤트 바인딩, 스킬 UI 그룹 구성.
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

        // 버튼 이벤트
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

        // 스킬 버튼 하위 UI 그룹화(E/Q)
        int E = (int)GlobalValue.SkillOrder.E;
        int Q = (int)GlobalValue.SkillOrder.Q;
        spellUIGroups[E] = new SpellUIGroup(
            button_ESpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_ESpell.GetComponentInChildren<Text>());
        spellUIGroups[Q] = new SpellUIGroup(
            button_QSpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_QSpell.GetComponentInChildren<Text>());

        // 시작 시 UI 비활성
        ActivateUIRoot(false);
    }

    /// <summary>
    /// 플랫폼(PC/모바일)에 따라 조이스틱/버튼/키보드 안내 등 레이아웃 전환.
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
    /// 착용한 스킬에 따라 스킬 버튼(E/Q)을 초기화한다.
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
                        // 1) 버튼 활성
                        ActivateButton(button_ESpell, true);
                        // 2) 아이콘 지정
                        var outer = spellEffect.OUTER;
                        var inner = spellEffect.INNER;
                        Sprite icon = Resources.Load<Sprite>(GetSPIconPath(outer, inner));
                        button_ESpell.GetComponent<Image>().sprite = icon;
                        // 3) 쿨다운 등록
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
    /// 지정 버튼의 활성/상호작용 가능 상태를 전환한다.
    /// </summary>
    private void ActivateButton(Button button, bool command)
    {
        button.interactable = command;
        button.gameObject.SetActive(command);
    }

    /// <summary>
    /// 매 프레임: HP/MP 텍스트, 스킬 쿨다운 진행 업데이트.
    /// </summary>
    private void Update()
    {
        UpdateHPBarIndex();
        UpdateHPText();
        UpdateMPText();
        UpdateSpellCooldown();
    }

    /// <summary>
    /// 매 프레임 이후: HP/MP 바, 스킬 버튼 UI(남은 쿨/마나 요구치) 반영.
    /// </summary>
    private void LateUpdate()
    {
        UpdateHPBar();
        UpdateMPBar();
        UpdateSpellButton();
    }

    /// <summary>
    /// 등록된 스킬들의 쿨다운 누적 및 종료(대기상태 진입) 처리.
    /// </summary>
    private void UpdateSpellCooldown()
    {
        foreach (var item in spellDelays)
        {
            if (item.DELAY == 0) continue; // 미등록 스킬
            if (item.WAIT) continue;       // 쿨다운 종료(입력 대기)

            item.DELTA += Time.deltaTime;

            if (item.DELTA >= item.DELAY)
            {
                item.DELTA = item.DELAY;
                item.WAIT = true;
            }
        }
    }

    /// <summary>
    /// 모든 스킬의 쿨다운을 초기화한다.
    /// (매개변수는 유지하되 현재 로직은 전부 리셋)
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
    /// MP 텍스트 갱신.
    /// </summary>
    private void UpdateMPText()
    {
        _MPText.text = Mathf.CeilToInt(_StatCtrl.MPCurrent) + "/" + ((int)_StatCtrl.MP).ToString();
    }

    /// <summary>
    /// HP 텍스트 갱신.
    /// </summary>
    private void UpdateHPText()
    {
        _HPText.text = Mathf.CeilToInt(_StatCtrl.HPCurrent) + "/" + ((int)_StatCtrl.HP).ToString();
    }

    /// <summary>
    /// 스킬 버튼 UI(남은 시간/MP 요구치/인터랙션 가능 여부) 갱신.
    /// </summary>
    private void UpdateSpellButton()
    {
        int LEN = (int)GlobalValue.SkillOrder.Count;
        int delayTextValue = 0;
        float fillAmountValue = 0f;

        for (int index = 0; index < LEN; index++)
        {
            int capturedIndex = index;

            if (!currentSpellEffects[capturedIndex]) continue; // 미장착 스킬

            // 1) 남은 쿨다운 텍스트
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

            // 2) MP 부족량 기반 쿨다운 이미지 채우기(0=가능, 1=부족)
            fillAmountValue =
                (currentSpellEffects[capturedIndex].Cost - _StatCtrl.MPCurrent) /
                currentSpellEffects[capturedIndex].Cost;
            fillAmountValue = Mathf.Clamp(fillAmountValue, 0f, 1f);
            spellUIGroups[capturedIndex].ImageCoolDown.fillAmount = fillAmountValue;

            // 3) 버튼 인터랙션 가능 여부(쿨다운 종료 & MP 충분)
            GetSpellButtonByIndex(capturedIndex).interactable =
                (spellDelays[capturedIndex].WAIT && fillAmountValue == 0f);
        }
    }

    /// <summary>
    /// HP 게이지 중 마지막 활성 인덱스를 추적(증감 시 다음 프레임에서 한 칸만 변경).
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
    /// 인덱스로 E/Q 버튼 참조를 얻는다.
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
    /// MP 바 갱신.
    /// </summary>
    private void UpdateMPBar()
    {
        _MPFill.fillAmount = _StatCtrl.MPCurrent / _StatCtrl.MP;
    }

    /// <summary>
    /// HP 바 갱신: 이전/필요 인덱스 차이만큼 한 칸씩 on/off.
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
    /// 피격 오버레이 표시(짧은 시간 노출).
    /// </summary>
    public void DisplayBloodOverlay()
    {
        if (!isBloodOverlayOnDisplay)
            StartCoroutine(InstBloodOverlay());
    }

    /// <summary>
    /// 피격 오버레이 인스턴스 생성/표시/해제.
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
    /// 메인+랜덤 하나로 구성된 피격 오버레이 세트 반환.
    /// </summary>
    private GameObject[] GetBloodOverlaySet(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }

    /// <summary>
    /// 게임 내 UI 루트 표시/숨김.
    /// </summary>
    public void ActivateUIRoot(bool command)
        => _UIRoot.SetActive(command);

    /// <summary>
    /// 스테이지 원소 아이콘 표시/숨김.
    /// </summary>
    public void ActivateELIcon(ElementalManager.ElementalType type, bool command)
    {
        if (type == ElementalManager.ElementalType.Count) return;

        GameObject icon = _IconBox.Find(GetELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    /// <summary>
    /// 원소 아이콘 이름 규칙.
    /// </summary>
    private string GetELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";

    /// <summary>
    /// 스킬 아이콘 리소스 경로 규칙.
    /// </summary>
    private string GetSPIconPath(SkillWindowController.SKType Outer, SkillWindowController.ELType Inner)
    {
        return $"{SPELL_ICON_PRELINK}{Outer}_{Inner}";
    }

    /// <summary>
    /// 지정 스킬의 쿨다운이 끝났는지 여부.
    /// </summary>
    public bool IsDelayOver(GlobalValue.SkillOrder order)
        => spellDelays[(int)order].WAIT;

    /// <summary>
    /// 일시정지 버튼 클릭 핸들러.
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
