using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform player;

    [Header("TopDown")]
    public float topDownHeight = 30f;

    [Header("Quarter View (수평 오빗)")]
    public float quarterDistance = 18f;
    public float quarterHeight = 12f;
    public float quarterPitch = 35f; // 고정 피치(상하 안 움직임)
    private float quarterYaw = 0f;

    [Header("First Person")]
    public Vector3 fpHeadOffset = new Vector3(0, 1.65f, 0);
    public float fpPitchMin = -85f;
    public float fpPitchMax = 85f;
    private float fpYaw = 0f;
    private float fpPitch = 0f;

    [Header("공통")]
    public float mouseSensitivity = 0.12f;  // 감도
    public float smoothPos = 10f;           // 위치 보간
    public float smoothRot = 12f;           // 회전 보간

    private enum Mode { TopDown, Quarter, FirstPerson }
    private Mode mode = Mode.TopDown;

    void OnEnable()
    {
        // 시작 각도 초기화
        if (player) fpYaw = quarterYaw = player.eulerAngles.y;
        SetCursor(mode);
    }

    void Update()
    {
        // 모드 전환 (임시 키)
        var kb = Keyboard.current;
        if (kb.digit1Key.wasPressedThisFrame) SwitchMode(Mode.TopDown);
        if (kb.digit2Key.wasPressedThisFrame) SwitchMode(Mode.Quarter);
        if (kb.digit3Key.wasPressedThisFrame) SwitchMode(Mode.FirstPerson);

        if (!player) return;

        var mouse = Mouse.current;
        Vector3 targetPos = transform.position;
        Quaternion targetRot = transform.rotation;

        switch (mode)
        {
            case Mode.TopDown:
            {
                // 위에서 정면으로 내려다봄
                targetPos = player.position + Vector3.up * topDownHeight;
                targetRot = Quaternion.Euler(90f, 0f, 0f);
                break;
            }

            case Mode.Quarter:
            {
                // 마우스 좌우만 반영: 수평 Yaw만 축적, Pitch는 고정
                if (mouse != null)
                    quarterYaw += mouse.delta.ReadValue().x * mouseSensitivity;

                // Yaw 기반으로 플레이어를 중심으로 원 궤도 배치
                Vector3 dir = Quaternion.Euler(0f, quarterYaw, 0f) * Vector3.back;
                Vector3 desired = player.position + dir * quarterDistance + Vector3.up * quarterHeight;

                targetPos = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothPos);
                targetRot = Quaternion.Slerp(transform.rotation,
                                             Quaternion.Euler(quarterPitch, quarterYaw, 0f),
                                             Time.deltaTime * smoothRot);
                break;
            }

            case Mode.FirstPerson:
            {
                // 완전 마우스룩: Yaw+Pitch
                if (mouse != null)
                {
                    Vector2 m = mouse.delta.ReadValue();
                    fpYaw   += m.x * mouseSensitivity;
                    fpPitch -= m.y * mouseSensitivity;
                    fpPitch = Mathf.Clamp(fpPitch, fpPitchMin, fpPitchMax);
                }

                // 플레이어의 수평 회전은 Yaw에 맞춤(원하면 끄기 가능)
                player.rotation = Quaternion.Euler(0f, fpYaw, 0f);

                Vector3 desired = player.position + fpHeadOffset;
                targetPos = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothPos);
                targetRot = Quaternion.Slerp(transform.rotation,
                                             Quaternion.Euler(fpPitch, fpYaw, 0f),
                                             Time.deltaTime * smoothRot);
                break;
            }
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    private void SwitchMode(Mode m)
    {
        mode = m;
        // 모드 진입 시 기준 각도 동기화(전환 순간 튐 방지)
        if (player)
        {
            if (mode == Mode.Quarter) quarterYaw = player.eulerAngles.y;
            if (mode == Mode.FirstPerson)
            {
                fpYaw = player.eulerAngles.y;
                // 현재 카메라 피치 추정
                Vector3 e = transform.rotation.eulerAngles;
                fpPitch = e.x > 180 ? e.x - 360f : e.x;
                fpPitch = Mathf.Clamp(fpPitch, fpPitchMin, fpPitchMax);
            }
        }
        SetCursor(mode);
    }

    private void SetCursor(Mode m)
    {
        bool lockIt = (m != Mode.TopDown); // 쿼터/1인칭에서 커서 잠금
        Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockIt;
    }
}
