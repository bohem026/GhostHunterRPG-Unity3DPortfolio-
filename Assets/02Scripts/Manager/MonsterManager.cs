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

    // 공격 스케줄: 몬스터 타입별로 동시에 대기 가능한 슬롯 관리
    private Dictionary<MonType, HashSet<MonsterController>> attackSchedules;
    private Dictionary<MonType, int> scheduleCapacities;

    /// <summary>
    /// 싱글톤 등록.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 타입별 공격 스케줄 컨테이너와 수용량 초기화.
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
    /// 남은 몬스터 유무를 주기적으로 갱신.
    /// </summary>
    private void Update()
    {
        MONS_LEFT = InstMonsTotal() != 0;
    }

    /// <summary>
    /// 스케줄이 가득 차지 않았고, 아직 등록되지 않았다면 true.
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
    /// 공격 루틴 대기열에 몬스터를 추가.
    /// </summary>
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

    /// <summary>
    /// 공격 루틴 대기열에서 몬스터 제거(없으면 무시).
    /// </summary>
    public void RmvFromAtkRoutine(MonsterController mon)
    {
        var type = mon.GetMonType();
        attackSchedules[type].Remove(mon);
    }

    /// <summary>
    /// 활성화된 몬스터 목록을 새로 수집하고 총수를 반환.
    /// </summary>
    public int InstMonsTotal()
    {
        monsTotal = FindObjectsByType<MonsterController>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
        return monsTotal.Length;
    }

    /// <summary>
    /// 조준 대상(mon.isTarget=true)만 필터링하여 개수를 반환.
    /// </summary>
    public int InstMonsAimed()
    {
        monsAimed.Clear(); // ※ 외부에서 리스트가 할당되어 있다고 가정

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
    /// 입력 컬렉션을 섞은 뒤 최대 maxCnt개를 반환(개수가 적으면 전체 반환).
    /// 비어있으면 null.
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
