using LEVCAN.NET;
using LEVCAN_Configurator.Properties;
using LEVCAN_Configurator_Shared;
using System;
using System.Collections.Generic;

namespace LEVCAN_Configurator.Tabs
{
    internal class EventsNotifications : IMGUI_TabInterface
    {
        LevcanHandler Lev;
        List<EventMessage> events = new List<EventMessage>();
        public bool Draw()
        {
            // Pop-up events
            for (int i = 0; i < events.Count; i++)
            {
                events[i].DrawMessage();
                if (events[i].ToDelete)
                {
                    events.RemoveAt(i);
                    i--;
                }
            }
            return false;
        }

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            Lev = lchandler;
            Lev.UpdateEventHandler += EvenCallback;
        }

        void EvenCallback(LC_Event_t data)
        {
            foreach (EventMessage e in events)
            {
                if (e.Sender == data.Sender)
                {
                    e.UpdateMessage(data);
                    return;
                }
            }
            events.Add(new EventMessage(data));
        }
    }
}
