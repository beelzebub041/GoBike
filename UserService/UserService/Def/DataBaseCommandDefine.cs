using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SqlSugar;

namespace UserService.Def
{
    public class UserInfo
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int UserID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string CREATE_DATE { get; set; }

    }
}
