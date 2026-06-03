using System;
using System.Collections.Generic;
using System.Text;

namespace CoreMVC.Domain.Authorization
{
    public static class Permissions
    {
        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
            public const string Delete = "Permissions.Users.Delete";
        }

        public static class Reports
        {
            public const string View = "Permissions.Reports.View";
            public const string Export = "Permissions.Reports.Export";
        }
    }
}
