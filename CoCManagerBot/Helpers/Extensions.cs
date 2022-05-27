using CoCManagerBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Helpers
{
    public static class Extensions
    {
        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static void Upsert(this WarResponse war)
        {
            var coll = Program.DB.GetCollection<WarResponse>("wars");
            //check for existing war
            var exist = coll.FindOne(x => x.clan.tag == war.clan.tag && x.opponent.tag == war.opponent.tag && x.startTime == war.startTime);
            if (exist != null)
            {
                war.id = exist.id;
                coll.Update(war);
            }
            else
            {
                coll.Insert(war);
            }
        }
    }
}
