using System;
namespace maintenanceService
{
    //public enum OpgaveType { Null, Service, Reparation }

    public class Anmodning
    {

        public int AnmodningID { get; set; }
        public int KøretøjID { get; set; }
        public string Beskrivelse { get; set; }
        public string OpgaveType { get; set; }
        public string Indsender { get; set; }

        public Anmodning(int anmodningid, int køretøjid, string beskrivelse, string opgaveType, string indsender)
        {
            this.AnmodningID = anmodningid;
            this.KøretøjID = køretøjid;
            this.Beskrivelse = beskrivelse;
            this.OpgaveType = opgaveType;
            this.Indsender = indsender;
        }

        public Anmodning()
        {
        }

    }
}

