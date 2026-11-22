using ArcaneRestoration.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneRestoration.UI
{
    /// <summary>
    /// Floating label above the item being levitated: what it is, what it is made of, and which bin it belongs in.
    /// One instance in the scene follows whichever item is held.
    /// </summary>
    [DisallowMultipleComponent]
    public class WasteLabel : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] TMP_Text m_Title;
        [SerializeField] TMP_Text m_Material;
        [SerializeField] TMP_Text m_Bin;
        [SerializeField] Image m_BinChip;
        [SerializeField] float m_HeightAbove = 0.25f;
        [SerializeField] float m_FollowSharpness = 14f;

        WasteItem m_Item;
        Transform m_Camera;

        void Awake()
        {
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        void OnEnable()
        {
            WasteItem.AnyGrabbed += Show;
            WasteItem.AnyReleased += Hide;
        }

        void OnDisable()
        {
            WasteItem.AnyGrabbed -= Show;
            WasteItem.AnyReleased -= Hide;
        }

        void Show(WasteItem item)
        {
            m_Item = item;
            if (m_Title != null) m_Title.text = item.displayName;
            if (m_Material != null) m_Material.text = item.materialDescription;
            if (m_Bin != null) m_Bin.text = $"{item.category.BinName()} · {item.category.DisplayName()}";
            if (m_BinChip != null) m_BinChip.color = item.category.BinColour();
            transform.position = TargetPosition();
            if (m_Root != null)
                m_Root.SetActive(true);
        }

        void Hide(WasteItem item)
        {
            if (item != m_Item)
                return;
            m_Item = null;
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        void LateUpdate()
        {
            if (m_Item == null)
                return;
            if (!m_Item.gameObject.activeInHierarchy)
            {
                Hide(m_Item);
                return;
            }
            if (m_Camera == null && Camera.main != null)
                m_Camera = Camera.main.transform;

            float k = 1f - Mathf.Exp(-m_FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, TargetPosition(), k);
            if (m_Camera != null)
            {
                Vector3 away = transform.position - m_Camera.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
            }
        }

        Vector3 TargetPosition() => m_Item.transform.position + Vector3.up * m_HeightAbove;
    }
}
