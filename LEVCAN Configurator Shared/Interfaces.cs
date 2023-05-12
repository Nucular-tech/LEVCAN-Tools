namespace LEVCAN_Configurator_Shared
{
    public interface LEVCAN_ConfiguratorDrawI
    {
        public bool Draw();
    }

    public interface LEVCAN_ConfiguratorTabI : LEVCAN_ConfiguratorDrawI
    {
        public void Initialize(LevcanHandler lchandler);
    }
}