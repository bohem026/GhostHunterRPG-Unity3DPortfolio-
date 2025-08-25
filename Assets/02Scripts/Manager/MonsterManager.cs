using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MonsterController;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Inst;
    public const int ATK_SCHEDULE_LIMIT = 2;

    [Space(20)]
    [SerializeField] private GameObject monSpellCanvas;
    [SerializeField] private GameObject MonSpellPrototype;

    [HideInInspector] public MonsterController[] monsTotal;
    [HideInInspector] public List<MonsterController> monsAimed;
    [HideInInspector] public bool MONS_LEFT;

    // ���� ������: ���� Ÿ�Ժ��� ���ÿ� ��� ������ ���� ����
    private Dictionary<MonType, HashSet<MonsterController>> attackSchedules;
    private Dictionary<MonType, int> scheduleCapacities;

    /// <summary>
    /// �̱��� ���.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// Ÿ�Ժ� ���� ������ �����̳ʿ� ���뷮 �ʱ�ȭ.
    /// </summary>
    private void Start()
    {
        attackSchedules = new Dictionary<MonType, HashSet<MonsterController>>();
        scheduleCapacities = new Dictionary<MonType, int>();

        foreach (MonType type in Enum.GetValues(typeof(MonType)))
        {
            if (type == MonType.Count) continue;

            attackSchedules[type] = new HashSet<MonsterController>();
            scheduleCapacities[type] = (type == MonType.Buff) ? 1 : ATK_SCHEDULE_LIMIT;
        }
    }

    /// <summary>
    /// ���� ���� ������ �ֱ������� ����.
    /// </summary>
    private void Update()
    {
        MONS_LEFT = InstMonsTotal() != 0;
    }

    /// <summary>
    /// �������� ���� ���� �ʾҰ�, ���� ��ϵ��� �ʾҴٸ� true.
    /// </summary>
    public bool CheckIsAtkAble(MonsterController mon)
    {
        var type = mon.GetMonType();
        var schedule = attackSchedules[type];

        if (schedule.Count >= scheduleCapacities[type])
            return false;

        return !schedule.Contains(mon);
    }

    /// <summary>
    /// ���� ��ƾ ��⿭�� ���͸� �߰�.
    /// </summary>
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

    /// <summary>
    /// ���� ��ƾ ��⿭���� ���� ����(������ ����).
    /// </summary>
    public void RmvFromAtkRoutine(MonsterController mon)
    {
        var type = mon.GetMonType();
        attackSchedules[type].Remove(mon);
    }

    /// <summary>
    /// Ȱ��ȭ�� ���� ����� ���� �����ϰ� �Ѽ��� ��ȯ.
    /// </summary>
    public int InstMonsTotal()
    {
        monsTotal = FindObjectsByType<MonsterController>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
        return monsTotal.Length;
    }

    /// <summary>
    /// ���� ���(mon.isTarget=true)�� ���͸��Ͽ� ������ ��ȯ.
    /// </summary>
    public int InstMonsAimed()
    {
        monsAimed.Clear(); // �� �ܺο��� ����Ʈ�� �Ҵ�Ǿ� �ִٰ� ����

        if (!MONS_LEFT)
            return 0;

        foreach (MonsterController mon in monsTotal)
        {
            if (!mon.gameObject.activeSelf) continue;
            if (mon.isTarget) monsAimed.Add(mon);
        }

        return monsAimed.Count;
    }

    /// <summary>
    /// �Է� �÷����� ���� �� �ִ� maxCnt���� ��ȯ(������ ������ ��ü ��ȯ).
    /// ��������� null.
    /// </summary>
    public IEnumerable<T> ShuffleAndGetSome<T>(IEnumerable<T> input, int maxCnt)
    {
        int count = input.Count();

        if (input == null || count == 0)
            return null;

        if (count <= maxCnt)
            return input;

        return input.OrderBy(_ => UnityEngine.Random.value).Take(maxCnt);
    }
}
