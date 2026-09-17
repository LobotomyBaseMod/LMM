using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.PlasticSCM.Editor
{
    internal struct UnityObjectInstance
    {
#if UNITY_6000_4_OR_NEWER
        internal static UnityObjectInstance FromEntityId(EntityId entityId)
        {
            return new UnityObjectInstance
            {
                mEntityIdValue = entityId
            };
        }

        EntityId mEntityIdValue;
#else
        internal static UnityObjectInstance FromInstanceId(int instanceID)
        {
            return new UnityObjectInstance
            {
                mIntValue = instanceID
            };
        }

        int mIntValue;
#endif

        internal Object FindObject()
        {
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(mEntityIdValue);
#elif UNITY_6000_3_OR_NEWER
            #pragma warning disable CS0618 // Type or member is obsolete
            return EditorUtility.EntityIdToObject(mIntValue);
            #pragma warning restore CS0618 // Type or member is obsolete
#else
            return EditorUtility.InstanceIDToObject(mIntValue);
#endif
        }

        internal bool MatchesScene(Scene scene)
        {
#if UNITY_6000_4_OR_NEWER
            return scene.handle == SceneHandle.FromRawData(EntityId.ToULong(mEntityIdValue));
#else
            return scene.handle == mIntValue;
#endif
        }
    }
}
