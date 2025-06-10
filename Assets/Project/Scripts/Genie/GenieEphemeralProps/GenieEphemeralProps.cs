using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Ephemeral props appear and disappear as the Genie needs them. These non-interactive objects support the Genie's 
    /// animations. For example, her smartphone idle animation requires a smartphone prop to appear and disappear as needed.
    /// As of writing, all prop appearances are triggered by animation events.
    /// </summary>
    public class GenieEphemeralProps : MonoBehaviour
    {
        private EphemeralProp[] _props;

        [NonSerialized]
        private Genie _genie;

        public void OnInitialize(Genie genie)
        {
            _genie = genie;
            _genie.genieAnimation.animEventDispatcher.EphemeralPropAppear += OnEnablePropAnimEvent;
            _genie.genieAnimation.animEventDispatcher.EphemeralPropDisappear += DisableAllProps;
            _props = GetComponentsInChildren<EphemeralProp>(true);
            DisableAllProps();
        }

        public void EnableProp(EphemeralProp.ID id, bool active)
        {
            foreach (var prop in _props)
            {
                if (prop.myID == id)
                {
                    if (active)
                    {
                        prop.Appear(_genie.genieAnimation.Animator);
                    }
                    else
                    {
                        prop.Disappear();
                    }
                }
            }
        }

        public void DisableAllProps()
        {
            foreach (var prop in _props)
            {
                prop.Disappear();
            }
        }

        private void OnEnablePropAnimEvent(EphemeralProp.ID iD)
        {
            EnableProp(iD, true);
        }
    }
}

