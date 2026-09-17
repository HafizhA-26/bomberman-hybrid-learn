using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts.UI_Manager
{
    [RequireComponent(typeof(RectTransform))]
    public class WebGLSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        [DllImport("__Internal")]
        public static extern string GetSafeArea();

        private void ApplySafeArea()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string safePadding = GetSafeArea();
            string[] insets = safePadding.Split(',');

            if(insets.Length == 4)
            {
                float top = float.Parse(insets[0]);
                float right = float.Parse(insets[1]);
                float bottom = float.Parse(insets[2]);
                float left = float.Parse(insets[3]);

                _rectTransform.offsetMin = new Vector2(left, bottom);
                _rectTransform.offsetMax = new Vector2(-right, -top);
            }
#endif
        }
       
    }
}