// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    public class TrafficIncidentHelper
    {
        public List<string> incidenttypelist;
        public List<string> severitylist;
        public List<string> statuslist;
        public List<string> idlist;
        public List<string> smgtaglist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public List<string> affectedrouteslist;
        public bool? roadclosure;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        //New Publishedonlist
        public List<string> publishedonlist;

        public static TrafficIncidentHelper Create(
            QueryFactory queryFactory,
            string? idfilter,
            string? incidenttypefilter,
            string? severityfilter,
            string? statusfilter,
            string? affectedroutesfilter,
            bool? roadclosurefilter,
            string? sourcefilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? lastchange,
            string? langfilter,
            string? publishedonfilter,
            CancellationToken cancellationToken
        )
        {
            return new TrafficIncidentHelper(
                idfilter: idfilter,
                incidenttypefilter: incidenttypefilter,
                severityfilter: severityfilter,
                statusfilter: statusfilter,
                affectedroutesfilter: affectedroutesfilter,
                roadclosurefilter: roadclosurefilter,
                sourcefilter: sourcefilter,
                activefilter: activefilter,
                smgactivefilter: smgactivefilter,
                smgtags: smgtags,
                lastchange: lastchange,
                languagefilter: langfilter,
                publishedonfilter: publishedonfilter
            );
        }

        private TrafficIncidentHelper(
            string? idfilter,
            string? incidenttypefilter,
            string? severityfilter,
            string? statusfilter,
            string? affectedroutesfilter,
            bool? roadclosurefilter,
            string? sourcefilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? lastchange,
            string? languagefilter,
            string? publishedonfilter
        )
        {
            incidenttypelist = String.IsNullOrEmpty(incidenttypefilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(incidenttypefilter);
            severitylist = String.IsNullOrEmpty(severityfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(severityfilter);
            statuslist = String.IsNullOrEmpty(statusfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(statusfilter);
            affectedrouteslist = String.IsNullOrEmpty(affectedroutesfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(affectedroutesfilter);

            idlist = String.IsNullOrEmpty(idfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(idfilter.ToUpper());
            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            roadclosure = roadclosurefilter;

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());
        }
    }
}