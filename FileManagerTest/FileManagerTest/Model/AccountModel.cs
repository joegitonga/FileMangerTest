using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerTest.Model
{
    public class userListModel
    {
        public Int32 UserID { get; set; }
        public int ColumnID { get; set; }
        public string CheckSumID { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string OtherNames { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string isActive { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
