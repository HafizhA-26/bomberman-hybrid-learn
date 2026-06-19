using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class StarterUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle _ruleBasedToggle;
        [SerializeField] private Toggle _mlAgentToggle;
    }

}
