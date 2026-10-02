using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class KillMessageItem : MonoBehaviour
    {
        public TextMeshProUGUI killer, killed;
        public RawImage how;

        public GameObject hitHead, wall,noscope;
        private void Start()
        {
            Destroy(gameObject,10f);
        }
    }
}
