using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPoolManager : MonoBehaviour
{
    public static MonsterPoolManager Inst;

    [Space(20)]
    [SerializeField] private Transform box;

    //List carries prefabs.
    List<MonsterEntry> monEntries;
    //List carries instantiated game objects.
    List<GameObject>[] monPools;
    StageSO asset;

    bool isInitialized = false;
    //bool doSummonWaveAtom = false;

    void Awake()
    {
        if (!Inst) Inst = this;
    }

    void Start()
    {
        if (!isInitialized) StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => StageManager.Inst);

        //1. Initialize asset.
        asset = StageManager.Inst.Asset;
        //2. Initialize variables.
        monEntries = asset.entries;
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

    public GameObject Get(int index)
    {
        //Return if index is out of range.
        if (index < 0 || index >= monEntries.Count)
            return null;

        GameObject selected = null;

        //1. Get already instantiated object inactive.
        foreach (GameObject item in monPools[index])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        //2. Instantiate more if there's no inactive one.
        if (!selected)
        {
            selected = Instantiate(monEntries[index].prefab, box.GetChild(index));
            monPools[index].Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// 스테이지 웨이브 당 1 부대의 몬스터들을 소환하는 메서드 입니다.
    /// </summary>
    public void SummonWaveAtom()
    {
        Debug.Log("[DEBUG] SUMMON WAVE ATOM");

        int currentWave = StageManager.Inst.CurrentWave;//1
        int currentWaveAtom = (StageManager.Inst.CurrentWaveAtom += 1);//1
        SphereTriggerZone currentSphereZone
            = StageManager.Inst.CurrentSphereZone;

        int currentWaveIndex = Mathf.Clamp
                            (currentWave - 1,
                            0,
                            StageManager.MAX_WAVE - 1);
        //Monsters' summon counts per 1 wave.
        int[] summonCounts = new int[monEntries.Count];

        //--- Calculate number of monsters summoned per 1 wave.
        //1. Go to next wave if there's no more monsters to summon.
        if (currentWaveAtom > StageManager.MAX_WAVE_ATOM)
        {
            /*Test*/
            StageManager.Inst.CurrentWaveAtom = 0;
            /*Test*/
            //Move to next wave.
            if (!StageManager.Inst.IsWaveInProgress)
                StartCoroutine(StageManager.Inst.StartWave());

            return;
        }
        //2. Summon rest of monsters if is 3rd(last) wave atom.
        else if (currentWaveAtom == StageManager.MAX_WAVE_ATOM)
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] = (monEntries[i].countPerWave[currentWaveIndex] /
                                StageManager.MAX_WAVE_ATOM) +
                                (monEntries[i].countPerWave[currentWaveIndex] %
                                StageManager.MAX_WAVE_ATOM);
            }

            StageManager.Inst.IsWaveInProgress = false;
        }
        //3. Summon monsters number of 1/3 of counts per wave 
        //   if is 1st and 2nd wave atom.
        else
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] = monEntries[i].countPerWave[currentWaveIndex] /
                                StageManager.MAX_WAVE_ATOM;
            }
        }
        //---

        int spCount = currentSphereZone.SpawnPoints.Count;
        Transform spawnPoint
            = currentSphereZone.SpawnPoints[Random.Range(0, spCount)];
        GameObject go = null;

        //--- Summon monsters.
        for (int outer = 0; outer < monEntries.Count; outer++)
        {
            for (int inner = 0; inner < summonCounts[outer]; inner++)
            {
                int capturedOuter = outer;
                int capturedInner = inner;

                go = Instantiate(monEntries[capturedOuter].prefab, box);
                go.transform.position = spawnPoint.position;
            }
        }
        //---
    }

    //public bool DoSummonWaveAtom { get; set; }
}
