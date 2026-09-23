using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Turns visual-only creature prefabs into playable NPCs by installing the full enemy FSM
/// stack that slime / mushroom scout / flying snake already have.
///
/// The remaining creatures in Assets/models/Enemys/Prefabs were bare FBX wrappers: no
/// EnemyBase, no StatsManager, no NavMeshAgent, no detection spheres. Dropping them into a
/// wave did nothing. This tool adds the same components and trigger children the working
/// prefabs use, then leaves PlaceholderEnemyAnimationGenerator to attach clips and hitboxes.
///
/// Run: Tools > Enemies > Wire Remaining NPCs (FSM + Placeholders)
/// Or headless: -executeMethod EnemyPrefabSetupGenerator.WireAll
/// Safe to re-run; already-wired prefabs are skipped.
/// </summary>
public static class EnemyPrefabSetupGenerator
{
    private const string DefaultStatsPath = "Assets/models/Enemys/Script_Folder/DefaultEnemy.asset";
    private const string SlimeStatsPath = "Assets/models/Enemys/Script_Folder/Slime.asset";
    private const int EnemyLayer = 3;

    /// <summary>Every spawnable creature. Already-complete prefabs are left alone; incomplete
    /// ones (e.g. flying snake had EnemyBase but no StatsManager / detection spheres) get the
    /// missing pieces filled in.</summary>
    private static readonly string[] PrefabsNeedingSetup =
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

    [MenuItem("Tools/Enemies/Wire Remaining NPCs (FSM + Placeholders)")]
    public static void WireAll()
    {
        BaseStatsSO stats = EnsureDefaultStats();

        int setup = 0;
        foreach (string path in PrefabsNeedingSetup)
        {
            if (SetupFsm(path, stats))
                setup++;
        }

        // Placeholders for every spawnable enemy, including the three already wired.
        PlaceholderEnemyAnimationGenerator.Generate();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"EnemyPrefabSetupGenerator: FSM installed on {setup}/{PrefabsNeedingSetup.Length} prefabs; " +
                  "placeholder animation pass finished.");

