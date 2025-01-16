using UnityEngine;

public class MoveFloor : MonoBehaviour
{

    Rigidbody2D rb;
    Vector2 startPos;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = Vector2.left * LevelGenerator.Instance.speed.Value;
        if(Vector3.Distance(startPos, transform.position) > 30)
        {
            transform.position = startPos;
        }
    }
}
