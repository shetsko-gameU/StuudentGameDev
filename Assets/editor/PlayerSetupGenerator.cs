using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot project setup for the player side of the state machine and animation work.
///
/// Three jobs, all of which are fiddly and error-prone to do by hand and easy to get subtly
/// wrong in a way nobody notices for weeks (which is roughly how the animation wiring ended
/// up broken in the first place):
///
///   1. Adds the Speed / Grounded / Dead parameters from PlayerAnimatorParams to both player
///      Animator Controllers, so PlayerStateMachine's writes actually land.
///   2. Builds Assets/UI/DeathScreen.prefab - the death screen that PlayerDeathHandler has
///      always tried to show but which never existed.
///   3. Adds PlayerStateMachine to both player prefabs and points PlayerDeathHandler at the
///      death screen prefab.
///
/// Run it from the menu: Tools > Player > Run Player Setup.
/// Or headless: Unity -batchmode -quit -executeMethod PlayerSetupGenerator.Run
/// Safe to re-run; every step checks for existing work first.
/// </summary>
public static class PlayerSetupGenerator
{
    private const string DeathScreenPrefabPath = "Assets/UI/DeathScreen.prefab";

    private static readonly string[] PlayerPrefabs =
    {
        "Assets/Player/Player.prefab",
        "Assets/Player/Player_Wizard.prefab",
    };

    private static readonly string[] PlayerControllers =
    {
        "Assets/Player/Player_Animation/Player.controller",
        "Assets/Player/Player_Animation/Player_Wizard.controller",
    };

    [MenuItem("Tools/Player/Run Player Setup")]
    public static void Run()
    {
        AddAnimatorParameters();
        WireLocomotion();
        GameObject deathScreen = BuildDeathScreenPrefab();
        WirePlayerPrefabs(deathScreen);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PlayerSetupGenerator: finished.");
    }

    /// <summary>
    /// Legacy placeholder locomotion. Skipped when Idle already uses an Fbx_exports clip —
    /// re-running must not overwrite baked skeletal takes with mesh bob.
    /// </summary>
    private static void WireLocomotion()
    {
        if (ControllerIdleIsFbx("Assets/Player/Player_Animation/Player.controller"))
        {
            Debug.Log("PlayerSetupGenerator: skipping placeholder locomotion — Player.controller already uses Fbx_exports.");
            return;
        }

        const string folder = "Assets/Player/Player_Animation";

        // Binding path "mesh" matches the child name on Player.prefab. Animating the Animator
        // root would fight NavMeshAgent; animating mesh keeps locomotion visual-only.
        AnimationClip idle = CreatePlayerIdleClip($"{folder}/PlayerIdle.anim", "mesh");
        AnimationClip walk = CreatePlayerWalkClip($"{folder}/PlayerWalk.anim", "mesh");
        AnimationClip death = CreatePlayerDeathClip($"{folder}/PlayerDeath.anim", "mesh");

        WireControllerLocomotion(
            "Assets/Player/Player_Animation/Player.controller",
            idle, walk, death,
            defaultStateName: "Idle",
            attackTrigger: "Attack");

        WireControllerLocomotion(
            "Assets/Player/Player_Animation/Player_Wizard.controller",
            idle, walk, death,
            defaultStateName: "New State",
            attackTrigger: "WizardAttack");
    }

