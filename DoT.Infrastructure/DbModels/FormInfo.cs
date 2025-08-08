namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class FormInfo
    {
        public FormInfo ShallowCopy()
        {
            return (FormInfo)MemberwiseClone();
        }
    }
}