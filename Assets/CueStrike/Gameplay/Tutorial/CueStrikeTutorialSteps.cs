using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Tutorial
{
    /// <summary>
    /// Defines tutorial steps for 8-Ball and 9-Ball game modes.
    /// Contains step data, instructions, and validation criteria.
    /// </summary>
    public static class CueStrikeTutorialSteps
    {
        public enum TutorialMode
        {
            EightBall = 0,
            NineBall = 1
        }

        public enum StepType
        {
            Instruction,      // Text/instruction only
            Interactive,      // Requires player action
            Demonstration,    // Show how to do something
            Practice,         // Free practice with guidance
            Validation       // Validate player performed action correctly
        }

        /// <summary>
        /// Represents a single tutorial step.
        /// </summary>
        [Serializable]
        public class TutorialStep
        {
            public int stepIndex;
            public string title;
            public string description;
            public string detailedInstruction;
            public StepType stepType;
            public TutorialMode mode;
            public string targetObjectName;      // GameObject to highlight
            public Vector3 highlightPosition;    // World position for arrow/indicator
            public float highlightRadius = 1.0f;
            public bool requireShot = false;
            public int requiredBallId = -1;      // Specific ball to pot (if applicable)
            public int requiredPocketIndex = -1; // Specific pocket (if applicable)
            public bool requireLegalShot = true; // Must be legal per WPA rules
            public string successMessage;
            public string failureMessage;
            public float timeLimit = 0f;         // 0 = no limit
            public bool autoAdvance = false;     // Auto-advance on success
            public Action onStepStart;
            public Action onStepComplete;
            public Action onStepFail;
        }

        /// <summary>
        /// Gets all 8-Ball tutorial steps.
        /// </summary>
        public static List<TutorialStep> GetEightBallSteps()
        {
            return new List<TutorialStep>
            {
                new TutorialStep
                {
                    stepIndex = 0,
                    title = "Welcome to 8-Ball",
                    description = "Learn the basics of 8-Ball pool following WPA rules.",
                    detailedInstruction = "Welcome to the 8-Ball tutorial! In this game, you'll play with 15 object balls (1-15) and a cue ball. Balls 1-7 are solids, 9-15 are stripes, and the 8-ball is black. The goal is to pocket all your group balls, then legally pocket the 8-ball to win.",
                    stepType = StepType.Instruction,
                    mode = TutorialMode.EightBall,
                    successMessage = "Let's start learning!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 1,
                    title = "Table Overview",
                    description = "Familiarize yourself with the pool table layout.",
                    detailedInstruction = "This is a standard 9-foot pool table. There are 6 pockets - one at each corner and one at the midpoint of each long rail. The head spot (where you break from) is at the top. The foot spot (where the rack goes) is at the bottom. The center spot is in the middle.",
                    stepType = StepType.Instruction,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "PoolTable",
                    highlightPosition = new Vector3(0, 0.85f, 0),
                    highlightRadius = 2.5f,
                    successMessage = "Good! Now let's learn cue control.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 2,
                    title = "Cue Ball Control - Aiming",
                    description = "Learn how to aim your shots.",
                    detailedInstruction = "Move your mouse (or VR controller) to aim. A preview line shows the cue ball's path. The white circle shows where the cue ball will contact the object ball. Try aiming at different balls.",
                    stepType = StepType.Interactive,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.0f),
                    highlightRadius = 0.3f,
                    requireShot = false,
                    successMessage = "Great aim! Now let's add power.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 3,
                    title = "Cue Ball Control - Power & Spin",
                    description = "Learn to control shot power and apply spin.",
                    detailedInstruction = "Pull back on the mouse (or VR controller) to increase power. The power meter shows your shot strength. Press A/D keys (or use VR touchpad) to apply left/right spin (english). Top spin = follow, back spin = draw. Try a shot with medium power.",
                    stepType = StepType.Interactive,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = 1,
                    successMessage = "Excellent! You made your first shot.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 4,
                    title = "The Break Shot",
                    description = "Learn the rules for a legal break in 8-Ball.",
                    detailedInstruction = "The break is the first shot of the frame. Place the cue ball anywhere behind the head string (the 'kitchen'). A legal break requires: (1) pocket a ball, OR (2) drive at least 4 object balls to cushions. If you pocket the 8-ball on break, it's spotted and you continue. Try a legal break now.",
                    stepType = StepType.Practice,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.8f),
                    highlightRadius = 0.5f,
                    requireShot = true,
                    requiredBallId = -1, // Any ball
                    successMessage = "Legal break! The table is now open.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 5,
                    title = "Open Table & Ball Assignment",
                    description = "Understand how ball groups (solids/stripes) are assigned.",
                    detailedInstruction = "After the break, the table is 'open' - no groups assigned yet. The first player to legally pocket a ball claims that group (solids 1-7 or stripes 9-15). The opponent gets the other group. Pocket a solid or stripe to claim your group.",
                    stepType = StepType.Practice,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = -1, // Any object ball
                    successMessage = "Group assigned! You are now solids/stripes.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 6,
                    title = "Call Shot Rule",
                    description = "Learn the WPA call shot requirement.",
                    detailedInstruction = "In WPA 8-Ball, you must call your shot for non-obvious shots. This means stating which ball you'll pocket and in which pocket. Obvious shots (straight in, close) don't require calling. For this tutorial, try to pocket a specific ball in a specific pocket.",
                    stepType = StepType.Validation,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -0.5f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = 2,
                    requiredPocketIndex = 3, // Example: top right pocket
                    requireLegalShot = true,
                    successMessage = "Perfect call shot!",
                    failureMessage = "Remember to call your shot - ball and pocket!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 7,
                    title = "Legal Shots & Common Fouls",
                    description = "Learn what makes a shot legal or a foul.",
                    detailedInstruction = "A legal shot requires: (1) Hit your group ball first (or 8-ball if group cleared), (2) After contact, any ball must hit a cushion OR a ball must be pocketed. Common fouls: scratching (cue ball pocketed), wrong ball first, no cushion after contact, ball off table. Try a legal shot with your group ball.",
                    stepType = StepType.Practice,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, 0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = -1, // Any of your group
                    requireLegalShot = true,
                    successMessage = "Legal shot! Your turn continues.",
                    failureMessage = "That was a foul. Remember the rules!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 8,
                    title = "Winning the Frame - The 8-Ball",
                    description = "Learn how to legally win by pocketing the 8-ball.",
                    detailedInstruction = "Once all your group balls are pocketed, you can shoot for the 8-ball. You MUST: (1) Call the pocket for the 8-ball, (2) Hit the 8-ball first, (3) Pocket it in the called pocket. If you pocket the 8-ball early, in the wrong pocket, or scratch on the 8-ball shot, you LOSE the frame. Pocket the 8-ball legally to win!",
                    stepType = StepType.Validation,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, 0.5f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = 8,
                    requiredPocketIndex = 4, // Example: bottom right pocket
                    requireLegalShot = true,
                    successMessage = "Congratulations! You won the frame!",
                    failureMessage = "Be careful! Wrong 8-ball shot loses the frame.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 9,
                    title = "Practice Frame",
                    description = "Play a complete practice frame against AI.",
                    detailedInstruction = "Now play a full 8-Ball frame! Apply everything you've learned: legal break, claim your group, call shots, avoid fouls, and pocket the 8-ball to win. Good luck!",
                    stepType = StepType.Practice,
                    mode = TutorialMode.EightBall,
                    targetObjectName = "PoolTable",
                    highlightPosition = new Vector3(0, 0.85f, 0),
                    highlightRadius = 2.5f,
                    requireShot = false,
                    successMessage = "Tutorial complete! You're ready to play 8-Ball.",
                    autoAdvance = false
                }
            };
        }

        /// <summary>
        /// Gets all 9-Ball tutorial steps.
        /// </summary>
        public static List<TutorialStep> GetNineBallSteps()
        {
            return new List<TutorialStep>
            {
                new TutorialStep
                {
                    stepIndex = 0,
                    title = "Welcome to 9-Ball",
                    description = "Learn the basics of 9-Ball pool following WPA rules.",
                    detailedInstruction = "Welcome to the 9-Ball tutorial! This fast-paced game uses balls 1-9 plus the cue ball. The balls must be hit in numerical order (lowest first), but ANY ball can be pocketed. The player who legally pockets the 9-ball wins the frame. Let's begin!",
                    stepType = StepType.Instruction,
                    mode = TutorialMode.NineBall,
                    successMessage = "Let's start learning!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 1,
                    title = "Table Overview",
                    description = "Familiarize yourself with the 9-Ball rack and table.",
                    detailedInstruction = "9-Ball uses a diamond-shaped rack with the 1-ball at the front (on the foot spot) and the 9-ball in the center. The other balls are placed randomly. The break is from behind the head string, same as 8-Ball.",
                    stepType = StepType.Instruction,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "PoolTable",
                    highlightPosition = new Vector3(0, 0.85f, 0),
                    highlightRadius = 2.5f,
                    successMessage = "Good! Now let's learn the most important rule.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 2,
                    title = "The Golden Rule - Lowest Ball First",
                    description = "You MUST hit the lowest numbered ball on the table first.",
                    detailedInstruction = "This is the core rule of 9-Ball: the cue ball must contact the lowest numbered ball remaining on the table FIRST. You can pocket ANY ball (including the 9-ball) as long as you hit the lowest ball first. This enables combination shots and 'slop' pots. Try hitting the 1-ball first.",
                    stepType = StepType.Validation,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = 1,
                    requireLegalShot = true,
                    successMessage = "Perfect! You hit the lowest ball first.",
                    failureMessage = "You must hit the lowest numbered ball first!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 3,
                    title = "The Break & Push-Out Rule",
                    description = "Learn the break requirements and unique push-out option.",
                    detailedInstruction = "The break requires: pocket a ball OR drive 4 balls to cushions. If legal break with no ball pocketed, the incoming player gets a 'Push-Out' option. During push-out: you can shoot anywhere (no need to hit lowest ball), no cushion requirement. But cue ball scratch = foul. Opponent then chooses to shoot or make you shoot. Try a break shot.",
                    stepType = StepType.Practice,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.8f),
                    highlightRadius = 0.5f,
                    requireShot = true,
                    requiredBallId = -1,
                    successMessage = "Break complete! Push-out available.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 4,
                    title = "Push-Out Practice",
                    description = "Try the push-out option.",
                    detailedInstruction = "Since no ball was pocketed on the break, you have the push-out option. You can: (1) Play a normal shot (hit lowest ball first), or (2) Declare push-out and shoot anywhere. During push-out, any ball pocketed stays down (except 9-ball which is spotted). Try a push-out shot.",
                    stepType = StepType.Practice,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -1.0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = -1,
                    successMessage = "Push-out executed! Turn passes to opponent.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 5,
                    title = "Legal Shots & Ball-in-Hand",
                    description = "Learn legal shot requirements and ball-in-hand after fouls.",
                    detailedInstruction = "After push-out, normal play resumes. Legal shot: hit lowest ball first, then any ball must hit cushion OR a ball pocketed. ANY foul gives opponent ball-in-hand ANYWHERE on the table. Common fouls: wrong ball first, no cushion, scratch, ball off table. Try a legal shot hitting the lowest ball.",
                    stepType = StepType.Validation,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, -0.5f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = -1, // Will validate lowest ball hit first
                    requireLegalShot = true,
                    successMessage = "Legal shot! Ball-in-hand on foul is powerful.",
                    failureMessage = "Foul! Opponent gets ball-in-hand anywhere.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 6,
                    title = "Winning - The 9-Ball",
                    description = "Learn how to win by pocketing the 9-ball legally.",
                    detailedInstruction = "The 9-ball can be pocketed at ANY time - even on the break! You just need to hit the lowest ball first. If the 9-ball is the last ball, pocket it legally to win. If pocketed on a foul, it's spotted and opponent gets ball-in-hand. Try a combination shot: hit lowest ball into the 9-ball.",
                    stepType = StepType.Validation,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "CueBall",
                    highlightPosition = new Vector3(0, 0.85f, 0f),
                    highlightRadius = 0.3f,
                    requireShot = true,
                    requiredBallId = 9,
                    requireLegalShot = true,
                    successMessage = "Excellent! You pocketed the 9-ball legally!",
                    failureMessage = "Remember: hit lowest ball first, then 9-ball.",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 7,
                    title = "Three Consecutive Fouls = Loss",
                    description = "Learn the three-foul rule unique to 9-Ball.",
                    detailedInstruction = "In 9-Ball, if a player commits THREE consecutive fouls in a single frame, they LOSE the frame immediately. This counter resets after a legal shot. This rule prevents intentional fouling. Be careful with your shot selection!",
                    stepType = StepType.Instruction,
                    mode = TutorialMode.NineBall,
                    successMessage = "Remember: three fouls and you're out!",
                    autoAdvance = true
                },
                new TutorialStep
                {
                    stepIndex = 8,
                    title = "Practice Frame",
                    description = "Play a complete practice frame against AI.",
                    detailedInstruction = "Now play a full 9-Ball frame! Apply everything: legal break, push-out strategy, lowest ball first, combinations on the 9-ball, ball-in-hand tactics, and avoid three fouls. The first to legally pocket the 9-ball wins!",
                    stepType = StepType.Practice,
                    mode = TutorialMode.NineBall,
                    targetObjectName = "PoolTable",
                    highlightPosition = new Vector3(0, 0.85f, 0),
                    highlightRadius = 2.5f,
                    requireShot = false,
                    successMessage = "Tutorial complete! You're ready to play 9-Ball.",
                    autoAdvance = false
                }
            };
        }

        /// <summary>
        /// Gets tutorial steps for a specific mode.
        /// </summary>
        public static List<TutorialStep> GetSteps(TutorialMode mode)
        {
            return mode == TutorialMode.EightBall ? GetEightBallSteps() : GetNineBallSteps();
        }

        /// <summary>
        /// Gets total step count for a mode.
        /// </summary>
        public static int GetStepCount(TutorialMode mode)
        {
            return GetSteps(mode).Count;
        }
    }
}