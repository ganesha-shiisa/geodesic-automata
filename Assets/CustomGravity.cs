using UnityEngine;

namespace UnityChan
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravity : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [Tooltip("重力の中心となるオブジェクト（未設定の場合は原点 (0,0,0) を使用）")]
        [SerializeField] private Transform gravityCenter;

        [Tooltip("重力加速度の大きさ")]
        [SerializeField] private float gravityStrength = 9.81f;

        [Tooltip("中心に到達した際、振動や行き過ぎを防ぐ最小距離")]
        [SerializeField] private float stopDistance = 0.01f;

        private Rigidbody rb;

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

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // 標準の真下方向重力を無効化
            rb.useGravity = false;
        }

        private void FixedUpdate()
        {
            ApplyGravity();
        }

        private void ApplyGravity()
        {
            Vector3 direction = GravityDirection;
            if (direction == Vector3.zero)
            {
                return;
            }

            rb.AddForce(direction * gravityStrength, ForceMode.Acceleration);
        }
    }
}