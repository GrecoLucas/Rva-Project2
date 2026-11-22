using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// A piece of trash that can be lifted with telekinesis (an <see cref="XRGrabInteractable"/> with far attach)
    /// and sorted into a <see cref="RecyclingBin"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [DisallowMultipleComponent]
    public class WasteItem : MonoBehaviour
    {
        /// <summary>Raised when any waste item starts being held.</summary>
        public static event Action<WasteItem> AnyGrabbed;
        /// <summary>Raised when any waste item stops being held.</summary>
        public static event Action<WasteItem> AnyReleased;

        [SerializeField] WasteCategory m_Category;
        [SerializeField] string m_DisplayName = "Trash";
        [Tooltip("Short material description shown on the floating label, e.g. \"PET plastic\".")]
        [SerializeField] string m_MaterialDescription = "";

        [Header("Holding feedback")]
        [SerializeField, Range(0f, 1f)] float m_HoldRumbleAmplitude = 0.06f;
        [SerializeField] float m_HoldRumbleInterval = 0.12f;

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        bool m_StartsKinematic;
        bool m_Consumed;
        float m_NextRumble;

        public WasteCategory category => m_Category;
        public string displayName => m_DisplayName;
        public string materialDescription => m_MaterialDescription;
        public bool isHeld => m_Grab != null && m_Grab.isSelected;
        public bool isConsumed => m_Consumed;
        public CleanableZone zone { get; private set; }

        /// <summary>The interactor holding (or that last held) this item, used for haptics.</summary>
        public XRBaseInputInteractor lastInteractor { get; private set; }

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_StartsKinematic = m_Body.isKinematic;
            zone = GetComponentInParent<CleanableZone>();
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        void Update()
        {
            if (!isHeld || lastInteractor == null || Time.time < m_NextRumble)
                return;
            m_NextRumble = Time.time + m_HoldRumbleInterval;
            lastInteractor.SendHapticImpulse(m_HoldRumbleAmplitude, m_HoldRumbleInterval);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            lastInteractor = args.interactorObject as XRBaseInputInteractor;
            Feedback.Play(Feedback.Sound.Lift, transform.position);
            AnyGrabbed?.Invoke(this);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_Consumed)
                return;
            // Floating trash starts kinematic; once picked up it should fall like the rest.
            if (m_StartsKinematic)
                StartCoroutine(MakeDynamicNextFrame());
            Feedback.Play(Feedback.Sound.Release, transform.position, 0.6f);
            AnyReleased?.Invoke(this);
        }

        IEnumerator MakeDynamicNextFrame()
        {
            yield return null;
            m_StartsKinematic = false;
            m_Body.isKinematic = false;
        }

        /// <summary>Sends a haptic impulse to the hand that held this item, if any.</summary>
        public void Haptic(float amplitude, float duration)
        {
            if (lastInteractor != null)
                lastInteractor.SendHapticImpulse(amplitude, duration);
        }

        /// <summary>Releases the item from any hand holding it.</summary>
        public void ForceRelease()
        {
            if (m_Grab.isSelected && m_Grab.interactionManager != null)
                m_Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)m_Grab);
        }

        /// <summary>
        /// Pulls the item into a bin opening, then removes it from the world and reports it to its zone.
        /// </summary>
        public void ConsumeInto(Transform mouth, Action onFinished)
        {
            if (m_Consumed)
                return;
            m_Consumed = true;
            bool wasHeld = isHeld;
            ForceRelease();
            if (wasHeld)
                AnyReleased?.Invoke(this);
            m_Grab.enabled = false;
            m_Body.isKinematic = true;
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;
            StartCoroutine(ConsumeRoutine(mouth, onFinished));
        }

        IEnumerator ConsumeRoutine(Transform mouth, Action onFinished)
        {
            const float duration = 0.35f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Vector3 inside = mouth.position + mouth.forward * 0.25f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.position = Vector3.Lerp(startPos, inside, k * k);
                transform.localScale = startScale * (1f - k * 0.9f);
                transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            gameObject.SetActive(false);
            if (zone != null)
                zone.ReportSorted(this);
            onFinished?.Invoke();
        }
    }
}
