namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefGLFunds
    {
        public int RefGLFundsID { get; set; }
        public string Funds { get; set; } 
        public bool ActiveRecord { get; set; }
        public string Name { get; set; }
    }
}
