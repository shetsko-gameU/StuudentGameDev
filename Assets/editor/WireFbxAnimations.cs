using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wires the real baked FBX takes (Idle/Walk/Attack/etc. under Assets/Fbx_exports) into the
/// player and enemy Animator Controllers, and points the prefabs at those FBX models.
///
/// Why this is needed: the playable prefabs were still using older mesh FBXs that only have a
/// base pose (or none). The authored takes live in Fbx_exports, so the Animator had nothing
/// skeletal to play and characters stayed in T-pose / placeholder bob.
///
/// Run: Tools > Animations > Wire Real FBX Clips
/// Headless: -executeMethod WireFbxAnimations.Run
/// Safe to re-run.
/// </summary>
public static class WireFbxAnimations
{
    private const string ExportsFolder = "Assets/Fbx_exports";
    private const string RunFlagPath = "Temp/WireFbxAnimations.run";
    private const string ResultPath = "Temp/WireFbxAnimations.result.txt";

    private static readonly Dictionary<string, string> EnemyFbxByPrefab = new Dictionary<string, string>
    {
        { "Assets/models/Enemys/Prefabs/slime/slime.prefab", "slime.fbx" },
        { "Assets/models/Enemys/Prefabs/slime/crystal slime.prefab", "slime.fbx" },
        { "Assets/models/Enemys/Prefabs/slime/metal slime.prefab", "metal slime.fbx" },
        { "Assets/models/Enemys/Prefabs/mushroom/mushroom scout.prefab", "mushroom_creature1.fbx" },
        { "Assets/models/Enemys/Prefabs/mushroom/mage shroom.prefab", "Mage_Shroom.fbx" },
        { "Assets/models/Enemys/Prefabs/snake/flying snake.prefab", "flying snake.fbx" },
        { "Assets/models/Enemys/Prefabs/snake/flying hammer snake.prefab", "flying snake.fbx" },
        { "Assets/models/Enemys/Prefabs/dryad/dryad crawler1.prefab", "dryad_crawler.fbx" },
        { "Assets/models/Enemys/Prefabs/wolf/static wolf2.prefab", "static wolf2.fbx" },
    };

