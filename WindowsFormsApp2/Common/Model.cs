using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    public class ModuleObj
    {
        public string ModuleID { get; set; }
        public string Pmax { get; set; }
        public string Voc { get; set; }
        public string Isc { get; set; }
        public string Vmp { get; set; }
        public string Imp { get; set; }
        public string FF { get; set; }

        public string ModuleCellSupplier { get; set; }
        public string ModuleDate { get; set; }
        public string CellDate { get; set; }
        public string ModuleCountry { get; set; }
        public string IECDate { get; set; }
        public string CertificationProvider { get; set; }
        public string ISO { get; set; }
        public string ProductType { get; set; }

    }
}
