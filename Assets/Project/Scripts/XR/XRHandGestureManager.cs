using UnityEngine;

namespace GeniesIRL
{
    public enum HandGestures
    {
        ThumbsUp,
        HighFive,
        PointAt,
        FistBump,
        OpenPalm
    }

    public class XRHandGestureManager : MonoBehaviour
    {
        private UserHandGesture[] userHandGestures;

        // For Gestures: index 0 Left hand, index 1 right hand
        [Header("Hand Gestures")]
        [Tooltip("index 0 Left hand, index 1 right hand")]
        [SerializeField]
        UserHandGesture[] ThumbsUpGesture = new UserHandGesture[2];
        
        [Tooltip("index 0 Left hand, index 1 right hand")]
        [SerializeField]
        UserHandGesture[] HighFiveGesture = new UserHandGesture[2];
        
        [Tooltip("index 0 Left hand, index 1 right hand")]
        [SerializeField]
        UserHandGesture[] PointAtGesture = new UserHandGesture[2];
        
        [Tooltip("index 0 Left hand, index 1 right hand")]
        [SerializeField]
        UserHandGesture[] FistBumpGesture = new UserHandGesture[2];

        [Tooltip("index 0 Left hand, index 1 right hand")]
        [SerializeField]
        UserHandGesture[] OpenPalmGesture = new UserHandGesture[2];

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            userHandGestures = GetComponentsInChildren<UserHandGesture>();
            // for (int i = 0; i < userHandGestures.Length; i++)
            // {
            //    Debug.Log(userHandGestures[i].name);
            // }
        }

        public UserHandGesture[] GetHandGesture(HandGestures handGesture, InputHand inputHand)
        {
            UserHandGesture[] gestureArray = handGesture switch
            {
                // Add new gestures when available
                HandGestures.ThumbsUp => ThumbsUpGesture,
                HandGestures.HighFive => HighFiveGesture,
                HandGestures.PointAt => PointAtGesture,
                HandGestures.FistBump => FistBumpGesture,
                HandGestures.OpenPalm => OpenPalmGesture,
                _ => null
            };

            return GetGestureArray(gestureArray, inputHand);
        }

        private UserHandGesture[] GetGestureArray(UserHandGesture[] gestureArray, InputHand inputHand)
        {
            if (gestureArray == null) return null;

            return inputHand == InputHand.Both
                ? gestureArray
                : new UserHandGesture[] { gestureArray[(int)inputHand] };
        }
        
        public void AssingTargetToHandGesture(HandGestures handGesture, InputHand inputHand, Transform target)
        {
            UserHandGesture[] handGestures = GetHandGesture(handGesture, inputHand);
            foreach (UserHandGesture gesture in handGestures)
            {
                gesture.targetTransform = target;
            }
        }
    }

}
