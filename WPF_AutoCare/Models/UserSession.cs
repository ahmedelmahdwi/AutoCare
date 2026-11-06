using System;

namespace C__project_Auto_Care.Models
{
    public static class UserSession
    {
        public static string Username { get; set; }
        public static string FullName { get; set; }
        public static string Role { get; set; }
        public static bool IsLoggedIn { get; set; }
        public static int? UserId { get; set; }
        public static int BranchId { get; set; } = 1;
        public static string BranchName { get; set; } = "الفرع الرئيسي";

        public static void Login(string username, string fullName, string role, int branchId = 1, string branchName = "الفرع الرئيسي")
        {
            Username = username;
            FullName = fullName;
            Role = role;
            BranchId = branchId;
            BranchName = branchName;
            IsLoggedIn = true;
        }

        public static void Logout()
        {
            Username = null;
            FullName = null;
            Role = null;
            BranchId = 1;
            BranchName = "الفرع الرئيسي";
            IsLoggedIn = false;
            UserId = null;
        }
    }
}
