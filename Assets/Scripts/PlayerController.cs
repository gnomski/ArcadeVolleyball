using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Animator animator;

    private Rigidbody2D rb;
    private bool isGrounded = false;

    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Движение игрока
        float move = 0f;
        

        if (Input.GetKey(leftKey)) move = -1f;
        else if (Input.GetKey(rightKey)) move = 1f;

        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);

        // Прыжок
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }
        animator.SetFloat("Speed", Mathf.Abs(move));
        Debug.Log(animator.GetFloat("Speed"));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
