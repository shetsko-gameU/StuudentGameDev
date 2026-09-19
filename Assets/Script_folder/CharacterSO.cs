using UnityEngine;

/// <summary>
/// One playable body. The two player prefabs were structural twins — same 27 root components,
/// same tag, same stats asset — differing only in their FBX mesh child, animator controller and
/// mesh offset, so a character is data rather than a separate prefab. CharacterLoader applies
/// one of these onto the single shared Player at spawn or when the hub swap station is used.
///
/// Setup:
///   1. Create via Assets > Create > Characters > Character.
///   2. Set id to something stable — it goes into the save file and PlayerPrefs, so renaming it
///      later resets everyone's remembered choice.
///   3. Drag the FBX model asset into meshPrefab and its controller into animatorController.
///   4. Add the asset to Assets/Resources/CharacterLibrary.asset or it can't be looked up by id.
/// </summary>
[CreateAssetMenu(menuName = "Characters/Character", fileName = "Character_New")]
public class CharacterSO : ScriptableObject
{
    [Tooltip("Stable key written to saves and PlayerPrefs. Changing it orphans existing choices.")]
    public string id;

    [Tooltip("Shown in the hub swap prompt.")]
    public string displayName;

    [Header("Body")]
    [Tooltip("The FBX model asset (or a prefab of it). Instantiated as the Player's 'mesh' child.")]
    public GameObject meshPrefab;

    public RuntimeAnimatorController animatorController;

    [Tooltip("Attack chain for this body. Leave empty to keep whatever the Player already has.")]
    public ComboSO combo;

    [Header("Placement")]
    [Tooltip("Local Y of the mesh child. Must pair with NavMeshAgent.baseOffset or the character " +
             "floats or sinks — PlayerMove's ground follow plants the root, not the model, so each " +
             "body carries its own offset (-0.754 for Dirk, -0.627 for Molly).")]
    public float meshLocalY = -0.754f;

    [Tooltip("Uniform local scale for the mesh child. A freshly instantiated FBX comes in at 1, " +
             "while the authored prefabs use 2.")]
    public float meshScale = 2f;

    [Header("Attack style")]
    [Tooltip("Ranged bodies spawn projectilePrefab instead of enabling the melee hitbox.")]
    public bool isRanged;

    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
}
