using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{

    public class MemberListResponse
    {
        public Memberlist[] items { get; set; }
        public Paging paging { get; set; }
    }

}
