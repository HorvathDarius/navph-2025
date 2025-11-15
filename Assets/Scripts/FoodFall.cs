using UnityEngine;

public class FoodFall : MonoBehaviour
{
    public float fallSpeed = 10f;
    public bool isGood = true;

    void Update()
    {
        transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
        if (transform.position.y < -12f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bowl"))
        {
            if (isGood)
                GameManager.Instance.AddGoodFood();
            else
                GameManager.Instance.AddBadFood();

            Destroy(gameObject);
        }
    }
}
