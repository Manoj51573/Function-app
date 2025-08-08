namespace eforms_middleware.InternalSystem
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class LookupFieldAttribute : System.Attribute
    {
        public LookupFieldAttribute()
        {
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class PersonFieldAttribute : System.Attribute
    {

        public PersonFieldAttribute()
        {
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class IgnoreFieldAttribute : System.Attribute
    {

        public IgnoreFieldAttribute()
        {
        }
    }
}
