using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public BallControll ball;
    public GameObject[] pins;

    private int currentFrame = 1;
    private int throwInFrame = 1;
    private int pinsKilledInFirstThrow = 0;
    private int totalScore = 0;

    private Vector3[] pinStartPositions;
    private Quaternion[] pinStartRotations;
    
    private List<int> bonusThrows = new List<int>();

    void Start()
    {
        pins = GameObject.FindGameObjectsWithTag("Pin");

        pinStartPositions = new Vector3[pins.Length];
        pinStartRotations = new Quaternion[pins.Length];

        for (int i = 0; i < pins.Length; i++)
        {
            pinStartPositions[i] = pins[i].transform.position;
            pinStartRotations[i] = pins[i].transform.rotation;
        }

        UpdateUI("Счёт: 0\nКликните на шар для броска");
    }

    public void CheckPinsAfterThrow()
    {
        StartCoroutine(CalculateScoreRoutine());
    }

    private IEnumerator CalculateScoreRoutine()
    {
        yield return new WaitForSeconds(4f);
        
        int currentFallenPins = CountFallenPins();
        
        int pinsThisThrow;

        if (throwInFrame == 1)
        {
            pinsThisThrow = currentFallenPins;
        }
        else
        {
            pinsThisThrow = currentFallenPins - pinsKilledInFirstThrow;
        }
        
        AddBonusPoints(pinsThisThrow);
        

        if (throwInFrame == 1)
        {
            if (currentFallenPins == 10)
            {
                totalScore += 10;
                
                bonusThrows.Add(2);

                UpdateUI(
                    $"STRIKE!\n" +
                    $"Счёт: {totalScore}"
                );

                EndFrame();
            }
            else
            {
                pinsKilledInFirstThrow = currentFallenPins;

                throwInFrame = 2;

                UpdateUI(
                    $"Сбито: {currentFallenPins}\n" +
                    $"Счёт: {totalScore}\n" +
                    $"Второй бросок"
                );

                ball.ResetBall();
            }
        }
        else if (throwInFrame == 2)
        {
            if (currentFallenPins == 10)
            {
                totalScore += 10;
                bonusThrows.Add(1);

                UpdateUI(
                    $"SPARE!\n" +
                    $"Счёт: {totalScore}"
                );
            }
            else
            {
                totalScore += currentFallenPins;

                UpdateUI(
                    $"Сбито за фрейм: {currentFallenPins}\n" +
                    $"Счёт: {totalScore}"
                );
            }

            EndFrame();
        }
    }

    private void AddBonusPoints(int pinsThisThrow)
    {
        for (int i = bonusThrows.Count - 1; i >= 0; i--)
        {
            totalScore += pinsThisThrow;
            bonusThrows[i]--;
            
            if (bonusThrows[i] == 0)
            {
                bonusThrows.RemoveAt(i);
            }
        }
    }

    private int CountFallenPins()
    {
        int fallen = 0;

        foreach (GameObject pin in pins)
        {
            if (pin != null)
            {
                if (Vector3.Angle(pin.transform.up, Vector3.up) > 15f)
                {
                    fallen++;
                }
            }
        }

        return fallen;
    }

    private void ResetPins()
    {
        for (int i = 0; i < pins.Length; i++)
        {
            Rigidbody rb = pins[i].GetComponent<Rigidbody>();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            pins[i].transform.position = pinStartPositions[i];
            pins[i].transform.rotation = pinStartRotations[i];
        }
    }

    private void EndFrame()
    {
        throwInFrame = 1;
        pinsKilledInFirstThrow = 0;

        currentFrame++;

        ResetPins();
        ball.ResetBall();
    }

    private void UpdateUI(string message)
    {
        if (scoreText != null)
        {
            scoreText.text =
                $"Фрейм: {currentFrame}\n" +
                message;
        }
    }
}
