using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// An ecoponto that accepts one <see cref="WasteCategory"/>.
    /// Correct items are swallowed; wrong items are pushed back out and the right bin of the set glows.
    /// </summary>
    [DisallowMultipleComponent]
    public class RecyclingBin : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] WasteCategory m_Accepts;
        [Tooltip("Opening of the bin (the Intake trigger). Its forward axis points into the bin.")]
        [SerializeField] Transform m_Mouth;
        [SerializeField] Renderer[] m_Renderers;

        [Header("Wrong item")]
        [SerializeField] float m_RejectImpulse = 2.2f;
        [SerializeField] float m_RejectCooldown = 1f;

        readonly Dictionary<WasteItem, float> m_LastReject = new Dictionary<WasteItem, float>();
        MaterialPropertyBlock m_Block;
        Coroutine m_Tint;

        public WasteCategory accepts => m_Accepts;

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            if (m_Mouth == null)
                m_Mouth = transform.Find("Intake");
            if (m_Renderers == null || m_Renderers.Length == 0)
                m_Renderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>Called by the <see cref="BinIntake"/> when a waste item reaches the opening.</summary>
        public void Receive(WasteItem item)
        {
            if (item.category == m_Accepts)
                Accept(item);
            else
                Reject(item);
        }

        void Accept(WasteItem item)
        {
            item.Haptic(0.7f, 0.15f);
            Feedback.Play(Feedback.Sound.BinGulp, m_Mouth.position);
            item.ConsumeInto(m_Mouth, () =>
            {
                Feedback.Play(Feedback.Sound.SortCorrect, m_Mouth.position);
                Feedback.Sparkle(m_Mouth.position, m_Accepts.BinColour());
            });
            Tint(Color.white * 1.6f, 0.4f, 1);
            CityCleanlinessManager.ReportSort(item, this, true);
        }

        void Reject(WasteItem item)
        {
            if (m_LastReject.TryGetValue(item, out var last) && Time.time - last < m_RejectCooldown)
                return;
            m_LastReject[item] = Time.time;

            Feedback.Play(Feedback.Sound.SortWrong, m_Mouth.position);
            Feedback.Sparkle(m_Mouth.position, new Color(1f, 0.25f, 0.2f));
            StartCoroutine(DoubleBuzz(item));
            Tint(new Color(1.6f, 0.35f, 0.3f), 0.5f, 1);

            if (!item.isHeld)
            {
                var body = item.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic)
                {
                    Vector3 push = (-m_Mouth.forward + Vector3.up * 0.8f).normalized * m_RejectImpulse;
                    body.linearVelocity = Vector3.zero;
                    body.AddForce(push, ForceMode.VelocityChange);
                    Feedback.Play(Feedback.Sound.Bounce, item.transform.position);
                }
            }

            // Show the player which bin was the right one.
            if (transform.parent != null)
                foreach (var bin in transform.parent.GetComponentsInChildren<RecyclingBin>())
                    if (bin != this && bin.accepts == item.category)
                        bin.Tint(Color.white * 1.8f, 1.5f, 3);

            CityCleanlinessManager.ReportSort(item, this, false);
        }

        static IEnumerator DoubleBuzz(WasteItem item)
        {
            item.Haptic(0.8f, 0.08f);
            yield return new WaitForSeconds(0.14f);
            item.Haptic(0.8f, 0.08f);
        }

        /// <summary>Briefly tints the bin (pulses <paramref name="pulses"/> times over <paramref name="duration"/> seconds).</summary>
        public void Tint(Color colour, float duration, int pulses)
        {
            if (m_Tint != null)
                StopCoroutine(m_Tint);
            m_Tint = StartCoroutine(TintRoutine(colour, duration, pulses));
        }

        IEnumerator TintRoutine(Color colour, float duration, int pulses)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float wave = Mathf.Sin(t / duration * pulses * Mathf.PI);
                SetColour(Color.Lerp(Color.white, colour, Mathf.Abs(wave)));
                yield return null;
            }
            foreach (var r in m_Renderers)
                r.SetPropertyBlock(null);
            m_Tint = null;
        }

        void SetColour(Color c)
        {
            m_Block.SetColor(BaseColorId, c);
            foreach (var r in m_Renderers)
                r.SetPropertyBlock(m_Block);
        }
    }
}
