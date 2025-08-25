using UnityEngine;

/// <summary>
/// 무기 루트(오른손/왼손)에서 각 무기 컨트롤러를 찾아 제공한다.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform rWeaponRoot; // 오른손 무기 루트
    [SerializeField] private Transform lWeaponRoot; // 왼손 무기 루트

    #region GET
    /// <summary>
    /// 왼손 무기 루트에서 스펠 컨트롤러를 가져온다.
    /// </summary>
    public SpellController GetSpellController()
        => lWeaponRoot.GetComponentInChildren<SpellController>();

    /// <summary>
    /// 오른손 무기 루트에서 근접 무기 컨트롤러를 가져온다.
    /// </summary>
    public MeleeController GetMeleeController()
        => rWeaponRoot.GetComponentInChildren<MeleeController>();
    #endregion
}
