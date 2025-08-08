namespace eforms_middleware.DataModel
{
    public class RefFormStatusInsertModel
    {
        public int? RefStatusesID { get; set; }
        public string Status { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class RefFormStatusGetModel
    {
        public int RefStatusesID { get; set; }
        public string Status { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
