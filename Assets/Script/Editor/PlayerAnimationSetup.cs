/*
First Date: 13/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: The one tool for the player's animations. For a new character: Tools > Player > Add Character Model, then
              Tools > Player > Add First Person Hands, then Tools > Player > Set Up Player Animations (GameScene open).
              Run it again whenever you add Mixamo files (to Assets/Imported3D/Mixamo, e.g. mining), rebuild the character
              or the hands, or the animations look out of step. It only puts things right: running it twice changes nothing.
              Hand-made steps: Docs/Mixamo Animations.md
Purpose: Puts the player's animations into their correct setting, in one click:
         1. Mixamo files (picked by words in their names, the Uses table, e.g. BotPunchingLeft = the left jab): Humanoid
            import (Avatar: Create From This Model), the clip named after the file, played once, root motion baked in.
            Only files from Mixamo (their animation is called "mixamo.com"): other packs' files with those words in
            their names, like Blink's GetHit and MiningLoop, are left alone.
         2. Both controllers, the body's (PlayerCharacter, Actions layer) and the first-person hands' (FirstPersonHands):
            each action state gets its Mixamo clip (new actions get their state, trigger and arrows), retired states go
            (HeavyUppercut), and EVERY action plays at PlayerAnimator.ActionSpeedOf, takes over in ActionBlendSeconds and
            starts again on a new click, so the hands and the body's shadow move together.
         3. The first-person arms: cut at the elbow and drawn from both sides (FirstPersonArmsHelper): no open shoulder,
            no see-through holes.
         4. The Player: PunchCombo (left click jabs, right click heavy punches), and PunchEffect's delays measured from
            the clips (when each fist or tool lands). Components whose script was deleted (e.g. the old ToolEquip) are
            taken off the Player.
         5. The bow (22/11/2026): a Mixamo file with "ShootingArrow" in its name (BotShootingArrow: reach to the quiver,
            nock, draw, aim, shoot, lower; 5 s) is cut into BowDraw (from the arrow nocked at the hip to full draw),
            BowAim (one frame of full draw, held still), BowRelease (the shot and the arms coming down), and for the
            first-person hands BowReady (one frame of the bow coming up in front: their pose while it's only held, so
            the bow shows) and BowReadyDraw (from there to full draw, no dip to the hip). A Mixamo file with "AimIdle"
            (BotStandingAimIdle01: holding the drawn bow, breathing, a loop) is the hold instead of the frozen frame, and
            the draw then ends where its hands are; one with "Overdraw" (BotStandingAimOverdraw: the string pulled
            further back) follows after holding PlayerAnimator.BowOverdrawAfter seconds. Where to cut is measured on the
            character on every run (the file is cut again only when that changes): the aim = the bow hand up at the
            head and the string hand at the face, the release = the end of the aim. Both controllers get the bow
            states and the BowDraw / BowRelease / BowCancel triggers (PlayerAnimator); the hands also get BowIdle and
            the BowHeld bool (FirstPersonHands sets it). BowDraw plays at the speed that makes it last BowShooter's Draw
            Seconds.
         One file can play several actions: BotAxeAttackDownward is the swing of every tool (Attack and Mine).
         Tools > Items > Build Bow And Arrows runs the file part of this too (FixAnimationFiles).
Requirement: PlayerAnimationSetup.cs x-> AnimatorBuildHelper.cs, FirstPersonArmsHelper.cs, PunchEffectBuilder.cs, PlayerAnimator.cs, PunchEffect.cs, PunchCombo.cs, PlayerInventory.cs, BowShooter.cs, UIBuildHelper.cs, MenuOrder.cs
             PlayerAnimationSetup.cs <-x BowBuilder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;   // AnimationClipPlayable, AnimationPlayableOutput (measuring the bow clip)
using UnityEngine.Playables;    // PlayableGraph
using static AnimatorBuildHelper;   // AddState, Instant, AfterPlaying
using static UIBuildHelper;         // AddToPlayer, FindPrefabPath

public static class PlayerAnimationSetup
{
    // ===== Fields =====
    const string ToolName = "Set Up Player Animations";
    static readonly string[] SearchFolders = { "Assets/Imported3D", "Assets/Animation" };   // Mixamo files: Assets/Imported3D/Mixamo
    const string MixamoTake = "mixamo.com";   // what Mixamo calls the animation in every file it gives you
    const string MixamoFolder = "/Mixamo/";   // where the guide puts them (Assets/Imported3D/Mixamo)
    const string BodyControllerPath = "Assets/Animation/Player/PlayerCharacter.controller";    // Add Character Model
    const string HandsControllerPath = "Assets/Animation/Player/FirstPersonHands.controller";  // Add First Person Hands
    static string HandsPrefabPath => FindPrefabPath("Assets/Prefabs/Player/FirstPersonHands.prefab");

    // A word in a Mixamo file's name -> the actions it plays. Read top to bottom: an earlier word wins an action, so a
    // clip made for one tool (a future "Mining" file) comes before the shared one (AxeAttack).
    static readonly (string word, PlayerAction[] actions)[] Uses =
    {
        ("LeftHook", new[] { PlayerAction.HeavyHook }),                   // BotHeavyLeftHook: right click (1st, 3rd...)
        ("HeavyPunch", new[] { PlayerAction.HeavyPunch }),                // BotHeavyPunchRight: right click (2nd, 4th...)
        ("PunchingLeft", new[] { PlayerAction.PunchLeft }),               // BotPunchingLeft: left click (1st, 3rd...)
        ("PunchingRight", new[] { PlayerAction.Punch }),                  // BotPunchingRight: left click (2nd, 4th...)
        ("GetHit", new[] { PlayerAction.Hit }),                           // BotGetHitBody: the flinch when hurt (body only)
        ("Mining", new[] { PlayerAction.Mine }),                          // a pickaxe-only swing (none yet)
        ("Slash", new[] { PlayerAction.Attack }),                         // a sword / axe-only swing (none yet)
        ("SwordSwing", new[] { PlayerAction.Attack }),
        ("AxeAttack", new[] { PlayerAction.Attack, PlayerAction.Mine }),  // BotAxeAttackDownward: every tool's swing
    };

    // Actions that were dropped: their states and triggers are taken out of the controllers.
    // 13/11/2026: right click's left punch is now the left hook; tools no longer play the Unarmed equip animations.
    static readonly string[] Retired = { "HeavyUppercut", "EquipUnderarm", "EquipOverShoulder" };

    // The bow: words in the Mixamo files' names, the clips cut from the shooting file, and the times (seconds in
    // BotShootingArrow) used when measuring can't tell: the arrow nocked at the hip 1.8, the bow up in front 2.2, full
    // draw 3.35 (2.65 = as far apart as BotStandingAimIdle01), the shot 4.0 (of 5).
    const string BowWord = "ShootingArrow";
    const string AimIdleWord = "AimIdle";     // holding the drawn bow, breathing (BotStandingAimIdle01): the hold
    const string OverdrawWord = "Overdraw";   // the drawn string pulled further back (BotStandingAimOverdraw)
    public const string BowDrawClip = "BowDraw";
    public const string BowAimClip = PlayerAnimator.BowAimStateName;
    public const string BowReleaseClip = "BowRelease";
    public const string BowReadyClip = "BowReady";           // the first-person hands holding the bow up, ready (one frame)
    public const string BowReadyDrawClip = "BowReadyDraw";   // their draw: from there to full draw
    const float UsualDrawFrom = 1.8f, UsualReady = 2.2f, UsualFullDraw = 3.35f, UsualAimIdleDraw = 2.65f, UsualRelease = 4f;
    const float ReadyShare = 0.6f;  // the ready pose: the bow hand 60 % of the way up from the hip to full draw
    const float SampleRate = 30f;   // looks per second when measuring (Mixamo's clips are 30 frames a second)

    class BowClips { public AnimationClip draw, aim, release, ready, readyDraw, aimLoop, overdraw; }
    struct BowTimes { public float from, ready, hold, release; }   // seconds in the shooting file

    // PunchEffect's delay field for each action it times.
    static readonly (PlayerAction action, string field)[] Delays =
    {
        (PlayerAction.Punch, "impactDelay"), (PlayerAction.PunchLeft, "leftJabDelay"),
        (PlayerAction.HeavyHook, "hookDelay"), (PlayerAction.HeavyPunch, "heavyPunchDelay"),
        (PlayerAction.Attack, "swingDelay"), (PlayerAction.Mine, "mineDelay"),
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Player/Set Up Player Animations", false, MenuOrder.SetUpPlayerAnimations)]
    static void Setup()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(BodyControllerPath) == null &&
            AssetDatabase.LoadAssetAtPath<AnimatorController>(HandsControllerPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no player character yet.\n\nRun Tools > Player > Add Character Model (and " +
                                        "Add First Person Hands) first, then this.", "OK");
            return;
        }

        List<string> report = FixAnimationFiles(out Dictionary<PlayerAction, AnimationClip> clips);
        report.Add(SetUpPlayer(clips));
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ":\n- " + string.Join("\n- ", report.FindAll(line => !string.IsNullOrEmpty(line))) +
                  "\nSave the scene, then Play: left click jabs, right click heavy punches; with a tool, click to swing it (every hit bursts wind).");
    }

    // ===== My methods =====

    // Everything this tool does to files (the Mixamo clips, the bow's clips, both controllers, the first-person arms),
    // without asking and without the scene part. Tools > Items > Build Bow And Arrows uses it. Returns report lines.
    public static List<string> FixAnimationFiles()
    {
        return FixAnimationFiles(out _);
    }

    static List<string> FixAnimationFiles(out Dictionary<PlayerAction, AnimationClip> clips)
    {
        List<string> report = new List<string>();
        clips = PrepareMixamoClips(report);
        BowClips bow = PrepareBowClips(report);

        report.Add("body controller: " + FixController(BodyControllerPath, clips, bow, true));
        report.Add("hands controller: " + FixController(HandsControllerPath, clips, bow, false));
        AssetDatabase.SaveAssets();

        report.Add(FixArms());
        AssetDatabase.SaveAssets();
        return report;
    }

    // ----- 1. Mixamo files -----

    // Every Mixamo file the Uses table knows, made Humanoid with its clip named after the file. Action -> clip.
    static Dictionary<PlayerAction, AnimationClip> PrepareMixamoClips(List<string> report)
    {
        Dictionary<PlayerAction, AnimationClip> clips = new Dictionary<PlayerAction, AnimationClip>();
        List<string> used = new List<string>();
        List<string> problems = new List<string>();
        Dictionary<string, AnimationClip> prepared = new Dictionary<string, AnimationClip>();   // each file only once
        foreach (KeyValuePair<PlayerAction, string> file in FindFiles())
        {
            if (!prepared.TryGetValue(file.Value, out AnimationClip clip))
            {
                clip = PrepareClip(file.Value);
                prepared[file.Value] = clip;
                if (clip == null || !clip.humanMotion) problems.Add(System.IO.Path.GetFileName(file.Value));
            }
            if (clip == null || !clip.humanMotion) continue;
            clips[file.Key] = clip;
            used.Add(clip.name + " = " + file.Key);
        }
        report.Add(used.Count > 0 ? "Mixamo clips: " + string.Join(", ", used) : "no Mixamo files in " + string.Join(" or ", SearchFolders) + " (Blink's clips stay)");
        if (problems.Count > 0)
        {
            Debug.LogWarning(ToolName + ": " + string.Join(", ", problems) + " couldn't be made Humanoid, so they're skipped. Select " +
                             "the file > Rig > Animation Type Humanoid, Avatar Definition Create From This Model > Apply, then run this again.");
        }
        return clips;
    }

    // The model file for each action: the first Mixamo file (IsMixamoFile) whose name has a Uses word, taking the words
    // top to bottom (an action already taken by an earlier word keeps that file).
    static Dictionary<PlayerAction, string> FindFiles()
    {
        Dictionary<PlayerAction, string> files = new Dictionary<PlayerAction, string>();
        string[] folders = System.Array.FindAll(SearchFolders, AssetDatabase.IsValidFolder);
        if (folders.Length == 0) return files;
        List<string> paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", folders)) paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach ((string word, PlayerAction[] actions) in Uses)
        {
            string path = paths.Find(p => System.IO.Path.GetFileNameWithoutExtension(p).ToLower().Contains(word.ToLower()) && IsMixamoFile(p));
            if (path == null) continue;
            foreach (PlayerAction action in actions)
            {
                if (!files.ContainsKey(action)) files[action] = path;
            }
        }
        return files;
    }

    // True for a file that came from Mixamo: its animation is called "mixamo.com" (Mixamo names every one that), or Unity
    // hasn't read its animation yet and it's in a folder named Mixamo. Other packs' animation files sit in Assets/Imported3D
    // too and must be left alone: Blink's GetHit and MiningLoop have the words "GetHit" and "Mining" in their names, and
    // on 22/11/2026 this tool made them "Mixamo clips", kept only their first clip (a one-frame T-pose) and put that in
    // the Hit and Mine states, so every pickaxe swing and every flinch showed a T-pose.
    static bool IsMixamoFile(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return false;
        TakeInfo[] takes = importer.importedTakeInfos ?? new TakeInfo[0];
        foreach (TakeInfo take in takes)
        {
            if (take.name == MixamoTake) return true;
        }
        return takes.Length == 0 && path.Replace('\\', '/').Contains(MixamoFolder);
    }

    // Sets a Mixamo file to Humanoid with one clip named after the file, played once (loop = looping, its ends matched so
    // it loops smoothly: the bow's breathing hold), root motion baked into the pose (In Place: the body stays where the
    // ball is). Returns that clip (null = no animation in the file).
    static AnimationClip PrepareClip(string path, bool loop = false)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return null;
        string name = System.IO.Path.GetFileNameWithoutExtension(path);

        if (importer.animationType != ModelImporterAnimationType.Human || !importer.importAnimation)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();   // read the file again as Humanoid before choosing its clip
        }

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0) return null;
        ModelImporterClipAnimation clip = importer.clipAnimations.Length > 0 ? importer.clipAnimations[0] : defaults[0];
        bool alreadyRight = importer.clipAnimations.Length == 1 && clip.name == name && clip.loopTime == loop && clip.loopPose == loop &&
                            clip.lockRootRotation && clip.lockRootHeightY && clip.lockRootPositionXZ;
        if (!alreadyRight)
        {
            clip.name = name;                 // "mixamo.com" in every file otherwise: the file's name tells them apart
            clip.loopTime = loop;             // a punch plays once; the bow's hold loops
            clip.loopPose = loop;             // "Loop Pose": the end flows into the start, no hitch
            clip.lockRootRotation = true;     // "Bake Into Pose": the clip can't turn or move the character's root
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
        }

        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (part is AnimationClip found && found.name == name) return found;
        }
        return null;
    }

    // ----- 1b. The bow's clips -----

    // The bow's clips: the shooting file cut into its parts (measured on the character on every run, cut again only when
    // that gives other frames, e.g. once an Aim Idle file is added), and the Aim Idle and Overdraw files if they're in
    // the project. null = no shooting file, or it couldn't be used.
    static BowClips PrepareBowClips(List<string> report)
    {
        string path = FindMixamoFile(BowWord);
        if (path == null)
        {
            report.Add("no bow clip (a Mixamo file with \"" + BowWord + "\" in its name, e.g. BotShootingArrow, in Assets/Imported3D/Mixamo)");
            return null;
        }
        string file = System.IO.Path.GetFileName(path);

        // The whole take, named after the file. Only a file that isn't ready yet goes through PrepareClip: on a file that
        // has been cut it would keep one clip and throw the cuts away.
        AnimationClip whole = ClipIn(path, System.IO.Path.GetFileNameWithoutExtension(path)) ?? PrepareClip(path);
        if (whole == null || !whole.humanMotion)
        {
            report.Add("bow: " + file + " couldn't be made Humanoid (select it > Rig > Humanoid > Apply, then run this again)");
            return null;
        }

        AnimationClip aimLoop = HoldClip(AimIdleWord, true);      // the hold, breathing (null = one frame of full draw)
        AnimationClip overdraw = HoldClip(OverdrawWord, false);   // held long: pulled further back (null = none)
        string how = MeasureBow(whole, aimLoop, out BowTimes times);
        bool cutAgain = CutBowClips(path, whole, times);
        BowClips bow = LoadBowClips(path);
        if (bow == null)
        {
            report.Add("bow: couldn't cut " + file + " into its clips");
            return null;
        }
        bow.aimLoop = aimLoop;
        bow.overdraw = overdraw;
        string hold = aimLoop != null ? aimLoop.name + " (looping)" : "one frame of full draw (no Mixamo file with \"" + AimIdleWord + "\")";
        if (overdraw != null) hold += aimLoop != null ? ", then " + overdraw.name + " after " + PlayerAnimator.BowOverdrawAfter + " s"
                                                      : "; " + overdraw.name + " isn't used without an Aim Idle file";
        report.Add("bow: " + file + " = BowDraw " + bow.draw.length.ToString("0.00") + " s, BowRelease " + bow.release.length.ToString("0.00") +
                   " s, the hands' ready pose (" + how + (cutAgain ? "; cut again" : "; cuts unchanged") + "); the hold: " + hold);
        return bow;
    }

    // A Mixamo file with this word in its name, made Humanoid (loop = looping), or null if there's none or it can't be.
    static AnimationClip HoldClip(string word, bool loop)
    {
        string path = FindMixamoFile(word);
        AnimationClip clip = path != null ? PrepareClip(path, loop) : null;
        return clip != null && clip.humanMotion ? clip : null;
    }

    // The first Mixamo file (IsMixamoFile) with this word in its name, or null.
    static string FindMixamoFile(string word)
    {
        string[] folders = System.Array.FindAll(SearchFolders, AssetDatabase.IsValidFolder);
        if (folders.Length == 0) return null;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).ToLower().Contains(word.ToLower()) && IsMixamoFile(path)) return path;
        }
        return null;
    }

    // The animation clip with this name in a model file, or null.
    static AnimationClip ClipIn(string path, string clipName)
    {
        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (part is AnimationClip clip && clip.name == clipName) return clip;
        }
        return null;
    }

    // The parts cut from the shooting file (draw, aim and release at least; the ready ones came 22/11/2026), else null.
    static BowClips LoadBowClips(string path)
    {
        BowClips bow = new BowClips
        {
            draw = ClipIn(path, BowDrawClip),
            aim = ClipIn(path, BowAimClip),
            release = ClipIn(path, BowReleaseClip),
            ready = ClipIn(path, BowReadyClip),
            readyDraw = ClipIn(path, BowReadyDrawClip),
        };
        return bow.draw != null && bow.aim != null && bow.release != null ? bow : null;
    }

    // The file's clips: the whole take (kept, named after the file) and the bow's parts, all with the root baked into the
    // pose like every Mixamo clip here (times in seconds of the whole clip):
    //   BowDraw       from the arrow nocked at the hip to full draw (the body's draw)
    //   BowAim        one frame of full draw, held still (the hold when there's no Aim Idle file)
    //   BowRelease    from the shot to the end (the arms come down)
    //   BowReady      one frame of the bow coming up in front, held still (the first-person hands while it's only held)
    //   BowReadyDraw  from there to full draw (the hands' draw: they start up in front, so there's no dip to the hip)
    // The one-frame clips play once and their state then holds the last frame. They don't loop: a one-frame loop flips
    // between its two frames 30 times a second, and the ready frame is in the middle of the raise (the grip moves 7 cm
    // in that frame), so the bow shook all the time (22/11/2026).
    // Writes them only when they differ from the file's. Returns true if it did.
    static bool CutBowClips(string path, AnimationClip whole, BowTimes times)
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
        ModelImporterClipAnimation[] now = importer.clipAnimations;
        ModelImporterClipAnimation take = System.Array.Find(now, c => c.name == whole.name);
        if (take == null) take = now.Length > 0 ? now[0] : importer.defaultClipAnimations[0];
        float first = take.firstFrame, last = take.lastFrame;
        float perSecond = whole.length > 0f ? (last - first) / whole.length : SampleRate;
        float hold = Mathf.Round(first + times.hold * perSecond);
        float ready = Mathf.Round(first + times.ready * perSecond);

        ModelImporterClipAnimation[] want =
        {
            take,
            BowPart(importer, BowDrawClip, Mathf.Round(first + times.from * perSecond), hold, false),
            BowPart(importer, BowAimClip, hold, hold + 1f, false),                    // one frame, played once = held still
            BowPart(importer, BowReleaseClip, Mathf.Round(first + times.release * perSecond), last, false),
            BowPart(importer, BowReadyClip, ready, ready + 1f, false),
            BowPart(importer, BowReadyDrawClip, ready, hold, false),
        };
        if (SameCuts(now, want)) return false;
        importer.clipAnimations = want;
        importer.SaveAndReimport();
        return true;
    }

    // True if the file already has exactly these clips with these frames (so it needn't be imported again).
    static bool SameCuts(ModelImporterClipAnimation[] now, ModelImporterClipAnimation[] want)
    {
        if (now.Length != want.Length) return false;
        foreach (ModelImporterClipAnimation part in want)
        {
            ModelImporterClipAnimation had = System.Array.Find(now, c => c.name == part.name);
            if (had == null || !Mathf.Approximately(had.firstFrame, part.firstFrame) ||
                !Mathf.Approximately(had.lastFrame, part.lastFrame) || had.loopTime != part.loopTime) return false;
        }
        return true;
    }

    static ModelImporterClipAnimation BowPart(ModelImporter importer, string name, float from, float to, bool loop)
    {
        ModelImporterClipAnimation part = importer.defaultClipAnimations[0];   // a fresh copy of the take's settings
        part.name = name;
        part.firstFrame = from;
        part.lastFrame = Mathf.Max(from + 1f, to);
        part.loopTime = loop;
        part.lockRootRotation = true;
        part.keepOriginalOrientation = true;
        part.lockRootHeightY = true;
        part.keepOriginalPositionY = true;
        part.lockRootPositionXZ = true;
        part.keepOriginalPositionXZ = true;
        return part;
    }

    // Where to cut, measured by playing the whole clip on a hidden copy of the character (like PunchEffectBuilder's
    // MeasureAction) and watching both hands and the head, all measured against the head's height:
    //   aiming    = the bow (left) hand up near the head's height and the string (right) hand at the face;
    //   release   = the end of the longest aiming stretch (the string hand leaves the face);
    //   draw from = where the bow hand is lowest in the 1.2 s before the aim (the arrow nocked at the hip);
    //   full draw = with an Aim Idle clip, the first moment the hands are as far apart as in its first frame, so the draw
    //               flows straight into its breathing; without one, the first moment in the aim they're 97 % as far
    //               apart as they get (not in its last 0.15 s: the shot itself pulls the hands apart);
    //   ready     = the first moment the bow hand has come Ready Share (60 %) of the way up from the hip to full draw.
    // Returns how it went, for the Console; the usual times when it can't tell.
    static string MeasureBow(AnimationClip clip, AnimationClip aimLoop, out BowTimes times)
    {
        times = new BowTimes { from = UsualDrawFrom, ready = UsualReady, hold = aimLoop != null ? UsualAimIdleDraw : UsualFullDraw, release = UsualRelease };
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath("Assets/Prefabs/Player/PlayerCharacter.prefab"));
        if (model == null) return "no PlayerCharacter to measure on, the usual times";

        GameObject copy = Object.Instantiate(model);
        copy.hideFlags = HideFlags.HideAndDontSave;   // never shown, never saved
        PlayableGraph graph = PlayableGraph.Create("Measure bow");
        try
        {
            Animator animator = copy.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman) return "the character isn't Humanoid, the usual times";
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (left == null || right == null || head == null) return "the character has no hands or head, the usual times";
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // it's hidden; animate it anyway

            AnimationClipPlayable play = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "bow", animator);
            output.SetSourcePlayable(play);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            Transform root = animator.transform;
            int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * SampleRate)) + 1;
            Vector3[] l = new Vector3[frames], r = new Vector3[frames], h = new Vector3[frames];
            for (int i = 0; i < frames; i++)
            {
                play.SetTime(Mathf.Min(clip.length, i / SampleRate));
                graph.Evaluate();
                l[i] = root.InverseTransformPoint(left.position);
                r[i] = root.InverseTransformPoint(right.position);
                h[i] = root.InverseTransformPoint(head.position);
            }

            // How far apart the hands are where the Aim Idle clip starts (its breathing begins there).
            float aimApart = 0f;
            if (aimLoop != null)
            {
                AnimationClipPlayable aimPlay = AnimationClipPlayable.Create(graph, aimLoop);
                output.SetSourcePlayable(aimPlay);
                aimPlay.SetTime(0);
                graph.Evaluate();
                aimApart = Vector3.Distance(root.InverseTransformPoint(left.position), root.InverseTransformPoint(right.position));
            }

            float tall = Mathf.Max(0.0001f, h[0].y);   // the head's height: the measuring stick
            int start = -1, run = 0;
            for (int i = 0; i < frames;)
            {
                if (!(l[i].y >= h[i].y - 0.2f * tall && Vector3.Distance(r[i], h[i]) <= 0.2f * tall))
                {
                    i++;
                    continue;
                }
                int j = i;
                while (j < frames && l[j].y >= h[j].y - 0.2f * tall && Vector3.Distance(r[j], h[j]) <= 0.2f * tall) j++;
                if (j - i > run)
                {
                    run = j - i;
                    start = i;
                }
                i = j;
            }
            if (run < 6) return "no aiming pose in " + clip.name + ", the usual times";

            int end = start + run - 1;
            int searchTo = Mathf.Max(start, end - 5);   // not the last 0.15 s: the shot itself pulls the hands apart
            int from = start;
            float lowest = float.MaxValue;
            for (int i = Mathf.Max(0, start - 36); i < start; i++)
            {
                if (l[i].y >= lowest) continue;
                lowest = l[i].y;
                from = i;
            }

            int full = -1;
            for (int i = from + 1; aimApart > 0f && i <= searchTo && full < 0; i++)
            {
                if (Vector3.Distance(l[i], r[i]) >= aimApart * 0.98f) full = i;
            }
            bool matched = full >= 0;
            if (!matched)
            {
                float apart = 0f;
                for (int i = start; i <= searchTo; i++) apart = Mathf.Max(apart, Vector3.Distance(l[i], r[i]));
                full = start;
                for (int i = start; i <= searchTo; i++)
                {
                    if (Vector3.Distance(l[i], r[i]) < apart * 0.97f) continue;
                    full = i;
                    break;
                }
            }

            int ready = from;
            float readyHeight = l[from].y + ReadyShare * (l[full].y - l[from].y);
            for (int i = from; i <= full; i++)
            {
                if (l[i].y < readyHeight) continue;
                ready = i;
                break;
            }

            BowTimes measured = new BowTimes { from = from / SampleRate, ready = ready / SampleRate, hold = full / SampleRate, release = end / SampleRate };
            if (!(measured.from <= measured.ready && measured.ready < measured.hold && measured.hold < measured.release))
                return "odd times measured in " + clip.name + ", the usual times";
            times = measured;
            return "measured: nocked " + measured.from.ToString("0.00") + " s, ready " + measured.ready.ToString("0.00") + " s, full draw " +
                   measured.hold.ToString("0.00") + " s" + (matched ? " (as far apart as " + aimLoop.name + ")" : "") + ", shot " +
                   measured.release.ToString("0.00") + " s";
        }
        catch (System.Exception e)
        {
            return "couldn't measure " + clip.name + " (" + e.Message + "), the usual times";
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(copy);
        }
    }

    // ----- 2. The controllers -----

    // One controller put right: retired states out, Mixamo clips in (new actions get their state), then every action
    // state's speed and arrows set to the shared timing. body = PlayerCharacter (Actions layer, back to Empty, not while
    // dead); else the hands (base layer, back to Idle, no flinch). Returns a short report.
    static string FixController(string path, Dictionary<PlayerAction, AnimationClip> clips, BowClips bow, bool body)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) return "not found (" + (body ? "Add Character Model" : "Add First Person Hands") + " makes it)";

        AnimatorStateMachine machine = null;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            if (!body || layer.name == PlayerAnimator.ActionLayerName)
            {
                machine = layer.stateMachine;
                break;
            }
        }
        if (machine == null) return "no Actions layer: rebuild it with Add Character Model";

        int removed = RemoveRetired(controller, machine);
        AnimatorState back = FindState(machine, body ? PlayerAnimator.EmptyStateName : "Idle");
        int filled = 0;
        foreach (KeyValuePair<PlayerAction, AnimationClip> pair in clips)
        {
            if (!body && pair.Key == PlayerAction.Hit) continue;
            string name = pair.Key.ToString();
            AnimatorState state = FindState(machine, name);
            if (state == null) state = AddAction(controller, machine, name, body, back);
            state.motion = pair.Value;
            EditorUtility.SetDirty(state);
            filled++;
        }

        string bowStates = FixBow(controller, machine, body, back, bow);
        int timed = 0;
        foreach (AnimatorControllerLayer layer in controller.layers) timed += SyncTiming(layer.stateMachine);
        EditorUtility.SetDirty(controller);
        return filled + " Mixamo clips, " + timed + " timing fixes" + (removed > 0 ? ", " + removed + " retired states removed" : "") + bowStates;
    }

    // The bow's states and their arrows, made again on every run (so a change here reaches old controllers):
    //   Any State -> BowDraw (BowDraw trigger; not while dead) -> BowAim once fully drawn (the clip's end)
    //   BowAim -> BowOverdraw after PlayerAnimator.BowOverdrawAfter seconds (with the Aim Idle and Overdraw files)
    //   BowDraw, BowAim or BowOverdraw -> BowRelease (BowRelease trigger) -> back once played
    //   BowDraw, BowAim or BowOverdraw -> back (BowCancel trigger: lowered without shooting)
    // The first-person hands (once the ready clips are cut) also get BowIdle, the bow held up in front: Idle <-> BowIdle
    // by the BowHeld bool, "back" is BowIdle while the bow is still held, and their BowDraw starts there (BowReadyDraw).
    static string FixBow(AnimatorController controller, AnimatorStateMachine machine, bool body, AnimatorState back, BowClips bow)
    {
        if (bow == null) return "";
        foreach (PlayerAction trigger in new[] { PlayerAction.BowDraw, PlayerAction.BowRelease, PlayerAction.BowCancel })
        {
            AddParameter(controller, trigger.ToString(), AnimatorControllerParameterType.Trigger);
        }
        bool ready = !body && back != null && bow.ready != null && bow.readyDraw != null;
        if (ready) AddParameter(controller, PlayerAnimator.BowHeldName, AnimatorControllerParameterType.Bool);

        float x = body ? 900f : 860f;
        AnimatorState draw = FindState(machine, BowDrawClip) ?? AddState(machine, BowDrawClip, null, new Vector3(x, 0f));
        AnimatorState aim = FindState(machine, BowAimClip) ?? AddState(machine, BowAimClip, null, new Vector3(x, 80f));
        AnimatorState release = FindState(machine, BowReleaseClip) ?? AddState(machine, BowReleaseClip, null, new Vector3(x, 160f));
        AnimatorState overdraw = Wanted(machine, PlayerAnimator.BowOverdrawStateName, bow.overdraw != null && bow.aimLoop != null, new Vector3(x + 230f, 80f));
        AnimatorState idle = Wanted(machine, PlayerAnimator.BowIdleStateName, ready, new Vector3(x - 230f, 80f));

        AnimationClip drawClip = ready ? bow.readyDraw : bow.draw;
        draw.motion = drawClip;
        draw.speed = drawClip.length / DrawSeconds();   // lasts BowShooter's Draw Seconds
        aim.motion = bow.aimLoop != null ? bow.aimLoop : bow.aim;
        aim.speed = 1f;
        release.motion = bow.release;
        release.speed = PlayerAnimator.BowReleaseSpeed;
        if (overdraw != null)
        {
            overdraw.motion = bow.overdraw;
            overdraw.speed = 1f;
        }
        if (idle != null)
        {
            idle.motion = bow.ready;
            idle.speed = 1f;
        }

        List<AnimatorState> mine = new List<AnimatorState> { draw, aim, release };
        if (overdraw != null) mine.Add(overdraw);
        if (idle != null) mine.Add(idle);
        foreach (AnimatorStateTransition old in machine.anyStateTransitions)
        {
            if (mine.Contains(old.destinationState)) machine.RemoveAnyStateTransition(old);
        }
        foreach (AnimatorState state in mine)
        {
            foreach (AnimatorStateTransition old in state.transitions) state.RemoveTransition(old);
        }
        if (back != null)
        {
            foreach (AnimatorStateTransition old in back.transitions)
            {
                if (mine.Contains(old.destinationState)) back.RemoveTransition(old);
            }
        }

        AnimatorStateTransition start = machine.AddAnyStateTransition(draw);
        start.AddCondition(AnimatorConditionMode.If, 0f, PlayerAction.BowDraw.ToString());
        if (body) start.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
        start.hasExitTime = false;
        start.hasFixedDuration = true;
        start.duration = PlayerAnimator.BowDrawBlendSeconds;
        start.canTransitionToSelf = true;   // drawn again right after a shot: start over

        AnimatorStateTransition drawn = draw.AddTransition(aim);   // fully drawn: hold it there
        drawn.hasExitTime = true;
        drawn.exitTime = 1f;
        drawn.hasFixedDuration = true;
        drawn.duration = bow.aimLoop != null ? 0.1f : 0f;   // into the breathing (another file): a short blend

        if (overdraw != null)
        {
            AnimatorStateTransition longer = aim.AddTransition(overdraw);   // held a long time: pull further back
            longer.hasExitTime = true;
            longer.exitTime = PlayerAnimator.BowOverdrawAfter / Mathf.Max(0.1f, bow.aimLoop.length);
            longer.hasFixedDuration = true;
            longer.duration = 0.3f;
        }

        List<AnimatorState> drawnStates = new List<AnimatorState> { draw, aim };
        if (overdraw != null) drawnStates.Add(overdraw);
        foreach (AnimatorState state in drawnStates)
        {
            BowArrow(state, release, PlayerAction.BowRelease, 0.05f);
            if (idle != null)
            {
                BowArrow(state, idle, PlayerAction.BowCancel, 0.15f).AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.BowHeldName);
                BowArrow(state, back, PlayerAction.BowCancel, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.BowHeldName);
            }
            else if (back != null) BowArrow(state, back, PlayerAction.BowCancel, 0.15f);
        }
        if (idle != null)
        {
            // After the shot the bow comes back up to the ready pose early (it doesn't drop out of view first), or down
            // if it was put away. A bow taken in hand comes up; put away, it goes.
            AfterPlaying(release, idle, 0.3f, 0.25f).AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.BowHeldName);
            AfterPlaying(release, back, 0.85f, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.BowHeldName);
            BoolArrow(back, idle, true, 0.2f);
            BoolArrow(idle, back, false, 0.15f);
        }
        else if (back != null) AfterPlaying(release, back, 0.85f, 0.15f);
        foreach (AnimatorState state in mine) EditorUtility.SetDirty(state);
        return ", bow states (draw at " + draw.speed.ToString("0.00") + "x" + (bow.aimLoop != null ? ", breathing hold" : "") +
               (overdraw != null ? ", overdraw" : "") + (idle != null ? ", ready pose while held" : "") + ")";
    }

    // An arrow between two states that waits for nothing but its trigger.
    static AnimatorStateTransition BowArrow(AnimatorState from, AnimatorState to, PlayerAction trigger, float blendSeconds)
    {
        AnimatorStateTransition arrow = from.AddTransition(to);
        arrow.AddCondition(AnimatorConditionMode.If, 0f, trigger.ToString());
        arrow.hasExitTime = false;
        arrow.hasFixedDuration = true;
        arrow.duration = blendSeconds;
        return arrow;
    }

    // An arrow that waits for nothing but the BowHeld bool (on = a bow taken in hand, off = put away).
    static void BoolArrow(AnimatorState from, AnimatorState to, bool on, float blendSeconds)
    {
        AnimatorStateTransition arrow = from.AddTransition(to);
        arrow.AddCondition(on ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, PlayerAnimator.BowHeldName);
        arrow.hasExitTime = false;
        arrow.hasFixedDuration = true;
        arrow.duration = blendSeconds;
    }

    // A state that should be there (found, or made) or not (taken out if an earlier run made it). Null when not wanted.
    static AnimatorState Wanted(AnimatorStateMachine machine, string name, bool wanted, Vector3 position)
    {
        AnimatorState state = FindState(machine, name);
        if (wanted) return state ?? AddState(machine, name, null, position);
        if (state != null) machine.RemoveState(state);
        return null;
    }

    static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    // BowShooter's Draw Seconds on the open scene's Player, else its default.
    static float DrawSeconds()
    {
        BowShooter shooter = Object.FindFirstObjectByType<BowShooter>();
        SerializedProperty seconds = shooter != null ? new SerializedObject(shooter).FindProperty("drawSeconds") : null;
        return seconds != null ? Mathf.Max(0.1f, seconds.floatValue) : BowShooter.DefaultDrawSeconds;
    }

    // A new action state like the builders make: its trigger, an arrow from Any State and an arrow back once it played.
    static AnimatorState AddAction(AnimatorController controller, AnimatorStateMachine machine, string name, bool body, AnimatorState back)
    {
        bool hasTrigger = false;
        foreach (AnimatorControllerParameter p in controller.parameters) hasTrigger |= p.name == name;
        if (!hasTrigger) controller.AddParameter(name, AnimatorControllerParameterType.Trigger);

        AnimatorState state = AddState(machine, name, null, new Vector3(body ? 600f : 560f, 70f * machine.states.Length));
        AnimatorStateTransition arrow = machine.AddAnyStateTransition(state);
        arrow.AddCondition(AnimatorConditionMode.If, 0f, name);
        if (body) arrow.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.DeadName);
        Instant(arrow, PlayerAnimator.ActionBlendSeconds);
        if (back != null) AfterPlaying(state, back, 0.85f, 0.15f);
        return state;   // its speed and restarting are set by SyncTiming
    }

    // Every action state (named like a PlayerAction) plays at PlayerAnimator.ActionSpeedOf, and its Any State arrow
    // takes over in ActionBlendSeconds and may start it again (a new click). Returns how many parts it changed.
    static int SyncTiming(AnimatorStateMachine machine)
    {
        int changed = 0;
        foreach (ChildAnimatorState child in machine.states)
        {
            if (!IsAction(child.state, out PlayerAction action) || PlayerAnimator.IsBowAction(action)) continue;   // the bow's own speeds (FixBow)
            float speed = PlayerAnimator.ActionSpeedOf(action);
            if (Mathf.Approximately(child.state.speed, speed)) continue;
            child.state.speed = speed;
            EditorUtility.SetDirty(child.state);
            changed++;
        }
        foreach (AnimatorStateTransition arrow in machine.anyStateTransitions)
        {
            if (!IsAction(arrow.destinationState, out PlayerAction to) || PlayerAnimator.IsBowAction(to)) continue;
            if (arrow.canTransitionToSelf && !arrow.hasExitTime && arrow.hasFixedDuration &&
                Mathf.Approximately(arrow.duration, PlayerAnimator.ActionBlendSeconds)) continue;
            arrow.hasExitTime = false;
            arrow.hasFixedDuration = true;   // the blend time in seconds, not a share of the animation
            arrow.duration = PlayerAnimator.ActionBlendSeconds;
            arrow.canTransitionToSelf = true;
            EditorUtility.SetDirty(arrow);
            changed++;
        }
        foreach (ChildAnimatorStateMachine inner in machine.stateMachines) changed += SyncTiming(inner.stateMachine);   // state groups
        return changed;
    }

    // Takes dropped actions' states (with the arrows to them) and triggers out. Returns how many states went.
    static int RemoveRetired(AnimatorController controller, AnimatorStateMachine machine)
    {
        int removed = 0;
        foreach (string name in Retired)
        {
            AnimatorState state = FindState(machine, name);
            if (state != null)
            {
                machine.RemoveState(state);
                removed++;
            }
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name != name) continue;
                controller.RemoveParameter(p);
                break;
            }
        }
        return removed;
    }

    static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == name) return child.state;
        }
        return null;
    }

    // True if the state is named exactly like one of the PlayerActions (e.g. "Punch").
    static bool IsAction(AnimatorState state, out PlayerAction action)
    {
        action = PlayerAction.Hit;
        if (state == null) return false;
        foreach (PlayerAction a in System.Enum.GetValues(typeof(PlayerAction)))
        {
            if (state.name != a.ToString()) continue;
            action = a;
            return true;
        }
        return false;
    }

    // ----- 3. The first-person arms -----

    static string FixArms()
    {
        string path = HandsPrefabPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return "first-person arms: no hands yet (Tools > Player > Add First Person Hands)";

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            string result = FirstPersonArmsHelper.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return "first-person arms: " + result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ----- 4. The Player -----

    // PunchCombo on the Player, and PunchEffect's delays measured from the clips.
    static string SetUpPlayer(Dictionary<PlayerAction, AnimationClip> clips)
    {
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return "no Player in the open scene: open GameScene and run this again for PunchCombo and the punch timing";

        AddToPlayer<PunchCombo>();
        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(player.gameObject);
        if (missing > 0)
        {
            Undo.RegisterCompleteObjectUndo(player.gameObject, ToolName);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player.gameObject);   // e.g. the old ToolEquip
        }
        string timing = "";
        PunchEffect effect = player.GetComponent<PunchEffect>();
        if (effect != null)
        {
            SerializedObject so = new SerializedObject(effect);
            List<string> measured = new List<string>();
            foreach ((PlayerAction action, string field) in Delays)
            {
                if (!clips.ContainsKey(action)) continue;
                float? delay = PunchEffectBuilder.MeasureAction(action, out string how);
                SerializedProperty property = so.FindProperty(field);
                if (delay.HasValue && property != null)
                {
                    property.floatValue = delay.Value;
                    measured.Add(action + " " + delay.Value.ToString("0.00") + " s");
                }
                else
                {
                    measured.Add(action + ": " + how);
                }
            }
            so.ApplyModifiedProperties();   // recorded for Undo
            if (measured.Count > 0) timing = "; punches land after: " + string.Join(", ", measured);
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        return "the Player has PunchCombo" + timing + (missing > 0 ? "; " + missing + " component(s) with a deleted script taken off it" : "");
    }
}
