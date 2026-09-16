using System;
using UnityEngine;

namespace Ohm.UISystem
{
    [Serializable]
    public class UITypeReference : ISerializationCallbackReceiver
    {
        [SerializeField] private string typeName;
        private Type type;

        public Type Type
        {
            get
            {
                if (type == null && !string.IsNullOrEmpty(typeName))
                {
                    type = Type.GetType(typeName);
                }
                return type;
            }
            set
            {
                type = value;
                typeName = type?.AssemblyQualifiedName;
            }
        }

        public void OnBeforeSerialize()
        {
            if (type != null)
            {
                typeName = type.AssemblyQualifiedName;
            }
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                type = Type.GetType(typeName);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is UITypeReference other)
                return Type == other.Type;
            if (obj is Type otherType)
                return Type == otherType;
            return false;
        }

        public override int GetHashCode()
        {
            return Type != null ? Type.GetHashCode() : 0;
        }

        public static bool operator ==(UITypeReference a, UITypeReference b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Type == b.Type;
        }

        public static bool operator !=(UITypeReference a, UITypeReference b)
        {
            return !(a == b);
        }

        public bool IsNone()
        {
            return Type == null;
        }
    }
}
