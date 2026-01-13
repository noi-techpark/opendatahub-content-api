using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZOHO
{
    public class ZohoResponse
    {
        public int code { get; set; }

        public ICollection<ZohoRootobject> data { get; set; }
    }

    public class ZohoRootobject
    {
        public string Codice_sentiero { get; set; }
        public string Numero_sentiero { get; set; }
        public ZohoPosition Posizione { get; set; }
        public string Note { get; set; }
        public string Denominazione_sentiero { get; set; }
        public string ID { get; set; }
        public string Stato_StatoStato { get; set; }
    }

    public class ZohoPosition
    {
        public string display_value { get; set; }
        public string country { get; set; }
        public string district_city { get; set; }
        public string latitude { get; set; }
        public string address_line_1 { get; set; }
        public string state_province { get; set; }
        public string address_line_2 { get; set; }
        public string postal_code { get; set; }
        public string longitude { get; set; }
    }

    public class ZohoAuthToken
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public string api_domain { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
}
