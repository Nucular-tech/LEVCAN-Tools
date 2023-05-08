using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN_Configurator
{
    internal interface IMGUI_TabInterface
    {
        public bool Draw();
        public void Initialize(LevcanHandler lchandler, Settings settings);       
    }
}
