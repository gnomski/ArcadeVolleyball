using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAI : MonoBehaviour
{
    public Transform ball;        // ���
    public Rigidbody2D rb;        // Rigidbody ������
    public float moveSpeed = 5f;  // �������� ��������
    public float jumpForce = 10f; // ���� ������
    public float jumpThreshold = 2f; // ������ ������ �� X
    public float homePositionX = 25f; // ����� ����� ��������
    public float attackForceX = -5f;  // ���� ����� �� X (� ������� ����������)
    public float attackForceY = 5f;   // �������������� ���� ����� �� Y

    private bool isJumping = false;

    void Update()
    {
        float distanceToBall = ball.position.x - transform.position.x;

        // ======= �������� =========
        if (ball.position.x < 0) // ��� �� ������ �������
        {
            float homeDistance = homePositionX - transform.position.x;

            if (Mathf.Abs(homeDistance) > 0.5f)
            {
                float direction = Mathf.Sign(homeDistance);
                rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        else // ��� �� ����� �������
        {
            if (Mathf.Abs(distanceToBall) > 0.5f)
            {
                float direction = Mathf.Sign(distanceToBall);
                rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

            // �������, ���� ��� ������ � ������
            if (Mathf.Abs(distanceToBall) < jumpThreshold && ball.position.y > transform.position.y + 1f && Mathf.Abs(rb.velocity.y) < 0.01f)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                isJumping = true;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // ���� AI �������� ���� � ������ - �������� ������ �� ������ �������
        if (collision.gameObject.CompareTag("Ball") && isJumping)
        {
            Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (ballRb != null)
            {
                // ���������� ��� � ������� ���������� � �������������� ������������ �����
                ballRb.velocity = new Vector2(attackForceX, attackForceY);
            }

            isJumping = false;
        }
    }
}
