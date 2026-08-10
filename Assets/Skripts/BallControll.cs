using UnityEngine;

public class BallControll : MonoBehaviour
{
    [SerializeField] private float sila_broska = 500f;
    [SerializeField] private ScoreManager score_manager;
    private Rigidbody rigidbody;
    private bool ball_proverka = false;
    private Vector3 start_posotion;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        start_posotion = transform.position;

        if (score_manager == null)
        {
            score_manager = FindObjectOfType<ScoreManager>();
        }
    }

    void OnMouseDown()
    {
        if (!ball_proverka)
        {
            ThrowBall();
        }
    }

    void ThrowBall()
    {
        ball_proverka = true;
        rigidbody.AddForce(Vector3.back * sila_broska, ForceMode.Impulse);

        if (score_manager != null)
        {
            score_manager.CheckPinsAfterThrow();
        }
    }

    public void ResetBall()
    {
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.position = start_posotion;
        ball_proverka = false;
    }
}
