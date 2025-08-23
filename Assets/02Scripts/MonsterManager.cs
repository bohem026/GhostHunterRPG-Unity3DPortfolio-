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

    // 게임 시작하자 마자 쿨타임 델타 누적<- 각각 몬스터마다 다르게 시간 배열 필요
    // 특정 시간에 도달하면 해당 몬스터 추출
    // 추출한 딕셔너리의 delta 누적(ATK_WARNING_SEC)
    // delta 최대로 누적될 동안 살아남으면 스킬 사용
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
            Debug.Log($"[{mon.name}] 공격 준비!!!");
            schedule.Add(mon);
        }
    }

    public void RmvFromAtkRoutine(MonsterController mon)
    {
        var type = mon.GetMonType();
        attackSchedules[type].Remove(mon); // 없으면 무시됨
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

        // 배열이 null이거나 비어 있는 경우 처리
        if (input == null || count == 0)
            return null;

        // 배열 길이가 limitCnt보다 작을 경우 전체 반환
        if (count <= maxCnt)
            return input;

        // 배열을 섞고 앞에서 limitCnt개 선택
        return input.OrderBy(x => UnityEngine.Random.value).Take(maxCnt);
    }
}
