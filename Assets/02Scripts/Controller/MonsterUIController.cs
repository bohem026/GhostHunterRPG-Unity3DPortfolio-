using UnityEngine;
using UnityEngine.UI;

public class MonsterUIController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Transform box; // ���� ��� UI�� ���� ĵ���� Ʈ������

    [Space(20)]
    [Header("UI: HP")]
    [SerializeField] private Image _HPBar;

    [Space(20)]
    [Header("UI: Icon")]
    [SerializeField] private Transform _IconBox;

    // --- Runtime refs ---
    private MonsterController _MonCtrl;
    private StatController _StatCtrl;

    /// <summary>
    /// �ʿ��� ������Ʈ�� ĳ���ϰ� �ʱ� ���� �����Ѵ�.
    /// </summary>
    private void OnEnable()
    {
        if (!_MonCtrl) _MonCtrl = GetComponent<MonsterController>();
        if (!_StatCtrl) _StatCtrl = GetComponent<StatController>();
        Init();
    }

    /// <summary>
    /// �� ������: UI�� ī�޶� �������� ����(������)�ϰ� HP�ٸ� �����Ѵ�.
    /// </summary>
    private void Update()
    {
        // Billboard: �׻� ī�޶� �ٶ󺸵��� ȸ�� ����
        box.LookAt(
            box.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up
        );

        UpdateHPBar();
    }

    /// <summary>
    /// HP�� ǥ�� ����/������/������ �����Ѵ�.
    /// </summary>
    private void UpdateHPBar()
    {
        // 1) ���� ���̸� ���� ���� �ƴ� ���� HP�� ǥ��
        ActivateHPBar(_MonCtrl.isTarget && !_MonCtrl.IsAttack);

        if (!_HPBar.IsActive()) return;

        // 2) ������ ä�� (���� HP / �ִ� HP)
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;

        // 3) ���� ���� ���� ���� �Ӽ��� ���� ���� ����
        _HPBar.color = GetColorOfHPBar(_StatCtrl.ELOnAttack);
    }

    /// <summary>
    /// ���� Ÿ�Կ� ���� HP�� ������ ��ȯ�Ѵ�.
    /// </summary>
    private Color GetColorOfHPBar(
        ElementalManager.ElementalType type = ElementalManager.ElementalType.Count
    )
    {
        switch (type)
        {
            case ElementalManager.ElementalType.Fire: return new Color(1f, 0.6470588f, 0f, 1f);
            case ElementalManager.ElementalType.Ice: return new Color(0f, 0.489625f, 1f, 1f);
            case ElementalManager.ElementalType.Light: return new Color(1f, 1f, 0f, 1f);
            case ElementalManager.ElementalType.Poison: return Color.green;
            default: return Color.white;
        }
    }

    /// <summary>
    /// �ʱ� HP�� ���¸� �����Ѵ�.
    /// </summary>
    private void Init()
    {
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;
    }

    /// <summary>
    /// HP�� ��Ʈ ������Ʈ�� ǥ�� ���θ� �����Ѵ�.
    /// </summary>
    public void ActivateHPBar(bool command)
        => _HPBar.transform.parent.gameObject.SetActive(command);

    /// <summary>
    /// Ư�� ���� �������� ǥ�� ���θ� �����Ѵ�.
    /// </summary>
    public void ActivateELIcon(ElementalManager.ElementalType type, bool command)
    {
        if (type == ElementalManager.ElementalType.Count) return;

        GameObject icon = _IconBox.Find(ELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    /// <summary>
    /// ���� Ÿ�Կ� �ش��ϴ� ������ ������Ʈ���� ��ȯ�Ѵ�.
    /// </summary>
    private string ELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";
}
