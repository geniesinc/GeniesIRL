using UnityEngine;

public class ConfettiController : MonoBehaviour
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private Color[] colors;
    private const string PROPERTY_NAME = "_BaseColor";

    void Awake()
    {
        // pick a random petal from the list
        int randomIndex = Random.Range(0, models.Length);

        // pick a random color from the list
        if(colors.Length > 0)
        {
            int randomColorIndex = Random.Range(0, colors.Length);
            models[randomIndex].GetComponent<Renderer>().material.SetColor(PROPERTY_NAME, colors[randomColorIndex]);
        }

        // turn off everything else
        for (int i = 0; i < models.Length; i++)
        {
            models[i].SetActive(i == randomIndex);
        }
    }
}
