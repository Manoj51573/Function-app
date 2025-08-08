namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefGLProjects
    {
        public int RefGLProjectsID { get; set; }
        public string Projects { get; set; }
        public bool ActiveRecord { get; set; }
        public string Name { get; set; }
    }
}
