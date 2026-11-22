using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// Trigger at the opening of an ecoponto. Forwards waste items that enter it to the parent <see cref="RecyclingBin"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BinIntake : MonoBehaviour
    {
        RecyclingBin m_Bin;

        void Awake()
        {
            m_Bin = GetComponentInParent<RecyclingBin>();
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other) => Forward(other);

        void OnTriggerStay(Collider other) => Forward(other);

        void Forward(Collider other)
        {
            if (m_Bin == null)
                return;
            var body = other.attachedRigidbody;
            var item = body != null ? body.GetComponent<WasteItem>() : other.GetComponentInParent<WasteItem>();
            if (item != null && !item.isConsumed)
                m_Bin.Receive(item);
        }
    }
}
