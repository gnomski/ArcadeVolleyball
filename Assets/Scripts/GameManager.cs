using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int leftPlayerScore = 0;
    public int rightPlayerScore = 0;

    public TMP_Text leftScoreText;
    public TMP_Text rightScoreText;

    public Transform ballStartPosition;
    private GameObject ball;

    void Start()
    {
        ball = GameObject.FindGameObjectWithTag("Ball");
        UpdateScoreUI();
    }

    public void BallTouchedGround()
    {
        if (ball.transform.position.x < 0)  // Если мяч слева
        {
            rightPlayerScore++;
        }
        else  // Если мяч справа
        {
            leftPlayerScore++;
        }

        Debug.Log("Left: " + leftPlayerScore + " - Right: " + rightPlayerScore);
        UpdateScoreUI();
        ResetBall();
    }

    void UpdateScoreUI()
    {
        leftScoreText.text = leftPlayerScore.ToString();
        rightScoreText.text = rightPlayerScore.ToString();
    }

    void ResetBall()
    {
        ball.transform.position = ballStartPosition.position;
        ball.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