    private static bool ControllerIdleIsFbx(string controllerPath)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return false;
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state == null || child.state.name != "Idle") continue;
            Motion motion = child.state.motion;
            if (motion == null) return false;
            string path = AssetDatabase.GetAssetPath(motion);
            return path.IndexOf("Fbx_exports", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
        return false;
    }

    private static AnimationClip CreatePlayerIdleClip(string assetPath, string bindingPath)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f, name = "PlayerIdle" };

        // Soft vertical breathe. Enough to prove Idle is playing instead of a dead T-pose bind.
        clip.SetCurve(bindingPath, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1.2f, 0.04f),
                new Keyframe(2.4f, 0f)));

        // Slight torso twist so idle doesn't read as a frozen statue.
        clip.SetCurve(bindingPath, typeof(Transform), "localEulerAnglesRaw.y",
            new AnimationCurve(
                new Keyframe(0f, -2f),
                new Keyframe(1.2f, 2f),
                new Keyframe(2.4f, -2f)));

        return SaveClip(clip, assetPath, loop: true);
    }

    private static AnimationClip CreatePlayerWalkClip(string assetPath, string bindingPath)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f, name = "PlayerWalk" };

        clip.SetCurve(bindingPath, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.08f),
                new Keyframe(0.3f, 0f),
                new Keyframe(0.45f, 0.08f),
                new Keyframe(0.6f, 0f)));

        clip.SetCurve(bindingPath, typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, -3f),
                new Keyframe(0.3f, 3f),
                new Keyframe(0.6f, -3f)));

        return SaveClip(clip, assetPath, loop: true);
    }

    private static AnimationClip CreatePlayerDeathClip(string assetPath, string bindingPath)
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f, name = "PlayerDeath" };

        clip.SetCurve(bindingPath, typeof(Transform), "m_LocalPosition.y",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, -0.35f)));

        clip.SetCurve(bindingPath, typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, 70f)));

        return SaveClip(clip, assetPath, loop: false);
    }

    private static AnimationClip SaveClip(AnimationClip clip, string assetPath, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    private static void WireControllerLocomotion(string controllerPath,
                                                 AnimationClip idle,
                                                 AnimationClip walk,
                                                 AnimationClip death,
                                                 string defaultStateName,
                                                 string attackTrigger)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"PlayerSetupGenerator: missing controller at '{controllerPath}'.");
            return;
        }

        AddParameterIfMissing(controller, PlayerAnimatorParams.Speed, AnimatorControllerParameterType.Float);
        AddParameterIfMissing(controller, PlayerAnimatorParams.Grounded, AnimatorControllerParameterType.Bool);
        AddParameterIfMissing(controller, PlayerAnimatorParams.Dead, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = FindState(stateMachine, defaultStateName)
                               ?? FindState(stateMachine, "Idle");
        if (idleState == null)
        {
            idleState = stateMachine.AddState("Idle");
            stateMachine.defaultState = idleState;
        }

        // Rename wizard's empty "New State" so the graph is readable.
        if (idleState.name != "Idle")
            idleState.name = "Idle";

        idleState.motion = idle;
        stateMachine.defaultState = idleState;

        AnimatorState walkState = FindState(stateMachine, "Walk");
        if (walkState == null)
            walkState = stateMachine.AddState("Walk");
        walkState.motion = walk;

        AnimatorState deathState = FindState(stateMachine, "Death");
        if (deathState == null)
            deathState = stateMachine.AddState("Death");
        deathState.motion = death;

        EnsureSpeedTransitions(idleState, walkState);
        EnsureDeathTransition(stateMachine, deathState);

        // Attacks must be reachable from Walk as well as Idle, otherwise swinging while
        // moving does nothing once locomotion is live.
        EnsureAttackFromState(walkState, stateMachine, attackTrigger);

        EditorUtility.SetDirty(controller);
        Debug.Log($"PlayerSetupGenerator: locomotion wired on '{System.IO.Path.GetFileName(controllerPath)}'.");
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state != null && child.state.name == name)
                return child.state;
        }

        return null;
    }

    private static void EnsureSpeedTransitions(AnimatorState idleState, AnimatorState walkState)
    {
        if (!HasFloatCondition(idleState, walkState, PlayerAnimatorParams.Speed, greater: true))
        {
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, PlayerAnimatorParams.Speed);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
        }

        if (!HasFloatCondition(walkState, idleState, PlayerAnimatorParams.Speed, greater: false))
        {
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, PlayerAnimatorParams.Speed);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.1f;
        }
    }

    private static void EnsureDeathTransition(AnimatorStateMachine stateMachine, AnimatorState deathState)
    {
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState == deathState)
                return;
        }

        AnimatorStateTransition anyToDeath = stateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimatorParams.Dead);
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.05f;
        anyToDeath.canTransitionToSelf = false;
    }

    private static void EnsureAttackFromState(AnimatorState fromState,
                                              AnimatorStateMachine stateMachine,
                                              string attackTrigger)
    {
        AnimatorState attackState = FindState(stateMachine, "PlayerAttack")
                                 ?? FindState(stateMachine, "PlayerWizardAttack")
                                 ?? FindState(stateMachine, "Sword_Swing");
        if (attackState == null || fromState == null) return;

        foreach (AnimatorStateTransition existing in fromState.transitions)
        {
            if (existing.destinationState == attackState)
                return;
        }

        AnimatorStateTransition transition = fromState.AddTransition(attackState);
        transition.AddCondition(AnimatorConditionMode.If, 0f, attackTrigger);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
    }

    private static bool HasFloatCondition(AnimatorState from, AnimatorState to, string parameter, bool greater)
    {
        foreach (AnimatorStateTransition transition in from.transitions)
        {
            if (transition.destinationState != to) continue;

            foreach (AnimatorCondition condition in transition.conditions)
            {
                bool modeMatch = greater
                    ? condition.mode == AnimatorConditionMode.Greater
                    : condition.mode == AnimatorConditionMode.Less;

                if (modeMatch && condition.parameter == parameter)
                    return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------ 1. Animator parameters

    /// <summary>
    /// Adds the contract parameters to each player controller if they are missing.
    ///
    /// Only parameters are touched. States, transitions and clips are left completely alone,
    /// because those carry the hand-authored combo work and are not ours to regenerate -
    /// unlike the enemy controllers, which had no content at all.
    /// </summary>
    private static void AddAnimatorParameters()
    {
        foreach (string path in PlayerControllers)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Debug.LogWarning($"PlayerSetupGenerator: no Animator Controller at '{path}'. Skipped.");
                continue;
            }

            AddParameterIfMissing(controller, PlayerAnimatorParams.Speed, AnimatorControllerParameterType.Float);
            AddParameterIfMissing(controller, PlayerAnimatorParams.Grounded, AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, PlayerAnimatorParams.Dead, AnimatorControllerParameterType.Bool);

            EditorUtility.SetDirty(controller);
            Debug.Log($"PlayerSetupGenerator: parameters ensured on '{System.IO.Path.GetFileName(path)}'.");
        }
    }

    private static void AddParameterIfMissing(AnimatorController controller,
                                              string name,
                                              AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter existing in controller.parameters)
        {
            if (existing.name == name) return;
        }

        controller.AddParameter(name, type);
    }

    // ------------------------------------------------------------------ 2. Death screen

    private static GameObject BuildDeathScreenPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/UI"))
            AssetDatabase.CreateFolder("Assets", "UI");

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DeathScreenPrefabPath);
        if (existing != null)
        {
            Debug.Log("PlayerSetupGenerator: death screen prefab already exists, left as-is.");
            return existing;
        }

        GameObject root = new GameObject("DeathScreen",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DeathScreenController));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Well above anything else so the death screen is never hidden behind gameplay HUD.
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateFullScreenBackdrop(root.transform);
        CreateLabel(root.transform, "Title", "YOU DIED", new Vector2(0f, 220f), new Vector2(900f, 160f), 96);

        Button restart = CreateButton(root.transform, "RestartButton", "Restart Run", new Vector2(0f, 20f));
        Button hub = CreateButton(root.transform, "HubButton", "Return to Hub", new Vector2(0f, -70f));
        Button menu = CreateButton(root.transform, "MainMenuButton", "Main Menu", new Vector2(0f, -160f));

        DeathScreenController controller = root.GetComponent<DeathScreenController>();
        controller.restartButton = restart;
        controller.hubButton = hub;
        controller.mainMenuButton = menu;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DeathScreenPrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"PlayerSetupGenerator: created '{DeathScreenPrefabPath}'.");
        return prefab;
    }

    private static void CreateFullScreenBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("Backdrop", typeof(Image));
        backdrop.transform.SetParent(parent, false);

        Image image = backdrop.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        // Stretch to fill the canvas whatever the resolution.
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateLabel(Transform parent, string name, string content,
                                    Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject label = new GameObject(name, typeof(Text));
        label.transform.SetParent(parent, false);

        Text text = label.GetComponent<Text>();
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;

        // The built-in legacy font. Using it avoids adding a TextMeshPro dependency to a
        // prefab that has to work in every scene, including ones with no TMP assets set up.
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.16f, 0.18f, 0.95f);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 72f);

        Text text = CreateLabel(buttonObject.transform, "Label", label, Vector2.zero, new Vector2(420f, 72f), 32);

        // Stretch the label to the button so it stays centred if the button is resized.
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        return button;
    }

    // ------------------------------------------------------------------ 3. Player prefabs

    private static void WirePlayerPrefabs(GameObject deathScreenPrefab)
    {
        foreach (string prefabPath in PlayerPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                if (root.GetComponent<PlayerStateMachine>() == null)
                {
                    root.AddComponent<PlayerStateMachine>();
                    Debug.Log($"PlayerSetupGenerator: added PlayerStateMachine to '{System.IO.Path.GetFileName(prefabPath)}'.");
                }

                PlayerDeathHandler deathHandler = root.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null && deathHandler.deathScreenPrefab == null)
                {
                    deathHandler.deathScreenPrefab = deathScreenPrefab;
                    Debug.Log($"PlayerSetupGenerator: assigned the death screen to '{System.IO.Path.GetFileName(prefabPath)}'.");
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
