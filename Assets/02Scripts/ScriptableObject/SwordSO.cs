using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Sword Asset")]
public class SwordSO : ScriptableObject
{
    [Header("Elemental_ Non-elemental type: 'Count'")]
    [SerializeField] private ElementalManager.ElementalType elType;

    [Space(20)]
    [Header("Damage Rate")]
    [SerializeField] private float minDamageRate;

    [Space(20)]
    [Header("Effect")]
    [SerializeField] private GameObject slashEffect;
    [SerializeField] private float effectRotX_NAtk;
    [SerializeField] private float effectRotX_SAtk;

    #region GET
    public ElementalManager.ElementalType ElType => elType;
    public GameObject SlashEffect => slashEffect;
    public float MinDamageRate => minDamageRate;
    public float EffectRotXNAtk => effectRotX_NAtk;
    public float EffectRotXSAtk => effectRotX_SAtk;
    #endregion
}
