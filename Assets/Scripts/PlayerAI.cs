using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAI : MonoBehaviour
{
    public Transform ball;        // Мяч
    public Rigidbody2D rb;        // Rigidbody игрока
    public float moveSpeed = 5f;  // Скорость движения
    public float jumpForce = 10f; // Сила прыжка
    public float jumpThreshold = 2f; // Радиус прыжка по X
    public float homePositionX = 25f; // Центр своей половины
    public float attackForceX = -5f;  // Сила удара по X (в сторону противника)
    public float attackForceY = 5f;   // Дополнительная сила удара по Y

    private bool isJumping = false;

    void Update()
    {
        float distanceToBall = ball.position.x - transform.position.x;

        // ======= ДВИЖЕНИЕ =========
        if (ball.position.x < 0) // Мяч на другой стороне
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
        else // Мяч на своей стороне
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

            // Прыгаем, если мяч высоко и близко
            if (Mathf.Abs(distanceToBall) < jumpThreshold && ball.position.y > transform.position.y + 1f && Mathf.Abs(rb.velocity.y) < 0.01f)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                isJumping = true;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Если AI касается мяча в прыжке - пытаемся отбить на другую сторону
        if (collision.gameObject.CompareTag("Ball") && isJumping)
        {
            Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (ballRb != null)
            {
                // Отправляем мяч в сторону противника с дополнительной вертикальной силой
                ballRb.velocity = new Vector2(attackForceX, attackForceY);
            }

            isJumping = false;
        }
    }
}
