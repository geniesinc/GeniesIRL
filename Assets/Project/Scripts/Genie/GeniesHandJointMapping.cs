using UnityEngine;
using UnityEngine.XR.Hands;

[CreateAssetMenu(fileName = "HandJointMapping", menuName = "XRGenies/GeniesHandJointMapping", order = 1)]
public class GeniesHandJointMapping : ScriptableObject
{
    [System.Serializable]
    public struct FingersJointMapping
    {
        public XRHandJointID jointID;
        public string jointRigName;
    }
    public string[] SidePrefix = new string[2];
    public Vector3 FingersOffset;
    public FingersJointMapping[] jointMappings;
}