using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Base Stat Asset")]
public class BaseStatSO : ScriptableObject
{
    /// <summary>���� ���� ��ü(�÷��̾�/��Ÿ)�� ���� ���� ����.</summary>
    public enum OWNER { Player, Other }

    [Header("STAT: ACTOR")]
    [SerializeField] private OWNER owner;                         // ���� ���� ��ü
    [SerializeField] private MonsterController.MonType monType;   // ���� Ÿ��

    [Header("STAT: BASE")]
    [SerializeField] private float baseHP;      // ü��
    [SerializeField] private float baseMP;      // ����(����: ���� �ֱ�)
    [SerializeField] private float baseMPITV;   // ���� ȸ����(����: �̻��)
    [SerializeField] private float baseMATK;    // ���� ���ݷ�
    [SerializeField] private float baseSATK;    // ���� ���ݷ�
    [SerializeField] private float baseDEF;     // ����
    [SerializeField] private float baseCTKR;    // ġ���
    [SerializeField] private float baseEVDR;    // ȸ�� ��ġ
    [SerializeField] private float baseSTBR;    // ���� ��ġ(����: �̻��)

    #region GET
    public OWNER Owner => owner;
    public MonsterController.MonType MonType => monType;
    public float BaseHP => baseHP;
    public float BaseMP => baseMP;
    public float BaseMPITV => baseMPITV;
    public float BaseMATK => baseMATK;
    public float BaseSATK => baseSATK;
    public float BaseDEF => baseDEF;
    public float BaseCTKR => baseCTKR;
    public float BaseEVDR => baseEVDR;
    public float BaseSTBR => baseSTBR;
    #endregion
}
