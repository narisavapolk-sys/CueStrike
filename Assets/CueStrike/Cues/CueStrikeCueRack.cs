using UnityEngine;
using CueStrike.Audio;

namespace CueStrike.Cues
{
    /// <summary>
    /// Implements a physical 3D Cue Rack in the scene.
    /// Spawns 3 grabable cue sticks (Maple Wood, Carbon Fiber, Premium Gold).
    /// Gripping a stick off the rack instantly swaps the player's active cue profile.
    /// </summary>
    public class CueStrikeCueRack : MonoBehaviour
    {
        public static CueStrikeCueRack Instance { get; private set; }

        [Header("Cue Profiles")]
        public CueProfile mapleWoodProfile;
        public CueProfile carbonFiberProfile;
        public CueProfile premiumGoldProfile;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildProceduralRack();
        }

        /// <summary>
        /// Spawns a physical wooden cue rack frame with 3 select/grab zones in the world.
        /// </summary>
        private void BuildProceduralRack()
        {
            // 1. Create a backboard stand
            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "RackBackboard";
            backboard.transform.SetParent(transform, false);
            backboard.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            backboard.transform.localScale = new Vector3(0.8f, 1.8f, 0.1f);
            
            var backboardRend = backboard.GetComponent<Renderer>();
            if (backboardRend != null)
            {
                backboardRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                backboardRend.material.color = new Color(0.25f, 0.15f, 0.1f); // Dark mahogany wood
                backboardRend.material.SetFloat("_Smoothness", 0.3f);
            }

            // Create 3 cue stand slots
            CreateCueStandSlot("MapleWoodSlot", new Vector3(-0.2f, 0f, 0.08f), mapleWoodProfile, new Color(0.6f, 0.45f, 0.3f));
            CreateCueStandSlot("CarbonFiberSlot", new Vector3(0f, 0f, 0.08f), carbonFiberProfile, new Color(0.1f, 0.1f, 0.12f));
            CreateCueStandSlot("PremiumGoldSlot", new Vector3(0.2f, 0f, 0.08f), premiumGoldProfile, new Color(0.85f, 0.7f, 0.2f));
        }

        private void CreateCueStandSlot(string name, Vector3 localOffset, CueProfile profile, Color color)
        {
            GameObject slotGO = new GameObject(name);
            slotGO.transform.SetParent(transform, false);
            slotGO.transform.localPosition = localOffset;

            // Visual cue shaft representation
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.transform.SetParent(slotGO.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            shaft.transform.localScale = new Vector3(0.02f, 0.85f, 0.02f);
            
            var rend = shaft.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.material.color = color;
                if (color == new Color(0.85f, 0.7f, 0.2f))
                {
                    rend.material.SetFloat("_Metallic", 0.9f);
                    rend.material.SetFloat("_Smoothness", 0.8f);
                }
            }

            // Add trigger selector collider
            var trigger = slotGO.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            trigger.size = new Vector3(0.15f, 1.8f, 0.15f);

            // Attach selector behavior
            var selector = slotGO.AddComponent<CueSelectTrigger>();
            selector.profileToEquip = profile;
        }
    }

    /// <summary>
    /// Helper component attached to each cue rack slot to detect VR hand select inputs.
    /// </summary>
    public class CueSelectTrigger : MonoBehaviour
    {
        public CueProfile profileToEquip;

        private void OnTriggerEnter(Collider other)
        {
            // Detect VR hand controller collision triggers
            if (other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("controller"))
            {
                EquipCue();
            }
        }

        private void EquipCue()
        {
            if (profileToEquip == null) return;

            var localCue = FindFirstObjectByType<CueStrikeCue>();
            if (localCue != null)
            {
                // CueStrikeCue has no 'length' field — scale the transform Z to match profile length
                localCue.transform.localScale = new Vector3(
                    localCue.transform.localScale.x,
                    localCue.transform.localScale.y,
                    profileToEquip.length
                );
                var rend = localCue.GetComponent<Renderer>();
                if (rend != null)
                {
                    // CueProfile stores cueColor — tint the existing material instance
                    var matInst = rend.material; // creates an instance (no shared mat mutation)
                    matInst.color = profileToEquip.cueColor;
                    rend.material = matInst;
                }
                Debug.Log($"[CueStrike CueRack] Physically equipped cue profile: {profileToEquip.name}");
                
                // Play chalk/wood pickup sound
                var audioMgr = CueStrikeAudioManager.Instance;
                if (audioMgr != null)
                {
                    audioMgr.PlayChalk(); // Click foley sound fallback
                }
            }
        }
    }
}
