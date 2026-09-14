using UnityEngine;
using TMPro;

public class PointsPopup : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float duration = 1f;

    private TMP_Text text;
    private float timer = 0f;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        timer += Time.deltaTime;

        float alpha = 1f - (timer / duration);

        Color color = text.color;
        color.a = alpha;
        text.color = color;

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}