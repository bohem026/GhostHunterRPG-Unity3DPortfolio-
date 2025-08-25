using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPoolManager : MonoBehaviour
{
    public static MonsterPoolManager Inst;

    [Space(20)]
    [SerializeField] private Transform box;

    // 프리팹 정의(스테이지 데이터로부터)
    private List<MonsterEntry> monEntries;
    // 타입별 인스턴스 풀
    private List<GameObject>[] monPools;
    private StageSO asset;

    private bool isInitialized = false;

    /// <summary>
    /// 싱글톤 설정.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 초기화 코루틴 시작.
    /// </summary>
    private void Start()
    {
        if (!isInitialized) StartCoroutine(Init());
    }

    /// <summary>
    /// StageManager 준비 대기 → 스테이지 에셋/엔트리/풀 초기화 →
    /// 타입별 하위 박스(부모 Transform) 생성.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => StageManager.Inst);

        // 1) 스테이지 에셋/엔트리 로드
        asset = StageManager.Inst.Asset;
        monEntries = asset.entries;

        // 2) 타입별 풀 생성 및 하위 박스 준비
        monPools = new List<GameObject>[monEntries.Count];
        for (int i = 0; i < monPools.Length; i++)
        {
            monPools[i] = new List<GameObject>();

            GameObject childBox = new GameObject($"Box {i}");
            childBox.transform.SetParent(box);
            childBox.transform.localPosition = Vector3.zero;
        }

        isInitialized = true;
    }

    /// <summary>
    /// 지정 인덱스의 몬스터 인스턴스를 가져온다.
    /// 비활성 풀에서 재사용하고, 없으면 새로 생성한다.
    /// </summary>
    public GameObject Get(int index)
    {
        // 인덱스 범위 체크
        if (index < 0 || index >= monEntries.Count)
            return null;

        GameObject selected = null;

        // 1) 비활성 오브젝트 재사용
        foreach (GameObject item in monPools[index])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 2) 없으면 새로 생성 후 풀에 추가
        if (!selected)
        {
            selected = Instantiate(monEntries[index].prefab, box.GetChild(index));
            monPools[index].Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// 스테이지의 한 웨이브를 구성하는 "원자 단위" 소환을 수행한다.
    /// (웨이브를 여러 번에 나눠 소환하는 단계 중 한 번)
    /// </summary>
    public void SummonWaveAtom()
    {
        Debug.Log("[DEBUG] SUMMON WAVE ATOM");

        int currentWave = StageManager.Inst.CurrentWave;                 // 1-based
        int currentWaveAtom = (StageManager.Inst.CurrentWaveAtom += 1);  // 진행 중인 원자 단계 증가
        SphereTriggerZone currentSphereZone = StageManager.Inst.CurrentSphereZone;

        int currentWaveIndex = Mathf.Clamp(currentWave - 1, 0, StageManager.MAX_WAVE - 1);

        // 타입별 소환 수
        int[] summonCounts = new int[monEntries.Count];

        // --- 웨이브 원자 단계별 소환 수 계산 ---
        // 1) 최대 원자 단계 초과 시: 다음 웨이브로 진행
        if (currentWaveAtom > StageManager.MAX_WAVE_ATOM)
        {
            StageManager.Inst.CurrentWaveAtom = 0;

            if (!StageManager.Inst.IsWaveInProgress)
                StartCoroutine(StageManager.Inst.StartWave());

            return;
        }
        // 2) 마지막 원자 단계: 남은 수를 모두 소환
        else if (currentWaveAtom == StageManager.MAX_WAVE_ATOM)
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] =
                    (monEntries[i].countPerWave[currentWaveIndex] / StageManager.MAX_WAVE_ATOM) +
                    (monEntries[i].countPerWave[currentWaveIndex] % StageManager.MAX_WAVE_ATOM);
            }

            StageManager.Inst.IsWaveInProgress = false;
        }
        // 3) 1~(마지막-1) 단계: 웨이브 수의 1/MAX_WAVE_ATOM만큼 소환
        else
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] =
                    monEntries[i].countPerWave[currentWaveIndex] / StageManager.MAX_WAVE_ATOM;
            }
        }
        // ---

        // 소환 위치 선택
        int spCount = currentSphereZone.SpawnPoints.Count;
        Transform spawnPoint = currentSphereZone.SpawnPoints[Random.Range(0, spCount)];

        // --- 몬스터 소환 ---
        for (int outer = 0; outer < monEntries.Count; outer++)
        {
            for (int inner = 0; inner < summonCounts[outer]; inner++)
            {
                int capturedOuter = outer;
                GameObject go = Instantiate(monEntries[capturedOuter].prefab, box);
                go.transform.position = spawnPoint.position;
            }
        }
        // ---
    }
}
