using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityChan
{
    /// <summary>
    /// クォータニオンを使用し、極（真上・真下）の特異点や角度制限なく
    /// 球面上を中心を向きながら全方位にぐるぐる周回できるカメラコントローラー
    /// </summary>
    public class SphericalOrbitCamera : MonoBehaviour
    {
        [Header("Center Settings")]
        [Tooltip("周回・注視の中心となるオブジェクト（未設定の場合は原点 (0,0,0) を使用）")]
        [SerializeField] private Transform centerTransform;

        [Header("Orbit Sphere Settings")]
        [Tooltip("中心からの球体半径（カメラの距離）")]
        [SerializeField] private float radius = 15f;

        [Tooltip("マウスドラッグでの回転感度")]
        [SerializeField] private float mouseSensitivity = 3f;

        [Tooltip("矢印キー等での周回移動速度（度/秒）")]
        [SerializeField] private float keyOrbitSpeed = 45f;

        [Tooltip("右クリックドラッグでのみ回転させるか（falseにするとマウスを動かすだけで回転）")]
        [SerializeField] private bool requireRightClickToDrag = true;

        [Header("Zoom Settings")]
        [Tooltip("マウスホイールでの半径ズーム速度")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minRadius = 3f;
        [SerializeField] private float maxRadius = 100f;

        [Header("Smooth Settings")]
        [Tooltip("移動・回転のスムーズ補間速度")]
        [SerializeField] private float smoothSpeed = 10f;

        // クォータニオンによる姿勢表現（ジンバルロック・特異点・89度制限なし）
        private Quaternion currentRotation = Quaternion.identity;
        private Quaternion targetRotation = Quaternion.identity;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Clamp(value, minRadius, maxRadius);
        }

        public Vector3 CenterPosition => centerTransform != null ? centerTransform.position : Vector3.zero;

        private void Start()
        {
            InitFromCurrentPosition();
        }

        private void Update()
        {
            HandleInput();
        }

        private void LateUpdate()
        {
            UpdateCameraTransform();
        }

        /// <summary>
        /// マウスおよびキーボード入力によるカメラ回転クォータニオンと半径の更新
        /// </summary>
        private void HandleInput()
        {
            Vector2 delta = Vector2.zero;

            // マウス入力
            if (Mouse.current != null)
            {
                bool canDrag = !requireRightClickToDrag || Mouse.current.rightButton.isPressed;
                if (canDrag)
                {
                    delta = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.1f);
                }

                // スクロールホイールで半径（距離）のズーム調整
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    radius = Mathf.Clamp(radius - Mathf.Sign(scroll) * zoomSpeed, minRadius, maxRadius);
                }
            }

            // キーボード入力（矢印キーで周回）
            if (Keyboard.current != null)
            {
                float keyX = 0f;
                float keyY = 0f;

                if (Keyboard.current.leftArrowKey.isPressed) keyX -= 1f;
                if (Keyboard.current.rightArrowKey.isPressed) keyX += 1f;
                if (Keyboard.current.upArrowKey.isPressed) keyY += 1f;
                if (Keyboard.current.downArrowKey.isPressed) keyY -= 1f;

                delta.x += keyX * keyOrbitSpeed * Time.deltaTime;
                delta.y += keyY * keyOrbitSpeed * Time.deltaTime;
            }

            if (delta.sqrMagnitude > 0.0001f)
            {
                // カメラから見たローカル軸（右軸・上軸）を基準に増分クォータニオンを合成
                // これにより、真上（北極）や真下（南極）を通過しても制限なくぐるぐる周回可能
                Quaternion deltaYaw = Quaternion.AngleAxis(delta.x, targetRotation * Vector3.up);
                Quaternion deltaPitch = Quaternion.AngleAxis(-delta.y, targetRotation * Vector3.right);

                targetRotation = deltaYaw * deltaPitch * targetRotation;
            }
        }

        /// <summary>
        /// クォータニオン姿勢から球面上位置を算出して配置し、中心を注視
        /// </summary>
        private void UpdateCameraTransform()
        {
            Vector3 center = CenterPosition;

            // 回転を滑らかに補間
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * smoothSpeed);

            // targetRotation の「後方ベクトル（-Z）」を中心からのオフセット位置として配置
            Vector3 cameraDirection = currentRotation * Vector3.back;
            transform.position = center + cameraDirection * radius;

            // 中心を注視しつつ、カメラのローカル姿勢を反映
            transform.rotation = currentRotation;
        }

        /// <summary>
        /// 現在のカメラ位置を基準に初期のクォータニオン姿勢を設定
        /// </summary>
        public void InitFromCurrentPosition()
        {
            Vector3 center = CenterPosition;
            Vector3 offset = transform.position - center;

            if (offset.sqrMagnitude < 0.001f)
            {
                offset = new Vector3(0f, 5f, -15f);
            }

            radius = Mathf.Clamp(offset.magnitude, minRadius, maxRadius);

            // カメラ位置から中心を向く回転
            Vector3 lookDir = (center - (center + offset)).normalized;
            currentRotation = Quaternion.LookRotation(lookDir, Vector3.up);
            targetRotation = currentRotation;
        }
    }
}
