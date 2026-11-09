using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public enum ChestPerkType
{
    MaxHealth,
    MaxStamina,
    MoveSpeed,
    Fireball
}

[CreateAssetMenu(
    fileName = "NewPerk",
    menuName = "Game/Perk Data"
)]
public class PerkData : ScriptableObject
{
    [Header("Display Information")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Perk Effect")]
    public ChestPerkType perkType;

    [Min(0f)]
    public float value;
}
