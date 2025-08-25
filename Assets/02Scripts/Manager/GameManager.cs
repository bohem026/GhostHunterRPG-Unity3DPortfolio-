using UnityEngine;

public class GameManager : MonoBehaviour
{
    // --- Singleton ---
    public static GameManager Inst;

    // --- References ---
    public PlayerController _plyCtrl;

    /// <summary>
    /// 초기 환경 설정 및 입력 모드 기본값을 적용한다.
    /// </summary>
    private void Awake()
    {
        // 플레이어 컨트롤러 존재 확인을 위한 검색 호출
        // (필드가 비어있을 때 씬에 컴포넌트가 있는지 확인용; 필드 값 자체는 유지됨)
        if (!_plyCtrl) GameObject.FindObjectOfType<PlayerController>();

        // 싱글톤 등록
        if (!Inst) Inst = this;

        // 성능 설정: 60 FPS 타깃, vSync 비활성
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        // 마우스 입력 모드 기본값(잠금/숨김)
        ChangeMouseInputMode(0);
    }

    /// <summary>
    /// 마우스 입력 모드 변경.
    /// 0 = 잠금/숨김, 1 = 창 내부로 커서 제한/표시
    /// </summary>
    /// <param name="mode">0 또는 1</param>
    public void ChangeMouseInputMode(int mode)
    {
        switch (mode)
        {
            case 0:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case 1:
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                break;
            default:
                break;
        }
    }
}