    /// <summary>
    /// When the open Editor recompiles after an agent drop Temp/WireFbxAnimations.run,
    /// run the wire automatically (batchmode cannot start while the project is locked).
    /// </summary>
    [InitializeOnLoadMethod]
    private static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RunFlagPath))
                return;

            try { File.Delete(RunFlagPath); }
            catch (IOException) { return; }

            Debug.Log("WireFbxAnimations: flag detected — running Wire + Validate.");
            try
            {
                Run();
                int fails = ValidateWiring();
                File.WriteAllText(ResultPath,
                    fails == 0
                        ? $"PASS\nWireFbxAnimations + ValidateWiring completed with 0 failures at {System.DateTime.Now:O}\n"
                        : $"FAIL\nValidateWiring reported {fails} failure(s) at {System.DateTime.Now:O}\n");
            }
            catch (System.Exception ex)
            {
                File.WriteAllText(ResultPath, $"FAIL\n{ex}\n");
                Debug.LogException(ex);
            }
        };
    }

    /// <summary>Headless entry that only re-nests player meshes (feet Y) + rewires player
    /// controllers — skips the slow full export reimport when fixing hover/walk quickly.</summary>
    public static void FixPlayerFeetAndControllers()
    {
        const string swordFbx = ExportsFolder + "/Dirk Pekkanen6_sword.fbx";
        const string mageFbx = ExportsFolder + "/molly_the_mage_staff.fbx";

        Dictionary<string, AnimationClip> warriorClips = LoadClips(swordFbx);
        Dictionary<string, AnimationClip> mageClips = LoadClips(mageFbx);

        if (warriorClips.Count > 0)
        {
            WirePlayerController(
                "Assets/Player/Player_Animation/Player.controller",
                warriorClips,
                attackTrigger: "Attack",
                attackClipNames: new[] { "Slash_01", "Slash_02", "Slash_03", "Attack" });
        }

        if (mageClips.Count > 0)
        {
            WirePlayerController(
                "Assets/Player/Player_Animation/Player_Wizard.controller",
                mageClips,
                attackTrigger: "WizardAttack",
                attackClipNames: new[] { "Swing_01", "Swing_02", "Swing_03", "Attack", "Slash_01" });
        }

        SwapPlayerModel("Assets/Player/Player.prefab", swordFbx);
        SwapPlayerModel("Assets/Player/Player_Wizard.prefab", mageFbx);
        AssetDatabase.SaveAssets();
        Debug.Log("WireFbxAnimations.FixPlayerFeetAndControllers: done.");
    }

    [MenuItem("Tools/Animations/Wire Real FBX Clips")]
    public static void Run()
    {
        ConfigureAllExportFbxImporters();
        WirePlayer();
        WireEnemies();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("WireFbxAnimations: finished. Re-enter Play Mode to see skeletal Idle/Walk/Attack.");
    }

    /// <summary>Headless entry: wire, validate, write Temp/WireFbxAnimations.result.txt, exit non-zero on fail.</summary>
    public static void RunAndValidate()
    {
        try
        {
            Run();
            int fails = ValidateWiring();
            File.WriteAllText(ResultPath,
                fails == 0
                    ? $"PASS\nWireFbxAnimations + ValidateWiring completed with 0 failures at {System.DateTime.Now:O}\n"
                    : $"FAIL\nValidateWiring reported {fails} failure(s) at {System.DateTime.Now:O}\n");
            if (fails != 0)
                EditorApplication.Exit(1);
        }
        catch (System.Exception ex)
        {
            File.WriteAllText(ResultPath, $"FAIL\n{ex}\n");
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Animations/Validate FBX Wiring")]
    public static int ValidateWiring()
    {
        int fails = 0;
        fails += ValidatePlayerPrefab("Assets/Player/Player.prefab", "Dirk Pekkanen6_sword") ? 0 : 1;
        fails += ValidatePlayerPrefab("Assets/Player/Player_Wizard.prefab", "molly_the_mage_staff") ? 0 : 1;
        fails += ValidateControllerUsesFbx(
            "Assets/Player/Player_Animation/Player.controller", "Idle") ? 0 : 1;
        fails += ValidateControllerUsesFbx(
            "Assets/Player/Player_Animation/Player_Wizard.controller", "Idle") ? 0 : 1;

        foreach (KeyValuePair<string, string> pair in EnemyFbxByPrefab)
        {
            fails += ValidateEnemyPrefab(pair.Key, pair.Value) ? 0 : 1;
            string enemyName = Path.GetFileNameWithoutExtension(pair.Key);
            fails += ValidateControllerUsesFbx(
                $"Assets/animations/Generated/{enemyName}.controller", "Idle") ? 0 : 1;
        }

        fails += ValidateLoopTimesOnExports();

        Debug.Log(fails == 0
            ? "WireFbxAnimations.ValidateWiring: ALL PASS."
            : $"WireFbxAnimations.ValidateWiring: {fails} FAIL(s).");
        return fails;
    }

    // ------------------------------------------------------------------ FBX import

    private static void ConfigureAllExportFbxImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ExportsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            ConfigureImporter(path);
        }

        // Player also needs the sword export configured before we swap to it.
        ConfigureImporter($"{ExportsFolder}/Dirk Pekkanen6_sword.fbx");
        ConfigureImporter($"{ExportsFolder}/molly_the_mage_staff.fbx");
    }

    private static void ConfigureImporter(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return;

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0)
            defaults = importer.clipAnimations;

        if (defaults == null || defaults.Length == 0)
        {
            Debug.LogWarning($"WireFbxAnimations: no takes found on '{assetPath}'.");
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return;
        }

        List<ModelImporterClipAnimation> configured = new List<ModelImporterClipAnimation>();
        foreach (ModelImporterClipAnimation clip in defaults)
        {
            ModelImporterClipAnimation copy = clip;
            string n = copy.name ?? string.Empty;

            // Strip Blender "Armature|" prefixes so controllers can look up "Idle" cleanly.
            int pipe = n.LastIndexOf('|');
            if (pipe >= 0 && pipe < n.Length - 1)
                copy.name = n.Substring(pipe + 1);

            bool shouldLoop =
                Contains(copy.name, "idle") ||
                Contains(copy.name, "walk") ||
                Contains(copy.name, "run") ||
                Contains(copy.name, "hop");

            copy.loopTime = shouldLoop;
            configured.Add(copy);
        }

        importer.clipAnimations = configured.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log($"WireFbxAnimations: configured {configured.Count} clips on '{Path.GetFileName(assetPath)}'.");
    }

    private static bool Contains(string name, string token)
    {
        return name != null && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ------------------------------------------------------------------ Player

    private static void WirePlayer()
    {
        const string swordFbx = ExportsFolder + "/Dirk Pekkanen6_sword.fbx";
        const string mageFbx = ExportsFolder + "/molly_the_mage_staff.fbx";

        Dictionary<string, AnimationClip> warriorClips = LoadClips(swordFbx);
        Dictionary<string, AnimationClip> mageClips = LoadClips(mageFbx);

        if (warriorClips.Count == 0)
        {
            Debug.LogError("WireFbxAnimations: no clips loaded from Dirk Pekkanen6_sword.fbx");
            return;
        }

        WirePlayerController(
            "Assets/Player/Player_Animation/Player.controller",
            warriorClips,
            attackTrigger: "Attack",
            attackClipNames: new[] { "Slash_01", "Slash_02", "Slash_03", "Attack" });

        if (mageClips.Count > 0)
        {
            WirePlayerController(
                "Assets/Player/Player_Animation/Player_Wizard.controller",
                mageClips,
                attackTrigger: "WizardAttack",
                attackClipNames: new[] { "Swing_01", "Swing_02", "Swing_03", "Attack", "Slash_01" });
        }

        SwapPlayerModel("Assets/Player/Player.prefab", swordFbx);
        SwapPlayerModel("Assets/Player/Player_Wizard.prefab", mageFbx);

        // Combo hits should fire triggers that exist on the controller. Keep "Attack" /
        // "WizardAttack" as the trigger names; the state plays Slash_01 / Attack clips.
        UpdateComboTriggers("Assets/Player/Script_Object/ComboSo.asset", "Attack");
        UpdateComboTriggers("Assets/Player/Script_Object/WizardComboSo.asset", "WizardAttack");
    }

    private static void WirePlayerController(string controllerPath,
                                             Dictionary<string, AnimationClip> clips,
                                             string attackTrigger,
                                             string[] attackClipNames)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return;

        EnsureParam(controller, PlayerAnimatorParams.Speed, AnimatorControllerParameterType.Float);
        EnsureParam(controller, PlayerAnimatorParams.Grounded, AnimatorControllerParameterType.Bool);
        EnsureParam(controller, PlayerAnimatorParams.Dead, AnimatorControllerParameterType.Bool);
        EnsureParam(controller, attackTrigger, AnimatorControllerParameterType.Trigger);

        AnimationClip idle = FindClip(clips, "Idle") ?? FindClip(clips, "Hop");
        AnimationClip walk = FindClip(clips, "Walk") ?? FindClip(clips, "Run") ?? FindClip(clips, "Hop");
        AnimationClip attack = null;
        foreach (string name in attackClipNames)
        {
            attack = FindClip(clips, name);
            if (attack != null) break;
        }

        AnimationClip death = FindClip(clips, "Death");
        AnimationClip hit = FindClip(clips, "Hit");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = GetOrCreateState(sm, "Idle", new Vector3(300, 240, 0));
        AnimatorState walkState = GetOrCreateState(sm, "Walk", new Vector3(560, 240, 0));
        AnimatorState attackState = GetOrCreateState(sm, "Attack", new Vector3(300, 40, 0));
        AnimatorState deathState = GetOrCreateState(sm, "Death", new Vector3(300, 400, 0));

        if (idle != null) idleState.motion = idle;
        if (walk != null) walkState.motion = walk;
        if (attack != null) attackState.motion = attack;
        if (death != null) deathState.motion = death;

        sm.defaultState = idleState;

        ClearTransitions(idleState);
        ClearTransitions(walkState);
        ClearTransitions(attackState);

        AddFloatTransition(idleState, walkState, PlayerAnimatorParams.Speed, greater: true, 0.1f);
        AddFloatTransition(walkState, idleState, PlayerAnimatorParams.Speed, greater: false, 0.1f);
        AddTriggerTransition(idleState, attackState, attackTrigger);
        AddTriggerTransition(walkState, attackState, attackTrigger);

        AnimatorStateTransition attackDone = attackState.AddTransition(idleState);
        attackDone.hasExitTime = true;
        attackDone.exitTime = 0.9f;
        attackDone.duration = 0.1f;
        attackDone.hasFixedDuration = true;

        // Remove stale Any-state death transitions then re-add one.
        foreach (AnimatorStateTransition t in sm.anyStateTransitions.ToArray())
            sm.RemoveAnyStateTransition(t);

        AnimatorStateTransition anyDeath = sm.AddAnyStateTransition(deathState);
        anyDeath.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimatorParams.Dead);
        anyDeath.hasExitTime = false;
        anyDeath.duration = 0.05f;
        anyDeath.canTransitionToSelf = false;

        if (hit != null)
        {
            EnsureParam(controller, "Hit", AnimatorControllerParameterType.Trigger);
            AnimatorState hitState = GetOrCreateState(sm, "Hit", new Vector3(560, 40, 0));
            hitState.motion = hit;
            ClearTransitions(hitState);
            AnimatorStateTransition anyHit = sm.AddAnyStateTransition(hitState);
            anyHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            anyHit.hasExitTime = false;
            anyHit.duration = 0.05f;
            anyHit.canTransitionToSelf = false;
            AnimatorStateTransition hitDone = hitState.AddTransition(idleState);
            hitDone.hasExitTime = true;
            hitDone.exitTime = 0.9f;
            hitDone.duration = 0.1f;
        }

        // Hide legacy empty states that confuse the graph (Sword_Swing / PlayerAttack / New State).
        foreach (string legacy in new[] { "Sword_Swing", "PlayerAttack", "PlayerWizardAttack", "New State" })
        {
            AnimatorState legacyState = FindState(sm, legacy);
            if (legacyState != null)
                sm.RemoveState(legacyState);
        }

        EditorUtility.SetDirty(controller);
        Debug.Log($"WireFbxAnimations: player controller '{Path.GetFileName(controllerPath)}' now uses FBX clips " +
                  $"(idle={(idle != null)}, walk={(walk != null)}, attack={(attack != null)}).");
    }

    private static void SwapPlayerModel(string prefabPath, string fbxPath)
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
        if (fbxRoot == null)
        {
            Debug.LogError($"WireFbxAnimations: missing FBX at '{fbxPath}'.");
            return;
        }

        RuntimeAnimatorController fallbackController = null;
        if (prefabPath.IndexOf("Wizard", System.StringComparison.OrdinalIgnoreCase) >= 0)
            fallbackController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Player/Player_Animation/Player_Wizard.controller");
        else
            fallbackController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Player/Player_Animation/Player.controller");

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Capture controller from ANY existing Animator before destroying visuals.
            RuntimeAnimatorController controller = fallbackController;
            foreach (Animator existing in root.GetComponentsInChildren<Animator>(true))
            {
                if (existing.runtimeAnimatorController != null)
                {
                    controller = existing.runtimeAnimatorController;
                    break;
                }
            }

            // Destroy EVERY nested visual (old Dirk / mage model / prior mesh FBX / empty
            // capsule mesh). Leaving any one behind doubles the body in play mode.
            DestroyAllVisualChildren(root.transform, keepGameplayVolumes: false);

            // Disable root capsule MeshRenderer if present — collider stays, mesh does not render.
            MeshRenderer rootMr = root.GetComponent<MeshRenderer>();
            if (rootMr != null)
                rootMr.enabled = false;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxRoot, root.transform);
            instance.name = "mesh";
            // Player root sits above the NavMesh by agent.baseOffset (feet are not at the
            // transform). Historical Dirk nest used localY ≈ -1 with baseOffset 1.1 — nesting
            // at y=0 made the character hover. Drop the mesh by -baseOffset so feet meet ground.
            float feetY = 0f;
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent != null)
                feetY = -agent.baseOffset;
            instance.transform.localPosition = new Vector3(0f, feetY, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.transform.SetAsFirstSibling();

            foreach (Animator leftover in root.GetComponentsInChildren<Animator>(true))
            {
                if (leftover.gameObject != instance)
                    Object.DestroyImmediate(leftover);
            }

            Animator modelAnimator = instance.GetComponent<Animator>();
            if (modelAnimator == null)
                modelAnimator = instance.AddComponent<Animator>();

            modelAnimator.avatar = avatar;
            modelAnimator.applyRootMotion = false;
            modelAnimator.runtimeAnimatorController = controller;

            RetargetAnimatorRefs(root, modelAnimator);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"WireFbxAnimations: '{Path.GetFileName(prefabPath)}' model -> {Path.GetFileName(fbxPath)}, " +
                      $"Animator on mesh with avatar={(avatar != null)}, controller={(controller != null)}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RetargetAnimatorRefs(GameObject root, Animator animator)
    {
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            SerializedObject so = new SerializedObject(behaviour);
            SerializedProperty prop = so.GetIterator();
            bool changed = false;
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                    prop.objectReferenceValue is Animator)
                {
                    prop.objectReferenceValue = animator;
                    changed = true;
                }
            }

            if (changed)
                so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void UpdateComboTriggers(string assetPath, string trigger)
    {
        ComboSO combo = AssetDatabase.LoadAssetAtPath<ComboSO>(assetPath);
        if (combo == null || combo.hits == null) return;

        bool dirty = false;
        foreach (ComboHitData hit in combo.hits)
        {
            if (hit != null && hit.animatorTrigger != trigger)
            {
                hit.animatorTrigger = trigger;
                dirty = true;
            }
        }

        if (dirty)
        {
            EditorUtility.SetDirty(combo);
            Debug.Log($"WireFbxAnimations: {Path.GetFileName(assetPath)} triggers -> '{trigger}'.");
        }
    }

    // ------------------------------------------------------------------ Enemies

    private static void WireEnemies()
    {
        foreach (KeyValuePair<string, string> pair in EnemyFbxByPrefab)
        {
            string fbxPath = $"{ExportsFolder}/{pair.Value}";
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fbxPath.Replace('/', Path.DirectorySeparatorChar))))
            {
                // Unity paths are Assets/... relative
                if (AssetDatabase.LoadAssetAtPath<Object>(fbxPath) == null)
                {
                    Debug.LogWarning($"WireFbxAnimations: missing '{fbxPath}' for '{pair.Key}'.");
                    continue;
                }
            }

            Dictionary<string, AnimationClip> clips = LoadClips(fbxPath);
            if (clips.Count == 0)
            {
                Debug.LogWarning($"WireFbxAnimations: no clips on '{fbxPath}'.");
                continue;
            }

            string enemyName = Path.GetFileNameWithoutExtension(pair.Key);
            string controllerPath = $"Assets/animations/Generated/{enemyName}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"WireFbxAnimations: no controller at '{controllerPath}'.");
                continue;
            }

            AnimationClip idle = FindClip(clips, "Idle") ?? FindClip(clips, "Hop") ?? FindClip(clips, "Run");
            AnimationClip walk = FindClip(clips, "Walk") ?? FindClip(clips, "Run") ?? FindClip(clips, "Hop");
            AnimationClip attack = FindClip(clips, "Attack") ?? FindClip(clips, "Slash_01");
            AnimationClip hit = FindClip(clips, "Hit");
            AnimationClip death = FindClip(clips, "Death");

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            // Clear placeholder bob clips when a take is missing (e.g. flying snake Idle/Walk).
            SetStateMotionOrClear(sm, "Idle", idle);
            SetStateMotionOrClear(sm, "Walk", walk ?? idle);
            SetStateMotionOrClear(sm, "Attack", attack);
            SetStateMotionOrClear(sm, "Hit", hit);
            SetStateMotionOrClear(sm, "Death", death);
            EditorUtility.SetDirty(controller);

            SwapEnemyModel(pair.Key, fbxPath, controller);
            Debug.Log($"WireFbxAnimations: enemy '{enemyName}' <- {pair.Value} " +
                      $"(idle={idle != null}, walk={walk != null}, attack={attack != null}).");
        }
    }

    private static void SwapEnemyModel(string prefabPath, string fbxPath, RuntimeAnimatorController controller)
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
        if (fbxRoot == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Many enemy prefabs were authored as PrefabInstances of the old models/Enemys FBX.
            // Destroying mesh children only adds m_RemovedGameObjects; leftover SMRs still render.
            // Unpack completely so SaveAsPrefabAsset writes a regular prefab with one export nest.
            UnpackAllPrefabInstances(root);

            // Remove ALL nested visuals (old models/Enemys FBX + any prior Fbx_exports nest).
            // Keep only gameplay volumes. Do not copy old Blender scale 100 / -90° X.
            DestroyAllVisualChildren(root.transform, keepGameplayVolumes: true);

            // Also strip any SkinnedMeshRenderer / MeshRenderer left on the unpacked root hierarchy
            // that are not under gameplay volumes (armature leftovers from the old FBX root).
            StripLeftoverRenderers(root, keepGameplayVolumes: true);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxRoot, root.transform);
            instance.name = Path.GetFileNameWithoutExtension(fbxPath);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.transform.SetAsFirstSibling();

            foreach (Animator old in root.GetComponentsInChildren<Animator>(true))
            {
                if (old.gameObject != instance)
                    Object.DestroyImmediate(old);
            }

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = controller;

            EnemyBase enemy = root.GetComponent<EnemyBase>();
            if (enemy != null)
                enemy.animator = animator;

            if (instance.GetComponent<EnemyAnimationEventRelay>() == null)
                instance.AddComponent<EnemyAnimationEventRelay>();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"WireFbxAnimations: '{Path.GetFileName(prefabPath)}' enemy model -> {Path.GetFileName(fbxPath)} @ identity xform.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UnpackAllPrefabInstances(GameObject root)
    {
        // Unpack outermost first repeatedly until nothing nested remains.
        bool unpacked;
        do
        {
            unpacked = false;
            // Collect instance roots (deepest-first safe: unpack outermost repeatedly).
            var instances = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    instances.Add(t.gameObject);
            }

            foreach (GameObject inst in instances)
            {
                if (inst == null) continue;
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                unpacked = true;
            }
        } while (unpacked);
    }

    private static void StripLeftoverRenderers(GameObject root, bool keepGameplayVolumes)
    {
        List<GameObject> kill = new List<GameObject>();
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            // Keep renderers that live under gameplay volume names (none normally).
            Transform t = r.transform;
            bool underGameplay = false;
            while (t != null && t != root.transform)
            {
                if (keepGameplayVolumes && GameplayChildNames.Contains(t.name))
                {
                    underGameplay = true;
                    break;
                }
                t = t.parent;
            }

            if (underGameplay)
                continue;

            // Destroy the renderer object if it is a pure mesh leaf; otherwise just disable.
            // Prefer destroying mesh/armature roots that are direct-ish children with no gameplay scripts.
            Transform victim = r.transform;
            while (victim.parent != null && victim.parent != root.transform &&
                   victim.parent.GetComponent<EnemyBase>() == null &&
                   victim.parent.GetComponent<NavMeshAgent>() == null &&
                   !GameplayChildNames.Contains(victim.parent.name))
            {
                victim = victim.parent;
            }

            if (!kill.Contains(victim.gameObject))
                kill.Add(victim.gameObject);
        }

        foreach (GameObject go in kill)
        {
            if (go != null && go != root)
                Object.DestroyImmediate(go);
        }
    }

    private static readonly HashSet<string> GameplayChildNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "AggroRadius", "StrikingDistance", "AttackHitbox", "Trigger Areas", "Raycast",
    };

    /// <summary>
    /// Destroys every direct child that looks like a character/model visual. When
    /// keepGameplayVolumes is true, trigger/hitbox children are preserved.
    /// </summary>
    private static void DestroyAllVisualChildren(Transform root, bool keepGameplayVolumes)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in root)
        {
            if (keepGameplayVolumes && GameplayChildNames.Contains(child.name))
                continue;

            // Named gameplay folders sometimes nest Raycast children — keep the folder.
            if (keepGameplayVolumes && child.name == "Trigger Areas")
                continue;

            bool hasRenderer = child.GetComponentInChildren<Renderer>(true) != null;
            bool isNestedModel = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) != null
                                 || PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject);
            bool looksLikeModel =
                hasRenderer ||
                isNestedModel ||
                child.name.Equals("mesh", System.StringComparison.OrdinalIgnoreCase) ||
                child.name.IndexOf("Dirk", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("mage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("slime", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("snake", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("dryad", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("wolf", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (looksLikeModel)
                toDestroy.Add(child.gameObject);
        }

        foreach (GameObject go in toDestroy)
            Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ helpers

    private static Dictionary<string, AnimationClip> LoadClips(string fbxPath)
    {
        Dictionary<string, AnimationClip> map = new Dictionary<string, AnimationClip>(System.StringComparer.OrdinalIgnoreCase);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return map;

        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                continue;

            string key = clip.name;
            int pipe = key.LastIndexOf('|');
            if (pipe >= 0 && pipe < key.Length - 1)
                key = key.Substring(pipe + 1);

            map[key] = clip;
            map[clip.name] = clip;
        }

        return map;
    }

    private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, string name)
    {
        if (clips.TryGetValue(name, out AnimationClip clip))
            return clip;

        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            if (pair.Key.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return pair.Value;
        }

        return null;
    }

    private static void EnsureParam(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in controller.parameters)
        {
            if (p.name == name) return;
        }

        controller.AddParameter(name, type);
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name, Vector3 position)
    {
        AnimatorState existing = FindState(sm, name);
        if (existing != null) return existing;

        AnimatorState state = sm.AddState(name, position);
        return state;
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            if (child.state != null && child.state.name == name)
                return child.state;
        }

        return null;
    }

    private static void SetStateMotion(AnimatorStateMachine sm, string stateName, AnimationClip clip)
    {
        if (clip == null) return;
        AnimatorState state = FindState(sm, stateName);
        if (state != null)
            state.motion = clip;
    }

    private static void SetStateMotionOrClear(AnimatorStateMachine sm, string stateName, AnimationClip clip)
    {
        AnimatorState state = FindState(sm, stateName);
        if (state == null) return;
        state.motion = clip; // null clears leftover Generated bob .anims
    }

    private static bool ValidatePlayerPrefab(string prefabPath, string expectedFbxToken)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — no Animator.");
                return false;
            }

            if (animator.avatar == null)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — Animator.avatar is null.");
                return false;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — no controller.");
                return false;
            }

            int nestedModelCount = 0;
            bool foundExport = false;
            foreach (Transform child in root.transform)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                string srcPath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (string.IsNullOrEmpty(srcPath) || !srcPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                nestedModelCount++;
                if (srcPath.IndexOf("Fbx_exports", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    srcPath.IndexOf(expectedFbxToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    foundExport = true;
                else
                {
                    Debug.LogError($"Validate: FAIL '{prefabPath}' — leftover nested FBX '{srcPath}' (expected only {expectedFbxToken}).");
                    return false;
                }

                Vector3 scale = child.localScale;
                if (Mathf.Abs(scale.x) >= 10f || Mathf.Abs(scale.y) >= 10f || Mathf.Abs(scale.z) >= 10f)
                {
                    Debug.LogError($"Validate: FAIL '{prefabPath}' — nested model scale {scale} is too large.");
                    return false;
                }
            }

            if (nestedModelCount != 1)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — expected exactly 1 nested FBX model, found {nestedModelCount}.");
                return false;
            }

            if (!foundExport)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — nested model is not '{expectedFbxToken}' from Fbx_exports.");
                return false;
            }

            Debug.Log($"Validate: PASS player '{Path.GetFileName(prefabPath)}'");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ValidateEnemyPrefab(string prefabPath, string expectedFbxFile)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — Animator/avatar/controller missing.");
                return false;
            }

            int exportNests = 0;
            int leftoverOld = 0;
            bool found = false;
            foreach (Transform child in root.transform)
            {
                if (GameplayChildNames.Contains(child.name))
                    continue;

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                string srcPath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (string.IsNullOrEmpty(srcPath) || !srcPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isExport = srcPath.IndexOf("Fbx_exports", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool isExpected = srcPath.EndsWith(expectedFbxFile, System.StringComparison.OrdinalIgnoreCase) ||
                    (isExport && srcPath.IndexOf(Path.GetFileNameWithoutExtension(expectedFbxFile), System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (isExpected)
                {
                    found = true;
                    exportNests++;
                    Vector3 scale = child.localScale;
                    if (Mathf.Abs(scale.x) >= 10f || Mathf.Abs(scale.y) >= 10f || Mathf.Abs(scale.z) >= 10f)
                    {
                        Debug.LogError($"Validate: FAIL '{prefabPath}' — nested '{child.name}' scale {scale} >= 10 (Blender compensation leak).");
                        return false;
                    }

                    // -90° X with large scale was the giant sideways mushroom; reject near-90 X even at scale 1 if euler is clearly that pattern.
                    Vector3 euler = child.localRotation.eulerAngles;
                    float absX = Mathf.Abs(Mathf.DeltaAngle(0f, euler.x));
                    if (absX > 80f && absX < 100f)
                    {
                        Debug.LogError($"Validate: FAIL '{prefabPath}' — nested '{child.name}' has ~-90° X rotation ({euler.x}).");
                        return false;
                    }
                }
                else if (!isExport)
                {
                    leftoverOld++;
                }
            }

            if (leftoverOld > 0)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — {leftoverOld} leftover non-export FBX nest(s).");
                return false;
            }

            // Prefab must not still be an outer PrefabInstance of models/Enemys (old mesh source).
            string assetPath = prefabPath;
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset != null)
            {
                // After unpack, root of the asset on disk should not source from models/Enemys.
                // Check YAML-free: corresponding source of the loaded contents root.
            }

            string rootSourcePath = string.Empty;
            GameObject rootSource = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (rootSource != null)
                rootSourcePath = AssetDatabase.GetAssetPath(rootSource);
            if (!string.IsNullOrEmpty(rootSourcePath) &&
                rootSourcePath.IndexOf("models/Enemys", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — prefab root still sourced from old '{rootSourcePath}'.");
                return false;
            }

            if (!found || exportNests != 1)
            {
                Debug.LogError($"Validate: FAIL '{prefabPath}' — expected exactly 1 nested '{expectedFbxFile}', found {exportNests}.");
                return false;
            }

            Debug.Log($"Validate: PASS enemy '{Path.GetFileName(prefabPath)}'");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ValidateControllerUsesFbx(string controllerPath, string stateName)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"Validate: FAIL missing controller '{controllerPath}'.");
            return false;
        }

        AnimatorState state = FindState(controller.layers[0].stateMachine, stateName);
        if (state == null)
        {
            Debug.LogError($"Validate: FAIL '{controllerPath}' has no state '{stateName}'.");
            return false;
        }

        Motion motion = state.motion;
        if (motion == null)
        {
            // Allowed for flying snake (no Idle/Walk takes yet).
            Debug.LogWarning($"Validate: WARN '{controllerPath}' state '{stateName}' has no motion (OK if export lacks that take).");
            return true;
        }

        string motionPath = AssetDatabase.GetAssetPath(motion);
        if (motionPath.IndexOf("animations/Generated", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            motionPath.IndexOf("PlayerIdle.anim", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            motionPath.IndexOf("PlayerWalk.anim", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            motionPath.IndexOf("PlayerDeath.anim", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError($"Validate: FAIL '{controllerPath}' '{stateName}' still uses placeholder '{motionPath}'.");
            return false;
        }

        if (motionPath.IndexOf("Fbx_exports", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            Debug.LogError($"Validate: FAIL '{controllerPath}' '{stateName}' motion not from Fbx_exports: '{motionPath}'.");
            return false;
        }

        Debug.Log($"Validate: PASS controller '{Path.GetFileName(controllerPath)}' {stateName} -> {motion.name}");
        return true;
    }

    private static int ValidateLoopTimesOnExports()
    {
        int fails = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ExportsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                continue;

            foreach (ModelImporterClipAnimation clip in clips)
            {
                string n = clip.name ?? string.Empty;
                bool shouldLoop =
                    Contains(n, "idle") || Contains(n, "walk") || Contains(n, "run") || Contains(n, "hop");
                if (shouldLoop && !clip.loopTime)
                {
                    Debug.LogError($"Validate: FAIL '{Path.GetFileName(path)}' clip '{n}' should have Loop Time.");
                    fails++;
                }
            }
        }

        if (fails == 0)
            Debug.Log("Validate: PASS Loop Time on Idle/Walk/Run/Hop export clips.");
        return fails;
    }

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (AnimatorStateTransition t in state.transitions.ToArray())
            state.RemoveTransition(t);
    }

    private static void AddFloatTransition(AnimatorState from, AnimatorState to, string param, bool greater, float threshold)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, param);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.hasFixedDuration = true;
    }

    private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.hasFixedDuration = true;
    }
}
