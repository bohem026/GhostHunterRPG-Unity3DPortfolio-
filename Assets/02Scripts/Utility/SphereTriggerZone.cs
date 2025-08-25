using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정 구역에 플레이어가 진입하면 구형 트랩을 활성화하고
/// 해당 구역을 현재 웨이브의 기준 구역으로 등록합니다.
/// </summary>
public class SphereTriggerZone : MonoBehaviour
{
    [Header("TRAP ZONE")]
    [SerializeField] private GameObject sphereTrapZone;

    [Header("SPAWN POINT")]
    [SerializeField] private List<Transform> spawnPoints;

    private bool isTrapZoneTriggered = false;

    /// <summary>
    /// 플레이어가 트리거에 진입하면 한 번만 트랩을 활성화하고
    /// 스테이지 매니저에 현재 구역을 등록한 뒤 웨이브를 시작합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isTrapZoneTriggered) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isTrapZoneTriggered = true;
            sphereTrapZone.SetActive(true);

            StageManager.Inst.UpdateCurrentSphereZone(this);
            StartCoroutine(StageManager.Inst.StartWave());
        }
    }

    // GET
    public SphereTrapZone SphereTrapZone => sphereTrapZone.GetComponent<SphereTrapZone>();
    public List<Transform> SpawnPoints => spawnPoints;
}
