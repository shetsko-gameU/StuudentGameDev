using UnityEngine;

/// <summary>
/// Defines a currency type (e.g. "Coins", "Gems").
/// Create via: Assets > Create > Game > Currency > Currency Type
/// </summary>
[CreateAssetMenu(fileName = "Currency", menuName = "Game/Currency/Currency Type")]
public class CurrencySO : ScriptableObject
{
    [Tooltip("Display name shown in the HUD (e.g. 'Coins').")]
    public string displayName = "Coins";

    [Tooltip("Icon shown next to the currency amount in the HUD.")]
    public Texture2D icon;
}
