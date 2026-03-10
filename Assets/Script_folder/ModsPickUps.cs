using UnityEngine;

public class ModifierPickup : MonoBehaviour
{
    [SerializeField] private StatsModifierSO modifierTemplate;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out StatManager stats)) return;

        var rolled = ModifierRoller.Roll(modifierTemplate);
        stats.AddRolledModifier(rolled);

        Destroy(gameObject);
    }
}
