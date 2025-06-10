using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Used by Launch UX to determine whether the scan is "good enough" to proceed.
    /// </summary>
    [Serializable]
    public class LaunchValidation
    {
        public int MinWalkableNodes = 20;

        private GeniesIrlBootstrapper _bootstrapper;

        public bool IsValidationComplete 
        {
            get
            {
                if (_bootstrapper == null) return false;

                // Check if the number of walkable nodes is greater than the minimum required.
                int walkableNodes = _bootstrapper.ARNavigation.CountWalkableNodes();

                if (walkableNodes > MinWalkableNodes)
                {
                    return true;
                }
                
                return false;
            }
        }

        public void StartValidation(GeniesIrlBootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
        }
    }
}

