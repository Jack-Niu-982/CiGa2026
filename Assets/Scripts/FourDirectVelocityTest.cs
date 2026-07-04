using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FourDirectionForceTest : MonoBehaviour
{
    [SerializeField] private float forceAmount = 3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            rb.AddForce(Vector2.up * forceAmount, ForceMode2D.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            rb.AddForce(Vector2.down * forceAmount, ForceMode2D.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            rb.AddForce(Vector2.left * forceAmount, ForceMode2D.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            rb.AddForce(Vector2.right * forceAmount, ForceMode2D.Impulse);
        }
    }
}