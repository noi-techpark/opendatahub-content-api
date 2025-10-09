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
    public class SensorHelper
    {
        public List<string> sensortypelist;
        public List<string> manufacturerlist;
        public List<string> modellist;
        public List<string> idlist;
        public List<string> smgtaglist;
        public List<string> sourcelist;
        public List<string> languagelist;
        public List<string> datasetidlist;
        public List<string> measurementtypenamelist;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        //New Publishedonlist
        public List<string> publishedonlist;

        public static SensorHelper Create(
            QueryFactory queryFactory,
            string? idfilter,
            string? sensortypefilter,
            string? manufacturerfilter,
            string? modelfilter,
            string? datasetidfilter,
            string? measurementtypenamefilter,
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
            return new SensorHelper(
                idfilter: idfilter,
                sensortypefilter: sensortypefilter,
                manufacturerfilter: manufacturerfilter,
                modelfilter: modelfilter,
                datasetidfilter: datasetidfilter,
                measurementtypenamefilter: measurementtypenamefilter,
                sourcefilter: sourcefilter,
                activefilter: activefilter,
                smgactivefilter: smgactivefilter,
                smgtags: smgtags,
                lastchange: lastchange,
                languagefilter: langfilter,
                publishedonfilter: publishedonfilter
            );
        }

        private SensorHelper(
            string? idfilter,
            string? sensortypefilter,
            string? manufacturerfilter,
            string? modelfilter,
            string? datasetidfilter,
            string? measurementtypenamefilter,
            string? sourcefilter,
            bool? activefilter,
            bool? smgactivefilter,
            string? smgtags,
            string? lastchange,
            string? languagefilter,
            string? publishedonfilter
        )
        {
            sensortypelist = String.IsNullOrEmpty(sensortypefilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(sensortypefilter);
            manufacturerlist = String.IsNullOrEmpty(manufacturerfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(manufacturerfilter);
            modellist = String.IsNullOrEmpty(modelfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(modelfilter);
            datasetidlist = String.IsNullOrEmpty(datasetidfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(datasetidfilter);
            measurementtypenamelist = String.IsNullOrEmpty(measurementtypenamefilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(measurementtypenamefilter);

            idlist = String.IsNullOrEmpty(idfilter)
                ? new List<string>()
                : CommonListCreator.CreateIdList(idfilter.ToUpper());
            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            sourcelist = Helper.CommonListCreator.CreateSourceList(sourcefilter);

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());
        }
    }
}
