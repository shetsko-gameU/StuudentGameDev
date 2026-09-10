using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Generates the placeholder animation set and Animator Controller for every NPC, and wires
/// the prefabs up so the enemy state machine has something to drive.
///
/// Why this exists: the enemy models are all imported as Generic rigs with no authored
/// animation clips, so there was literally nothing for the FSM to play. Generic rigs cannot
/// retarget, so Mixamo or Asset Store humanoid clips cannot be dropped onto a slime. What
/// DOES work on an unrigged mesh is transform-level animation - moving, rotating and scaling
/// the model object itself. The one pre-existing enemy clip in the project,
/// Assets/animations/Flying Snake Hover.anim, is exactly that: it animates m_LocalPosition
/// on a child called "Model" and nothing else. These generated clips follow that precedent.
///
/// The clips are intentionally crude. They exist so that combat is playable and the
/// animation wiring is proven end to end before any real animation is authored. When real
/// clips arrive, a designer replaces the motion on each state in the generated controller
/// and deletes nothing else - the parameter contract and transitions stay valid.
///
/// Clips are bound to the model child, never the enemy root, because the NavMeshAgent owns
/// the root's position. Animating the root would fight the agent.
///
/// Run it from the menu: Tools > Enemies > Generate Placeholder Animations.
/// Or headless: Unity -batchmode -quit -executeMethod PlaceholderEnemyAnimationGenerator.Generate
/// It is safe to re-run; it overwrites the generated assets in place.
/// </summary>
public static class PlaceholderEnemyAnimationGenerator
{
    private const string GeneratedFolder = "Assets/animations/Generated";

    /// <summary>Every spawnable creature prefab. EnemyPrefabSetupGenerator installs the FSM
    /// stack on any that are still visual-only; this pass then attaches placeholder clips,
    /// controllers, hitboxes and animation-event relays. EnemyManger is a spawner and is
    /// intentionally excluded.</summary>
    private static readonly string[] EnemyPrefabs =
    {
        "Assets/models/Enemys/Prefabs/slime/slime.prefab",
        "Assets/models/Enemys/Prefabs/slime/crystal slime.prefab",
        "Assets/models/Enemys/Prefabs/slime/metal slime.prefab",
        "Assets/models/Enemys/Prefabs/mushroom/mushroom scout.prefab",
        "Assets/models/Enemys/Prefabs/mushroom/mage shroom.prefab",
        "Assets/models/Enemys/Prefabs/snake/flying snake.prefab",
        "Assets/models/Enemys/Prefabs/snake/flying hammer snake.prefab",
        "Assets/models/Enemys/Prefabs/dryad/dryad crawler1.prefab",
        "Assets/models/Enemys/Prefabs/wolf/static wolf2.prefab",
    };

    /// <summary>The player sits on the Default layer in this project - there is no dedicated
    /// Player layer. EnemyHitbox additionally requires a StatsManager on whatever it touches,
    /// and the player is the only Default-layer object that has one, so this is safe today.
    /// Adding a real Player layer would make it safer still.</summary>
    private const string PlayerLayerName = "Default";

    [MenuItem("Tools/Enemies/Generate Placeholder Animations")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            Directory_CreateRecursive(GeneratedFolder);
        }

        int generated = 0;

