using UnityEngine;

/// <summary>
/// �� ������ �Ӽ�/����� ����/����Ʈ ������ �� ȸ������ �����ϴ� ScriptableObject.
/// ��Ÿ�ӿ��� ȿ�� ����� ����� ��� �� �����˴ϴ�.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Object/Sword Asset")]
public class SwordSO : ScriptableObject
{
    [Header("Elemental (���Ӽ��� 'Count')")]
    [SerializeField] private ElementalManager.ElementalType elType;

    [Space(20)]
    [Header("Damage Rate")]
    [SerializeField] private float minDamageRate;      // �ּ� ����� ����(���� Ÿ�Ժ� ����ġ�� ���)

    [Space(20)]
    [Header("Effect")]
    [SerializeField] private GameObject slashEffect;   // ���� ����Ʈ ������
    [SerializeField] private float effectRotX_NAtk;    // �Ϲ� ���� ����Ʈ X�� ȸ�� ����
    [SerializeField] private float effectRotX_SAtk;    // ������ ����Ʈ X�� ȸ�� ����

    #region GET
    public ElementalManager.ElementalType ElType => elType;
    public float MinDamageRate => minDamageRate;
    public GameObject SlashEffect => slashEffect;
    public float EffectRotXNAtk => effectRotX_NAtk;
    public float EffectRotXSAtk => effectRotX_SAtk;
    #endregion
}
