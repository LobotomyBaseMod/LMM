using System;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    [Serializable]
    struct EquatableType : IEquatable<EquatableType>, IEquatable<Type>
    {
        [SerializeField]
        public string typeFullNanme;
        [SerializeField]
        public string assemblyName;

        public bool Equals(EquatableType other)
        {
            return typeFullNanme == other.typeFullNanme && assemblyName == other.assemblyName;
        }

        public bool Equals(Type other)
        {
            return other?.FullName == typeFullNanme && other?.Assembly.GetName().Name == assemblyName;
        }

        public static implicit operator EquatableType(Type type)
        {
            return new EquatableType()
            {
                typeFullNanme = type.FullName,
                assemblyName = type.Assembly.GetName().Name
            };
        }

        public static implicit operator Type(EquatableType type)
        {
            return Type.GetType($"{type.typeFullNanme}, {type.assemblyName}");
        }
    }

    [Serializable]
    class SpriteFrameModeUndoObject : UndoObject<EquatableType>
    {
    }
}
