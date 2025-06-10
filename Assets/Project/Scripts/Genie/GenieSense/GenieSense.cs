using System;
using Unity.AppUI.UI;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Represents the Genie's physical senses. Ultimately to inform the Genie's Brain and decision-making.
    /// </summary>
    [Serializable]
    public class GenieSense
    {
        public Genie Genie {get; private set;}

        public DetectUserOfferingItem detectUserOfferingItem;

        public DetectUserSolicitingHighFive detectUserSolicitingHighFive;

        public DetectImpactFromUserProjectile detectImpactFromUserProjectile;

        public PersonalSpace personalSpace;

        public void OnStart(Genie genie)
        {
            Genie = genie;
            detectUserOfferingItem.OnStart(this);
            detectUserSolicitingHighFive.OnStart(this);
            detectImpactFromUserProjectile.OnStart(this);
            personalSpace.OnStart(this);
        }

        public void OnUpdate()
        {
            detectUserOfferingItem.OnUpdate();
            detectUserSolicitingHighFive.OnUpdate();
            personalSpace.OnUpdate();
        }

        public void OnCollisionEnter(Collision collision)
        {
            detectImpactFromUserProjectile.OnCollisionEnter(collision);
        }

        public void OnDrawGizmosSelected()
        {
            personalSpace.OnDrawGizmosSelected();
        }
    }
}

