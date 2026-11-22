using System.Collections;
using ArcaneRestoration.World;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ArcaneRestoration.Magic
{
    /// <summary>
    /// The wizard's wand, held in place of a controller model.
    /// The tip glows in the colour of the bin that the held item belongs to, leaves a trail while
    /// levitating, and flashes green or red after a sorting attempt.
    /// </summary>
    [DisallowMultipleComponent]
    public class Wand : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] InteractorHandedness m_Hand = InteractorHandedness.Right;
        [SerializeField] Transform m_Tip;
        [SerializeField] Renderer m_TipRenderer;
        [SerializeField] TrailRenderer m_Trail;
        [Tooltip("Renderers of the controller model that the wand replaces.")]
        [SerializeField] Renderer[] m_HiddenControllerRenderers;

        [Header("Colours")]
        [SerializeField] Color m_IdleColour = new Color(0.75f, 0.9f, 1f);
        [SerializeField] Color m_CorrectColour = new Color(0.3f, 1f, 0.45f);
        [SerializeField] Color m_WrongColour = new Color(1f, 0.25f, 0.2f);
        [SerializeField, Range(1f, 4f)] float m_GlowIntensity = 2f;

        MaterialPropertyBlock m_Block;
        WasteItem m_Held;
        Coroutine m_Flash;

        public Transform tip => m_Tip;

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            SetTipColour(m_IdleColour);
            if (m_Trail != null)
                m_Trail.emitting = false;
        }

        void OnEnable()
        {
            SetControllerVisible(false);
            WasteItem.AnyGrabbed += OnGrabbed;
            WasteItem.AnyReleased += OnReleased;
            CityCleanlinessManager.SortAttempted += OnSortAttempted;
        }

        void OnDisable()
        {
            SetControllerVisible(true);
            WasteItem.AnyGrabbed -= OnGrabbed;
            WasteItem.AnyReleased -= OnReleased;
            CityCleanlinessManager.SortAttempted -= OnSortAttempted;
        }

        void LateUpdate()
        {
            // Some controller models are spawned or re-enabled at runtime; keep them hidden.
            SetControllerVisible(false);
        }

        bool IsMine(WasteItem item) => item.lastInteractor != null && item.lastInteractor.handedness == m_Hand;

        void OnGrabbed(WasteItem item)
        {
            if (!IsMine(item))
                return;
            m_Held = item;
            if (m_Flash == null)
                SetTipColour(item.category.BinColour());
            if (m_Trail != null)
            {
                m_Trail.startColor = item.category.BinColour();
                m_Trail.endColor = new Color(1f, 1f, 1f, 0f);
                m_Trail.emitting = true;
            }
        }

        void OnReleased(WasteItem item)
        {
            if (item != m_Held)
                return;
            m_Held = null;
            if (m_Trail != null)
                m_Trail.emitting = false;
            if (m_Flash == null)
                SetTipColour(m_IdleColour);
        }

        void OnSortAttempted(WasteItem item, RecyclingBin bin, bool correct)
        {
            if (!IsMine(item))
                return;
            if (m_Flash != null)
                StopCoroutine(m_Flash);
            m_Flash = StartCoroutine(Flash(correct ? m_CorrectColour : m_WrongColour));
        }

        IEnumerator Flash(Color colour)
        {
            for (int i = 0; i < 2; i++)
            {
                SetTipColour(colour);
                yield return new WaitForSeconds(0.12f);
                SetTipColour(m_IdleColour);
                yield return new WaitForSeconds(0.08f);
            }
            m_Flash = null;
            SetTipColour(m_Held != null ? m_Held.category.BinColour() : m_IdleColour);
        }

        void SetTipColour(Color c)
        {
            if (m_TipRenderer == null)
                return;
            m_Block.SetColor(BaseColorId, c * m_GlowIntensity);
            m_TipRenderer.SetPropertyBlock(m_Block);
        }

        void SetControllerVisible(bool visible)
        {
            if (m_HiddenControllerRenderers == null)
                return;
            foreach (var r in m_HiddenControllerRenderers)
                if (r != null && r.enabled != visible)
                    r.enabled = visible;
        }
    }
}
