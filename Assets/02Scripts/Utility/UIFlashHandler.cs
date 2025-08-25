using System.Collections;
using UnityEngine;

/// <summary>
/// UI �÷��� ���� ���� �� ȿ������ ����ϰ�,
/// �ִϸ��̼� ���� �� �ڽ��� ��Ȱ��ȭ�ϴ� �ڵ鷯.
/// </summary>
public class UIFlashHandler : MonoBehaviour
{
    [SerializeField] private AudioClip sfxClip;

    /// <summary>
    /// ������Ʈ Ȱ��ȭ ��, ȿ������ �����Ǿ� ������ ��� �ڷ�ƾ�� �����մϴ�.
    /// </summary>
    private void OnEnable()
    {
        if (sfxClip == null) return;
        StartCoroutine(PlaySFX());
    }

    /// <summary>
    /// ����� �Ŵ����� �غ�� ������ ����� �� UI�� SFX�� 1ȸ ����մϴ�.
    /// </summary>
    private IEnumerator PlaySFX()
    {
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.UI, sfxClip);
    }

    /// <summary>
    /// �÷��� �ִϸ��̼� ���� �� ȣ��Ǿ�, ������Ʈ�� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void OnFlashAnimFinished()
    {
        gameObject.SetActive(false);
    }
}
