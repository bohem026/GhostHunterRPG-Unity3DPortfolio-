using System.Collections;
using UnityEngine;

/// <summary>
/// UI 플래시 연출 시작 시 효과음을 재생하고,
/// 애니메이션 종료 시 자신을 비활성화하는 핸들러.
/// </summary>
public class UIFlashHandler : MonoBehaviour
{
    [SerializeField] private AudioClip sfxClip;

    /// <summary>
    /// 오브젝트 활성화 시, 효과음이 지정되어 있으면 재생 코루틴을 시작합니다.
    /// </summary>
    private void OnEnable()
    {
        if (sfxClip == null) return;
        StartCoroutine(PlaySFX());
    }

    /// <summary>
    /// 오디오 매니저가 준비될 때까지 대기한 뒤 UI용 SFX를 1회 재생합니다.
    /// </summary>
    private IEnumerator PlaySFX()
    {
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.UI, sfxClip);
    }

    /// <summary>
    /// 플래시 애니메이션 종료 시 호출되어, 오브젝트를 비활성화합니다.
    /// </summary>
    public void OnFlashAnimFinished()
    {
        gameObject.SetActive(false);
    }
}
