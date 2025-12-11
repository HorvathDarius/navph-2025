using System;
using UnityEngine;

public class CarMovement : MonoBehaviour
{

    [SerializeField] public float minSpeed = 5f;
    [SerializeField] public float maxSpeed = 12f;
    [SerializeField] public Sprite vehicleSprite;
    [SerializeField] public Sprite vehicleSpriteBack;
    public bool vehicleIsMovingDown = true;
    private Rigidbody2D rb2d;
    public float speed;


    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        Vector2 movingDirection = vehicleIsMovingDown ? Vector2.down : Vector2.up;
        rb2d.linearVelocity = movingDirection * speed;

        if (transform.position.y <= -10f || transform.position.y >= 10f)
        {
            DestoryObject();
        }
    }

    private void DestoryObject()
    {
        Destroy(gameObject);
    }
}
