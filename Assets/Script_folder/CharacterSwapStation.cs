using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hub mannequin the player walks up to and presses E at to play as that character. Stations
/// only exist in the hub, which is what keeps "you can only change body between runs" true
/// without any extra gating.
///
/// Proximity is a distance check rather than a trigger collider: the player is moved by
/// transform writes (PlayerMove owns the Y position), and this needs no collider or tag setup on
/// the mannequin art.
///
/// Setup:
///   1. Add to a mannequin GameObject in the hub.
///   2. Assign character.
///   3. Optionally assign prompt (a world-space "Press E" object, hidden until in range) and
///      promptLabel to have the character's name filled in.
/// </summary>
public class CharacterSwapStation : MonoBehaviour
{
    /// <summary>Every enabled station, so PlayerInteract can pick the closest one in range.</summary>
    public static readonly List<CharacterSwapStation> Active = new List<CharacterSwapStation>();

    [Header("Character")]
    public CharacterSO character;

    [Header("Proximity")]
    [Tooltip("How close the player has to be, in metres, for E to do anything.")]
    public float interactRadius = 3f;

    [Header("Prompt")]
    [Tooltip("Shown only while the player is in range. Any GameObject — world-space text, a sprite, a canvas.")]
    public GameObject prompt;

    [Tooltip("Optional label inside the prompt. {0} is the Interact key, {1} is the character name.")]
    public TMPro.TMP_Text promptLabel;

    public string promptFormat = "Press {0} to play as {1}";

    public bool PlayerInRange { get; private set; }

    private Transform player;
    private PlayerInteract playerInteract;

    private void OnEnable()
    {
        Active.Add(this);

        if (prompt != null)
            prompt.SetActive(false);

        RefreshPrompt();
    }

    private void OnDisable()
    {
        Active.Remove(this);
        PlayerInRange = false;
    }

    private void Update()
    {
        // The hub's Player may start inactive (StartPortal reveals it), so keep looking until it
        // shows up rather than resolving once in Start.
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return;
            player = found.transform;
        }

        if (playerInteract == null)
            playerInteract = player.GetComponent<PlayerInteract>();

        bool inRange = (player.position - transform.position).sqrMagnitude <= interactRadius * interactRadius;
        if (inRange != PlayerInRange)
        {
            PlayerInRange = inRange;
            if (prompt != null)
                prompt.SetActive(inRange);
            if (inRange)
                RefreshPrompt();
        }

        if (prompt != null && prompt.activeSelf)
            FaceCamera();
    }

    /// <summary>
    /// Fills the prompt with the live Interact binding so a keyboard player sees E and a
    /// gamepad player sees the north face button, rather than a hardcoded key that can drift.
    /// </summary>
    public void RefreshPrompt()
    {
        if (promptLabel == null) return;

        string key = playerInteract != null ? playerInteract.BindingDisplayString() : "E";
        promptLabel.text = string.Format(promptFormat, key, DisplayName);
    }

    private void FaceCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || prompt == null) return;

        Vector3 toCamera = prompt.transform.position - cam.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f) return;
        prompt.transform.rotation = Quaternion.LookRotation(toCamera);
    }

    public float DistanceTo(Vector3 point)
    {
        return Vector3.Distance(point, transform.position);
    }

    public string DisplayName
    {
        get
        {
            if (character == null) return "???";
            return string.IsNullOrEmpty(character.displayName) ? character.id : character.displayName;
        }
    }

    /// <summary>
    /// Applies this station's character to the player and remembers the choice, both in PlayerPrefs
    /// for next launch and in the save file so a resumed run keeps the same body.
    /// </summary>
    public void Use(CharacterLoader loader)
    {
        if (loader == null || character == null) return;

        if (loader.Current == character)
        {
            Debug.Log($"CharacterSwapStation on '{name}': already playing as {DisplayName}.");
            return;
        }

        loader.Apply(character);
        RunSaveSerializer.WriteCharacterChoice(character.id);

        Debug.Log($"CharacterSwapStation on '{name}': swapped to {DisplayName}.");
    }
}
