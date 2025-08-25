using UnityEngine;

/// <summary>
/// ���� ��Ʈ(������/�޼�)���� �� ���� ��Ʈ�ѷ��� ã�� �����Ѵ�.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform rWeaponRoot; // ������ ���� ��Ʈ
    [SerializeField] private Transform lWeaponRoot; // �޼� ���� ��Ʈ

    #region GET
    /// <summary>
    /// �޼� ���� ��Ʈ���� ���� ��Ʈ�ѷ��� �����´�.
    /// </summary>
    public SpellController GetSpellController()
        => lWeaponRoot.GetComponentInChildren<SpellController>();

    /// <summary>
    /// ������ ���� ��Ʈ���� ���� ���� ��Ʈ�ѷ��� �����´�.
    /// </summary>
    public MeleeController GetMeleeController()
        => rWeaponRoot.GetComponentInChildren<MeleeController>();
    #endregion
}
