using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Skill Asset")]
public class SkillSO : ScriptableObject
{
    /// <summary>
    /// ��ų �ɷ�ġ ���� (�Ϻδ� Ư�� ���� ������ ����)
    /// </summary>
    public enum AbilityType
    {
        DMG,   // �⺻ �����
        ITV,   // ����(��: Light)
        RNG,   // ����(��: Fire)
        DUR,   // ���ӽð�(��: Ice)
        CNT,   // Ÿ��/����(��: Poison)
        Count
    }

    public enum ValueType
    {
        Num,   // ����/��ġ
        Sec,   // �� ����
        Rate,  // ����(0~1)
        Count
    }

    [Header("ID")]
    [SerializeField] private SkillWindowController.SKType Outer; // ��ų �з�(�нú�/���潺/��Ƽ��)
    [SerializeField] private SkillWindowController.ELType Inner; // ���� Ÿ��
    [SerializeField] private int Id;                             // ���� ID

    [Header("UI")]
    [SerializeField] private Sprite Icon;   // ������
    [SerializeField] private string Name;   // ��ų��
    [SerializeField] private string Intro;  // �Ұ�/����

    [Header("VALUE")]
    public SkillAbility MainAbility;  // �� �ɷ�ġ
    [Space(10)]
    public SkillAbility SubAbility;   // ���� �ɷ�ġ
    [Space(10)]
    [SerializeField] private int MaxLv; // �ִ� ����
    [SerializeField] private int Cost;  // ����/��ȭ ���

    #region GET
    public SkillWindowController.SKType OUTER => Outer;
    public SkillWindowController.ELType INNER => Inner;
    public int ID => Id;
    public Sprite ICON => Icon;
    public string NAME => Name;
    public string INTRO => Intro;
    public int MAXLV => MaxLv;
    public int COST => Cost;
    #endregion

    [Serializable]
    public class SkillAbility
    {
        public string NAME;        // �ɷ�ġ ǥ���
        public AbilityType TYPE;   // �ɷ� Ÿ��
        public ValueType FORMAT;   // �� ǥ�� ����
        public float BASE;         // �⺻��
        public float PER;          // ������ ������
    }
}
