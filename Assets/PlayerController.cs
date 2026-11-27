using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityChan
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [Tooltip("重力の中心となるオブジェクト（未設定の場合は原点 (0,0,0) を使用）")]
        [SerializeField] private Transform gravityCenter;

        [Tooltip("重力加速度の大きさ")]
        [SerializeField] private float gravityStrength = 9.81f;

        [Tooltip("中心に到達した際、振動や行き過ぎを防ぐ最小距離")]
        [SerializeField] private float stopDistance = 0.01f;

        [Header("Camera / Reference (Optional)")]
        [Tooltip("移動基準となるカメラ（未設定の場合は Main Camera を使用）")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float rotateSpeed = 10f;
        [SerializeField] private float alignSpeed = 15f;

        private Rigidbody rb;
        private Animator animator;

        private Vector2 _moveInput;
        private bool _jumpPressed;

        private bool isGrounded;
        private float _jumpCooldownTimer;

        /// <summary>
        /// 現在の重力の中心座標を取得する
        /// </summary>
        public Vector3 CenterPosition => gravityCenter != null ? gravityCenter.position : Vector3.zero;

        /// <summary>
        /// 現在の位置から重力中心へ向かう単位ベクトル（下方向ベクトル）
        /// </summary>
        public Vector3 GravityDirection
        {
            get
            {
                Vector3 toCenter = CenterPosition - rb.position;
                return toCenter.sqrMagnitude > stopDistance * stopDistance
                    ? toCenter.normalized
                    : Vector3.zero;
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            // 標準の真下方向重力を無効化
            rb.useGravity = false;

            // 物理シミュレーションによる意図しない転倒・回転を防ぐ
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        void Update()
        {

        }

        void FixedUpdate()
        {
            Vector3 gravityDir = GravityDirection;

            // 1. 重力適用
            ApplyGravity(gravityDir);

            // 重力の反対方向（上方向）
            Vector3 up = gravityDir != Vector3.zero ? -gravityDir : transform.up;

            // 2. 移動方向の計算
            Vector3 moveDirection = CalculateMoveDirection(up);

            if (_jumpCooldownTimer > 0f)
            {
                _jumpCooldownTimer -= Time.fixedDeltaTime;
            }

            // 3. 移動実行
            Move(moveDirection);

            // 4. 重力直立と旋回を1つの回転として統合して適用
            UpdateOrientation(up, moveDirection);

            UpdateAnimation(moveDirection);
        }

        private Vector3 CalculateMoveDirection(Vector3 up)
        {
            if (_moveInput.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forwardReference;
            Vector3 rightReference;

            if (cameraTransform != null)
            {
                // カメラの向きを重力平面に投影して基準軸を作成
                forwardReference = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
                rightReference = Vector3.ProjectOnPlane(cameraTransform.right, up).normalized;

                // 真上・真下を向いていて投影が消失した場合のフォールバック
                if (forwardReference.sqrMagnitude < 0.001f)
                {
                    forwardReference = Vector3.ProjectOnPlane(cameraTransform.up, up).normalized;
                }
            }
            else
            {
                // カメラが無い場合は現在のプレイヤー姿勢を基準平面へ投影
                forwardReference = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                rightReference = Vector3.ProjectOnPlane(transform.right, up).normalized;
            }

            Vector3 dir = rightReference * _moveInput.x + forwardReference * _moveInput.y;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private void ApplyGravity(Vector3 direction)
        {
            if (direction == Vector3.zero)
            {
                return;
            }

            rb.AddForce(direction * gravityStrength, ForceMode.Acceleration);
        }

        private void UpdateOrientation(Vector3 up, Vector3 moveDirection)
        {
            // 目標の正面方向を決定（移動入力があるときは進行方向、ないときは現在の正面を重力平面に投影したもの）
            Vector3 targetForward;
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                targetForward = moveDirection;
            }
            else
            {
                targetForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                if (targetForward.sqrMagnitude < 0.001f)
                {
                    targetForward = Vector3.ProjectOnPlane(transform.up, up).normalized;
                }
            }

            if (targetForward.sqrMagnitude < 0.001f || up.sqrMagnitude < 0.001f)
            {
                return;
            }

            // 上方向(up)と前方向(targetForward)から目標姿勢を算出
            Quaternion targetRotation = Quaternion.LookRotation(targetForward, up);

            // 旋回入力時と直立維持時でスムーズに補間して1回だけ適用
            float speed = (moveDirection.sqrMagnitude > 0.01f) ? rotateSpeed : alignSpeed;
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, speed * Time.fixedDeltaTime);

            rb.MoveRotation(newRotation);
        }

        private void Move(Vector3 direction)
        {
            Vector3 nextPosition =
                rb.position
                + direction * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(nextPosition);
        }

        private void UpdateAnimation(Vector3 direction)
        {
            bool isWalking =
                direction.sqrMagnitude > 0.01f;

            bool isJump = _jumpPressed;

            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isJump", isJump);
            // animator.SetBool("isGrounded", isGrounded);
            _jumpPressed = false;
        }

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                _jumpPressed = true;
            }
        }

        // private void OnCollisionStay(Collision collision)
        // {
        //     // ジャンプ直後の数フレームは接地判定を戻さない
        //     if (_jumpCooldownTimer > 0f) return;

        //     isGrounded = true;
        // }

        // private void OnCollisionExit(Collision collision)
        // {
        //     isGrounded = false;
        // }
    }
}