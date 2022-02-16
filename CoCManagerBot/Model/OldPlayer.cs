using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{
    class OldPlayer
    {
        public int id { get; set; }
        public int TelegramId { get; set; }
        public string CoCId { get; set; }
        public string ClanId { get; set; }
        public LastNotification LastNotification { get; set; } = LastNotification.None;
    }
}
