using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Routes the Player/Interact action (E) to whatever is in range — currently just the hub's
/// character swap stations.
///
/// Unlike the other player input scripts, this subscribes to the action in code instead of being
/// wired through PlayerInput's event list. Interact had no listeners at all, so there is nothing
/// to preserve, and a code subscription can't be silently lost when the prefab is re-saved.
///
/// Setup: add to the Player prefab. Nothing to wire — it finds PlayerInput itself. (Note that
/// Player/Craft is bound to E as well; harmless while crafting is retired.)
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Tooltip("Auto-found on this GameObject.")]
    public PlayerInput playerInput;

    [Tooltip("Auto-found on this GameObject.")]
    public CharacterLoader characterLoader;

    [Tooltip("Name of the action in the Player map.")]
    public string actionName = "Interact";

    private InputAction interactAction;

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (characterLoader == null) characterLoader = GetComponent<CharacterLoader>();
    }

    private void OnEnable()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning($"PlayerInteract on '{name}': no PlayerInput with an actions asset — E will do nothing.");
            return;
        }

        interactAction = playerInput.actions.FindAction(actionName);

        if (interactAction == null)
        {
            Debug.LogWarning($"PlayerInteract on '{name}': no '{actionName}' action in " +
                             $"'{playerInput.actions.name}' — E will do nothing.");
            return;
        }

        // Interact is authored with a Hold interaction. performed only fires after the hold,
        // which would make a "Press E" prompt a lie — started fires on the key-down itself.
        interactAction.started += HandleInteract;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.started -= HandleInteract;
    }

    /// <summary>The Interact binding as the player would read it, e.g. "E" or "Y".</summary>
    public string BindingDisplayString()
    {
        if (interactAction == null) return "E";

        string scheme = playerInput != null ? playerInput.currentControlScheme : null;
        InputBinding mask = string.IsNullOrEmpty(scheme)
            ? default(InputBinding)
            : InputBinding.MaskByGroup(scheme);

        string display = interactAction.GetBindingDisplayString(
            mask, InputBinding.DisplayStringOptions.DontIncludeInteractions);

        return string.IsNullOrEmpty(display) ? "E" : display;
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        Interact();
    }

    /// <summary>Public so a UI button or a test can trigger the same thing E does.</summary>
    public void Interact()
    {
        CharacterSwapStation station = NearestStationInRange();
        if (station == null) return;

        station.Use(characterLoader);
    }

    private CharacterSwapStation NearestStationInRange()
    {
        CharacterSwapStation best = null;
        float bestDistance = float.MaxValue;

        // Two mannequins can overlap, so pick the closest rather than the first found.
        foreach (CharacterSwapStation station in CharacterSwapStation.Active)
        {
            if (station == null || !station.PlayerInRange) continue;

            float distance = station.DistanceTo(transform.position);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = station;
        }

        return best;
    }
}
