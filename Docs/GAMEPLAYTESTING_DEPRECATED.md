# GamePlayTesting — do not use for combat

`Assets/Scenes/GamePlayTesting.unity` is **deprecated** for gameplay / combat / movement QA.

- No baked NavMesh (`NavMeshSettings.m_NavMeshData` is null), so `NavMeshAgent` characters cannot path.
- The scene Player still uses legacy `Player_Move` instead of `PlayerMove` + `PlayerStateMachine`.

Use `hub` or a forest room from Build Settings instead. See `Docs/DESIGNER_GUIDE.md` §1 Scenes.