        foreach (string prefabPath in EnemyPrefabs)
        {
            if (GenerateFor(prefabPath))
                generated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"PlaceholderEnemyAnimationGenerator: finished. {generated}/{EnemyPrefabs.Length} enemy prefabs wired.");
    }

    private static bool GenerateFor(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"PlaceholderEnemyAnimationGenerator: prefab not found at '{prefabPath}'.");
            return false;
        }

        string enemyName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);

        // LoadPrefabContents gives an editable throwaway instance of the prefab; changes only
        // persist if SaveAsPrefabAsset is called. This is the supported way to edit a prefab
        // asset from script without instantiating it into a scene.
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                // Slime and mushroom scout have no Animator at all: their model is a nested FBX
                // instance and nobody ever added one. Add it to the model child so the clips
                // below have something to play them.
                Transform model = FindModelChild(root);
                if (model == null)
                {
                    Debug.LogError($"PlaceholderEnemyAnimationGenerator: '{enemyName}' has no child with a " +
                                   "Renderer, so there is no model to animate. Skipped.");
                    return false;
                }

                animator = model.gameObject.AddComponent<Animator>();
                Debug.Log($"PlaceholderEnemyAnimationGenerator: added an Animator to '{enemyName}/{model.name}'.");
            }

            // The clips animate this transform. It must never be the enemy root, because the
            // NavMeshAgent writes the root's position every frame.
            Transform visual = animator.transform == root.transform
                ? FindModelChild(root)
                : animator.transform;

            if (visual == null)
            {
                Debug.LogError($"PlaceholderEnemyAnimationGenerator: '{enemyName}' has its Animator on the " +
                               "root and no model child to animate. Skipped.");
                return false;
            }

            string bindingPath = AnimationUtility.CalculateTransformPath(visual, animator.transform);

            AnimationClip idle = CreateIdleClip(enemyName, bindingPath);
            AnimationClip walk = CreateWalkClip(enemyName, bindingPath);
            AnimationClip attack = CreateAttackClip(enemyName, bindingPath);
            AnimationClip hit = CreateHitClip(enemyName, bindingPath);
            AnimationClip death = CreateDeathClip(enemyName, bindingPath);

            AnimatorController controller = BuildController(enemyName, idle, walk, attack, hit, death);
            animator.runtimeAnimatorController = controller;

            // Root motion off, always. On the slime the Animator sits on the prefab root - the
            // same transform the NavMeshAgent drives - so if root motion were ever enabled the
            // two would fight for the enemy's position. The generated clips animate a child
            // and produce no root motion anyway; this makes that guarantee explicit rather
            // than dependent on each FBX's import settings.
            animator.applyRootMotion = false;

            EnsureHitbox(root, visual);
            EnsureEventRelay(animator.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"PlaceholderEnemyAnimationGenerator: wired '{enemyName}' (clips bound to '{bindingPath}').");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------ Clip builders

    /// <summary>Slow vertical bob. Loops. Reads as "alive but not moving".</summary>
    private static AnimationClip CreateIdleClip(string enemyName, string path)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1f, 0.08f),
                new Keyframe(2f, 0f)));

        return SaveClip(clip, enemyName, "Idle", loop: true);
    }

    /// <summary>Faster bob plus a slight squash, so movement reads differently from idle.</summary>
    private static AnimationClip CreateWalkClip(string enemyName, string path)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.12f),
                new Keyframe(0.3f, 0f),
                new Keyframe(0.45f, 0.12f),
                new Keyframe(0.6f, 0f)));

        clip.SetCurve(path, typeof(Transform), "m_LocalScale.y",
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.15f, 0.94f),
                new Keyframe(0.3f, 1f),
                new Keyframe(0.45f, 0.94f),
                new Keyframe(0.6f, 1f)));

        return SaveClip(clip, enemyName, "Walk", loop: true);
    }

    /// <summary>
    /// Lunge forward and recover. Carries the two animation events that actually deal damage,
    /// so the hit window is defined by the animation rather than by a timer.
    /// </summary>
    private static AnimationClip CreateAttackClip(string enemyName, string path)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, -0.15f),   // wind up
                new Keyframe(0.25f, 0.55f),    // lunge
                new Keyframe(0.5f, 0f)));      // recover

        clip.SetCurve(path, typeof(Transform), "m_LocalScale.z",
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.25f, 1.12f),
                new Keyframe(0.5f, 1f)));

        AnimationClip saved = SaveClip(clip, enemyName, "Attack", loop: false);

        // The hit window is open only across the lunge itself. EnemyAnimationEventRelay
        // receives these because it lives on the Animator's GameObject; it forwards them to
        // EnemyHitbox, which is on the attack point.
        AnimationUtility.SetAnimationEvents(saved, new[]
        {
            new AnimationEvent { time = 0.18f, functionName = "EnableHitbox" },
            new AnimationEvent { time = 0.34f, functionName = "DisableHitbox" },
        });

        EditorUtility.SetDirty(saved);
        return saved;
    }

    /// <summary>Short recoil so damage has visible feedback.</summary>
    private static AnimationClip CreateHitClip(string enemyName, string path)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.08f, -0.25f),
                new Keyframe(0.25f, 0f)));

        clip.SetCurve(path, typeof(Transform), "m_LocalScale.x",
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.08f, 1.15f),
                new Keyframe(0.25f, 1f)));

        return SaveClip(clip, enemyName, "Hit", loop: false);
    }

    /// <summary>
    /// Sink and shrink. EnemyDeath measures this clip's length to decide how long to leave
    /// the corpse up before destroying it, and it finds it by looking for "death" in the clip
    /// name - so the name matters.
    /// </summary>
    private static AnimationClip CreateDeathClip(string enemyName, string path)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.8f, -0.4f)));

        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.85f),
            new Keyframe(0.8f, 0.01f));

        clip.SetCurve(path, typeof(Transform), "m_LocalScale.x", shrink);
        clip.SetCurve(path, typeof(Transform), "m_LocalScale.y", shrink);
        clip.SetCurve(path, typeof(Transform), "m_LocalScale.z", shrink);

        return SaveClip(clip, enemyName, "Death", loop: false);
    }

    private static AnimationClip SaveClip(AnimationClip clip, string enemyName, string stateName, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = $"{GeneratedFolder}/{enemyName} {stateName}.anim";

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            // Overwrite in place so the controller's existing reference (and the asset GUID)
            // survives a re-run.
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ------------------------------------------------------------------ Controller

    /// <summary>
    /// Builds the Animator Controller implementing the EnemyAnimatorParams contract.
    ///
    /// Layout: Idle and Walk blend on Speed; Attack, Hit and Death are entered from Any
    /// State so they interrupt whatever is playing. Death is terminal - EnemyDeath destroys
    /// the GameObject, so there is deliberately no transition out of it.
    /// </summary>
    private static AnimatorController BuildController(string enemyName,
                                                      AnimationClip idle,
                                                      AnimationClip walk,
                                                      AnimationClip attack,
                                                      AnimationClip hit,
                                                      AnimationClip death)
    {
        string path = $"{GeneratedFolder}/{enemyName}.controller";

        // Always rebuild from scratch: editing an existing controller's states in place is far
        // more error-prone than recreating a file whose only content is generated anyway.
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter(EnemyAnimatorParams.Speed, AnimatorControllerParameterType.Float);
        controller.AddParameter(EnemyAnimatorParams.Attack, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(EnemyAnimatorParams.Hit, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(EnemyAnimatorParams.Dead, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idle;
        stateMachine.defaultState = idleState;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = walk;

        AnimatorState attackState = stateMachine.AddState("Attack");
        attackState.motion = attack;

        AnimatorState hitState = stateMachine.AddState("Hit");
        hitState.motion = hit;

        AnimatorState deathState = stateMachine.AddState("Death");
        deathState.motion = death;

        // Speed threshold of 0.1 rather than 0: NavMeshAgent.velocity is rarely exactly zero.
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, EnemyAnimatorParams.Speed);
        idleToWalk.duration = 0.15f;
        idleToWalk.hasExitTime = false;

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, EnemyAnimatorParams.Speed);
        walkToIdle.duration = 0.15f;
        walkToIdle.hasExitTime = false;

        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimatorParams.Attack);
        anyToAttack.duration = 0.05f;
        anyToAttack.hasExitTime = false;
        // Without this, the attack trigger can re-enter Attack from Attack and the clip
        // restarts mid-swing, which would re-open the damage window.
        anyToAttack.canTransitionToSelf = false;

        AnimatorStateTransition anyToHit = stateMachine.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimatorParams.Hit);
        anyToHit.duration = 0.05f;
        anyToHit.hasExitTime = false;
        anyToHit.canTransitionToSelf = false;

        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimatorParams.Dead);
        anyToDeath.duration = 0.05f;
        anyToDeath.hasExitTime = false;
        anyToDeath.canTransitionToSelf = false;

        // Attack and Hit fall back to Idle when their clip finishes. Idle/Walk sort themselves
        // out immediately afterwards via the Speed conditions above.
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        AnimatorStateTransition hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.9f;
        hitToIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // ------------------------------------------------------------------ Prefab wiring

    /// <summary>
    /// Adds an EnemyHitbox on a child at the front of the enemy, if one is not already there.
    /// Without this the animation events have nothing to call and enemies stay harmless.
    /// </summary>
    private static void EnsureHitbox(GameObject root, Transform visual)
    {
        EnemyHitbox existing = root.GetComponentInChildren<EnemyHitbox>();
        if (existing != null)
        {
            existing.playerLayer = LayerMask.GetMask(PlayerLayerName);
            return;
        }

        GameObject hitboxObject = new GameObject("AttackHitbox");
        hitboxObject.transform.SetParent(root.transform, false);

        // Sit it slightly in front of the enemy so a lunge sweeps it through the player.
        hitboxObject.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);

        SphereCollider collider = hitboxObject.AddComponent<SphereCollider>();
        collider.radius = 0.7f;
        collider.isTrigger = true;

        EnemyHitbox hitbox = hitboxObject.AddComponent<EnemyHitbox>();
        hitbox.playerLayer = LayerMask.GetMask(PlayerLayerName);
        hitbox.attackerStats = root.GetComponent<StatsManager>();

        // The generated attack clip carries real EnableHitbox/DisableHitbox events, so the
        // timer fallback is not needed for these enemies.
        hitbox.useSwingWindow = false;

        EnemyBase enemyBase = root.GetComponent<EnemyBase>();
        if (enemyBase != null)
            enemyBase.hitbox = hitbox;
    }

    /// <summary>Animation events only reach components on the Animator's own GameObject, so
    /// the relay has to live there.</summary>
    private static void EnsureEventRelay(GameObject animatorObject)
    {
        if (animatorObject.GetComponent<EnemyAnimationEventRelay>() == null)
            animatorObject.AddComponent<EnemyAnimationEventRelay>();
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>
    /// The visual model of the enemy: the first child whose hierarchy contains a Renderer.
    /// Deliberately excludes the root, whose transform belongs to the NavMeshAgent.
    /// </summary>
    private static Transform FindModelChild(GameObject root)
    {
        foreach (Transform child in root.transform)
        {
            if (child.GetComponentInChildren<Renderer>() != null)
                return child;
        }

        return null;
    }

    private static void Directory_CreateRecursive(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string running = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{running}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(running, parts[i]);

            running = next;
        }
    }
}
