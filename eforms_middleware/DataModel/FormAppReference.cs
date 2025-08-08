using System;

namespace eforms_middleware.DataModel
{
    public class FormAppReferenceInsertModel
    {
        public int? FormAppReferenceID { get; set; }
        public int? AllFormsID { get; set; }
        public string LogicAppID { get; set; }
        public string LogicAppFor { get; set; }
        public string FunctionAppID { get; set; }
        public string FunctionAppFor { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class FormAppReferenceGetModel
    {
        public string FormAppReferenceID { get; set; }
        public string AllFormsID { get; set; }
        public string LogicAppID { get; set; }
        public string LogicAppFor { get; set; }
        public string FunctionAppID { get; set; }
        public string FunctionAppFor { get; set; }
        public string Modified { get; set; }
        public string ModifiedBy { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class AllFormAppReferenceModel
    {
        public int? FormAppReferenceID { get; set; }
        public int? AllFormsID { get; set; }
        public string LogicAppID { get; set; }
        public string LogicAppFor { get; set; }
        public string FunctionAppID { get; set; }
        public string FunctionAppFor { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
