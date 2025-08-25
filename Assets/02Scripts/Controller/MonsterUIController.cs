using UnityEngine;
using UnityEngine.UI;

public class MonsterUIController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Transform box; // 몬스터 상단 UI가 붙을 캔버스 트랜스폼

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
    /// 필요한 컴포넌트를 캐시하고 초기 값을 세팅한다.
    /// </summary>
    private void OnEnable()
    {
        if (!_MonCtrl) _MonCtrl = GetComponent<MonsterController>();
        if (!_StatCtrl) _StatCtrl = GetComponent<StatController>();
        Init();
    }

    /// <summary>
    /// 매 프레임: UI를 카메라 정면으로 고정(빌보드)하고 HP바를 갱신한다.
    /// </summary>
    private void Update()
    {
        // Billboard: 항상 카메라를 바라보도록 회전 고정
        box.LookAt(
            box.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up
        );

        UpdateHPBar();
    }

    /// <summary>
    /// HP바 표시 여부/게이지/색상을 갱신한다.
    /// </summary>
    private void UpdateHPBar()
    {
        // 1) 조준 중이며 공격 중이 아닐 때만 HP바 표시
        ActivateHPBar(_MonCtrl.isTarget && !_MonCtrl.IsAttack);

        if (!_HPBar.IsActive()) return;

        // 2) 게이지 채움 (현재 HP / 최대 HP)
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;

        // 3) 현재 적용 중인 원소 속성에 따라 색상 변경
        _HPBar.color = GetColorOfHPBar(_StatCtrl.ELOnAttack);
    }

    /// <summary>
    /// 원소 타입에 따른 HP바 색상을 반환한다.
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
    /// 초기 HP바 상태를 세팅한다.
    /// </summary>
    private void Init()
    {
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;
    }

    /// <summary>
    /// HP바 루트 오브젝트의 표시 여부를 제어한다.
    /// </summary>
    public void ActivateHPBar(bool command)
        => _HPBar.transform.parent.gameObject.SetActive(command);

    /// <summary>
    /// 특정 원소 아이콘의 표시 여부를 제어한다.
    /// </summary>
    public void ActivateELIcon(ElementalManager.ElementalType type, bool command)
    {
        if (type == ElementalManager.ElementalType.Count) return;

        GameObject icon = _IconBox.Find(ELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    /// <summary>
    /// 원소 타입에 해당하는 아이콘 오브젝트명을 반환한다.
    /// </summary>
    private string ELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";
}
