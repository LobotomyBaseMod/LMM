using System;
using UnityEngine;

namespace Unity.CodeEditor.Platform
{
    internal sealed class KeyGesture : IEquatable<KeyGesture>
    {
        internal KeyGesture(KeyCode key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Key = key;
            KeyModifiers = modifiers;
        }

        public bool Equals(KeyGesture other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Key == other.Key && KeyModifiers == other.KeyModifiers;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is KeyGesture gesture && Equals(gesture);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Key * 397) ^ (int)KeyModifiers;
            }
        }

        public static bool operator ==(KeyGesture left, KeyGesture right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KeyGesture left, KeyGesture right)
        {
            return !Equals(left, right);
        }

        internal KeyCode Key { get; }

        internal KeyModifiers KeyModifiers { get; }
    }
}
