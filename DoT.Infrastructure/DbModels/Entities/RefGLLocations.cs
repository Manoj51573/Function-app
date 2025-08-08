namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefGLLocations
    {
        public int RefGLLocationsID { get; set; }
        public string Locations { get; set; }
        public bool ActiveRecord { get; set; }
        public string Name { get; set; }
    }
}
