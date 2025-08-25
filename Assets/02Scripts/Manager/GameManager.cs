using UnityEngine;

public class GameManager : MonoBehaviour
{
    // --- Singleton ---
    public static GameManager Inst;

    // --- References ---
    public PlayerController _plyCtrl;

    /// <summary>
    /// �ʱ� ȯ�� ���� �� �Է� ��� �⺻���� �����Ѵ�.
    /// </summary>
    private void Awake()
    {
        // �÷��̾� ��Ʈ�ѷ� ���� Ȯ���� ���� �˻� ȣ��
        // (�ʵ尡 ������� �� ���� ������Ʈ�� �ִ��� Ȯ�ο�; �ʵ� �� ��ü�� ������)
        if (!_plyCtrl) GameObject.FindObjectOfType<PlayerController>();

        // �̱��� ���
        if (!Inst) Inst = this;

        // ���� ����: 60 FPS Ÿ��, vSync ��Ȱ��
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        // ���콺 �Է� ��� �⺻��(���/����)
        ChangeMouseInputMode(0);
    }

    /// <summary>
    /// ���콺 �Է� ��� ����.
    /// 0 = ���/����, 1 = â ���η� Ŀ�� ����/ǥ��
    /// </summary>
    /// <param name="mode">0 �Ǵ� 1</param>
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