        ValidateAll();
    }

    /// <summary>Headless smoke check. Logs PASS/FAIL per prefab; returns without throwing so
    /// batchmode still exits cleanly. Look for "FAIL" in the log.</summary>
    [MenuItem("Tools/Enemies/Validate NPC Wiring")]
    public static void ValidateAll()
    {
        string[] all =
        {
            "Assets/models/Enemys/Prefabs/slime/slime.prefab",
            "Assets/models/Enemys/Prefabs/mushroom/mushroom scout.prefab",
            "Assets/models/Enemys/Prefabs/snake/flying snake.prefab",
            "Assets/models/Enemys/Prefabs/dryad/dryad crawler1.prefab",
            "Assets/models/Enemys/Prefabs/mushroom/mage shroom.prefab",
            "Assets/models/Enemys/Prefabs/slime/crystal slime.prefab",
            "Assets/models/Enemys/Prefabs/slime/metal slime.prefab",
            "Assets/models/Enemys/Prefabs/snake/flying hammer snake.prefab",
            "Assets/models/Enemys/Prefabs/wolf/static wolf2.prefab",
        };

        int passed = 0;
        foreach (string path in all)
        {
            if (ValidateOne(path))
                passed++;
        }

        Debug.Log($"EnemyPrefabSetupGenerator.ValidateAll: {passed}/{all.Length} prefabs PASS.");
    }

    private static bool SetupFsm(string prefabPath, BaseStatsSO stats)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (asset == null)
        {
            Debug.LogError($"EnemyPrefabSetupGenerator: missing prefab '{prefabPath}'.");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        string name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);

        try
        {
            bool changed = false;

            // Whole hierarchy on the Enemy layer so player hitboxes can find them.
            SetLayerRecursive(root, EnemyLayer);

            EnemyBase enemy = root.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                enemy = root.AddComponent<EnemyBase>();
                enemy.sightRange = 5f;
                enemy.randomMovementRange = 5f;
                enemy.randomMovementSpeed = 1f;
                enemy.moveTuning = new EnemyMoveTuning();
                enemy.attackTuning = new EnemyAttackTuning();
                changed = true;
            }

            StatsManager statsManager = root.GetComponent<StatsManager>();
            if (statsManager == null)
            {
                statsManager = root.AddComponent<StatsManager>();
                AssignBaseStats(statsManager, stats);
                changed = true;
            }

            if (enemy.stats == null)
            {
                enemy.stats = statsManager;
                changed = true;
            }

            if (root.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = root.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation.None;
                changed = true;
            }

            if (root.GetComponent<CapsuleCollider>() == null &&
                root.GetComponent<BoxCollider>() == null &&
                root.GetComponent<SphereCollider>() == null)
            {
                CapsuleCollider body = root.AddComponent<CapsuleCollider>();
                body.height = 2f;
                body.radius = 0.5f;
                body.center = new Vector3(0f, 1f, 0f);
                body.isTrigger = false;
                changed = true;
            }

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = root.AddComponent<NavMeshAgent>();
                agent.speed = enemy.moveTuning.moveSpeed;
                agent.acceleration = enemy.moveTuning.acceleration;
                agent.angularSpeed = enemy.moveTuning.angularSpeed;
                agent.stoppingDistance = enemy.moveTuning.stoppingDistance;
                agent.radius = 0.5f;
                agent.height = 2f;
                changed = true;
            }

            if (enemy.navMeshAgent == null)
            {
                enemy.navMeshAgent = agent;
                changed = true;
            }

            if (root.GetComponentInChildren<EnemyAggroCheck>() == null)
            {
                EnsureTriggerChild(root, "AggroRadius", 5f, typeof(EnemyAggroCheck), enemy);
                changed = true;
            }

            if (root.GetComponentInChildren<EnemyStrikeDistanceCheck>() == null)
            {
                EnsureTriggerChild(root, "StrikingDistance", 2.5f, typeof(EnemyStrikeDistanceCheck), enemy);
                changed = true;
            }

            if (!changed)
            {
                Debug.Log($"EnemyPrefabSetupGenerator: '{name}' already complete — skipped.");
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"EnemyPrefabSetupGenerator: installed/repaired FSM stack on '{name}'.");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureTriggerChild(GameObject root, string childName, float radius,
                                          System.Type checkType, EnemyBase enemy)
    {
        Transform existing = root.transform.Find(childName);
        GameObject child = existing != null ? existing.gameObject : new GameObject(childName);
        if (existing == null)
            child.transform.SetParent(root.transform, false);

        child.layer = EnemyLayer;

        SphereCollider sphere = child.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = child.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;
        sphere.center = Vector3.zero;

        Component check = child.GetComponent(checkType);
        if (check == null)
            check = child.AddComponent(checkType);

        // Both check scripts expose a public EnemyBase enemy field.
        SerializedObject so = new SerializedObject(check);
        SerializedProperty enemyProp = so.FindProperty("enemy");
        if (enemyProp != null)
        {
            enemyProp.objectReferenceValue = enemy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AssignBaseStats(StatsManager statsManager, BaseStatsSO stats)
    {
        SerializedObject so = new SerializedObject(statsManager);
        SerializedProperty prop = so.FindProperty("baseStats");
        if (prop != null)
        {
            prop.objectReferenceValue = stats;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static BaseStatsSO EnsureDefaultStats()
    {
        BaseStatsSO existing = AssetDatabase.LoadAssetAtPath<BaseStatsSO>(DefaultStatsPath);
        if (existing != null)
            return existing;

        // Prefer cloning the slime's numbers so new enemies feel in-band with the one that
        // already ships in waves. Fall back to BaseStatsSO defaults if slime's asset is gone.
        BaseStatsSO slime = AssetDatabase.LoadAssetAtPath<BaseStatsSO>(SlimeStatsPath);
        BaseStatsSO created = ScriptableObject.CreateInstance<BaseStatsSO.EnemyStatsSO>();
        created.unitType = UnitType.Enemy;
        created.displayName = "Default Enemy";

        if (slime != null)
        {
            created.maxHealth = slime.maxHealth;
            created.attack = slime.attack;
            created.defense = slime.defense;
            created.moveSpeed = slime.moveSpeed;
            created.attackSpeed = slime.attackSpeed;
            created.dodgeChance = slime.dodgeChance;
            created.healthSteal = slime.healthSteal;
        }
        else
        {
            created.maxHealth = 30f;
            created.attack = 5f;
            created.moveSpeed = 5f;
        }

        AssetDatabase.CreateAsset(created, DefaultStatsPath);
        return created;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static bool ValidateOne(string prefabPath)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            System.Text.StringBuilder fails = new System.Text.StringBuilder();

            EnemyBase enemy = root.GetComponent<EnemyBase>();
            if (enemy == null) fails.Append("EnemyBase ");

            if (root.GetComponent<StatsManager>() == null) fails.Append("StatsManager ");
            if (root.GetComponent<NavMeshAgent>() == null) fails.Append("NavMeshAgent ");

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                fails.Append("Animator+Controller ");
            else if (animator.avatar == null)
                fails.Append("Avatar ");
            else
            {
                AnimatorController ctrl = animator.runtimeAnimatorController as AnimatorController;
                if (ctrl == null && animator.runtimeAnimatorController is AnimatorOverrideController ov)
                    ctrl = ov.runtimeAnimatorController as AnimatorController;
                if (ctrl != null)
                {
                    foreach (ChildAnimatorState child in ctrl.layers[0].stateMachine.states)
                    {
                        if (child.state == null || child.state.name != "Idle") continue;
                        Motion motion = child.state.motion;
                        if (motion == null) break; // snake OK
                        string motionPath = AssetDatabase.GetAssetPath(motion);
                        if (motionPath.IndexOf("animations/Generated", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            fails.Append("IdleStillBob ");
                        else if (motionPath.IndexOf("Fbx_exports", System.StringComparison.OrdinalIgnoreCase) < 0)
                            fails.Append("IdleNotFbx ");
                        break;
                    }
                }
            }

            if (root.GetComponentInChildren<EnemyHitbox>() == null) fails.Append("EnemyHitbox ");
            if (root.GetComponentInChildren<EnemyAggroCheck>() == null) fails.Append("EnemyAggroCheck ");
            if (root.GetComponentInChildren<EnemyStrikeDistanceCheck>() == null)
                fails.Append("EnemyStrikeDistanceCheck ");

            // Detection spheres must be triggers or OnTriggerEnter never fires.
            foreach (EnemyAggroCheck aggro in root.GetComponentsInChildren<EnemyAggroCheck>())
            {
                SphereCollider col = aggro.GetComponent<SphereCollider>();
                if (col == null || !col.isTrigger)
                    fails.Append("AggroTrigger ");
            }

            foreach (EnemyStrikeDistanceCheck strike in root.GetComponentsInChildren<EnemyStrikeDistanceCheck>())
            {
                SphereCollider col = strike.GetComponent<SphereCollider>();
                if (col == null || !col.isTrigger)
                    fails.Append("StrikeTrigger ");
            }

            if (fails.Length > 0)
            {
                Debug.LogError($"EnemyPrefabSetupGenerator: FAIL '{name}' — missing: {fails}");
                return false;
            }

            Debug.Log($"EnemyPrefabSetupGenerator: PASS '{name}'");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
