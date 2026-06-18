using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class DesktopUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _controlHintGroup;
        [SerializeField] private Image _bombCountContainer;
        [SerializeField] private TextMeshProUGUI _bombCountText;

        public void SetBombCount(int count)
        {
            _bombCountText.text = count.ToString();
            if (count > 0) _bombCountContainer.color = Color.white;
            else _bombCountContainer.color = new Color(1, 1, 1, 0.7f);
        }

    }
}