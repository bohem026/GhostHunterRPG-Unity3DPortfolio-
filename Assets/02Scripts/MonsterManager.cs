using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static AnimController;
using static MonsterController;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Inst;
    public const int ATK_SCHEDULE_LIMIT = 2;

    //[SerializeField] private GameObject[] MonPrototypes;

    [Space(20)]
    [SerializeField] private GameObject monSpellCanvas;
    [SerializeField] private GameObject MonSpellPrototype;

    [HideInInspector] public MonsterController[] monsTotal;
    [HideInInspector] public List<MonsterController> monsAimed;
    [HideInInspector] public bool MONS_LEFT;

    // ���� �������� ���� ��Ÿ�� ��Ÿ ����<- ���� ���͸��� �ٸ��� �ð� �迭 �ʿ�
    // Ư�� �ð��� �����ϸ� �ش� ���� ����
    // ������ ��ųʸ��� delta ����(ATK_WARNING_SEC)
    // delta �ִ�� ������ ���� ��Ƴ����� ��ų ���
    private Dictionary<MonType, HashSet<MonsterController>> attackSchedules;
    private Dictionary<MonType, int> scheduleCapacities;

    void Awake()
    {
        if (!Inst) Inst = this;
    }

    // Start is called before the first frame update

    void Start()
    {
        attackSchedules = new Dictionary<MonType, HashSet<MonsterController>>();
        scheduleCapacities = new Dictionary<MonType, int>();

        foreach (MonType type in Enum.GetValues(typeof(MonType)))
        {
            if (type == MonType.Count) continue;

            attackSchedules[type] = new HashSet<MonsterController>();
            if (type == MonType.Buff)
                scheduleCapacities[type] = 1;
            else
                scheduleCapacities[type] = ATK_SCHEDULE_LIMIT;
        }
    }

    // True if schedule is not full and mon is not already in it.
    public bool CheckIsAtkAble(MonsterController mon)
    {
        var type = mon.GetMonType();
        var schedule = attackSchedules[type];

        if (schedule.Count >= scheduleCapacities[type])
            return false;

        return !schedule.Contains(mon);
    }

    public void AddToAtkRoutine(MonsterController mon)
    {
        var type = mon.GetMonType();
        var schedule = attackSchedules[type];

        if (schedule.Count < scheduleCapacities[type] && !schedule.Contains(mon))
        {
            Debug.Log($"[{mon.name}] ���� �غ�!!!");
            schedule.Add(mon);
        }
    }

    public void RmvFromAtkRoutine(MonsterController mon)
    {
        var type = mon.GetMonType();
        attackSchedules[type].Remove(mon); // ������ ���õ�
    }

    // Update is called once per frame
    void Update()
    {
        MONS_LEFT = InstMonsTotal() != 0;
    }

    public int InstMonsTotal()
    {
        monsTotal = null;
        monsTotal = FindObjectsByType<MonsterController>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);

        return monsTotal.Length;
    }

    public int InstMonsAimed()
    {
        monsAimed.Clear();

        if (!MONS_LEFT)
            return 0;

        foreach (MonsterController mon in monsTotal)
        {
            if (!mon.gameObject.activeSelf) continue;
            if (mon.isTarget) monsAimed.Add(mon);
        }

        return monsAimed.Count;
    }

    public IEnumerable<T> ShuffleAndGetSome<T>(IEnumerable<T> input, int maxCnt)
    {
        int count = input.Count();

        // �迭�� null�̰ų� ��� �ִ� ��� ó��
        if (input == null || count == 0)
            return null;

        // �迭 ���̰� limitCnt���� ���� ��� ��ü ��ȯ
        if (count <= maxCnt)
            return input;

        // �迭�� ���� �տ��� limitCnt�� ����
        return input.OrderBy(x => UnityEngine.Random.value).Take(maxCnt);
    }
}
