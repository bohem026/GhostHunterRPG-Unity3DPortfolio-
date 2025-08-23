using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterUIController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Transform box;     //Canvas

    [Space(20)]
    [Header("UI: HP")]
    [SerializeField] private Image _HPBar;

    [Space(20)]
    [Header("UI: Icon")]
    [SerializeField] private Transform _IconBox;

    private MonsterController _MonCtrl;
    private StatController _StatCtrl;
    private AttackLineController _ALCtrl;

    private void OnEnable()
    {
        if (!_MonCtrl) _MonCtrl = GetComponent<MonsterController>();
        if (!_StatCtrl) _StatCtrl = GetComponent<StatController>();

        Init();
    }

    private void Update()
    {
        //Fix canvas rotation.
        box.LookAt(box.position + Camera.main.transform.rotation * Vector3.forward,
                         Camera.main.transform.rotation * Vector3.up);

        UpdateHPBar();
    }

    private void UpdateHPBar()
    {
        //1. Activate HPBar.
        ActivateHPBar(_MonCtrl.isTarget && !_MonCtrl.IsAttack);

        if (!_HPBar.IsActive()) return;

        //2. Update fillAmount value.
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;

        //3. Update Bar color by ELCurrent.
        _HPBar.color = GetColorOfHPBar(_StatCtrl.ELOnAttack);
    }

    private Color GetColorOfHPBar
        (ElementalManager.ElementalType type
        = ElementalManager.ElementalType.Count)
    {
        Color result;

        switch (type)
        {
            case ElementalManager.ElementalType.Fire:
                result = new Color(1f, 0.6470588f, 0f, 1f);
                break;
            case ElementalManager.ElementalType.Ice:
                result = new Color(0f, 0.489625f, 1f, 1f);
                break;
            case ElementalManager.ElementalType.Light:
                result = new Color(1f, 1f, 0f, 1f);
                break;
            case ElementalManager.ElementalType.Poison:
                result = Color.green;
                break;
            default:
                result = Color.white;
                break;
        }

        return result;
    }

    private void Init()
    {
        _HPBar.fillAmount = _StatCtrl.HPCurrent / _StatCtrl.HP;
    }

    public void ActivateHPBar(bool command)
        => _HPBar.transform.parent.gameObject.SetActive(command);

    public void ActivateELIcon
        (ElementalManager.ElementalType type
        , bool command)
    {
        if (type == ElementalManager.ElementalType.Count)
            return;

        GameObject icon = _IconBox.Find(ELIconName(type)).gameObject;
        icon.SetActive(command);
    }

    private string ELIconName(ElementalManager.ElementalType type)
        => $"Icon_{type}";
}
