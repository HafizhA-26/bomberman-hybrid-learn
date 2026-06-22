using UnityEngine;
using UnityEngine.EventSystems;

namespace BombermanRL.UI
{
    public class ClickEffectUI : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_ButtonClick", true);
        }
    }

}
