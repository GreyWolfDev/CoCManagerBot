using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Model
{

    public class WarLogResponse
    {
        public int id { get; set; }
        public Item[] items { get; set; }
        public Paging paging { get; set; }
        public string reason { get; set; }
    }

    public class Paging
    {
        public Cursors cursors { get; set; }
    }

    public class Cursors
    {
    }

    public class Item
    {
        public string result { get; set; }
        public string endTime { get; set; }
        public int teamSize { get; set; }
        public Clan clan { get; set; }
        public Opponent opponent { get; set; }
    }
}
