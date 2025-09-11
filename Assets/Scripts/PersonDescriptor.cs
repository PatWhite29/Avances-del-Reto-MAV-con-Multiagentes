using UnityEngine;

public enum JacketColor { Orange, Red, Blue, Green, Yellow, Black }
public enum HelmetColor { Yellow, White, Orange, None }

[DisallowMultipleComponent]
public class PersonDescriptor : MonoBehaviour
{
    [Header("Atributos (se pueden randomizar en Start)")]
    public JacketColor jacketColor = JacketColor.Orange;
    public HelmetColor  helmet = HelmetColor.Yellow;

    [Tooltip("Opcional: forzar aleatorizar en Start()")]
    public bool randomizeOnStart = true;

    void Start()
    {
        if (!randomizeOnStart) return;
        jacketColor = (JacketColor)Random.Range(0, System.Enum.GetValues(typeof(JacketColor)).Length);
        helmet = (HelmetColor)Random.Range(0, System.Enum.GetValues(typeof(HelmetColor)).Length);
    }

    // Frase corta útil para depurar o UI
    public string ShortText()
    {
        string h = (helmet == HelmetColor.None) ? "no helmet" : $"{helmet} helmet";
        return $"{jacketColor} jacket, {h}";
    }
}
