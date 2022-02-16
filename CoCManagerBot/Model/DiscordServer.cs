using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{
    public class DiscordServer
    {
        public int id { get; set; }
        public long ServerId { get; set; }
        public long PrimaryChannelId { get; set; }
        public string ClanTag { get; set; }
    }
}
