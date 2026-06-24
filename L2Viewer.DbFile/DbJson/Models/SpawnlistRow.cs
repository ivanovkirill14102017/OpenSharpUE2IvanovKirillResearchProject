namespace L2Viewer.DbFile.DbJson.Models
{
    public sealed class SpawnlistRow
    {
        public int id { get; set; }
        public string location { get; set; }
        public int count { get; set; }
        public int npc_templateid { get; set; }
        public int locx { get; set; }
        public int locy { get; set; }
        public int locz { get; set; }
        public int randomx { get; set; }
        public int randomy { get; set; }
        public int heading { get; set; }
        public int respawn_delay { get; set; }
        public int loc_id { get; set; }
        public decimal periodOfDay { get; set; }

    }
}
