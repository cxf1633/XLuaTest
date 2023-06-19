using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.LWRP
{
    [Obsolete("LWRP -> Universal (UnityUpgradable) -> UnityEngine.Rendering.Universal.UniversalAdditionalLightData", true)]
    public class LWRPAdditionalLightData
    {
    }
}


namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class UniversalAdditionalLightData : MonoBehaviour
    {
        [Tooltip("Controls the usage of pipeline settings.")]
        [SerializeField] bool m_UsePipelineSettings = true;

        public bool usePipelineSettings
        {
            get { return m_UsePipelineSettings; }
            set { m_UsePipelineSettings = value; }
        }

        [SerializeField]
        private Vector2 m_LightCookieSize = Vector2.one;

        public Vector2 lightCookieSize {
            get => m_LightCookieSize;
            set => m_LightCookieSize = value;
        }

        [SerializeField]
        private Vector2 m_LightCookieOffset = Vector2.zero;

        public Vector2 lightCookieOffset {
            get => m_LightCookieOffset;
            set => m_LightCookieOffset = value;
        }
    }
}
