using UnityEngine;

[System.Serializable]
public class NPCConfig
{
    [Header("Configuración del NPC")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero; // En grados
    public Vector3 localScale = Vector3.one;
}

[System.Serializable]
public class HatConfig
{
    public GameObject prefab;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero; // En grados
    public Vector3 localScale = Vector3.one;
}

[System.Serializable]
public class AccessoryConfig
{
    public GameObject prefab;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero; // En grados
    public Vector3 localScale = Vector3.one;
}