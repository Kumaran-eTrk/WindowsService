using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonService
{
    public class UserInfo
    {
        public string? username { get; set; }
        public string? domainname { get; set; }
        public DateTime logonTime { get; set; }
        public DateTime currentTime { get; set; }
    }

}
