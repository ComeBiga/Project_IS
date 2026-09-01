using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URPMovement
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : MonoBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 5f;
        [SerializeField]
        private float _jumpForce = 5f;
        [SerializeField]
        private float turnSpeed = 10f;
        [SerializeField]
        private LayerMask _groundLayer;
        //[SerializeField]
        //private Transform _groundCheck;
        [SerializeField]
        private float _groundCheckRadius = .2f;

        private Rigidbody mRb;
        private Animator mAnimator;
        private bool mbisGrounded;
        private Vector3 targetDirection = Vector3.right;

        // Start is called before the first frame update
        void Start()
        {
            mRb = GetComponent<Rigidbody>();
            mAnimator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            float moveInput = Input.GetAxis("Horizontal");

            // 좌우 이동 (2.5D라서 z축은 고정)
            mAnimator.SetFloat("Horizontal", moveInput);

            // 좌우 이동
            Vector3 velocity = mRb.velocity;
            mRb.velocity = new Vector3(moveInput * _moveSpeed, velocity.y, 0f); // z는 고정

            // 방향 전환 처리 (움직일 때만 회전)
            if (moveInput != 0)
            {
                targetDirection = moveInput > 0 ? Vector3.right : Vector3.left;
                rotateTowards(targetDirection);
            }

            // 점프
            mbisGrounded = Physics.CheckSphere(transform.position, _groundCheckRadius, _groundLayer);
            if (Input.GetButtonDown("Jump") && mbisGrounded)
            {
                mAnimator.SetTrigger("Jump");
                mRb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            }
        }

        private void rotateTowards(Vector3 direction)
        {
            // 2D 평면 상에서만 회전 (y축 기준)
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up); // 기본 z축 바라보는 회전

            if (direction == Vector3.right)
                targetRotation = Quaternion.Euler(0, 90, 0);
            else if (direction == Vector3.left)
                targetRotation = Quaternion.Euler(0, -90, 0);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }
}
