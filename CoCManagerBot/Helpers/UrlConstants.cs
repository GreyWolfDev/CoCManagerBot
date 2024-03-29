﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Helpers
{
    public class UrlConstants
    {
        /// <summary>
        /// Gets clan information
        /// </summary>
        public const string GetClanInformationUrlTemplate = @"https://api.greywolfdev.net/clash/v1/clans/{0}";
        /// <summary>
        /// Lists clan members
        /// </summary>
        public const string ListClanMembersUrlTemplate = @"https://api.greywolfdev.net/clash/v1/clans/{0}/members";
        /// <summary>
        /// Searches clans
        /// </summary>
        public const string SearchClansUrlTemplate = @"https://api.greywolfdev.net/clash/v1/clans{0}";
        /// <summary>
        /// Gets current war status
        /// </summary>
        public const string GetCurrentWarInformationTemplate = @"https://api.greywolfdev.net/clash/v1/clans/{0}/currentwar";
        /// <summary>
        /// Get war log for clan
        /// </summary>
        public const string GetWarLogInformationTemplate = @"https://api.greywolfdev.net/clash/v1/clans/{0}/warlog";
        /// <summary>
        /// Gets player information
        /// </summary>
        public const string GetPlayerInformationTemplate = @"https://api.greywolfdev.net/clash/v1/players/{0}";
        /// <summary>
        /// Gets CWL info - Teams and War Tags
        /// Needs clan tag passed
        /// </summary>
        public const string GetCWLInfo = @"https://api.greywolfdev.net/clash/v1/clans/{0}/currentwar/leaguegroup";
        /// <summary>
        /// Gets CWL war info
        /// Needs war tag passed
        /// </summary>
        public const string GetCWLWarInfo = @"https://api.greywolfdev.net/clash/v1/clanwarleagues/wars/{0}";

    }
}
