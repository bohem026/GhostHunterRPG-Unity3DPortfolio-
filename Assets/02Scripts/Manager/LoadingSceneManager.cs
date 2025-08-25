using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    // --- Constants / Static state ---
    private const string NAME_LOADING_SCENE = "LoadingScene";
    public static string mainSceneName;
    public static string addSceneName;

    // 로딩 팁 메시지 목록
    private static readonly string[] tips =
    {
        "공격 스킬은 E 키로 사용할 수 있어요.",
        "방어 스킬은 Q 키로 사용할 수 있어요.",
        "제한 시간 안에 유령을 목표치만큼 퇴치하세요.",
        "몬스터마다 약한 속성이 존재해요.",
        "상성을 갖는 속성을 준비하는 것이 효과적이에요!",
        "로비에서 스킬을 연구하고 장착할 수 있어요.",
        "로비에서 스탯을 강화할 수 있어요.",
        "전투에서 얻은 장비는 로비에서 장착할 수 있어요."
    };

    // --- UI References ---
    [Header("BODY")]
    public Text text_Tip;

    [Header("FOOT")]
    public Slider slider_Progress;
    public Text text_Progress;

    /// <summary>
    /// 로딩 코루틴 시작 및 팁 텍스트 초기화.
    /// </summary>
    private void Start()
    {
        StartCoroutine(LoadSceneCoroutine());
        InitTipText();
    }

    /// <summary>
    /// 팁 텍스트를 무작위로 선택해 표시한다.
    /// </summary>
    private void InitTipText()
    {
        text_Tip.text = tips[Random.Range(0, tips.Length)];
    }

    /// <summary>
    /// 메인 씬을 Additive로 로드하여 활성화하고(진행도 표시),
    /// 필요 시 추가 씬도 로드한 뒤 로딩 씬을 언로드한다.
    /// </summary>
    private IEnumerator LoadSceneCoroutine()
    {
        yield return null; // 첫 프레임 대기(안정적 로딩 시작을 위해)

        // 1단계: 메인 씬 로드(예: Level01Scene)
        AsyncOperation mainSceneOp =
            SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        mainSceneOp.allowSceneActivation = false;

        // 진행도 표시(엔진 특성상 0.9 이전까지 progress가 0~0.9 구간)
        while (mainSceneOp.progress < 0.75f)
        {
            slider_Progress.value = mainSceneOp.progress;
            text_Progress.text = (mainSceneOp.progress * 100).ToString("F2") + "%";
            yield return null;
        }

        // 연출상 진행도 100% 고정 표시
        slider_Progress.value = 1f;
        text_Progress.text = "100.00%";

        // 약간 대기 후 활성화
        yield return new WaitForSeconds(1.5f);
        mainSceneOp.allowSceneActivation = true;

        // 메인 씬이 완전히 활성화될 때까지 대기
        while (!mainSceneOp.isDone)
        {
            yield return null;
        }

        // 2단계: 부속(additive) 씬 로드(UI 등)
        if (addSceneName != null)
        {
            AsyncOperation baseSceneOp =
                SceneManager.LoadSceneAsync(addSceneName, LoadSceneMode.Additive);
            while (!baseSceneOp.isDone)
            {
                yield return null;
            }
        }

        // 3단계: 로딩 씬 언로드
        SceneManager.UnloadSceneAsync(NAME_LOADING_SCENE);
    }

    /// <summary>
    /// 로딩 씬을 통해 지정한 씬(과 선택적 추가 씬)을 로드한다.
    /// </summary>
    public static void LoadSceneWithLoading(string mainScene, string addScene = null)
    {
        mainSceneName = mainScene;
        addSceneName = addScene;
        SceneManager.LoadScene(NAME_LOADING_SCENE);
    }
}
