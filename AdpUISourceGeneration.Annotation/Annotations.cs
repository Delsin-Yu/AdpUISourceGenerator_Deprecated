using System;

namespace AdpUISourceGeneration.Annotation
{
    public enum AccessModifier
    {
        Private,
        Protected,
        Public
    }

    public abstract class AccessModifierPropertyAttribute : Attribute
    {
        public AccessModifier accessModifier;
        protected AccessModifierPropertyAttribute(AccessModifier accessModifier) => this.accessModifier = accessModifier;
    }

    public class TMPTextAttribute : AccessModifierPropertyAttribute
    {
        public TMPTextAttribute(AccessModifier accessModifier) : base(accessModifier)
        {
        }
    }
}
