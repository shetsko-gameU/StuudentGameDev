using UnityEngine;

[CreateAssetMenu(menuName = "Game/Abilities/Dash")]
public class DashAbilitySO : AbilitySO
{
    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.12f;

    [Tooltip("If you have a CharacterController, this will use it.")]
    public bool useCharacterControllerIfFound = true;

    public override bool CanUse(GameObject user)
    {
        // Example: cannot dash if dead
        StatsManager stats = user.GetComponent<StatsManager>();
        if (stats != null && stats.IsDead)
        {
            return false;
        }

        return true;
    }

    public override void Activate(GameObject user)
    {
        // Start coroutine safely from any MonoBehaviour on the user
        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            Debug.LogWarning("DashAbilitySO: No MonoBehaviour found on user to run coroutine.");
            return;
        }

        runner.StartCoroutine(DashRoutine(user));
    }

    private System.Collections.IEnumerator DashRoutine(GameObject user)
    {
        Vector3 dir = user.transform.forward;
        float duration = Mathf.Max(0.01f, dashDuration);
        float speed = dashDistance / duration;

        CharacterController cc = null;
        if (useCharacterControllerIfFound)
        {
            cc = user.GetComponent<CharacterController>();
        }

        float t = 0f;
        while (t < dashDuration)
        {
            float dt = Time.deltaTime;
            t += dt;

            Vector3 move = dir * speed * dt;

            if (cc != null)
            {
                cc.Move(move);
            }
            else
            {
                user.transform.position += move;
            }

            yield return null;
        }
    }
}
