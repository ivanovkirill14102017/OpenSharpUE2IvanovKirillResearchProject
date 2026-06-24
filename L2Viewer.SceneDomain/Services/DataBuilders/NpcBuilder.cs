using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services.DataBuilders
{
    internal class NpcBuilder
    {
        public SceneNpcData Build()
        {
            var spawns = DbFile.DbJson.TableJsonMapper.Read<DbFile.DbJson.Models.SpawnlistRow>("C:\\Users\\User\\Downloads\\Lineage2_Interlude_windows10\\data\\InterludeDb\\spawnlist.json");
            var npcs = DbFile.DbJson.TableJsonMapper.Read<DbFile.DbJson.Models.NpcRow>("C:\\Users\\User\\Downloads\\Lineage2_Interlude_windows10\\data\\InterludeDb\\npc.json");
            var npc = npcs.Single(x => x.name == "Northwind");
            var npcSpawn = spawns.Single(x => x.id == npc.id);

            return new SceneNpcData
            {
                Name = npc.name,
                VisualLink = npc.@class,
                Position = new(npcSpawn.locx, npcSpawn.locy, npcSpawn.locz),
            };
        }
    }
}
