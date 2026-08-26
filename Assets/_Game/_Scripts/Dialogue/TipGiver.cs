using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TipGiver", menuName = "Taxi/Tip Giver")]
public class TipGiver : ScriptableObject
{
    public string displayName;

    [Range(0f, 1f)]
    public float lieChance = 0.45f;

    [Header("Lines ({dest} {a} {b} {dir})")]
    public List<string> truthLines = new List<string>();
    public List<string> lieLines = new List<string>();

    [Header("Portraits")]
    public Sprite truthSprite;
    public Sprite lieSprite;
}
