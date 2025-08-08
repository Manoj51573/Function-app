namespace eforms_middleware.DataModel
{
    public class RefDirectoratesInsertModel
    {
        public int? RefDirectoratesID { get; set; }
        public string Directorate { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class RefDirectoratesGetModel
    {
        public int RefDirectoratesID { get; set; }
        public string Directorate { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
