using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LEVCAN_Configurator_Shared;

namespace LEVCAN_Configurator
{

    internal interface IMGUI_TabInterface : LEVCAN_ConfiguratorDrawI
    {
        public void Initialize(LevcanHandler lchandler, Settings settings);
    }

}
