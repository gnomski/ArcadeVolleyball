using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public GameManager gameManager;  // Ссылка на GameManager

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, если мяч коснулся пола
        if (collision.gameObject.CompareTag("Ground"))
        {
            gameManager.BallTouchedGround();
        }
    }
}
