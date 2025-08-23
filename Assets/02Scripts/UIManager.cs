using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const string SPELL_ICON_PRELINK = "Icon/";
    private const string COOLDOWN_IMAGE_NAME = "Image_CoolDown";
    private const float BLOOD_OVERLAY_LIFETIME = 0.15f;
    private const float SPELL_DELAY = 3f;
    private const float STATUSBOX_ANDROID_XPOS = 600f;
    private const float SATKBTN_ANDROID_YPOS = 250f;
    private const float SATKBTN_ANDROID_SCALE = 0.75f;

    public static UIManager Inst;

    [Space(20)]
    [Header("ROOT")]
    [SerializeField] private GameObject _UIRoot;
    [Header("UNIVERSAL")]
    [SerializeField] private GameObject _StatusBox;
    [SerializeField] private GameObject _JoystickBox;
    [SerializeField] private GameObject[] _KBImages;
    [SerializeField] private GameObject _PauseButton;
    //[SerializeField] private Transform root_Melee;
    [Space(20)]
    [Header("STATUS\n\nHP")]
    [SerializeField] private Transform _HPBox;
    [SerializeField] private Text _HPText;
    [Header("MP")]
    [SerializeField] private Image _MPFill;
    [SerializeField] private Text _MPText;
    [Header("ICON")]
    [SerializeField] private Transform _IconBox;
    int lastHPGuageIdx;

    [Space(20)]
    [Header("BUTTON\n\nMELEE")]
    [SerializeField] private Button button_NAttack;
    [SerializeField] private Button button_SAttack;
    [Header("SPELL")]
    [SerializeField] private Button button_ESpell;
    [SerializeField] private Button button_QSpell;
    List<SpellEffectController> currentSpellEffects;
    List<SpellUIGroup> spellUIGroups;
    List<SpellDelay> spellDelays;

    [Space(20)]
    [Header("EFFECT\n\nHIT")]
    [SerializeField] private GameObject bloodOverlayMain;
    [SerializeField] private GameObject[] bloodOverlays;
    GameObject[] bloodOverlaySet;
    bool isBloodOverlayOnDisplay;

    PlayerController _PlyCtrl;
    AnimController _AnimCtrl;
    StatController _StatCtrl;
    WeaponController _WpnCtrl;
    SpellController _SpellCtrl;
    bool isInitialized;

    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    private void Start()
    {
        if (!isInitialized) Init();
        isInitialized = true;
    }

    private void Init()
    {
        /*Test*/
        //Cursor.lockState = CursorLockMode.Confined;
        //Cursor.visible = true;
        /*Test*/

        //--- [Components]
        _PlyCtrl = GameManager.Inst._plyCtrl;
        _AnimCtrl = _PlyCtrl.GetComponent<AnimController>();
        _StatCtrl = _PlyCtrl.GetComponent<StatController>();
        _WpnCtrl = _PlyCtrl.GetComponent<WeaponController>();
        _SpellCtrl = _WpnCtrl.GetSpellController();
        //---

        //--- [Universal UI]
        SwitchUIFormByPlatform();
        //---

        //--- [UIs]
        lastHPGuageIdx = _HPBox.childCount - 1;
        int LEN = (int)GlobalValue.SkillOrder.Count;
        currentSpellEffects = new List<SpellEffectController>(LEN)
        {null,null};
        spellUIGroups = new List<SpellUIGroup>(LEN)
        {null,null};
        spellDelays = new List<SpellDelay>(LEN)
        {
            new SpellDelay(),
            new SpellDelay()
        };

        button_NAttack.onClick.AddListener(() =>
        {
            if (!_AnimCtrl.IsStillNA)
                _PlyCtrl.isNAttack = true;
            //_AnimCtrl.Play_NAtkAnim();
        });
        button_SAttack.onClick.AddListener(() =>
        {
            if (!_AnimCtrl.IsStillSA)
                _PlyCtrl.isSAttack = true;
            //_AnimCtrl.Play_SAtkAnim();
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
        //---

        //Grouping UIs in spell button.
        int E = (int)GlobalValue.SkillOrder.E;
        int Q = (int)GlobalValue.SkillOrder.Q;
        spellUIGroups[E] = new SpellUIGroup
            (button_ESpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_ESpell.GetComponentInChildren<Text>());
        spellUIGroups[Q] = new SpellUIGroup
            (button_QSpell.transform.Find(COOLDOWN_IMAGE_NAME).GetComponent<Image>(),
            button_QSpell.GetComponentInChildren<Text>());

        //Deactivate UIRoot from start.
        ActivateUIRoot(false);
    }

    private void SwitchUIFormByPlatform()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //1. Joystick
        if (_JoystickBox.activeSelf)
            _JoystickBox.SetActive(false);
        //2. StatusBox
        _StatusBox.GetComponent<RectTransform>().anchoredPosition
            = Vector2.zero;
        //3. NAttack button
        if (button_NAttack.gameObject.activeSelf)
            button_NAttack.gameObject.SetActive(false);
        //4. SAttack button
        button_SAttack.GetComponent<RectTransform>().anchoredPosition
            = Vector2.zero;
        button_SAttack.GetComponent<RectTransform>().localScale
            = Vector2.one;
        //5. Pause button
        if (_PauseButton.activeSelf)
            _PauseButton.SetActive(false);
        //6. Keyboard images
        foreach (var item in _KBImages)
        {
            if (!item.activeSelf)
                item.SetActive(true);
        }
#elif UNITY_ANDROID || UNITY_IOS
        //1. Joystick
        if (!_JoystickBox.activeSelf) 
            _JoystickBox.SetActive(true);
        //2. StatusBox
        _StatusBox.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(STATUSBOX_ANDROID_XPOS, 0f);
        //3. NAttack button
        if (!button_NAttack.gameObject.activeSelf)
            button_NAttack.gameObject.SetActive(true);
        //4. SAttack button
        button_SAttack.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(0f, SATKBTN_ANDROID_YPOS);
        button_SAttack.GetComponent<RectTransform>().localScale
            = Vector2.one * SATKBTN_ANDROID_SCALE;
        //5. Pause button
        if (!_PauseButton.activeSelf)
            _PauseButton.SetActive(true);
        //6. Keyboard images
        foreach(var item in _KBImages)
        {
            if (item.activeSelf)
                item.SetActive(false);
        }
#endif
    }

    /// <summary>
    /// 착용한 스킬에 따라 UI를 초기화 하는 메서드 입니다.
    /// </summary>
    /// <param name="spellEffect">착용 스킬</param>
    /// <param name="order">E, Q버튼 구분자</param>
    internal void InitSpellButton
        (
        SpellEffectController spellEffect,
        GlobalValue.SkillOrder order
        )
    {
        switch (order)
        {
            case GlobalValue.SkillOrder.E:
                int E = (int)GlobalValue.SkillOrder.E;

                if (!spellEffect)
                {
                    ActivateButton(button_ESpell, false);
                }
                else
                {
                    //1. Activate E button.
                    ActivateButton(button_ESpell, true);
                    //2. Set equipped E spell's icon to button.
                    SkillWindowController.SKType Outer = spellEffect.OUTER;
                    SkillWindowController.ELType Inner = spellEffect.INNER;
                    Sprite icon = Resources.Load<Sprite>(GetSPIconPath(Outer, Inner));
                    button_ESpell.GetComponent<Image>().sprite = icon;
                    //3. Activate E spell delay.
                    spellDelays[E].DELAY = SPELL_DELAY;
                }

                currentSpellEffects[E] = spellEffect;
                break;
            case GlobalValue.SkillOrder.Q:
                int Q = (int)GlobalValue.SkillOrder.Q;

                if (!spellEffect)
                {
                    ActivateButton(button_QSpell, false);
                }
                else
                {
                    //1. Activate Q button.
                    ActivateButton(button_QSpell, true);
                    //2. Set equipped Q spell's icon to button.
                    SkillWindowController.SKType Outer = spellEffect.OUTER;
                    SkillWindowController.ELType Inner = spellEffect.INNER;
                    Sprite icon = Resources.Load<Sprite>(GetSPIconPath(Outer, Inner));
                    button_QSpell.GetComponent<Image>().sprite = icon;
                    //3. Activate Q spell cooldown.
                    spellDelays[Q].DELAY = SPELL_DELAY;
                }

                currentSpellEffects[Q] = spellEffect;
                break;
            default:
                break;
        }
    }

    private void ActivateButton(Button button, bool command)
    {
        button.interactable = command;
        button.gameObject.SetActive(command);
    }

    private void Update()
    {
        //--- [STATUS]
        UpdateHPBarIndex();
        UpdateHPText();
        UpdateMPText();
        //---

        //--- [SPELL]
        UpdateSpellCooldown();
        //---
    }

    private void UpdateSpellCooldown()
    {
        foreach (var item in spellDelays)
        {
            //Skip if spell is not registered yet.
            if (item.DELAY == 0) continue;
            //Skip if is cooldown over and wait for player input. 
            if (item.WAIT) continue;

            item.DELTA += Time.deltaTime;
            //Set spell cooldown over to wait for player input. 
            if (item.DELTA >= item.DELAY)
            {
                item.DELTA = item.DELAY;
                item.WAIT = true;
            }
        }
    }

    public void ResetSpellCoolDown(GlobalValue.SkillOrder order)
    {
        foreach (var item in spellDelays)
        {
            item.DELTA = 0f;
            item.WAIT = false;
        }
    }

    private void UpdateMPText()
    {
        _MPText.text
            = Mathf.CeilToInt(_StatCtrl.MPCurrent).ToString()
            + "/"
            + ((int)_StatCtrl.MP).ToString();
    }

    private void UpdateHPText()
    {
        _HPText.text
            = Mathf.CeilToInt(_StatCtrl.HPCurrent).ToString()
            + "/"
            + ((int)_StatCtrl.HP).ToString();
    }

    private void LateUpdate()
    {
        UpdateHPBar();
        UpdateMPBar();
        UpdateSpellButton();
    }

    private void UpdateSpellButton()
    {
        int LEN = (int)GlobalValue.SkillOrder.Count;
        int delayTextValue = 0;
        float fillAmountValue = 0f;

        for (int index = 0; index < LEN; index++)
        {
            int capturedIndex = index;

            //Skip if spell is not equipped.
            if (!currentSpellEffects[capturedIndex]) continue;

            //1. Update delay text.
            if (spellDelays[capturedIndex].WAIT)
            {
                spellUIGroups[capturedIndex].TextDelay.text = "";
            }
            else
            {
                delayTextValue = Mathf.CeilToInt
                                (spellDelays[capturedIndex].DELAY -
                                spellDelays[capturedIndex].DELTA);
                spellUIGroups[capturedIndex].TextDelay.text =
                    delayTextValue.ToString();
            }

            //2. Update cooldown image.
            fillAmountValue =
                (currentSpellEffects[capturedIndex].Cost - _StatCtrl.MPCurrent) /
                currentSpellEffects[capturedIndex].Cost;
            fillAmountValue = Mathf.Clamp(fillAmountValue, 0f, 1f);
            spellUIGroups[capturedIndex].ImageCoolDown.fillAmount = fillAmountValue;

            //3. Check if button is interactable.
            GetSpellButtonByIndex(capturedIndex).interactable
                = (spellDelays[capturedIndex].WAIT && fillAmountValue == 0f);
        }
    }

    private void UpdateHPBarIndex()
    {
        //--- HPBar
        //Update index of last guage image activated.
        //-1 if HPCurrent is 0.
        if (lastHPGuageIdx < 0)
            return;

        for (int i = 0; i < _HPBox.childCount; i++)
        {
            if (!_HPBox.GetChild(i).gameObject.activeSelf)
            {
                lastHPGuageIdx = i - 1;
                break;
            }
        }
        //---
    }

    private Button GetSpellButtonByIndex(int index)
    {
        int LEN = (int)GlobalValue.SkillOrder.Count;
        if (index < 0 || index > LEN) return null;

        GlobalValue.SkillOrder order = (GlobalValue.SkillOrder)index;
        switch (order)
        {
            case GlobalValue.SkillOrder.E:
                return button_ESpell;
            case GlobalValue.SkillOrder.Q:
                return button_QSpell;
            default:
                return null;
        }
    }

    private void UpdateMPBar()
    {
        _MPFill.fillAmount = _StatCtrl.MPCurrent / _StatCtrl.MP;
    }

    private void UpdateHPBar()
    {
        //1. 최대 이미지 개수 저장(초기값= 24(childCount))
        //2. 현재 필요한 이미지 개수 저장(HPCurrent / HP * 최대 이미지 개수=> floorToInt)
        int requiredHPGuageIdx = Mathf.CeilToInt(
            (_StatCtrl.HPCurrent / _StatCtrl.HP) * _HPBox.childCount) - 1;
        //3. 만약 현재 존재하는 이미지 개수(childCount)가 현재 필요한 이미지 개수보다 적거나 많다면
        //  마지막 인덱스 접근해서 활성화/비활성화

        if (requiredHPGuageIdx < 0)
            requiredHPGuageIdx = -1;

        if (requiredHPGuageIdx < lastHPGuageIdx)
            _HPBox.GetChild(lastHPGuageIdx).gameObject.SetActive(false);
        else if (requiredHPGuageIdx > lastHPGuageIdx)
            _HPBox.GetChild(lastHPGuageIdx + 1).gameObject.SetActive(true);
    }

    public void DisplayBloodOverlay()
    {
        if (!isBloodOverlayOnDisplay)
            StartCoroutine(InstBloodOverlay());
    }

    public IEnumerator InstBloodOverlay()
    {
        isBloodOverlayOnDisplay = true;
        //int ranIdx = Random.Range(0, _boCtrl.CountBloodOverlays());
        int ranIdx = Random.Range(0, bloodOverlays.Length);

        bloodOverlaySet = GetBloodOverlaySet(ranIdx);
        foreach (GameObject item in bloodOverlaySet)
        {
            item.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(BLOOD_OVERLAY_LIFETIME);
        foreach (GameObject item in bloodOverlaySet)
        {
            item.SetActive(false);
        }

        isBloodOverlayOnDisplay = false;
    }

    private GameObject[] GetBloodOverlaySet(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }

    public void ActivateUIRoot(bool command)
        => _UIRoot.SetActive(command);

    public void ActivateELIcon
        (ElementalManager.ElementalType type
        , bool command)
    {
        if (type == ElementalManager.ElementalType.Count)
            return;

        GameObject icon = _IconBox.Find(GetELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    private string GetELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";

    private string GetSPIconPath
        (
        SkillWindowController.SKType Outer,
        SkillWindowController.ELType Inner
        )
    {
        return $"{SPELL_ICON_PRELINK}{Outer}_{Inner}";
    }

    public bool IsDelayOver(GlobalValue.SkillOrder order)
        => spellDelays[(int)order].WAIT;

    public void OnPauseButtonClick()
    {
        if (!StageManager.Inst.IsInitialized) return;
        if (!_PlyCtrl.inputEnabled) return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

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

    public SpellUIGroup
        (
        Image imageCoolDown = null,
        Text textDelay = null
        )
    {
        ImageCoolDown = imageCoolDown;
        TextDelay = textDelay;
    }
}