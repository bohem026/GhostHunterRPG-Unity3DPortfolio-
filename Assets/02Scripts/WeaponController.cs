using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform rWeaponRoot;
    [SerializeField] private Transform lWeaponRoot;

    #region GET
    public SpellController GetSpellController()
        => lWeaponRoot.GetComponentInChildren<SpellController>();
    public MeleeController GetMeleeController()
        => rWeaponRoot.GetComponentInChildren<MeleeController>();
    #endregion
}
