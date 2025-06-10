using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// Managers that derive from this class are expected to be spawned by the GeniesIrlBootstrapper. Once the Bootstrapper
    /// spawns all GeniesIrlSubManagers, it will call OnSceneBootstrapped and pass a reference to the bootstrapper. This will
    /// hopefully cut down on our usage of Singletons as managers.
    /// </summary>
    public abstract class GeniesIrlSubManager : MonoBehaviour
    {
        /// <summary>
        /// This gets set on Awake() by the Bootstrapper, so if reference it in Start() or later, you should be good to go.
        /// </summary>
        public GeniesIrlBootstrapper Bootstrapper { get; private set; }

        /// <summary>
        /// Called by the GeniesIrlBootstrapper after all GeniesIrlSubManagers have been spawned.
        /// IMPORTANT: If you override it, please call base.OnSceneBootstrapped() inside.
        /// </summary>
        /// <param name="bootstrapper"></param>
        public virtual void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            Bootstrapper = bootstrapper;
        }
    }
}

