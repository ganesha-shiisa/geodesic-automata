using UnityEngine;
using UnityEngine.InputSystem; // 書き加える

namespace UnityChan
{
    public class PlayerController : MonoBehaviour
    {

        private Rigidbody rb;
        private Animator animator;

        private Vector2 _moveInput;
        private bool _jumpPressed;

        private bool isGrounded;

        private float moveSpeed = 1f;
        private float rotateSpeed = 10f;
        private float _jumpCooldownTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        void Update()
        {

        }

        void FixedUpdate()
        {
            Vector3 moveDirection =
                new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            if (_jumpCooldownTimer > 0f)
            {
                _jumpCooldownTimer -= Time.fixedDeltaTime;
            }


            Move(moveDirection);
            Rotate(moveDirection);
            UpdateAnimation(moveDirection);
        }

        private void Move(Vector3 direction)
        {
            Vector3 nextPosition =
                rb.position
                + direction * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(nextPosition);
        }

        private void Rotate(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            Quaternion newRotation =
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime
                );

            rb.MoveRotation(newRotation);
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