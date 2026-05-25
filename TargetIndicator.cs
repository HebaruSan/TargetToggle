using System;
using UnityEngine;
using KSP.UI.Screens;

namespace TargetToggle
{
    // --- SETTINGS MENU INTEGRATION ---
    public class TargetToggleSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Off-Screen Target Indicator"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "TargetToggle"; } }
        public override string DisplaySection { get { return "TargetToggle"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Enable Off-Screen Arrow", toolTip = "Shows a magenta edge marker pointing to targets that are off-screen.")]
        public bool enableIndicator = true;
    }

    // --- MAIN MOD LOGIC ---
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TargetIndicator : MonoBehaviour
    {
        private Texture2D markerTexture;
        private Color markerColor = Color.magenta;
        private float markerWidth = 30f;
        private float markerHeight = 4f;
        private float screenMargin = 15f;

        private bool lastLabelState;
        private bool isUIHidden = false;

        // Cached once per frame in Update so OnGUI (which fires multiple
        // times per frame) doesn't repeatedly walk the settings tree.
        private bool isModEnabled = true;

        // Custom logging helper to keep KSP.log clean and searchable
        private void Log(string message)
        {
            Debug.Log("[TargetToggle] " + message);
        }

        public void Awake()
        {
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);

            Log("Awake: Subscribed to UI events.");
        }

        public void Start()
        {
            markerTexture = new Texture2D(1, 1);
            markerTexture.SetPixel(0, 0, markerColor);
            markerTexture.Apply();

            lastLabelState = GameSettings.FLT_VESSEL_LABELS;

            Log("Start: Mod initialized successfully. Initial F4 state is: " + lastLabelState);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            // Cache the settings value once per frame.
            if (HighLogic.CurrentGame != null)
            {
                isModEnabled = HighLogic.CurrentGame.Parameters.CustomParams<TargetToggleSettings>().enableIndicator;
            }

            // Watch for any changes to the core game setting
            if (GameSettings.FLT_VESSEL_LABELS != lastLabelState)
            {
                lastLabelState = GameSettings.FLT_VESSEL_LABELS;

                string messageText = lastLabelState ? "Vessel markers: Enabled" : "Vessel markers: Disabled";
                ScreenMessages.PostScreenMessage(messageText, 2.5f, ScreenMessageStyle.UPPER_CENTER);

                // Log only when the state flips, preventing lag
                Log("State Change: Vessel labels toggled to " + lastLabelState);
            }
        }

        public void OnGUI()
        {
            // Bail out early on all the cheap conditions, including map view —
            // the marker is meaningless (and visually broken) over the map.
            if (!HighLogic.LoadedSceneIsFlight
                || isUIHidden
                || MapView.MapIsEnabled
                || !isModEnabled
                || !GameSettings.FLT_VESSEL_LABELS
                || FlightGlobals.fetch == null
                || FlightGlobals.fetch.VesselTarget == null)
            {
                return;
            }

            Camera cam = FlightCamera.fetch.mainCamera;
            if (cam == null) return;

            ITargetable target = FlightGlobals.fetch.VesselTarget;

            // GetTransform() can return null for targets that are unloaded or
            // mid-transition. Guard against it to avoid per-frame exceptions.
            Transform targetTransform = target.GetTransform();
            if (targetTransform == null) return;

            Vector3 targetWorldPos = targetTransform.position;
            Vector3 screenPos = cam.WorldToScreenPoint(targetWorldPos);

            screenPos.y = Screen.height - screenPos.y;

            bool isBehindCamera = screenPos.z < 0;
            bool isOffScreenX = screenPos.x < 0 || screenPos.x > Screen.width;
            bool isOffScreenY = screenPos.y < 0 || screenPos.y > Screen.height;

            if (!isBehindCamera && !isOffScreenX && !isOffScreenY)
            {
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 targetPos2D = new Vector2(screenPos.x, screenPos.y);

            Vector2 direction = targetPos2D - screenCenter;

            if (isBehindCamera)
            {
                direction *= -1f;
            }

            direction.Normalize();

            float halfWidth = (Screen.width / 2f) - screenMargin;
            float halfHeight = (Screen.height / 2f) - screenMargin;

            // Find how far along the ray we travel to hit each edge, then take
            // the nearer one. direction is normalized, so scaling it by `scale`
            // lands exactly on the correct screen edge in any quadrant.
            float scaleX = direction.x != 0f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = direction.y != 0f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 intersectionPoint = screenCenter + direction * scale;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, intersectionPoint);

            Rect markerRect = new Rect(
                intersectionPoint.x - (markerWidth / 2f),
                intersectionPoint.y - (markerHeight / 2f),
                markerWidth,
                markerHeight
            );

            GUI.DrawTexture(markerRect, markerTexture);
            GUI.matrix = previousMatrix;
        }

        private void OnHideUI()
        {
            isUIHidden = true;
            Log("State Change: UI Hidden (F2)");
        }

        private void OnShowUI()
        {
            isUIHidden = false;
            Log("State Change: UI Shown (F2)");
        }

        public void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);

            if (markerTexture != null)
            {
                Destroy(markerTexture);
            }

            Log("OnDestroy: Mod unloaded safely.");
        }
    }
}
