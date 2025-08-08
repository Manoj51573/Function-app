using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Graph;
using Microsoft.Graph.SecurityNamespace;
using Microsoft.Graph.TermStore;
using System.Security.Cryptography.Xml;

namespace eforms_middleware.Constants;

public static class HomeGaragingTemplates 
{


    public const string SUBMITTED_TO_LINEMANAGER_TEMPLATE =
       "<div>Hi,</div><br/>" +
       "<div>{0} has submitted a {1} Request eForm #{2} for your review.<br/></div>" +
       "<div>Please {3} to review and action the eForm.</div><br/>" +
       "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>" +
       "<div>Procurement and Fleet Management</div>" +
        "<div>Finance and Procurement Services</div>";


    public const string APPROVED_TEMPLATE_ED=
        "<div>Hi,</div><br/>" +
        "<div>{0} has submitted a {1} Request eForm #{2} that has been endorsed by senior management and requires your approval.<br/></div>" +
        "<div>Please {3} to review and action the eForm.</div><br/>" +
        "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>"  +
        "<div>Procurement and Fleet Management</div>" +
        "<div>Finance and Procurement Services</div>";

    public const string APPROVED_TEMPLATE_PFM=
       "<div>Hi,</div><br/>" +
       "<div>{0} has submitted a {1} Request eForm #{2} for your review.<br/></div>" +
       "<div>Please {3} to review and action the eForm.</div><br/> " +
       "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>"  +
       "<div>Procurement and Fleet Management</div>" +
       "<div>Finance and Procurement Services</div>";



    public const string APPROVED_TEMPLATE_FLEET_GROUP=
        "<div>Hi,</div><br/>" +
        "<div>{0} has submitted a {1} Request eForm #{2} that has been approved by senior management and requires your completion.<br/></div>" +
        "<div>Please {3} to action the eForm.</div><br/>" +
        "<div>Procurement and Fleet Management</div>" +
        "<div>Finance and Procurement Services</div>";



    public const string COMPLETED_TEMPLATE_TO_MANAGER =
            "<div>Hi {0},  </div><br/>" +
            "Your {1} Request eForm #{2} has now been approved by senior management and completed.</div><br/>" +
            "<div>Please {3} to review and action the eForm.</div><br/>" +
            "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>"  +
            "<div>Procurement and Fleet Management</div>" +
            "<div>Finance and Procurement Services</div>";




    public const string DELEGATE_TEMPLATE =
            "<div>Hi, </div><br/>" +
            "<div>{0} has submitted a {1} Request eForm #{2} that has now been delegated for your review.<br/></div>" +
            "<div>Please {3} to review and action the eForm.</div><br/> " +
            "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>"  +
            "<div>Procurement and Fleet Management</div>" +
            "<div>Finance and Procurement Services</div>";




    public const string RECALL_OWNER_TEMPLATE =
      "<div>Hi {0},</div><br/>" +
      "<div>The {2} Request eForm #{3} has been recalled by {1}.</div><br/>" +
      "<div>Please {4} to review and action the eForm.</div><br/> " +
    "<div>Please contact Procurement and Fleet Management on {5} if you need further assistance.</div> <br/>"+
    "<div>Procurement and Fleet Management</div>" +
    "<div>Finance and Procurement Services</div>";


    public const string REJECTED_TEMPLATE =
        "<div>Hi {0},</div><br/>" +
        "<div>Your {2} Request eForm #{3} has been rejected by{1}. The following reasons were given:</br>" +
        "<div>{4}</div><br/>" +
       "<div>Please {5} to review and action the eForm.</div><br/> " +
    "<div>Please contact Procurement and Fleet Management on {6} if you need further assistance.</div> <br/>" +
    "<div>Procurement and Fleet Management</div>" +
    "<div>Finance and Procurement Services</div>";



    public const string ESCALATION_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>{1} has submitted a {2} Request eForm #{3} that has now escalated for your review.</div> <br/>" +
       "<div>Please click {4} to review and action the eForm.</div><br/>" +
        "<div>Please contact {5} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";



    public const string ESCALATION_FLEETGROUP_TEMPLATE =
    "<div>Hi Team, </div><br/>" +
    "<div>{0} has submitted a {1} Request eForm #{2} that has now escalated for your review and requires your action.</div> <br/>" +
   "<div>Please click {3}  to review and re-direct it to an appropriate approver for their action.</div><br/>" +
    "<div>Please contact {4} if you need further assistance.</div> <br/>" +
    "<br/><div>Employee Services</div>" +
     "<div>People and Culture</div>";






}