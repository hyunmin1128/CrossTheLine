using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaGoldLabel : MonoBehaviour
    {
        public CanaWallet wallet;
        public TextMeshPro label;
        private void LateUpdate() => label.text = wallet.Gold.ToString();
    }
}
