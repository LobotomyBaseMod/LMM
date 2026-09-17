using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
{
    internal class LoadingSpinner : VisualElement
    {
        internal LoadingSpinner()
        {
            // add child elements to set up centered spinner rotation
            mSpinner = new VisualElement();
            Add(mSpinner);

            mSpinner.style.backgroundImage = Images.GetImage(Images.Name.Loading);
            mSpinner.style.position = Position.Absolute;
            mSpinner.style.width = 16;
            mSpinner.style.height = 16;
            mSpinner.style.left = -8;
            mSpinner.style.top = -8;

            style.position = Position.Relative;
            style.width = 16;
            style.height = 16;
            style.left = 8;
            style.top = 8;
        }

        internal void Start()
        {
            if (mRotationEvent != null)
                return;

            mRotationEvent = mSpinner.schedule.Execute(UpdateProgress).Every(ROTATION_REFRESH_RATE);

            mStartTime = EditorApplication.timeSinceStartup;
        }

        internal void Stop()
        {
            if (mRotationEvent == null)
                return;

            mRotationEvent.Pause();
            mRotationEvent = null;
        }

        void UpdateProgress()
        {
            double elapsedTime = EditorApplication.timeSinceStartup - mStartTime;
            int rotation = (int)(ROTATION_SPEED * elapsedTime) % 360;

            mSpinnerStyleRotate.value = new Rotate(rotation);
            mSpinner.style.rotate = mSpinnerStyleRotate;
        }

        double mStartTime;
        VisualElement mSpinner;
        IVisualElementScheduledItem mRotationEvent;
        StyleRotate mSpinnerStyleRotate;

        const int ROTATION_SPEED = 360; // Euler degrees per second
        const int ROTATION_REFRESH_RATE = 32; // (ms) roughly 30 FPS
    }
}
