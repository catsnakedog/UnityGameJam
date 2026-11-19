using TMPro;
using UnityEngine;

namespace UnityGameJam.Zombies
{
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class CombatDamagePopup : MonoBehaviour
    {
        TextMeshPro label;
        Vector3 origin;
        float elapsed;

        public void Show(int amount)
        {
            origin = transform.position;
            label = GetComponent<TextMeshPro>();
            label.SetText("-{0}", amount);
        }

        void Update()
        {
            const float duration = .85f;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = origin + Vector3.up * (.55f * t);
            label.alpha = Mathf.Clamp01((1f - t) * 3f);
            if (elapsed >= duration) Destroy(gameObject);
        }
    }
}
