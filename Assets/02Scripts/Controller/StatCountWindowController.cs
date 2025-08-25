using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ���� �ϰ� ��ȭ �˾�:
/// - ���õ� ����/����/��ȭ �������� �ϰ� ��ȭ ���� ���
/// - �����̴��� ��ȭ Ƚ�� ����, Ȯ�� �� ���� ���/��ȭ ����/���� UI ����
/// </summary>
public class StatCountWindowController : MonoBehaviour
{
    [Header("HEADER")]
    [SerializeField] private Button buttonArea_Close;
    [SerializeField] private Button button_Close;

    [Header("BODY")]
    [SerializeField] private Image image_Icon;
    [SerializeField] private Text text_Name;
    [SerializeField] private TextMeshProUGUI text_Level;
    [SerializeField] private Slider slider_Count;

    [Header("FOOT")]
    [SerializeField] private Button button_Confirm;

    private StatSO selectedAsset;
    private StatController.StatType selectedType;
    private int selectedLevel;      // ���� ����(��ȭ �� ����)
    private int selectedCount = 1;  // �ϰ� ��ȭ Ƚ��

    private void OnEnable()
    {
        Init();
        InitBody();
        InitFoot();
    }

    private void Start()
    {
        InitListener();
    }

    /// <summary>
    /// ��ư/�����̴� ������ ���.
    /// </summary>
    private void InitListener()
    {
        // HEADER
        buttonArea_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });
        button_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // BODY
        slider_Count.onValueChanged.AddListener(CountSliderValueOnChange);

        // FOOT
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
    }

    /// <summary>
    /// ���� ���õ� ����/����/�ʱ� ��ȭ Ƚ�� ����.
    /// </summary>
    private void Init()
    {
        selectedAsset = StatDetailWindowController.Inst.SelectedAsset;
        selectedType = selectedAsset.TYPE;
        selectedLevel = GlobalValue.Instance.GetStatLevelByType(selectedType);
        selectedCount = 1;
    }

    /// <summary>
    /// ���� UI(������/�̸�/����/�����̴�) �ʱ�ȭ.
    /// </summary>
    private void InitBody()
    {
        image_Icon.sprite = selectedAsset.ICON;
        text_Name.text = selectedAsset.NAME;
        text_Level.text = $"<color=#00ff00><size=125%>{(selectedLevel + selectedCount):00}</size></color> /{selectedAsset.MAXLV}";

        InitCountSlider();
    }

    /// <summary>
    /// �ϴ� ��ư �ʱ� �ؽ�Ʈ ����.
    /// </summary>
    private void InitFoot()
    {
        button_Confirm.GetComponentInChildren<Text>().text = $"{selectedCount}ȸ ��ȭ";
    }

    /// <summary>
    /// ���� GEM/�ִ� ���� �������� �����̴� ����/�� �ʱ�ȭ.
    /// </summary>
    private void InitCountSlider()
    {
        int minLevel = selectedLevel + selectedCount;
        int possibleCount = Mathf.FloorToInt(GlobalValue.Instance._Inven.STGEM_CNT / selectedAsset.COST);
        int maxLevel = Mathf.Clamp(selectedLevel + possibleCount, minLevel, selectedAsset.MAXLV);

        slider_Count.minValue = minLevel;
        slider_Count.maxValue = maxLevel;
        slider_Count.value = slider_Count.minValue;
    }

    /// <summary>
    /// Ȯ�� ��ư: �ϰ� ��ȭ ����(���� ����, GEM ����, ���� UI ����) �� â �ݱ�.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);

        // 1) ���� ����
        GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, selectedCount);

        // 2) GEM ���� ����
        GlobalValue.Instance._Inven.STGEM_CNT -= selectedAsset.COST * selectedCount;
        GlobalValue.Instance.SaveInven();

        // 3) ���� UI ����
        StatDetailWindowController.Inst.UpdateDetailUIByType(selectedType);
        StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// �����̴� �� ���� �� ��ȭ Ƚ��/ǥ�� �ؽ�Ʈ ����.
    /// </summary>
    private void CountSliderValueOnChange(float _)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Slider);

        selectedCount = (int)slider_Count.value - selectedLevel;

        text_Level.text = $"<color=#00ff00><size=125%>{(selectedLevel + selectedCount):00}</size></color> /{selectedAsset.MAXLV}";
        button_Confirm.GetComponentInChildren<Text>().text = $"{selectedCount}ȸ ��ȭ";
    }
}
