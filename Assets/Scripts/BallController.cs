using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public GameManager gameManager;  // ������ �� GameManager

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ���������, ���� ��� �������� ����
        if (collision.gameObject.CompareTag("Ground"))
        {
            gameManager.BallTouchedGround();
        }
    }
}
