using System;

namespace eforms_middleware.DataModel
{
    //Class AllFormsInsertModel
    public class AllFormsInsertModel
    {
        public int? AllFormsID { get; set; }
        public string FormCode { get; set; }
        public string FormTitle { get; set; }
        public string FormID { get; set; }
        public string BusinessOwner { get; set; }
        public int? BusinessDirectorateID { get; set; }
        public string FormType { get; set; }
        public string FormCategory { get; set; }
        public string VisibleTo { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string FormURL { get; set; }
        public string ArchiveURL { get; set; }
        public bool? ActiveRecord { get; set; }
    }

    public class AllFormsGetModel
    {
        public int? AllFormsID { get; set; }
        public string FormCode { get; set; }
        public string FormTitle { get; set; }
        public string FormID { get; set; }
        public string BusinessOwner { get; set; }
        public int? BusinessDirectorateID { get; set; }
        public string FormType { get; set; }
        public string FormCategory { get; set; }
        public string VisibleTo { get; set; }
        public string Created { get; set; }
        public string CreatedBy { get; set; }
        public string Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string FormURL { get; set; }
        public string ArchiveURL { get; set; }
        public bool? ActiveRecord { get; set; }
    }
}
