using UnityEngine;
using UnityEngine.InputSystem; // 書き加える

namespace UnityChan
{
    public class PlayerController : MonoBehaviour
    {

        private Rigidbody rb;
        private Animator animator;

        private Vector2 _moveInput;

        private float moveSpeed = 1f;
        private float rotateSpeed = 10f;

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

            animator.SetBool("isWalking", isWalking);
        }

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

    }
}