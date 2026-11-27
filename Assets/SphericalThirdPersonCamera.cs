using UnityEngine;

namespace UnityChan
{
    /// <summary>
    /// 球面やカスタム重力環境に対応した三人称追従カメラ
    /// </summary>
    public class SphericalThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("追従対象のプレイヤー")]
        [SerializeField] private Transform target;

        [Tooltip("重力中心（未設定の場合は target の PlayerController や原点から取得）")]
        [SerializeField] private Transform gravityCenter;

        [Header("Offset Settings")]
        [Tooltip("キャラクターからの距離（後方への距離）")]
        [SerializeField] private float distance = 3.5f;

        [Tooltip("キャラクターの頭上方向（Up）への高さオフセット")]
        [SerializeField] private float height = 1.8f;

        [Tooltip("注視点の高さオフセット（キャラクターの頭や胸の高さ）")]
        [SerializeField] private float lookAtHeight = 1.2f;

        [Header("Smooth Settings")]
        [Tooltip("位置追従のスムーズ時間（小さいほど素早く追従、提示コードの1.5f等）")]
        [SerializeField] private float positionSmoothTime = 0.25f;

        [Tooltip("回転の補間速度")]
        [SerializeField] private float rotationSpeed = 5.0f;

        private Vector3 cameraVelocity = Vector3.zero;
        private PlayerController playerController;

        private void Awake()
        {
            if (target == null)
            {
                // PlayerController を探して対象に設定
                playerController = Object.FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    target = playerController.transform;
                }
            }
            else
            {
                playerController = target.GetComponent<PlayerController>();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. 上方向 (Up) を取得（重力中心と逆方向）
            Vector3 center = GetGravityCenter();
            Vector3 up = (target.position - center);
            if (up.sqrMagnitude < 0.001f)
            {
                up = target.up;
            }
            else
            {
                up.Normalize();
            }

            // 2. プレイヤーの後方ベクトル（重力平面上に投影）
            Vector3 forward = Vector3.ProjectOnPlane(target.forward, up).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = target.forward;
            }
            Vector3 back = -forward;

            // 3. カメラの理想の目標位置（後方 + 上方）
            Vector3 targetPosition = target.position + (back * distance) + (up * height);

            // 4. SmoothDamp による位置のスムーズ追従
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref cameraVelocity,
                positionSmoothTime
            );

            // 5. 注視点（キャラクターの少し上）
            Vector3 lookAtTarget = target.position + (up * lookAtHeight);
            Vector3 lookDirection = lookAtTarget - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                // 重力の上方向を維持して注視
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, up);

                // Slerp による回転補間
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }

        private Vector3 GetGravityCenter()
        {
            if (gravityCenter != null) return gravityCenter.position;
            if (playerController != null) return playerController.CenterPosition;
            return Vector3.zero;
        }

        /// <summary>
        /// カメラ位置・回転を即座に目標値に初期化する（ワープ時や開始時用）
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 center = GetGravityCenter();
            Vector3 up = (target.position - center).normalized;
            if (up.sqrMagnitude < 0.001f) up = target.up;

            Vector3 forward = Vector3.ProjectOnPlane(target.forward, up).normalized;
            Vector3 targetPosition = target.position - (forward * distance) + (up * height);
            transform.position = targetPosition;

            Vector3 lookAtTarget = target.position + (up * lookAtHeight);
            transform.rotation = Quaternion.LookRotation(lookAtTarget - transform.position, up);
            cameraVelocity = Vector3.zero;
        }
    }
}
