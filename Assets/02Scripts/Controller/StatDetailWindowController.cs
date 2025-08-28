using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���� ��/��ȭ â:
/// - ���õ� ������ ������/�̸�/����/��ġ/��� ǥ��
/// - ���� ��ȭ, �ʱ�ȭ(����/��ȭ �ѹ�), ���� ��ȭ â ȣ��
/// - ��ư Ȱ�� ����(�ִ� ����/��ȭ ����) �ݿ�
/// </summary>
public class StatDetailWindowController : MonoBehaviour
{
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const string POSTNOTICE_RESET = "�� ��ȭ ������ �ʱ�ȭ�߽��ϴ�.";
    private const string ALERT_MAX_LEVEL = "�̹� �ְ� �����Դϴ�.";
    private const string ALERT_GEM = "��ȭ�� �����մϴ�.";

    public static StatDetailWindowController Inst;

    [Header("BODY")]
    [SerializeField] private Image Image_Icon;
    [SerializeField] private Text Text_Name;
    [SerializeField] private Text Text_Intro;
    [SerializeField] private TextMeshProUGUI Text_Detail;
    [SerializeField] private Text Text_Cost;

    [Header("FOOT")]
    [SerializeField] private Button button_Confirm;
    [SerializeField] private Button button_Reset;

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Count;

    private StatController.StatType selectedType;
    private StatSO selectedAsset;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// ���� ������ �ε� ��� �� Ǫ�� ��ư ������ �ʱ�ȭ.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);
        Inst = this;
        InitFoot();
    }

    /// <summary>
    /// �ϴ� ��ư ������ ����.
    /// </summary>
    private void InitFoot()
    {
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
        button_Reset.onClick.AddListener(ResetButtonOnClick);
    }

    /// <summary>
    /// Ȯ�� ��ư ��/����(�ð�) �ʱ�ȭ. (�ִ� ����/��ȭ ���� �� ��Ȱ�� ��)
    /// </summary>
    private void InitConfirmButton()
    {
        if ((GlobalValue.Instance.GetStatLevelByType(selectedType) == selectedAsset.MAXLV) ||
            (selectedAsset.COST > GlobalValue.Instance._Inven.STGEM_CNT))
        {
            ChangeButtonToDisabled(button_Confirm, true);
        }
        else
        {
            ChangeButtonToDisabled(button_Confirm, false);
        }
    }

    /// <summary>
    /// �ʱ�ȭ ��ư Ȱ�� ����(���� ���� ����) �ݿ�.
    /// </summary>
    private void InitResetButton()
    {
        button_Reset.interactable = GlobalValue.Instance.GetStatLevelByType(selectedType) > 0;
    }

    /// <summary>
    /// Ÿ�Կ� �ش��ϴ� ���� �� UI�� ����.
    /// </summary>
    public void UpdateDetailUIByType(StatController.StatType type)
    {
        InitSelected(type);
        if (!selectedAsset)
        {
            Debug.Log("[!!ERROR!!] FAILED TO LOAD ASSET");
            return;
        }

        Image_Icon.sprite = selectedAsset.ICON;
        Text_Name.text = $"[Lv.{GlobalValue.Instance.GetStatLevelByType(type):00}] {selectedAsset.NAME}";
        Text_Intro.text = selectedAsset.INTRO;
        Text_Detail.text = GetDetailText(selectedAsset);
        Text_Cost.text = selectedAsset.COST.ToString();

        InitConfirmButton();
        InitResetButton();
    }

    /// <summary>
    /// ���� ���(Ÿ��/����) ����.
    /// </summary>
    private void InitSelected(StatController.StatType type)
    {
        selectedType = type;
        selectedAsset = ResourceUtility.GetResourceByType<StatSO>(GetAssetPath(selectedType));
    }

    /// <summary>
    /// �ʱ�ȭ: ���� ������ŭ GEM ȯ�� �� ���� 0���� �ѹ�, �޽��� �� UI ����.
    /// </summary>
    private void ResetButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay) return;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        int currentLevel = GlobalValue.Instance.GetStatLevelByType(selectedType);

        GlobalValue.Instance._Inven.STGEM_CNT += selectedAsset.COST * currentLevel;
        GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, -currentLevel);

        StartCoroutine(StatWindowController.Inst.DisplayMessageWindow($"[{selectedAsset.NAME}]{POSTNOTICE_RESET}", 1.5f));

        UpdateDetailUIByType(selectedType);
        StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);
    }

    /// <summary>
    /// ���� ��ȭ �Ǵ� ���� ��ȭ â ȣ��(���� �� ���� ��): ������/��ȭ ����/����/UI ����.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay) return;

        // �ִ� ����
        if (GlobalValue.Instance.GetStatLevelByType(selectedType) == StatController.MAX_STAT_LEVEL)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(StatWindowController.Inst.DisplayMessageWindow(ALERT_MAX_LEVEL, 1f));
            return;
        }

        // ��ȭ ����
        if (selectedAsset.COST > GlobalValue.Instance._Inven.STGEM_CNT)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(StatWindowController.Inst.DisplayMessageWindow(ALERT_GEM, 1f));
            return;
        }

        // ������ Ƚ�� ���
        int possibleCount = Mathf.FloorToInt(GlobalValue.Instance._Inven.STGEM_CNT / selectedAsset.COST);
        possibleCount = Mathf.Clamp(
            possibleCount,
            0,
            selectedAsset.MAXLV - GlobalValue.Instance.GetStatLevelByType(selectedType));

        // 2ȸ �̻� ����: �ϰ� ��ȭ â ����
        if (possibleCount > 1)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            window_Count.gameObject.SetActive(true);
        }
        // ���� ��ȭ ����
        else
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);

            GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, 1);
            GlobalValue.Instance._Inven.STGEM_CNT -= selectedAsset.COST;
            GlobalValue.Instance.SaveInven();

            UpdateDetailUIByType(selectedType);
            StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);

            Debug.Log($"{selectedType}: {GlobalValue.Instance.GetStatLevelByType(selectedType)}");
        }
    }

    /// <summary>
    /// ��ư ���� �ٲ� ��Ȱ�� ����ó�� ���̰� ��.
    /// </summary>
    private void ChangeButtonToDisabled(Button button, bool command)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = command
            ? new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f)
            : Color.white;
    }

    private string GetDetailText(StatSO asset)
    {
        int LV = GlobalValue.Instance.GetStatLevelByType(asset.TYPE);
        float preValue = asset.BASE + asset.PER * LV;
        float postValue = preValue + asset.PER;

        string textPreValue;
        string textPostValue;

        switch (asset.FORMAT)
        {
            case StatSO.ValueType.Sec:
                textPreValue = $"{preValue:F3}�ʴ� 1";
                textPostValue = $"{postValue:F3}�ʴ� 1";
                break;
            case StatSO.ValueType.Rate:
                textPreValue = $"{(preValue * 100f):F1}%";
                textPostValue = $"{(postValue * 100f):F1}%";
                break;
            default:
                textPreValue = $"{preValue}";
                textPostValue = $"{postValue}";
                break;
        }

        return $"�⺻ {asset.NAME}(��)�� " +
               $"<size=180%><color=#ffff00>{textPreValue}</color></size>���� " +
               $"<size=180%><color=#00ff00>{textPostValue}</color></size>�� ����";
    }

    private string GetAssetPath(StatController.StatType type)
        => STAT_SO_PRELINK + type + STAT_SO_POSTLINK;

    public StatSO SelectedAsset => selectedAsset;
}
