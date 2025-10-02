using UnityEngine;

namespace StageEngine.Core.Utility
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;

        private SignedLogger logger;
        protected SignedLogger Logger => logger;

        protected virtual void Awake()
        {
            logger = SignedLogger.GetInstance(GetType());

            if (Instance == null)
            {
                Instance = this as T;
            }
            else
            {
                logger.LogWarning($"Multiple instances of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
    }
}
