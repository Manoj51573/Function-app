namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefGLCostCentres
    {
        public int RefGLCostCentresID { get; set; }
        public string CostCentre { get; set; }
        public bool ActiveRecord { get; set; }
        public string Name { get; set; }
    }
}
