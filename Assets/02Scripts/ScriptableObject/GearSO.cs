using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Gear Asset")]
public class GearSO : ScriptableObject
{
    public enum StatType
    {
        HP,
        DEF,
        MATK,
        SATK,
        CTKR,
        EVDR,
        Count /*Length*/
    }

    public enum ValueType
    {
        Num,
        Rate,
        Count /*Length*/
    }

    [Header("ID")]
    [SerializeField] private GearController.Rarity Outer;     // ��͵�
    [SerializeField] private GearController.GearType Inner;   // ��� ����

    [Header("UI")]
    [SerializeField] private Sprite Icon;     // �κ�/�� ������
    [SerializeField] private Button Button;   // UI ��ư ������
    [SerializeField] private string Name;     // ����
    [SerializeField] private string Intro;    // ����

    [Header("VALUE")]
    public GearStat MainStat;  // �� �ɼ�
    [Space(10)]
    public GearStat SubStat;   // �� �ɼ�

    #region GET
    public GearController.Rarity OUTER => Outer;
    public GearController.GearType INNER => Inner;
    public Sprite ICON => Icon;
    public Button BUTTON => Button;
    public string NAME => Name;
    public string INTRO => Intro;
    #endregion

    [Serializable]
    public class GearStat
    {
        public string NAME;          // �ɼǸ�(ǥ���)
        public StatType TYPE;        // ���� ��� ����
        public ValueType FORMAT;     // �� ����(��ġ/����)
        public float BASE;           // �⺻��
    }
}
